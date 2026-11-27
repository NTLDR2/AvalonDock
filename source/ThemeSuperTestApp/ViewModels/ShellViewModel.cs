using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonDock.Core;
using AvalonDock.Layout;
using Caliburn.Micro;
using Microsoft.Win32;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;
using ThemeSuperTestApp.ViewModels.Documents;
using ThemeSuperTestApp.ViewModels.Tools;

namespace ThemeSuperTestApp.ViewModels
{
	/// <summary>
	/// Main shell ViewModel conducting specimen documents and orchestrating theme selection, Windows OS mode diagnostics,
	/// chrome simulation, multi-window/overflow stress testing, and full Markdown diagnostic report export.
	/// </summary>
	public sealed class ShellViewModel : DockLayoutConductor<DocumentScreen>, IAgentBridgeShellHost
	{
		private static readonly SolidColorBrush NeutralRow1Brush = CreateFrozenBrush(0xF3, 0xF4, 0xF6);
		private static readonly SolidColorBrush NeutralRow2Brush = CreateFrozenBrush(0xEA, 0xEC, 0xF0);
		private static readonly SolidColorBrush NeutralBorderBrush = CreateFrozenBrush(0xD0, 0xD4, 0xDC);
		private static readonly SolidColorBrush NeutralForegroundBrush = CreateFrozenBrush(0x1E, 0x1E, 0x22);

		private static readonly SolidColorBrush DarkRow1Brush = CreateFrozenBrush(0x1E, 0x1E, 0x22);
		private static readonly SolidColorBrush DarkRow2Brush = CreateFrozenBrush(0x25, 0x26, 0x2B);
		private static readonly SolidColorBrush DarkBorderBrush = CreateFrozenBrush(0x3E, 0x40, 0x48);
		private static readonly SolidColorBrush DarkForegroundBrush = CreateFrozenBrush(0xE8, 0xEA, 0xEE);

		private static readonly SolidColorBrush BlueRow1Brush = CreateFrozenBrush(0x29, 0x39, 0x55);
		private static readonly SolidColorBrush BlueRow2Brush = CreateFrozenBrush(0x34, 0x47, 0x69);
		private static readonly SolidColorBrush BlueBorderBrush = CreateFrozenBrush(0x4A, 0x60, 0x8A);
		private static readonly SolidColorBrush BlueForegroundBrush = CreateFrozenBrush(0xF5, 0xF7, 0xFA);

		private static readonly Color[] SpecimenBadgeColors = new[]
		{
			Color.FromRgb(0xEF, 0x44, 0x44),
			Color.FromRgb(0xF5, 0x9E, 0x0B),
			Color.FromRgb(0x10, 0xB9, 0x81),
			Color.FromRgb(0x06, 0xB6, 0xD4),
			Color.FromRgb(0x3B, 0x82, 0xF6),
			Color.FromRgb(0x8B, 0x5C, 0xF6),
			Color.FromRgb(0xEC, 0x48, 0x99),
		};

		private readonly ThemeCatalogService _themeCatalog;
		private readonly WindowsOsThemeService _osThemeService;
		private readonly DwmWindowChromeHelper _dwmChromeHelper;
		private readonly LayoutPersistenceService _layoutPersistence;
		private readonly DiagnosticReportExporterService _reportExporter;
		private readonly AgentBridgeService _agentBridge;
		private readonly AgentBridgeToolboxViewModel _agentBridgeToolbox;
		private readonly ThemeOverviewDocumentViewModel _overviewDocument;
		private readonly ControlGalleryDocumentViewModel _controlGalleryDocument;
		private readonly StressSpecimenToolboxViewModel _stressToolbox;
		private readonly WindowSharingTrackerViewModel _sharingTracker;

		private ThemeDescriptor? _selectedTheme;
		private DwmTitleBarModeOption _selectedDwmTitleBarMode;
		private bool _useCustomDocumentHeaderTemplate = true;
		private bool _styleOuterShellFromTheme;
		private bool _autoSyncThemeToOsMode;
		private int _nextSpecimenIndex = 1;
		private string _statusText = "Ready";

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellViewModel"/> class.
		/// </summary>
		/// <param name="layoutService">The AvalonDock layout service.</param>
		/// <param name="themeCatalog">The lazy 20-theme catalog service.</param>
		/// <param name="osThemeService">The Windows OS Dark/Light/High-Contrast detector service.</param>
		/// <param name="dwmChromeHelper">The opt-in Win32 DWM title-bar helper.</param>
		/// <param name="layoutPersistence">The XML layout persistence service.</param>
		/// <param name="reportExporter">The Markdown diagnostic report exporter service.</param>
		/// <param name="agentBridge">The bidirectional Antigravity file-mailbox service.</param>
		/// <param name="agentBridgeToolbox">Tool 6 Antigravity bridge toolbox ViewModel.</param>
		/// <param name="overviewDocument">The singleton theme swatch overview document.</param>
		/// <param name="controlGalleryDocument">The singleton WPF control &amp; SystemColors contrast document.</param>
		/// <param name="stressToolbox">The dedicated tear-off specimen toolbox.</param>
		/// <param name="sharingTracker">Tool 3 multi-window sharing tracker.</param>
		public ShellViewModel(
			IDockLayoutService layoutService,
			ThemeCatalogService themeCatalog,
			WindowsOsThemeService osThemeService,
			DwmWindowChromeHelper dwmChromeHelper,
			LayoutPersistenceService layoutPersistence,
			DiagnosticReportExporterService reportExporter,
			AgentBridgeService agentBridge,
			AgentBridgeToolboxViewModel agentBridgeToolbox,
			ThemeOverviewDocumentViewModel overviewDocument,
			ControlGalleryDocumentViewModel controlGalleryDocument,
			StressSpecimenToolboxViewModel stressToolbox,
			WindowSharingTrackerViewModel sharingTracker)
			: base(layoutService)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_osThemeService = osThemeService ?? throw new ArgumentNullException(nameof(osThemeService));
			_dwmChromeHelper = dwmChromeHelper ?? throw new ArgumentNullException(nameof(dwmChromeHelper));
			_layoutPersistence = layoutPersistence ?? throw new ArgumentNullException(nameof(layoutPersistence));
			_reportExporter = reportExporter ?? throw new ArgumentNullException(nameof(reportExporter));
			_agentBridge = agentBridge ?? throw new ArgumentNullException(nameof(agentBridge));
			_agentBridgeToolbox = agentBridgeToolbox ?? throw new ArgumentNullException(nameof(agentBridgeToolbox));
			_agentBridge.AttachShellHost(this);
			_overviewDocument = overviewDocument ?? throw new ArgumentNullException(nameof(overviewDocument));
			_controlGalleryDocument = controlGalleryDocument ?? throw new ArgumentNullException(nameof(controlGalleryDocument));
			_stressToolbox = stressToolbox ?? throw new ArgumentNullException(nameof(stressToolbox));
			_sharingTracker = sharingTracker ?? throw new ArgumentNullException(nameof(sharingTracker));

			DisplayName = "ThemeSuperTestApp - AvalonDock v5 Theme Debugger & Best-Practices Auditor (.NET Framework 4.8)";
			Tools = layoutService.Anchorables.OfType<ToolboxScreen>().ToList().AsReadOnly();

			DwmTitleBarModes = new[]
			{
				new DwmTitleBarModeOption(DwmTitleBarMode.DefaultUntouched, "Untouched (net48 Default)"),
				new DwmTitleBarModeOption(DwmTitleBarMode.MatchWindowsOsMode, "Match Windows OS Mode"),
				new DwmTitleBarModeOption(DwmTitleBarMode.MatchActiveAvalonDockTheme, "Match Active AvalonDock Theme"),
			};
			_selectedDwmTitleBarMode = DwmTitleBarModes[0];

			foreach (var tool in Tools)
				tool.IsOpen = tool.IsOpenByDefault;

			AttachSelectedThemeObserver(_themeCatalog.ActiveTheme ?? _themeCatalog.FindById(ThemeCatalogService.DefaultThemeId));
			_themeCatalog.ActiveThemeChanged += OnActiveThemeChanged;
			_osThemeService.OsThemeChanged += OnOsThemeChanged;
		}

		/// <summary>
		/// Raised when <see cref="UseCustomDocumentHeaderTemplate"/> changes so <c>ShellView</c> can toggle <c>DockingManager.DocumentHeaderTemplate</c>.
		/// </summary>
		public event EventHandler? DocumentHeaderTemplateOptionChanged;

		/// <summary>Gets the observable catalog of all registered theme descriptors.</summary>
		public BindableCollection<ThemeDescriptor> Themes => _themeCatalog.Themes;

		/// <summary>Gets the registered diagnostic toolboxes.</summary>
		public IReadOnlyList<ToolboxScreen> Tools { get; }

		/// <summary>Gets the selectable Win32 DWM title-bar override modes.</summary>
		public IReadOnlyList<DwmTitleBarModeOption> DwmTitleBarModes { get; }

		/// <summary>Gets the latest Windows OS Dark/Light/High-Contrast/Accent snapshot.</summary>
		public WindowsOsThemeSnapshot OsSnapshot => _osThemeService.CurrentSnapshot;

		/// <summary>
		/// Gets or sets the currently selected theme descriptor.
		/// </summary>
		public ThemeDescriptor? SelectedTheme
		{
			get => _selectedTheme;
			set
			{
				if (value == null || ReferenceEquals(_selectedTheme, value))
					return;

				if (!_themeCatalog.ApplyTheme(value))
					return;

				AttachSelectedThemeObserver(value);
				RaiseSelectedThemeComputedProperties();
				StatusText = $"Applied '{value.DisplayName}' [{value.BaseClassBadgeText}] ({value.ClrNamespace})";
			}
		}

		/// <summary>
		/// Gets or sets the selected DWM non-client title-bar mode.
		/// </summary>
		public DwmTitleBarModeOption SelectedDwmTitleBarMode
		{
			get => _selectedDwmTitleBarMode;
			set
			{
				if (value == null || !Set(ref _selectedDwmTitleBarMode, value))
					return;

				_dwmChromeHelper.Mode = value.Mode;
				StatusText = $"DWM Title-Bar Mode: {value.DisplayName}";
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether <c>DockingManager.DocumentHeaderTemplate</c> uses the app's icon-enabled header template
		/// (<see langword="true"/>) or <see langword="null"/> to test each theme's native tab header template (<see langword="false"/>).
		/// </summary>
		public bool UseCustomDocumentHeaderTemplate
		{
			get => _useCustomDocumentHeaderTemplate;
			set
			{
				if (!Set(ref _useCustomDocumentHeaderTemplate, value))
					return;

				DocumentHeaderTemplateOptionChanged?.Invoke(this, EventArgs.Empty);
				StatusText = value
					? "DocumentHeaderTemplate: Custom Icon + Dirty Dot Template"
					: "DocumentHeaderTemplate: <null> (Theme-Native Tab Header Template)";
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the outer shell header, menu, and status bar are tinted to match the active theme's polarity.
		/// </summary>
		public bool StyleOuterShellFromTheme
		{
			get => _styleOuterShellFromTheme;
			set
			{
				if (!Set(ref _styleOuterShellFromTheme, value))
					return;

				RaiseOuterShellBrushProperties();
				StatusText = value
					? "Outer Shell Chrome: Styled from active theme polarity"
					: "Outer Shell Chrome: Raw unthemed net48 default";
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the active AvalonDock theme automatically follows Windows OS <c>AppsUseLightTheme</c>.
		/// </summary>
		public bool AutoSyncThemeToOsMode
		{
			get => _autoSyncThemeToOsMode;
			set
			{
				if (!Set(ref _autoSyncThemeToOsMode, value))
					return;

				if (value)
					SyncThemeToOsNow();
				else
					StatusText = "Auto-Sync Theme to OS Mode disabled";
			}
		}

		/// <summary>Gets the formatted cold constructor and apply timing pill for <see cref="SelectedTheme"/>.</summary>
		public string SelectedThemeTimingText
		{
			get
			{
				if (_selectedTheme == null)
					return ".ctor: - | Apply: -";

				var ctorText = _selectedTheme.LastColdConstructorMs.HasValue
					? string.Format(CultureInfo.InvariantCulture, "{0:F2} ms", _selectedTheme.LastColdConstructorMs.Value)
					: "-";
				var applyText = _selectedTheme.LastApplyMs.HasValue
					? string.Format(CultureInfo.InvariantCulture, "{0:F2} ms", _selectedTheme.LastApplyMs.Value)
					: "-";
				return $".ctor: {ctorText} | Apply: {applyText}";
			}
		}

		/// <summary>Gets a value indicating whether the active AvalonDock theme polarity conflicts with the Windows OS mode or High Contrast.</summary>
		public bool HasThemeVsOsMismatch
		{
			get
			{
				var os = OsSnapshot;
				if (os.IsHighContrastEnabled)
					return true;
				if (_selectedTheme == null || os.AppsThemeMode == OsLightDarkState.NotConfiguredOrLegacyOs)
					return false;

				var isThemeDark = _selectedTheme.Polarity == ThemePolarity.Dark;
				var isOsDark = os.AppsThemeMode == OsLightDarkState.ExplicitDark;
				return isThemeDark != isOsDark;
			}
		}

		/// <summary>Gets the diagnostic explanation comparing the active AvalonDock theme against Windows OS Dark/Light/High-Contrast state.</summary>
		public string ThemeVsOsNoticeText
		{
			get
			{
				var os = OsSnapshot;
				if (os.IsHighContrastEnabled)
					return "High Contrast ON: SystemColors.* are overridden OS-wide; verify templates for hardcoded vs. SystemColors collisions.";

				if (_selectedTheme == null)
					return "No AvalonDock theme selected.";

				if (os.AppsThemeMode == OsLightDarkState.NotConfiguredOrLegacyOs)
					return "Legacy / Unconfigured OS Personalize key: net48 Aero2 defaults to Light chrome.";

				var isThemeDark = _selectedTheme.Polarity == ThemePolarity.Dark;
				var isOsDark = os.AppsThemeMode == OsLightDarkState.ExplicitDark;

				if (isThemeDark && !isOsDark)
					return "Mismatch: AvalonDock theme is Dark while Windows Apps Mode (& net48 Aero2) is Explicit Light.";
				if (!isThemeDark && isOsDark)
					return "Mismatch: AvalonDock theme is Light/Blue while Windows Apps Mode is Explicit Dark.";
				if (isThemeDark && isOsDark)
					return "Aligned (Both Dark): Note that net48 Aero2 client controls & untouched Win32 title bars remain Light.";

				return "Aligned (Both Light): AvalonDock theme polarity matches Windows Apps Mode & net48 Aero2.";
			}
		}

		/// <summary>Gets the background brush for Row 1 of the shell header bar.</summary>
		public SolidColorBrush ShellRow1BackgroundBrush => ResolveShellPalette().Row1;

		/// <summary>Gets the background brush for Row 2 (OS banner) and the status bar.</summary>
		public SolidColorBrush ShellRow2BackgroundBrush => ResolveShellPalette().Row2;

		/// <summary>Gets the border brush for the shell header and status bar.</summary>
		public SolidColorBrush ShellBorderBrush => ResolveShellPalette().Border;

		/// <summary>Gets the primary text foreground brush for the shell header and status bar.</summary>
		public SolidColorBrush ShellForegroundBrush => ResolveShellPalette().Foreground;

		/// <summary>Gets the bidirectional Antigravity bridge service (bound to the header status pill).</summary>
		public AgentBridgeService AgentBridge => _agentBridge;

		/// <inheritdoc/>
		public string UserSnapshotNote
		{
			get => _agentBridgeToolbox.UserSnapshotNote;
			set => _agentBridgeToolbox.UserSnapshotNote = value;
		}

		/// <summary>Gets or sets the status bar message.</summary>
		public string StatusText
		{
			get => _statusText;
			set => Set(ref _statusText, value);
		}

		/// <summary>Gets a value indicating whether a saved XML layout exists on disk.</summary>
		public bool CanLoadLayout => _layoutPersistence.HasSavedLayout;

		/// <summary>Opens or activates the Theme Swatch Sheet document.</summary>
		/// <returns>A task that completes when active.</returns>
		public Task OpenThemeSwatchSheet() => ActivateItemAsync(_overviewDocument, CancellationToken.None);

		/// <summary>Opens or activates the WPF Control &amp; SystemColors Contrast Matrix document.</summary>
		/// <returns>A task that completes when active.</returns>
		public Task OpenControlGallery() => ActivateItemAsync(_controlGalleryDocument, CancellationToken.None);

		/// <summary>
		/// Spawns 10 icon + dirty-marker specimen documents to overflow the document tab strip and test <c>MenuItemEx</c> &amp; <c>NavigatorWindow</c>.
		/// </summary>
		/// <returns>A task that completes when all 10 tabs are added.</returns>
		public async Task SpawnOverflowTabs()
		{
			string[] sampleNames =
			{
				"ThemeResourceBuilder_DiagnosticSpecimen.cs",
				"VsJsonThemeParser_OverridePalette.json",
				"DetachedAnchorableWindow_ChromeTest.xaml",
				"MenuItemEx_OverflowDropdownCheck.xaml",
				"NavigatorWindow_CtrlTabPreview.cs",
				"FreezableBrushAudit_PaletteTokens.md",
				"AutoHideFlyout_AirspaceCheck.cs",
				"OverlayWindow_DropTargetsPreview.xaml",
				"SystemColors_HighContrastMatrix.md",
				"XmlnsDefinition_NamespaceMapping.cs",
			};

			SpecimenNoteDocumentViewModel? lastCreated = null;
			for (var i = 0; i < sampleNames.Length; i++)
			{
				var number = _nextSpecimenIndex++;
				var badgeColor = SpecimenBadgeColors[(number - 1) % SpecimenBadgeColors.Length];
				var isModified = (i % 2) == 0;
				var doc = new SpecimenNoteDocumentViewModel(number, $"{number:D2}_{sampleNames[i]}", badgeColor, isModified);
				Adopt(doc);
				lastCreated = doc;
			}

			if (lastCreated != null)
				await ActivateItemAsync(lastCreated, CancellationToken.None);

			StatusText = $"Spawned 10 overflow specimen tabs ({Items.Count} total documents). Click the tab strip's overflow arrow or press Ctrl+Tab!";
		}

		/// <summary>Closes all spawned <see cref="SpecimenNoteDocumentViewModel"/> tabs while keeping core overview documents open.</summary>
		/// <returns>A task that completes when all specimen note tabs are closed.</returns>
		public async Task CloseSpecimenTabs()
		{
			var specimens = Items.OfType<SpecimenNoteDocumentViewModel>().ToList();
			foreach (var doc in specimens)
				await DeactivateItemAsync(doc, close: true, CancellationToken.None);

			StatusText = $"Closed {specimens.Count} specimen note tab(s).";
		}

		/// <summary>
		/// Programmatically floats a specimen document (<c>LayoutDocumentFloatingWindowControl</c>) and the tear-off specimen toolbox
		/// (<c>LayoutAnchorableFloatingWindowControl</c>) to test floating chrome and Tool 3 dictionary sharing.
		/// </summary>
		/// <returns>A task that completes once both floating windows are spawned.</returns>
		public async Task SpawnFloatingToolAndDocument()
		{
			var dm = _themeCatalog.AttachedDockingManager;
			if (dm?.Layout == null)
				return;

			var targetDoc = Items.OfType<SpecimenNoteDocumentViewModel>().FirstOrDefault();
			if (targetDoc == null)
			{
				var num = _nextSpecimenIndex++;
				targetDoc = new SpecimenNoteDocumentViewModel(num, $"{num:D2}_FloatingDocumentSpecimen.cs", SpecimenBadgeColors[0], isModified: true);
				await ActivateItemAsync(targetDoc, CancellationToken.None);
			}

			var layoutDoc = dm.Layout.Descendents().OfType<LayoutDocument>().FirstOrDefault(d => ReferenceEquals(d.Content, targetDoc));
			if (layoutDoc != null && !layoutDoc.IsFloating)
				layoutDoc.Float();

			_stressToolbox.IsOpen = true;
			var layoutTool = dm.Layout.Descendents().OfType<LayoutAnchorable>().FirstOrDefault(a => ReferenceEquals(a.Content, _stressToolbox));
			if (layoutTool != null && !dm.DetachedAnchorables.Contains(layoutTool) && !layoutTool.IsFloating)
				layoutTool.Float();

			_sharingTracker.RefreshSnapshot();
			StatusText = "Spawned floating document + floating toolbox windows. Inspect Tool 3 (Multi-Window Dict Sharing)!";
		}

		/// <summary>
		/// Programmatically detaches <see cref="StressSpecimenToolboxViewModel"/> into a standalone top-level OS
		/// <c>DetachedAnchorableWindow</c> ("Window" mode).
		/// </summary>
		public void SpawnDetachedOsWindow()
		{
			var dm = _themeCatalog.AttachedDockingManager;
			if (dm?.Layout == null)
				return;

			_stressToolbox.IsOpen = true;
			var anchorable = dm.Layout.Descendents()
				.OfType<LayoutAnchorable>()
				.FirstOrDefault(a => ReferenceEquals(a.Content, _stressToolbox));

			if (anchorable == null)
				return;

			if (anchorable.IsHidden)
				anchorable.Show();
			if (anchorable.IsFloating)
				anchorable.Dock();

			if (!dm.DetachedAnchorables.Contains(anchorable))
				dm.DetachAnchorableToWindow(anchorable);

			_dwmChromeHelper.ApplyCurrentModeToAllWindows();
			_sharingTracker.RefreshSnapshot();
			StatusText = "Detached 'Tear-Off Specimen Tool' into standalone OS DetachedAnchorableWindow. Test DWM Title Bars & Tool 3!";
		}

		/// <summary>Re-docks all detached OS windows and floating windows back into the main <c>DockingManager</c>.</summary>
		public void DockAllWindows()
		{
			var dm = _themeCatalog.AttachedDockingManager;
			if (dm?.Layout == null)
				return;

			dm.ReattachAllDetachedAnchorables();

			var floatingItems = dm.Layout.FloatingWindows
				.SelectMany(fw => fw.Descendents().OfType<LayoutContent>())
				.ToList();

			foreach (var content in floatingItems)
				content.Dock();

			_sharingTracker.RefreshSnapshot();
			StatusText = "Re-docked all floating and detached windows.";
		}

		/// <inheritdoc/>
		public void SetDwmTitleBarModeFromAgent(DwmTitleBarMode mode)
		{
			var match = DwmTitleBarModes.FirstOrDefault(m => m.Mode == mode);
			if (match != null)
				SelectedDwmTitleBarMode = match;
		}

		/// <summary>
		/// Captures dual-mode WPF + Win32 DWM screenshots, multi-window composite PNG, and full diagnostic Markdown into
		/// <c>AgentBridge/latest/</c> (<c>F12</c>) so Antigravity can inspect the live visual state immediately.
		/// </summary>
		/// <returns>A task that completes when all PNG and Markdown files are written atomically.</returns>
		public async Task SendSnapshotToAntigravity()
		{
			var bundle = await _agentBridge.CaptureManualSnapshotAsync(_agentBridgeToolbox.UserSnapshotNote);
			if (bundle == null)
				return;

			var dwmNote = bundle.MainDwmPngPath != null ? "WPF + Win32 DWM + Composite" : "WPF + Composite";
			StatusText = $"Snapshot sent to Antigravity ({dwmNote}) -> {bundle.LatestCompositePngPath}";
		}

		/// <summary>Prompts for a destination path and exports the full Markdown theme diagnostic report.</summary>
		public void ExportDiagnosticReport()
		{
			var dialog = new SaveFileDialog
			{
				Title = "Export Full AvalonDock Theme Diagnostic Report",
				Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*",
				FileName = $"AvalonDock-Theme-Diagnostic-Report-{DateTime.Now:yyyyMMdd-HHmmss}.md",
				DefaultExt = ".md",
			};

			if (dialog.ShowDialog() != true)
				return;

			try
			{
				_reportExporter.ExportToFile(dialog.FileName);
				StatusText = $"Exported full diagnostic report to '{dialog.FileName}'.";
			}
			catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
			{
				StatusText = $"Failed to save report: {ex.Message}";
			}
		}

		/// <summary>Copies the full Markdown diagnostic report directly to the Windows clipboard.</summary>
		public void CopyFullDiagnosticReport()
		{
			var md = _reportExporter.BuildFullMarkdownReport();
			try
			{
				Clipboard.SetDataObject(md, copy: true);
				StatusText = $"Copied full Markdown diagnostic report to clipboard ({md.Length} chars).";
			}
			catch (ExternalException ex)
			{
				StatusText = $"Clipboard busy ({ex.Message}).";
			}
		}

		/// <summary>Selects the next theme in <see cref="Themes"/> (<c>F8</c>).</summary>
		public void NextTheme()
		{
			if (Themes.Count == 0)
				return;

			var index = _selectedTheme != null ? Themes.IndexOf(_selectedTheme) : -1;
			SelectedTheme = Themes[(index + 1) % Themes.Count];
		}

		/// <summary>Selects the previous theme in <see cref="Themes"/> (<c>Shift+F8</c>).</summary>
		public void PreviousTheme()
		{
			if (Themes.Count == 0)
				return;

			var index = _selectedTheme != null ? Themes.IndexOf(_selectedTheme) : 0;
			SelectedTheme = Themes[(index - 1 + Themes.Count) % Themes.Count];
		}

		/// <summary>Forces constructing a fresh <c>Theme</c> instance for <see cref="SelectedTheme"/> and re-applying it to measure cold cost.</summary>
		public void ForceColdReloadCurrentTheme()
		{
			if (_selectedTheme == null)
				return;

			_themeCatalog.ApplyTheme(_selectedTheme, forceFreshInstance: true);
			RaiseSelectedThemeComputedProperties();
			StatusText = $"Cold re-instantiated '{_selectedTheme.DisplayName}' ({SelectedThemeTimingText})";
		}

		/// <summary>Re-reads Windows OS registry/DWM settings immediately and updates the OS banner.</summary>
		public void RefreshOsThemeState()
		{
			_osThemeService.Refresh();
			StatusText = $"Refreshed OS state: {OsSnapshot.OsVersionCaption}, Apps={OsSnapshot.AppsThemeModeText}, Accent={OsSnapshot.OsAccentHex}";
		}

		/// <summary>Applies a representative Dark (<c>VS2022Dark</c>) or Light (<c>VS2022Light</c>) theme matching Windows OS <c>AppsUseLightTheme</c>.</summary>
		public void SyncThemeToOsNow()
		{
			var targetId = OsSnapshot.AppsThemeMode == OsLightDarkState.ExplicitDark
				? "VS2022Dark"
				: "VS2022Light";
			var descriptor = _themeCatalog.FindById(targetId);
			if (descriptor != null)
			{
				SelectedTheme = descriptor;
				StatusText = $"Synced AvalonDock theme to OS Apps Mode ({OsSnapshot.AppsThemeModeText}) -> {descriptor.DisplayName}";
			}
		}

		/// <summary>Saves the current docking layout to XML.</summary>
		public void SaveLayout()
		{
			if (_layoutPersistence.Save())
			{
				NotifyOfPropertyChange(nameof(CanLoadLayout));
				StatusText = $"Saved layout to {_layoutPersistence.LayoutFilePath}";
			}
		}

		/// <summary>Loads the previously saved XML docking layout.</summary>
		public void LoadLayout()
		{
			if (_layoutPersistence.TryLoad(ResolveLayoutContent))
				StatusText = "Restored saved layout";
		}

		/// <summary>Resets the docking layout to the startup factory snapshot.</summary>
		public void ResetLayout()
		{
			if (_layoutPersistence.TryReset(ResolveLayoutContent))
			{
				foreach (var tool in Tools)
					tool.IsOpen = tool.IsOpenByDefault;

				StatusText = "Reset layout to startup defaults";
			}
		}

		/// <summary>Closes the main window.</summary>
		/// <returns>A task representing the asynchronous close operation.</returns>
		public Task Exit() => TryCloseAsync();

		/// <inheritdoc/>
		protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
		{
			await base.OnInitializedAsync(cancellationToken);

			Adopt(_controlGalleryDocument);
			await ActivateItemAsync(_overviewDocument, cancellationToken);

			_layoutPersistence.CaptureDefaultLayout();

			if (_themeCatalog.ActiveTheme != null)
			{
				AttachSelectedThemeObserver(_themeCatalog.ActiveTheme);
				RaiseSelectedThemeComputedProperties();
				StatusText = $"Ready - Active Theme: {_themeCatalog.ActiveTheme.DisplayName} [{_themeCatalog.ActiveTheme.BaseClassBadgeText}]";
			}
		}

		/// <inheritdoc/>
		protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
			{
				_themeCatalog.ActiveThemeChanged -= OnActiveThemeChanged;
				_osThemeService.OsThemeChanged -= OnOsThemeChanged;
				if (_selectedTheme != null)
					_selectedTheme.PropertyChanged -= OnSelectedThemePropertyChanged;
			}

			await base.OnDeactivateAsync(close, cancellationToken);
		}

		private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
		{
			var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
			brush.Freeze();
			return brush;
		}

		private (SolidColorBrush Row1, SolidColorBrush Row2, SolidColorBrush Border, SolidColorBrush Foreground) ResolveShellPalette()
		{
			if (!_styleOuterShellFromTheme || _selectedTheme == null)
				return (NeutralRow1Brush, NeutralRow2Brush, NeutralBorderBrush, NeutralForegroundBrush);

			switch (_selectedTheme.Polarity)
			{
				case ThemePolarity.Dark:
					return (DarkRow1Brush, DarkRow2Brush, DarkBorderBrush, DarkForegroundBrush);
				case ThemePolarity.Blue:
					return (BlueRow1Brush, BlueRow2Brush, BlueBorderBrush, BlueForegroundBrush);
				default:
					return (NeutralRow1Brush, NeutralRow2Brush, NeutralBorderBrush, NeutralForegroundBrush);
			}
		}

		private void AttachSelectedThemeObserver(ThemeDescriptor? next)
		{
			if (ReferenceEquals(_selectedTheme, next))
				return;

			if (_selectedTheme != null)
				_selectedTheme.PropertyChanged -= OnSelectedThemePropertyChanged;

			_selectedTheme = next;

			if (_selectedTheme != null)
				_selectedTheme.PropertyChanged += OnSelectedThemePropertyChanged;

			NotifyOfPropertyChange(nameof(SelectedTheme));
		}

		private void OnSelectedThemePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			NotifyOfPropertyChange(nameof(SelectedThemeTimingText));
		}

		private void OnActiveThemeChanged(object? sender, EventArgs e)
		{
			if (_themeCatalog.ActiveTheme != null)
			{
				AttachSelectedThemeObserver(_themeCatalog.ActiveTheme);
				RaiseSelectedThemeComputedProperties();
			}
		}

		private void OnOsThemeChanged(object? sender, EventArgs e)
		{
			NotifyOfPropertyChange(nameof(OsSnapshot));
			NotifyOfPropertyChange(nameof(HasThemeVsOsMismatch));
			NotifyOfPropertyChange(nameof(ThemeVsOsNoticeText));

			if (_autoSyncThemeToOsMode)
				SyncThemeToOsNow();
			else
				StatusText = $"OS theme updated live -> Apps: {OsSnapshot.AppsThemeModeText}, System: {OsSnapshot.SystemThemeModeText}, Accent: {OsSnapshot.OsAccentHex}";
		}

		private void RaiseSelectedThemeComputedProperties()
		{
			NotifyOfPropertyChange(nameof(SelectedThemeTimingText));
			NotifyOfPropertyChange(nameof(HasThemeVsOsMismatch));
			NotifyOfPropertyChange(nameof(ThemeVsOsNoticeText));
			if (_styleOuterShellFromTheme)
				RaiseOuterShellBrushProperties();
		}

		private void RaiseOuterShellBrushProperties()
		{
			NotifyOfPropertyChange(nameof(ShellRow1BackgroundBrush));
			NotifyOfPropertyChange(nameof(ShellRow2BackgroundBrush));
			NotifyOfPropertyChange(nameof(ShellBorderBrush));
			NotifyOfPropertyChange(nameof(ShellForegroundBrush));
		}

		private object? ResolveLayoutContent(string contentId)
		{
			return Tools.FirstOrDefault(t => string.Equals(t.Id, contentId, StringComparison.Ordinal))
				?? (object?)Items.FirstOrDefault(d => string.Equals(d.Id, contentId, StringComparison.Ordinal));
		}
	}
}