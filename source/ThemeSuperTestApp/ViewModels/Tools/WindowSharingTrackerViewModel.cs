using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AvalonDock.Core;
using Caliburn.Micro;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Tool 3: Multi-window <c>ResourceDictionary</c> object-identity tracker proving shared <c>DictionaryTheme</c> references
	/// versus duplicated per-window BAML dictionaries on URI <c>Theme</c>s.
	/// </summary>
	public sealed class WindowSharingTrackerViewModel : ToolboxScreen
	{
		private readonly ThemeCatalogService _themeCatalog;
		private readonly WindowDictionaryTrackerService _trackerService;
		private readonly DispatcherTimer _pollTimer;

		private WindowSharingReport? _currentReport;
		private bool _autoPollEnabled = true;
		private string _trackerStatusText = "Ready";

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSharingTrackerViewModel"/> class.
		/// </summary>
		/// <param name="themeCatalog">The shared 20-theme catalog service.</param>
		/// <param name="trackerService">The multi-window dictionary tracker service.</param>
		public WindowSharingTrackerViewModel(ThemeCatalogService themeCatalog, WindowDictionaryTrackerService trackerService)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_trackerService = trackerService ?? throw new ArgumentNullException(nameof(trackerService));

			Id = "Tool.WindowSharingTracker";
			DisplayName = "3. Multi-Window Dict Sharing";
			ToolTipText = "Inspect ResourceDictionary object identity across DockingManager, Floating, Detached, Navigator, and Overlay windows (Ctrl+Alt+W)";
			Zone = DockZone.RightBottom;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+W";

			Hosts = new BindableCollection<WindowDictionarySnapshot>();
			ObservedTransientHosts = new BindableCollection<WindowDictionarySnapshot>();

			_pollTimer = new DispatcherTimer(DispatcherPriority.Background)
			{
				Interval = TimeSpan.FromMilliseconds(900),
			};
			_pollTimer.Tick += OnPollTimerTick;
			_pollTimer.Start();

			_themeCatalog.ActiveThemeChanged += OnActiveThemeChanged;
			RefreshSnapshot();
		}

		/// <summary>Gets the live AvalonDock hosts and their attached <c>ResourceDictionary</c> object IDs.</summary>
		public BindableCollection<WindowDictionarySnapshot> Hosts { get; }

		/// <summary>Gets sticky snapshots of recently observed transient windows (<c>OverlayWindow</c> / <c>NavigatorWindow</c>).</summary>
		public BindableCollection<WindowDictionarySnapshot> ObservedTransientHosts { get; }

		/// <summary>Gets the latest sharing report.</summary>
		public WindowSharingReport? CurrentReport => _currentReport;

		/// <summary>Gets a value indicating whether multiple hosts are open and duplicating dictionaries.</summary>
		public bool HasDuplicatedDictionaries => _currentReport?.HasDuplicatedDictionaries == true;

		/// <summary>
		/// Gets or sets a value indicating whether open AvalonDock windows are polled every 900 ms to catch tear-offs and transient overlays automatically.
		/// </summary>
		public bool AutoPollEnabled
		{
			get => _autoPollEnabled;
			set
			{
				if (!Set(ref _autoPollEnabled, value))
					return;

				if (value)
					_pollTimer.Start();
				else
					_pollTimer.Stop();
			}
		}

		/// <summary>Gets or sets the status message shown in the toolbar.</summary>
		public string TrackerStatusText
		{
			get => _trackerStatusText;
			set => Set(ref _trackerStatusText, value);
		}

		/// <summary>Captures a fresh multi-window sharing snapshot immediately.</summary>
		public void RefreshSnapshot()
		{
			var active = _themeCatalog.ActiveTheme;
			var report = _trackerService.CaptureReport(
				active?.DisplayName ?? "(None)",
				active?.CachedInstance,
				_themeCatalog.AttachedDockingManager);

			_currentReport = report;
			Hosts.Clear();
			Hosts.AddRange(report.Hosts);

			foreach (var transient in report.Hosts.Where(h =>
				h.HostKind.StartsWith("OverlayWindow", StringComparison.Ordinal) ||
				h.HostKind.StartsWith("NavigatorWindow", StringComparison.Ordinal)))
			{
				if (!ObservedTransientHosts.Any(existing =>
					existing.HostKind == transient.HostKind &&
					existing.RawIdentityHash == transient.RawIdentityHash))
				{
					ObservedTransientHosts.Insert(0, transient);
					while (ObservedTransientHosts.Count > 20)
						ObservedTransientHosts.RemoveAt(ObservedTransientHosts.Count - 1);
				}
			}

			NotifyOfPropertyChange(nameof(CurrentReport));
			NotifyOfPropertyChange(nameof(HasDuplicatedDictionaries));
			TrackerStatusText = $"Hosts: {report.TotalThemedHosts} | Distinct Instances: {report.DistinctDictionaryInstances} | Redundant Parses: {report.RedundantBamlParsesCount}";
		}

		/// <summary>Clears the sticky transient window log.</summary>
		public void ClearTransientHistory()
		{
			ObservedTransientHosts.Clear();
		}

		/// <summary>Copies the Markdown multi-window sharing table to the clipboard.</summary>
		public void CopySharingMarkdown()
		{
			if (_currentReport == null)
				return;

			var md = _trackerService.BuildMarkdownReport(_currentReport);
			try
			{
				Clipboard.SetDataObject(md, copy: true);
				TrackerStatusText = $"Copied Markdown multi-window sharing report ({md.Length} chars).";
			}
			catch (ExternalException ex)
			{
				TrackerStatusText = $"Clipboard busy ({ex.Message}).";
			}
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
			{
				_pollTimer.Stop();
				_pollTimer.Tick -= OnPollTimerTick;
				_themeCatalog.ActiveThemeChanged -= OnActiveThemeChanged;
			}

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private void OnActiveThemeChanged(object? sender, EventArgs e)
		{
			ObservedTransientHosts.Clear();
			RefreshSnapshot();
		}

		private void OnPollTimerTick(object? sender, EventArgs e)
		{
			if (IsOpen)
				RefreshSnapshot();
		}
	}
}