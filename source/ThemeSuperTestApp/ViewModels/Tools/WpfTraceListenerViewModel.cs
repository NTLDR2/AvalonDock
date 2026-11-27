using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AvalonDock.Core;
using Caliburn.Micro;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Tool 4: Live <c>PresentationTraceSources</c> XAML data-binding and resource-dictionary error/warning listener.
	/// </summary>
	public sealed class WpfTraceListenerViewModel : ToolboxScreen
	{
		private readonly WpfTraceCaptureService _traceService;
		private readonly ThemeCatalogService _themeCatalog;

		private WpfTraceEventRecord? _selectedEvent;
		private bool _filterCurrentThemeOnly;
		private string _searchFilter = string.Empty;
		private string _traceStatusText = "Listening on DataBindingSource, ResourceDictionarySource, and MarkupSource (Warning | Error).";

		/// <summary>
		/// Initializes a new instance of the <see cref="WpfTraceListenerViewModel"/> class.
		/// </summary>
		/// <param name="traceService">The in-process WPF trace capture service.</param>
		/// <param name="themeCatalog">The shared 20-theme catalog service.</param>
		public WpfTraceListenerViewModel(WpfTraceCaptureService traceService, ThemeCatalogService themeCatalog)
		{
			_traceService = traceService ?? throw new ArgumentNullException(nameof(traceService));
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));

			Id = "Tool.WpfTraceListener";
			DisplayName = "4. WPF XAML Binding & Resource Trace";
			ToolTipText = "Captures PresentationTraceSources DataBinding and ResourceDictionary errors/warnings live with active theme tags (Ctrl+Alt+T)";
			Zone = DockZone.BottomRight;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+T";

			FilteredEvents = new BindableCollection<WpfTraceEventRecord>();

			_traceService.EventsUpdated += OnTraceEventsUpdated;
			_themeCatalog.ActiveThemeChanged += OnActiveThemeChanged;
			RebuildFilteredEvents();
		}

		/// <summary>Gets the filtered WPF trace events displayed in the master grid.</summary>
		public BindableCollection<WpfTraceEventRecord> FilteredEvents { get; }

		/// <summary>Gets or sets a value indicating whether live WPF trace capture is enabled.</summary>
		public bool IsCaptureEnabled
		{
			get => _traceService.IsCaptureEnabled;
			set
			{
				if (_traceService.IsCaptureEnabled == value)
					return;

				_traceService.IsCaptureEnabled = value;
				NotifyOfPropertyChange(nameof(IsCaptureEnabled));
				TraceStatusText = value ? "WPF Trace capture resumed." : "WPF Trace capture paused.";
			}
		}

		/// <summary>Gets or sets a value indicating whether only events tagged with the currently active theme are shown.</summary>
		public bool FilterCurrentThemeOnly
		{
			get => _filterCurrentThemeOnly;
			set
			{
				if (Set(ref _filterCurrentThemeOnly, value))
					RebuildFilteredEvents();
			}
		}

		/// <summary>Gets or sets the substring search filter.</summary>
		public string SearchFilter
		{
			get => _searchFilter;
			set
			{
				if (Set(ref _searchFilter, value ?? string.Empty))
					RebuildFilteredEvents();
			}
		}

		/// <summary>Gets or sets the selected trace event whose full diagnostic text is shown in the detail pane.</summary>
		public WpfTraceEventRecord? SelectedEvent
		{
			get => _selectedEvent;
			set
			{
				if (Set(ref _selectedEvent, value))
					NotifyOfPropertyChange(nameof(SelectedEventDetailText));
			}
		}

		/// <summary>Gets the full formatted detail text for <see cref="SelectedEvent"/>.</summary>
		public string SelectedEventDetailText => _selectedEvent == null
			? "Select a captured trace row above to inspect the full WPF BindingExpression / ResourceDictionary diagnostic message."
			: $"[{_selectedEvent.TimestampText}] #{_selectedEvent.SequenceNumber} | {_selectedEvent.Category} {_selectedEvent.Severity} (Id {_selectedEvent.EventId}) | Theme: {_selectedEvent.ThemeName} [{_selectedEvent.BaseClassBadge}]{Environment.NewLine}{_selectedEvent.Message}";

		/// <summary>Gets the total count of captured errors.</summary>
		public int TotalErrorCount => _traceService.Events.Count(e => e.Severity == TraceEventType.Error || e.Severity == TraceEventType.Critical);

		/// <summary>Gets the total count of captured warnings.</summary>
		public int TotalWarningCount => _traceService.Events.Count(e => e.Severity == TraceEventType.Warning);

		/// <summary>Gets a value indicating whether any errors have been captured.</summary>
		public bool HasErrors => TotalErrorCount > 0;

		/// <summary>Gets or sets the status message shown in the toolbar.</summary>
		public string TraceStatusText
		{
			get => _traceStatusText;
			set => Set(ref _traceStatusText, value);
		}

		/// <summary>Clears all captured trace events.</summary>
		public void ClearEvents()
		{
			_traceService.Clear();
			SelectedEvent = null;
			RebuildFilteredEvents();
			TraceStatusText = "Cleared trace buffer.";
		}

		/// <summary>Fires a deliberate synthetic binding error (<c>Error 40</c>) to confirm live trace capture.</summary>
		public void TriggerTestBindingError()
		{
			_traceService.TriggerSyntheticBindingErrorForVerification();
			RebuildFilteredEvents();
			SelectedEvent = FilteredEvents.LastOrDefault();
			TraceStatusText = "Triggered synthetic Binding Error 40 probe to verify PresentationTraceSources listener.";
		}

		/// <summary>Copies either the selected event or all visible filtered events as Markdown to the clipboard.</summary>
		public void CopyVisibleTraceMarkdown()
		{
			var sb = new StringBuilder();
			sb.AppendLine("## Captured WPF PresentationTraceSources Events");
			sb.AppendLine();

			if (FilteredEvents.Count == 0)
			{
				sb.AppendLine("_No WPF binding or resource errors captured._");
			}
			else
			{
				sb.AppendLine("| # | Time | Category | Severity | ID | Theme | Message |");
				sb.AppendLine("| ---: | :--- | :--- | :--- | ---: | :--- | :--- |");
				foreach (var ev in FilteredEvents)
				{
					var safeMsg = ev.Message.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
					sb.AppendFormat(
						CultureInfo.InvariantCulture,
						"| {0} | `{1}` | `{2}` | `{3}` | {4} | {5} | {6} |",
						ev.SequenceNumber,
						ev.TimestampText,
						ev.Category,
						ev.Severity,
						ev.EventId,
						ev.ThemeName,
						safeMsg);
					sb.AppendLine();
				}
			}

			var text = sb.ToString();
			try
			{
				Clipboard.SetDataObject(text, copy: true);
				TraceStatusText = $"Copied {FilteredEvents.Count} trace event(s) to clipboard ({text.Length} chars).";
			}
			catch (ExternalException ex)
			{
				TraceStatusText = $"Clipboard busy ({ex.Message}).";
			}
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
			{
				_traceService.EventsUpdated -= OnTraceEventsUpdated;
				_themeCatalog.ActiveThemeChanged -= OnActiveThemeChanged;
			}

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private void OnTraceEventsUpdated(object? sender, EventArgs e)
		{
			RebuildFilteredEvents();
		}

		private void OnActiveThemeChanged(object? sender, EventArgs e)
		{
			if (_filterCurrentThemeOnly)
				RebuildFilteredEvents();
		}

		private void RebuildFilteredEvents()
		{
			var activeThemeName = _themeCatalog.ActiveTheme?.DisplayName;
			var query = _searchFilter.Trim();

			var matches = _traceService.Events.Where(ev =>
			{
				if (_filterCurrentThemeOnly && !string.IsNullOrEmpty(activeThemeName) &&
					!string.Equals(ev.ThemeName, activeThemeName, StringComparison.Ordinal))
				{
					return false;
				}

				if (string.IsNullOrEmpty(query))
					return true;

				return ev.Message.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
					ev.ThemeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
					ev.Category.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
					ev.EventId.ToString(CultureInfo.InvariantCulture).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
			}).ToList();

			FilteredEvents.Clear();
			FilteredEvents.AddRange(matches);

			NotifyOfPropertyChange(nameof(TotalErrorCount));
			NotifyOfPropertyChange(nameof(TotalWarningCount));
			NotifyOfPropertyChange(nameof(HasErrors));
		}
	}
}