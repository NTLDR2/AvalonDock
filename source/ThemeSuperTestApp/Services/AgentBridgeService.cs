using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using Caliburn.Micro;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Bidirectional XML file-mailbox service bridging <c>ThemeSuperTestApp</c> and Antigravity via
	/// <see cref="AgentBridgePaths"/> (<c>heartbeat.xml</c>, <c>inbox/*.xml</c>, <c>outbox/*-response.xml</c>, and <c>latest/*</c>).
	/// </summary>
	public sealed class AgentBridgeService : PropertyChangedBase, IDisposable
	{
		private const int MaxOutboxFiles = 60;
		private const int MaxLogEntries = 300;

		private readonly AgentBridgePaths _paths;
		private readonly ThemeCatalogService _themeCatalog;
		private readonly WindowsOsThemeService _osThemeService;
		private readonly ResourceDictionaryAuditService _auditService;
		private readonly WindowDictionaryTrackerService _windowTracker;
		private readonly WpfTraceCaptureService _traceCapture;
		private readonly WindowScreenshotService _screenshotService;
		private readonly DiagnosticReportExporterService _reportExporter;

		private readonly SemaphoreSlim _processingGate = new SemaphoreSlim(1, 1);
		private readonly DateTimeOffset _startedAtUtc = DateTimeOffset.UtcNow;
		private readonly Dispatcher _dispatcher;

		private FileSystemWatcher? _inboxWatcher;
		private DispatcherTimer? _pollTimer;
		private DispatcherTimer? _heartbeatTimer;
		private FrameworkElement? _visualRoot;
		private IAgentBridgeShellHost? _shellHost;

		private bool _isListening = true;
		private bool _isStarted;
		private bool _isDisposed;
		private int _processedCommandCount;
		private string _lastBridgeSummary = "Bridge ready.";

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgeService"/> class.
		/// </summary>
		/// <param name="paths">Bridge folder paths.</param>
		/// <param name="themeCatalog">18-theme catalog service.</param>
		/// <param name="osThemeService">Windows OS theme service.</param>
		/// <param name="auditService">ResourceDictionary &amp; Freezable audit service.</param>
		/// <param name="windowTracker">Multi-window dictionary sharing tracker.</param>
		/// <param name="traceCapture">WPF trace capture service.</param>
		/// <param name="screenshotService">Dual-mode screenshot service.</param>
		/// <param name="reportExporter">Full Markdown diagnostic report exporter.</param>
		public AgentBridgeService(
			AgentBridgePaths paths,
			ThemeCatalogService themeCatalog,
			WindowsOsThemeService osThemeService,
			ResourceDictionaryAuditService auditService,
			WindowDictionaryTrackerService windowTracker,
			WpfTraceCaptureService traceCapture,
			WindowScreenshotService screenshotService,
			DiagnosticReportExporterService reportExporter)
		{
			_paths = paths ?? throw new ArgumentNullException(nameof(paths));
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_osThemeService = osThemeService ?? throw new ArgumentNullException(nameof(osThemeService));
			_auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
			_windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
			_traceCapture = traceCapture ?? throw new ArgumentNullException(nameof(traceCapture));
			_screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
			_reportExporter = reportExporter ?? throw new ArgumentNullException(nameof(reportExporter));

			var app = Application.Current;
			_dispatcher = (app != null && app.CheckAccess()) ? app.Dispatcher : Dispatcher.CurrentDispatcher;

			LogEntries = new BindableCollection<AgentBridgeLogEntry>();
			_themeCatalog.ActiveThemeChanged += OnEnvironmentChanged;
			_osThemeService.OsThemeChanged += OnEnvironmentChanged;
		}

		/// <summary>Gets the bridge directory configuration.</summary>
		public AgentBridgePaths Paths => _paths;

		/// <summary>Gets the UI-thread activity log (newest entries at index 0).</summary>
		public BindableCollection<AgentBridgeLogEntry> LogEntries { get; }

		/// <summary>
		/// Gets or sets a value indicating whether incoming commands in <c>AgentBridge/inbox/</c> are actively processed.
		/// </summary>
		public bool IsListening
		{
			get => _isListening;
			set
			{
				if (!Set(ref _isListening, value))
					return;

				NotifyOfPropertyChange(nameof(BridgeStatusBadgeText));
				WriteHeartbeatSafe(value ? "Listening" : "Paused");
				if (value && _isStarted)
					_ = ProcessPendingInboxAsync();
			}
		}

		/// <summary>Gets the number of Antigravity commands processed during this session.</summary>
		public int ProcessedCommandCount
		{
			get => _processedCommandCount;
			private set
			{
				if (Set(ref _processedCommandCount, value))
					NotifyOfPropertyChange(nameof(BridgeStatusBadgeText));
			}
		}

		/// <summary>Gets the compact header pill text summarizing live bridge status and command count.</summary>
		public string BridgeStatusBadgeText => _isListening
			? $"Antigravity Bridge: Listening ({_processedCommandCount})"
			: $"Antigravity Bridge: Paused ({_processedCommandCount})";

		/// <summary>Gets or sets the most recent bridge activity summary message.</summary>
		public string LastBridgeSummary
		{
			get => _lastBridgeSummary;
			private set => Set(ref _lastBridgeSummary, value);
		}

		/// <summary>
		/// Attaches the high-level shell host (<c>ShellViewModel</c>) so shell/window stress commands can execute.
		/// </summary>
		/// <param name="shellHost">The shell host implementation.</param>
		public void AttachShellHost(IAgentBridgeShellHost shellHost)
		{
			_shellHost = shellHost ?? throw new ArgumentNullException(nameof(shellHost));
		}

		/// <summary>
		/// Attaches the main visual root (<c>ShellView</c> or <c>DockingManager</c>) used for screenshot captures.
		/// </summary>
		/// <param name="visualRoot">The root visual element.</param>
		public void AttachVisualRoot(FrameworkElement visualRoot)
		{
			_visualRoot = visualRoot ?? throw new ArgumentNullException(nameof(visualRoot));
		}

		/// <summary>
		/// Starts `heartbeat.xml` publishing, the `FileSystemWatcher` on `inbox/`, and the fallback poll timer.
		/// </summary>
		public void Start()
		{
			if (_isDisposed || _isStarted)
				return;

			_paths.EnsureDirectoriesExist();
			_isStarted = true;

			WriteHeartbeatSafe(_isListening ? "Listening" : "Paused");

			_inboxWatcher = new FileSystemWatcher(_paths.InboxDirectory, "*.xml")
			{
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
				EnableRaisingEvents = true,
			};
			_inboxWatcher.Created += OnInboxFileSignaled;
			_inboxWatcher.Renamed += OnInboxFileSignaled;
			_inboxWatcher.Changed += OnInboxFileSignaled;

			_pollTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(350), DispatcherPriority.Background, OnPollTimerTick, _dispatcher);
			_pollTimer.Start();

			_heartbeatTimer = new DispatcherTimer(TimeSpan.FromSeconds(2.5), DispatcherPriority.Background, OnHeartbeatTimerTick, _dispatcher);
			_heartbeatTimer.Start();

			LastBridgeSummary = $"Listening on {_paths.InboxDirectory}";
			if (_isListening)
				_ = ProcessPendingInboxAsync();
		}

		/// <summary>
		/// Captures a user-initiated (<c>F12</c>) multimodal snapshot bundle and records it in <see cref="LogEntries"/>.
		/// </summary>
		/// <param name="userNote">Optional operator note embedded in <c>latest-context.md</c>.</param>
		/// <returns>The saved <see cref="ScreenshotCaptureBundle"/>, or <see langword="null"/> if no visual root is attached.</returns>
		public async Task<ScreenshotCaptureBundle?> CaptureManualSnapshotAsync(string? userNote)
		{
			var target = ResolveCaptureTarget();
			if (target == null)
				return null;

			if (!target.CheckAccess())
			{
				var marshaled = await target.Dispatcher.InvokeAsync(() => CaptureManualSnapshotAsync(userNote));
				return await marshaled.ConfigureAwait(false);
			}

			var sw = Stopwatch.StartNew();
			target.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render);

			var slug = _themeCatalog.ActiveTheme?.Id ?? "NoTheme";
			var bundle = _screenshotService.CaptureSnapshotBundle(
				target,
				slug,
				string.IsNullOrWhiteSpace(userNote) ? null : userNote,
				includeDwmCapture: true,
				updateLatestFolder: true);

			sw.Stop();
			AppendLogEntry(new AgentBridgeLogEntry(
				DateTimeOffset.Now,
				"User F12 -> Agent",
				bundle.CaptureId,
				$"SendSnapshot ({slug})",
				"Ok",
				sw.Elapsed.TotalMilliseconds,
				bundle.LatestCompositePngPath,
				$"Saved latest-composite.png & latest-context.md"));

			WriteHeartbeatSafe(_isListening ? "Listening" : "Paused");
			return bundle;
		}

		/// <summary>
		/// Deterministically processes all currently queued <c>*.xml</c> command files in <c>AgentBridge/inbox/</c> in FIFO order.
		/// </summary>
		/// <returns>Number of command files processed.</returns>
		public async Task<int> ProcessPendingInboxAsync()
		{
			if (_isDisposed || !_isListening)
				return 0;

			var targetDispatcher = ResolveCaptureTarget()?.Dispatcher ?? _dispatcher;
			if (!targetDispatcher.CheckAccess())
			{
				var marshaled = await targetDispatcher.InvokeAsync(ProcessPendingInboxCoreAsync);
				return await marshaled.ConfigureAwait(false);
			}

			return await ProcessPendingInboxCoreAsync().ConfigureAwait(true);
		}

		private async Task<int> ProcessPendingInboxCoreAsync()
		{
			if (_isDisposed || !_isListening)
				return 0;

			_paths.EnsureDirectoriesExist();

			if (!await _processingGate.WaitAsync(0).ConfigureAwait(true))
				return 0;

			var count = 0;
			try
			{
				while (!_isDisposed && _isListening)
				{
					var files = Directory.GetFiles(_paths.InboxDirectory, "*.xml")
						.Where(f => !f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
						.OrderBy(f => File.GetCreationTimeUtc(f))
						.ThenBy(f => f, StringComparer.OrdinalIgnoreCase)
						.ToList();

					if (files.Count == 0)
						break;

					foreach (var file in files)
					{
						if (_isDisposed || !_isListening)
							break;

						await ProcessSingleCommandFileAsync(file).ConfigureAwait(true);
						count++;
					}
				}
			}
			finally
			{
				if (!_isDisposed)
					_processingGate.Release();
			}

			return count;
		}

		/// <summary>Clears the in-memory UI command history log.</summary>
		public void ClearLog() => LogEntries.Clear();

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			_themeCatalog.ActiveThemeChanged -= OnEnvironmentChanged;
			_osThemeService.OsThemeChanged -= OnEnvironmentChanged;

			if (_pollTimer != null)
			{
				_pollTimer.Stop();
				_pollTimer = null;
			}

			if (_heartbeatTimer != null)
			{
				_heartbeatTimer.Stop();
				_heartbeatTimer = null;
			}

			if (_inboxWatcher != null)
			{
				_inboxWatcher.EnableRaisingEvents = false;
				_inboxWatcher.Created -= OnInboxFileSignaled;
				_inboxWatcher.Renamed -= OnInboxFileSignaled;
				_inboxWatcher.Changed -= OnInboxFileSignaled;
				_inboxWatcher.Dispose();
				_inboxWatcher = null;
			}

			WriteHeartbeatSafe("Stopped");
			_processingGate.Dispose();
		}

		private static string? ReadFileWithRetry(string filePath)
		{
			for (var attempt = 0; attempt < 6; attempt++)
			{
				try
				{
					if (!File.Exists(filePath))
						return null;

					return File.ReadAllText(filePath, Encoding.UTF8);
				}
				catch (IOException) when (attempt < 5)
				{
					Thread.Sleep(25);
				}
			}

			return File.Exists(filePath) ? File.ReadAllText(filePath, Encoding.UTF8) : null;
		}

		private static void DeleteFileQuietly(string filePath)
		{
			try
			{
				if (File.Exists(filePath))
					File.Delete(filePath);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		private FrameworkElement? ResolveCaptureTarget()
		{
			return _visualRoot ?? _themeCatalog.AttachedDockingManager;
		}

		private async Task ProcessSingleCommandFileAsync(string filePath)
		{
			var sw = Stopwatch.StartNew();
			var fallbackId = Path.GetFileNameWithoutExtension(filePath);

			string? xmlText;
			try
			{
				xmlText = ReadFileWithRetry(filePath);
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				sw.Stop();
				WriteErrorResponse(fallbackId, "read-inbox", sw.Elapsed.TotalMilliseconds, $"Failed reading inbox file: {ex.Message}");
				DeleteFileQuietly(filePath);
				return;
			}

			if (xmlText == null)
				return;

			DeleteFileQuietly(filePath);

			AgentBridgeCommand cmd;
			try
			{
				cmd = AgentBridgeCommand.ParseXml(xmlText, filePath);
			}
			catch (Exception ex) when (ex is FormatException || ex is XmlException)
			{
				sw.Stop();
				WriteErrorResponse(fallbackId, "parse-xml", sw.Elapsed.TotalMilliseconds, $"Invalid command XML: {ex.Message}");
				ProcessedCommandCount++;
				AppendLogEntry(new AgentBridgeLogEntry(
					DateTimeOffset.Now,
					"Agent -> App",
					fallbackId,
					"parse-xml",
					"Error",
					sw.Elapsed.TotalMilliseconds,
					null,
					ex.Message));
				return;
			}

			try
			{
				var outcome = await ExecuteCommandAsync(cmd).ConfigureAwait(true);
				sw.Stop();

				var responsePath = WriteSuccessResponse(cmd, sw.Elapsed.TotalMilliseconds, outcome.Message, outcome.Bundle, outcome.ReportPath);
				ProcessedCommandCount++;
				LastBridgeSummary = $"[{cmd.RequestId}] {cmd.Action} -> Ok ({sw.Elapsed.TotalMilliseconds:F1} ms)";

				AppendLogEntry(new AgentBridgeLogEntry(
					DateTimeOffset.Now,
					"Agent -> App",
					cmd.RequestId,
					outcome.ActionSummary,
					"Ok",
					sw.Elapsed.TotalMilliseconds,
					outcome.Bundle?.LatestCompositePngPath ?? outcome.ReportPath ?? responsePath,
					outcome.Message));
			}
			catch (Exception ex) when (
				ex is FormatException ||
				ex is ArgumentException ||
				ex is InvalidOperationException ||
				ex is IOException ||
				ex is XmlException)
			{
				sw.Stop();
				var responsePath = WriteErrorResponse(cmd.RequestId, cmd.Action, sw.Elapsed.TotalMilliseconds, ex.Message);
				ProcessedCommandCount++;
				LastBridgeSummary = $"[{cmd.RequestId}] {cmd.Action} -> Error: {ex.Message}";

				AppendLogEntry(new AgentBridgeLogEntry(
					DateTimeOffset.Now,
					"Agent -> App",
					cmd.RequestId,
					cmd.Action,
					"Error",
					sw.Elapsed.TotalMilliseconds,
					responsePath,
					ex.Message));
			}

			PruneOutboxFiles();
			WriteHeartbeatSafe(_isListening ? "Listening" : "Paused");
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteCommandAsync(
			AgentBridgeCommand cmd)
		{
			var verb = cmd.Action.ToLowerInvariant();

			switch (verb)
			{
				case "ping":
				case "get-state":
					return (verb, $"Alive - ActiveTheme='{_themeCatalog.ActiveTheme?.DisplayName ?? "(None)"}'", null, null);

				case "capture-snapshot":
					return await ExecuteCaptureSnapshotAsync(cmd).ConfigureAwait(true);

				case "apply-theme":
					return await ExecuteApplyThemeAsync(cmd).ConfigureAwait(true);

				case "set-dwm-mode":
					return await ExecuteSetDwmModeAsync(cmd).ConfigureAwait(true);

				case "set-shell-options":
					return await ExecuteSetShellOptionsAsync(cmd).ConfigureAwait(true);

				case "spawn-overflow-tabs":
				case "close-overflow-tabs":
				case "spawn-floating":
				case "spawn-detached":
				case "dock-all":
				case "reset-layout":
					return await ExecuteStressLayoutCommandAsync(verb, cmd).ConfigureAwait(true);

				case "benchmark-cold-ctors":
				case "export-full-report":
					return ExecuteReportCommand(verb, cmd);

				case "load-inline-json":
					return await ExecuteLoadInlineJsonAsync(cmd).ConfigureAwait(true);

				case "sweep-all-18-screenshots":
				case "sweep-all-20-screenshots":
				case "sweep-all-screenshots":
				{
					var (reportPath, builtInCount) = await ExecuteAllBuiltInScreenshotSweepAsync(cmd.RequestId).ConfigureAwait(true);
					return (verb, $"Completed {builtInCount}-theme visual screenshot sweep.", null, reportPath);
				}

				default:
					throw new ArgumentException($"Unsupported AgentBridge command Action '{cmd.Action}'.");
			}
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteCaptureSnapshotAsync(
			AgentBridgeCommand cmd)
		{
			var note = !string.IsNullOrWhiteSpace(cmd.Payload) ? cmd.Payload : cmd.GetString("Note");
			var includeDwm = cmd.GetBool("IncludeDwm", defaultValue: true);
			var bundle = await CaptureIfPossibleAsync(cmd.RequestId, note, includeDwm).ConfigureAwait(true);
			return ("capture-snapshot", "Captured dual-mode screenshot + context Markdown bundle.", bundle, bundle?.LatestContextMarkdownPath);
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteApplyThemeAsync(
			AgentBridgeCommand cmd)
		{
			var themeKey = cmd.GetString("ThemeId") ?? cmd.GetString("Theme")
				?? throw new ArgumentException("Missing required 'ThemeId' parameter for 'apply-theme'.");
			var descriptor = _themeCatalog.FindById(themeKey)
				?? _themeCatalog.Themes.FirstOrDefault(t => string.Equals(t.DisplayName, themeKey, StringComparison.OrdinalIgnoreCase))
				?? throw new ArgumentException($"Unknown ThemeId '{themeKey}'.");

			var forceCold = cmd.GetBool("ForceCold", defaultValue: false);
			_themeCatalog.ApplyTheme(descriptor, forceCold);
			await YieldRenderAsync().ConfigureAwait(true);

			ScreenshotCaptureBundle? bundle = null;
			if (cmd.GetBool("Capture", defaultValue: true))
				bundle = await CaptureIfPossibleAsync($"{cmd.RequestId}-{descriptor.Id}", cmd.Payload, includeDwm: true).ConfigureAwait(true);

			return (
				$"apply-theme ({descriptor.Id})",
				$"Applied '{descriptor.DisplayName}' [{descriptor.BaseClassBadgeText}].",
				bundle,
				bundle?.LatestContextMarkdownPath);
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteSetDwmModeAsync(
			AgentBridgeCommand cmd)
		{
			var rawMode = cmd.GetString("Mode") ?? throw new ArgumentException("Missing 'Mode' parameter for 'set-dwm-mode'.");
			if (!Enum.TryParse<DwmTitleBarMode>(rawMode, ignoreCase: true, out var mode))
				throw new ArgumentException($"Invalid DwmTitleBarMode '{rawMode}'.");

			_shellHost?.SetDwmTitleBarModeFromAgent(mode);
			await YieldRenderAsync().ConfigureAwait(true);

			var bundle = cmd.GetBool("Capture", defaultValue: false)
				? await CaptureIfPossibleAsync($"{cmd.RequestId}-dwm", null, includeDwm: true).ConfigureAwait(true)
				: null;

			return ($"set-dwm-mode ({mode})", $"Set DWM Title-Bar Mode to '{mode}'.", bundle, null);
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteSetShellOptionsAsync(
			AgentBridgeCommand cmd)
		{
			if (_shellHost != null)
			{
				if (cmd.Parameters.ContainsKey("StyleOuterShell"))
					_shellHost.StyleOuterShellFromTheme = cmd.GetBool("StyleOuterShell", _shellHost.StyleOuterShellFromTheme);
				if (cmd.Parameters.ContainsKey("UseCustomHeader"))
					_shellHost.UseCustomDocumentHeaderTemplate = cmd.GetBool("UseCustomHeader", _shellHost.UseCustomDocumentHeaderTemplate);
				if (cmd.Parameters.ContainsKey("AutoSyncOs"))
					_shellHost.AutoSyncThemeToOsMode = cmd.GetBool("AutoSyncOs", _shellHost.AutoSyncThemeToOsMode);
			}

			await YieldRenderAsync().ConfigureAwait(true);
			var bundle = cmd.GetBool("Capture", defaultValue: false)
				? await CaptureIfPossibleAsync($"{cmd.RequestId}-options", null, includeDwm: true).ConfigureAwait(true)
				: null;

			return ("set-shell-options", "Updated shell chrome/header options.", bundle, null);
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteStressLayoutCommandAsync(
			string verb,
			AgentBridgeCommand cmd)
		{
			var defaultCapture = verb == "spawn-overflow-tabs" || verb == "spawn-floating" || verb == "spawn-detached";
			var summaryMessage = await InvokeShellLayoutActionAsync(verb).ConfigureAwait(true);
			await YieldRenderAsync().ConfigureAwait(true);

			var bundle = cmd.GetBool("Capture", defaultValue: defaultCapture)
				? await CaptureIfPossibleAsync($"{cmd.RequestId}-{verb}", null, includeDwm: true).ConfigureAwait(true)
				: null;

			return (verb, summaryMessage, bundle, null);
		}

		private async Task<string> InvokeShellLayoutActionAsync(string verb)
		{
			switch (verb)
			{
				case "spawn-overflow-tabs":
					if (_shellHost != null)
						await _shellHost.SpawnOverflowTabs().ConfigureAwait(true);
					return "Spawned 10 overflow specimen tabs.";

				case "close-overflow-tabs":
					if (_shellHost != null)
						await _shellHost.CloseSpecimenTabs().ConfigureAwait(true);
					return "Closed spawned overflow specimen tabs.";

				case "spawn-floating":
					if (_shellHost != null)
						await _shellHost.SpawnFloatingToolAndDocument().ConfigureAwait(true);
					return "Spawned floating document and toolbox windows.";

				case "spawn-detached":
					_shellHost?.SpawnDetachedOsWindow();
					return "Detached specimen toolbox to standalone OS DetachedAnchorableWindow.";

				case "dock-all":
					_shellHost?.DockAllWindows();
					return "Re-docked all floating and detached windows.";

				default:
					_shellHost?.ResetLayout();
					return "Reset docking layout to startup defaults.";
			}
		}

		private (string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath) ExecuteReportCommand(
			string verb,
			AgentBridgeCommand cmd)
		{
			if (verb == "benchmark-cold-ctors")
			{
				_themeCatalog.BenchmarkAllColdConstructors();
				var md = _themeCatalog.BuildMarkdownBenchmarkReport();
				var benchmarkPath = Path.Combine(_paths.OutboxDirectory, $"{cmd.RequestId}-benchmark.md");
				WindowScreenshotService.SaveTextAtomic(md, benchmarkPath);
				return ("benchmark-cold-ctors", "Completed cold constructor benchmark across all registered themes.", null, benchmarkPath);
			}

			var reportPath = Path.Combine(_paths.OutboxDirectory, $"{cmd.RequestId}-full-report.md");
			_reportExporter.ExportToFile(reportPath);
			return ("export-full-report", "Exported full 6-section Markdown diagnostic report.", null, reportPath);
		}

		private async Task<(string ActionSummary, string Message, ScreenshotCaptureBundle? Bundle, string? ReportPath)> ExecuteLoadInlineJsonAsync(
			AgentBridgeCommand cmd)
		{
			var json = !string.IsNullOrWhiteSpace(cmd.Payload)
				? cmd.Payload!
				: (cmd.GetString("Json") ?? throw new ArgumentException("Missing <Payload> or 'Json' parameter for 'load-inline-json'."));

			var baseKey = cmd.GetString("BasePalette", "VS2022Dark") ?? "VS2022Dark";
			var templateKey = cmd.GetString("TemplateSet", "VS2022") ?? "VS2022";
			var customTitle = cmd.GetString("DisplayName", "Agent Inline JSON") ?? "Agent Inline JSON";

			var prepared = CustomRuntimeVsTheme.PrepareInlineJsonFactory(json, baseKey, templateKey, customTitle);
			var descriptor = _themeCatalog.RegisterRuntimeTheme(
				"Runtime.AgentInlineJson",
				customTitle,
				ThemePolarity.Dark,
				typeof(CustomRuntimeVsTheme),
				prepared.Factory);

			_themeCatalog.ApplyTheme(descriptor, forceFreshInstance: true);
			await YieldRenderAsync().ConfigureAwait(true);

			var bundle = cmd.GetBool("Capture", defaultValue: true)
				? await CaptureIfPossibleAsync($"{cmd.RequestId}-inlinejson", null, includeDwm: true).ConfigureAwait(true)
				: null;

			return (
				"load-inline-json",
				$"Parsed and applied '{customTitle}' ({prepared.OverrideCount} override token(s)).",
				bundle,
				bundle?.LatestContextMarkdownPath);
		}

		private async Task<(string ReportPath, int BuiltInCount)> ExecuteAllBuiltInScreenshotSweepAsync(string requestId)
		{
			var target = ResolveCaptureTarget();
			var sweepDir = Path.Combine(_paths.CapturesDirectory, $"sweep-{SanitizeId(requestId)}");
			Directory.CreateDirectory(sweepDir);

			var previous = _themeCatalog.ActiveTheme;
			var builtIns = _themeCatalog.Themes.Where(t => !t.IsCustomRuntimeTheme).ToList();

			var sb = new StringBuilder();
			sb.AppendLine($"# {builtIns.Count}-Theme Visual Screenshot Sweep — `{requestId}`");
			sb.AppendLine();
			sb.AppendLine("| # | Theme | Base Class | Apply (ms) | Unfrozen Freezables | WPF Screenshot | DWM Screenshot |");
			sb.AppendLine("| ---: | :--- | :--- | ---: | ---: | :--- | :--- |");

			for (var i = 0; i < builtIns.Count; i++)
			{
				var desc = builtIns[i];
				_themeCatalog.ApplyTheme(desc, forceFreshInstance: false);
				await YieldRenderAsync().ConfigureAwait(true);

				var audit = _auditService.AuditDescriptor(desc, _themeCatalog.AttachedDockingManager);
				var wpfPath = "-";
				var dwmPath = "-";

				if (target != null)
				{
					var wpfBmp = WindowScreenshotService.CaptureWpfVisualBitmap(target);
					wpfPath = Path.Combine(sweepDir, $"{i + 1:D2}-{desc.Id}-wpf.png");
					WindowScreenshotService.SavePngAtomic(wpfBmp, wpfPath);

					if (target is Window win)
					{
						var dwmBmp = WindowScreenshotService.TryCaptureWin32DwmWindowBitmap(win);
						if (dwmBmp != null)
						{
							dwmPath = Path.Combine(sweepDir, $"{i + 1:D2}-{desc.Id}-dwm.png");
							WindowScreenshotService.SavePngAtomic(dwmBmp, dwmPath);
						}
					}
				}

				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"| {0} | {1} | `{2}` | {3:F2} | {4} | `{5}` | `{6}` |",
					i + 1,
					desc.DisplayName,
					desc.BaseClassBadgeText,
					desc.LastApplyMs ?? 0.0,
					audit.UnfrozenFreezableCount,
					wpfPath,
					dwmPath);
				sb.AppendLine();
			}

			if (previous != null)
			{
				_themeCatalog.ApplyTheme(previous, forceFreshInstance: false);
				await YieldRenderAsync().ConfigureAwait(true);
			}

			var indexPath = Path.Combine(sweepDir, "sweep-index.md");
			WindowScreenshotService.SaveTextAtomic(sb.ToString(), indexPath);
			return (indexPath, builtIns.Count);
		}

		private async Task<ScreenshotCaptureBundle?> CaptureIfPossibleAsync(string slug, string? note, bool includeDwm)
		{
			var target = ResolveCaptureTarget();
			if (target == null)
				return null;

			await YieldRenderAsync().ConfigureAwait(true);
			return _screenshotService.CaptureSnapshotBundle(target, slug, note, includeDwmCapture: includeDwm, updateLatestFolder: true);
		}

		private Task YieldRenderAsync()
		{
			var target = ResolveCaptureTarget();
			var disp = target?.Dispatcher ?? _dispatcher;
			if (disp.CheckAccess())
			{
				disp.Invoke(static () => { }, DispatcherPriority.Render);
				return Task.CompletedTask;
			}

			return disp.InvokeAsync(static () => { }, DispatcherPriority.Render).Task;
		}

		private string WriteSuccessResponse(
			AgentBridgeCommand cmd,
			double durationMs,
			string message,
			ScreenshotCaptureBundle? bundle,
			string? reportPath)
		{
			_traceCapture.FlushPendingNow();
			var active = _themeCatalog.ActiveTheme;
			var os = _osThemeService.CurrentSnapshot;

			var unfrozenCount = 0;
			if (active != null)
				unfrozenCount = _auditService.AuditDescriptor(active, _themeCatalog.AttachedDockingManager).UnfrozenFreezableCount;

			var sharing = _windowTracker.CaptureReport(
				active?.DisplayName ?? "(None)",
				active?.CachedInstance,
				_themeCatalog.AttachedDockingManager);

			var traceErrors = _traceCapture.Events.Count(e => e.Severity == TraceEventType.Error || e.Severity == TraceEventType.Critical);

			var xml = AgentBridgeResponse.FormatSuccessXml(
				cmd,
				durationMs,
				message,
				(active, os),
				(unfrozenCount, sharing.TotalThemedHosts, sharing.DistinctDictionaryInstances, traceErrors),
				(bundle, reportPath, _paths));

			var outPath = Path.Combine(_paths.OutboxDirectory, $"{SanitizeId(cmd.RequestId)}-response.xml");
			WindowScreenshotService.SaveTextAtomic(xml, outPath);
			return outPath;
		}

		private string WriteErrorResponse(string requestId, string action, double durationMs, string errorMessage)
		{
			var xml = AgentBridgeResponse.FormatErrorXml(requestId, action, durationMs, errorMessage);
			var outPath = Path.Combine(_paths.OutboxDirectory, $"{SanitizeId(requestId)}-response.xml");
			WindowScreenshotService.SaveTextAtomic(xml, outPath);
			return outPath;
		}

		private void WriteHeartbeatSafe(string state)
		{
			try
			{
				_paths.EnsureDirectoriesExist();
				var active = _themeCatalog.ActiveTheme;
				var traceErrors = _traceCapture.Events.Count(e => e.Severity == TraceEventType.Error || e.Severity == TraceEventType.Critical);
				int pid;
				using (var proc = Process.GetCurrentProcess())
					pid = proc.Id;

				var xml = AgentBridgeResponse.FormatHeartbeatXml(
					(pid, state, _startedAtUtc, _processedCommandCount, traceErrors),
					active,
					_paths);

				WindowScreenshotService.SaveTextAtomic(xml, _paths.HeartbeatFilePath);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}

		private void PruneOutboxFiles()
		{
			if (!Directory.Exists(_paths.OutboxDirectory))
				return;

			var files = new DirectoryInfo(_paths.OutboxDirectory)
				.GetFiles()
				.Where(f => !f.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
				.OrderByDescending(f => f.LastWriteTimeUtc)
				.Skip(MaxOutboxFiles)
				.ToList();

			foreach (var old in files)
				DeleteFileQuietly(old.FullName);
		}

		private void AppendLogEntry(AgentBridgeLogEntry entry)
		{
			LogEntries.Insert(0, entry);
			while (LogEntries.Count > MaxLogEntries)
				LogEntries.RemoveAt(LogEntries.Count - 1);
		}

		private void OnInboxFileSignaled(object sender, FileSystemEventArgs e)
		{
			if (_isDisposed || !_isListening || e.Name == null || e.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
				return;

			_dispatcher.BeginInvoke(DispatcherPriority.Background, new System.Action(() => _ = ProcessPendingInboxAsync()));
		}

		private void OnPollTimerTick(object? sender, EventArgs e)
		{
			if (!_isDisposed && _isListening)
				_ = ProcessPendingInboxAsync();
		}

		private void OnHeartbeatTimerTick(object? sender, EventArgs e)
		{
			if (!_isDisposed)
				WriteHeartbeatSafe(_isListening ? "Listening" : "Paused");
		}

		private void OnEnvironmentChanged(object? sender, EventArgs e)
		{
			if (_isStarted && !_isDisposed)
				WriteHeartbeatSafe(_isListening ? "Listening" : "Paused");
		}

		private static string SanitizeId(string raw)
		{
			var sb = new StringBuilder(raw.Length);
			foreach (var ch in raw)
			{
				if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
					sb.Append(ch);
			}

			return sb.Length > 0 ? sb.ToString() : "req";
		}
	}
}