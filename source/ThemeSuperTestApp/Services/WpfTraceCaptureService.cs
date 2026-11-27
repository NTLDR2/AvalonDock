using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Caliburn.Micro;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Attaches a custom <see cref="TraceListener"/> to <see cref="PresentationTraceSources.DataBindingSource"/>,
	/// <see cref="PresentationTraceSources.ResourceDictionarySource"/>, and <see cref="PresentationTraceSources.MarkupSource"/>
	/// on <c>.NET Framework 4.8</c> to capture XAML binding and resource lookup warnings/errors live in-process.
	/// </summary>
	public sealed class WpfTraceCaptureService : TraceListener
	{
		private const int MaxRetainedEvents = 1000;

		[ThreadStatic]
		private static bool _isDispatching;

		private readonly ThemeCatalogService _themeCatalog;
		private readonly ConcurrentQueue<WpfTraceEventRecord> _pendingQueue = new ConcurrentQueue<WpfTraceEventRecord>();
		private readonly Dispatcher? _uiDispatcher;

		private int _sequenceCounter;
		private int _flushScheduled;
		private bool _isCaptureEnabled = true;
		private bool _isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="WpfTraceCaptureService"/> class and attaches to WPF presentation trace sources.
		/// </summary>
		/// <param name="themeCatalog">The theme catalog used to tag every trace event with the active theme.</param>
		public WpfTraceCaptureService(ThemeCatalogService themeCatalog)
			: base("ThemeSuperTestApp.WpfTraceCapture")
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			var app = Application.Current;
			_uiDispatcher = (app != null && app.CheckAccess()) ? app.Dispatcher : Dispatcher.CurrentDispatcher;

			Events = new BindableCollection<WpfTraceEventRecord>();
			AttachSources();
		}

		/// <summary>
		/// Raised on the UI thread after queued trace events are flushed into <see cref="Events"/>.
		/// </summary>
		public event EventHandler? EventsUpdated;

		/// <summary>
		/// Gets the bounded UI-thread collection of captured WPF trace events.
		/// </summary>
		public BindableCollection<WpfTraceEventRecord> Events { get; }

		/// <summary>
		/// Gets or sets a value indicating whether incoming WPF trace events are recorded.
		/// </summary>
		public bool IsCaptureEnabled
		{
			get => _isCaptureEnabled;
			set => _isCaptureEnabled = value;
		}

		/// <summary>
		/// Clears all captured trace events and resets the pending queue.
		/// </summary>
		public void Clear()
		{
			while (_pendingQueue.TryDequeue(out _))
			{
			}

			if (_uiDispatcher != null && !_uiDispatcher.CheckAccess())
			{
				_uiDispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
				{
					Events.Clear();
					EventsUpdated?.Invoke(this, EventArgs.Empty);
				}));
				return;
			}

			Events.Clear();
			EventsUpdated?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Synchronously flushes any queued trace events onto <see cref="Events"/> (useful for deterministic STA testing).
		/// </summary>
		public void FlushPendingNow()
		{
			FlushQueueCore();
		}

		/// <summary>
		/// Deliberately evaluates a broken one-time WPF <see cref="Binding"/> on an isolated off-tree <see cref="FrameworkElement"/>
		/// to verify that <c>PresentationTraceSources.DataBindingSource</c> is actively capturing <c>Error 40</c> events.
		/// </summary>
		public void TriggerSyntheticBindingErrorForVerification()
		{
			var probe = new FrameworkElement
			{
				DataContext = new SyntheticTraceProbeModel(),
			};

			var brokenBinding = new Binding("IntentionallyMissingDiagnosticProperty")
			{
				Mode = BindingMode.OneWay,
			};

			_ = BindingOperations.SetBinding(probe, FrameworkElement.TagProperty, brokenBinding);
			FlushQueueCore();
		}

		/// <inheritdoc/>
		public override void TraceEvent(
			TraceEventCache? eventCache,
			string source,
			TraceEventType eventType,
			int id,
			string? message)
		{
			EnqueueRecord(source, eventType, id, message ?? string.Empty);
		}

		/// <inheritdoc/>
		public override void TraceEvent(
			TraceEventCache? eventCache,
			string source,
			TraceEventType eventType,
			int id,
			string? format,
			params object?[]? args)
		{
			string formatted;
			if (string.IsNullOrEmpty(format))
			{
				formatted = string.Empty;
			}
			else if (args == null || args.Length == 0)
			{
				formatted = format!;
			}
			else
			{
				try
				{
					formatted = string.Format(CultureInfo.InvariantCulture, format!, args);
				}
				catch (FormatException)
				{
					formatted = format!;
				}
			}

			EnqueueRecord(source, eventType, id, formatted);
		}

		/// <inheritdoc/>
		public override void Write(string? message)
		{
			// Structured WPF events arrive via TraceEvent; ignore partial Write fragments.
		}

		/// <inheritdoc/>
		public override void WriteLine(string? message)
		{
			if (!string.IsNullOrWhiteSpace(message))
				EnqueueRecord("WPF", TraceEventType.Warning, 0, message!);
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			if (!_isDisposed && disposing)
			{
				_isDisposed = true;
				DetachSources();
			}

			base.Dispose(disposing);
		}

		private static string NormalizeCategory(string? sourceName)
		{
			if (string.IsNullOrEmpty(sourceName))
				return "WPF";
			if (sourceName!.IndexOf("Data", StringComparison.OrdinalIgnoreCase) >= 0)
				return "DataBinding";
			if (sourceName.IndexOf("ResourceDictionary", StringComparison.OrdinalIgnoreCase) >= 0)
				return "ResourceDictionary";
			if (sourceName.IndexOf("Markup", StringComparison.OrdinalIgnoreCase) >= 0)
				return "Markup";

			return sourceName;
		}

		private void AttachSources()
		{
			// Refresh() MUST be called BEFORE setting Switch.Level; calling Refresh() afterward resets switches to app.config defaults.
			PresentationTraceSources.Refresh();
			AttachSingleSource(PresentationTraceSources.DataBindingSource);
			AttachSingleSource(PresentationTraceSources.ResourceDictionarySource);
			AttachSingleSource(PresentationTraceSources.MarkupSource);
		}

		private void AttachSingleSource(TraceSource? source)
		{
			if (source == null)
				return;

			if (!source.Listeners.Contains(this))
				source.Listeners.Add(this);

			source.Switch.Level = SourceLevels.Warning | SourceLevels.Error | SourceLevels.Critical;
		}

		private void DetachSources()
		{
			PresentationTraceSources.DataBindingSource?.Listeners.Remove(this);
			PresentationTraceSources.ResourceDictionarySource?.Listeners.Remove(this);
			PresentationTraceSources.MarkupSource?.Listeners.Remove(this);
		}

		private void EnqueueRecord(string source, TraceEventType eventType, int id, string message)
		{
			if (_isDisposed || !_isCaptureEnabled || _isDispatching || string.IsNullOrWhiteSpace(message))
				return;

			var active = _themeCatalog.ActiveTheme;
			var seq = Interlocked.Increment(ref _sequenceCounter);
			var record = new WpfTraceEventRecord(
				seq,
				DateTime.Now,
				NormalizeCategory(source),
				eventType,
				id,
				active?.DisplayName ?? "(Startup / None)",
				active?.BaseClassBadgeText ?? "-",
				message.Trim());

			_pendingQueue.Enqueue(record);
			ScheduleFlush();
		}

		private void ScheduleFlush()
		{
			if (_uiDispatcher == null)
			{
				FlushQueueCore();
				return;
			}

			if (Interlocked.Exchange(ref _flushScheduled, 1) == 0)
			{
				_uiDispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() =>
				{
					Interlocked.Exchange(ref _flushScheduled, 0);
					FlushQueueCore();
				}));
			}
		}

		private void FlushQueueCore()
		{
			if (_isDisposed || _isDispatching)
				return;

			_isDispatching = true;
			try
			{
				var addedAny = false;
				while (_pendingQueue.TryDequeue(out var item))
				{
					Events.Add(item);
					addedAny = true;
					while (Events.Count > MaxRetainedEvents)
						Events.RemoveAt(0);
				}

				if (addedAny)
					EventsUpdated?.Invoke(this, EventArgs.Empty);
			}
			finally
			{
				_isDispatching = false;
			}
		}

		private sealed class SyntheticTraceProbeModel
		{
		}
	}
}