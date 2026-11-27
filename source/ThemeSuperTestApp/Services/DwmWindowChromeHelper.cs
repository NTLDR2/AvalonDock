using System;
using System.Windows;
using System.Windows.Interop;
using AvalonDock.Controls;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Opt-in helper that applies <c>DWMWA_USE_IMMERSIVE_DARK_MODE</c> to the main shell window and standalone
	/// <see cref="DetachedAnchorableWindow"/> instances on Windows 10 (1809+) and Windows 11.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="DwmTitleBarMode.DefaultUntouched"/> and performs zero Win32 DWM calls until the user
	/// explicitly selects a non-default mode, preserving raw <c>.NET Framework 4.8</c> chrome behavior by default.
	/// </remarks>
	public sealed class DwmWindowChromeHelper : IDisposable
	{
		private static bool _classHandlerRegistered;
		private static DwmWindowChromeHelper? _activeInstance;

		private readonly WindowsOsThemeService _osThemeService;
		private readonly ThemeCatalogService _themeCatalog;
		private Window? _mainWindow;
		private DwmTitleBarMode _mode = DwmTitleBarMode.DefaultUntouched;
		private bool _hasEverModifiedTitleBars;
		private bool _isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="DwmWindowChromeHelper"/> class.
		/// </summary>
		/// <param name="osThemeService">The Windows OS theme detector service.</param>
		/// <param name="themeCatalog">The AvalonDock theme catalog service.</param>
		public DwmWindowChromeHelper(WindowsOsThemeService osThemeService, ThemeCatalogService themeCatalog)
		{
			_osThemeService = osThemeService ?? throw new ArgumentNullException(nameof(osThemeService));
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));

			_osThemeService.OsThemeChanged += OnEnvironmentOrThemeChanged;
			_themeCatalog.ActiveThemeChanged += OnEnvironmentOrThemeChanged;

			EnsureDetachedWindowClassHandler();
			_activeInstance = this;
		}

		/// <summary>
		/// Gets or sets the active DWM title-bar mode.
		/// </summary>
		public DwmTitleBarMode Mode
		{
			get => _mode;
			set
			{
				if (_mode == value)
					return;

				_mode = value;
				ApplyCurrentModeToAllWindows();
			}
		}

		/// <summary>
		/// Attaches the main shell window so its HWND receives opt-in DWM title-bar updates.
		/// </summary>
		/// <param name="mainWindow">The main shell window.</param>
		public void AttachMainWindow(Window mainWindow)
		{
			_mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
			_mainWindow.SourceInitialized += OnMainWindowSourceInitialized;
			ApplyCurrentModeToAllWindows();
		}

		/// <summary>
		/// Re-evaluates and applies <see cref="Mode"/> across the main window and all open <see cref="DetachedAnchorableWindow"/>s.
		/// </summary>
		public void ApplyCurrentModeToAllWindows()
		{
			if (_isDisposed)
				return;

			var snapshot = _osThemeService.CurrentSnapshot;
			if (!snapshot.SupportsDwmImmersiveDarkMode)
				return;

			// Preserve 100% untouched net48 Win32 state until the user opts into a DWM override at least once.
			if (_mode == DwmTitleBarMode.DefaultUntouched && !_hasEverModifiedTitleBars)
				return;

			var useDarkTitleBar = EvaluateShouldUseDarkTitleBar(snapshot);
			if (_mode != DwmTitleBarMode.DefaultUntouched)
				_hasEverModifiedTitleBars = true;

			if (_mainWindow != null)
				ApplyToWindow(_mainWindow, snapshot.BuildNumber, useDarkTitleBar);

			var app = Application.Current;
			if (app == null || !app.CheckAccess())
				return;

			foreach (Window window in app.Windows)
			{
				if (window is DetachedAnchorableWindow detachedWindow && detachedWindow.CheckAccess())
					ApplyToWindow(detachedWindow, snapshot.BuildNumber, useDarkTitleBar);
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			_osThemeService.OsThemeChanged -= OnEnvironmentOrThemeChanged;
			_themeCatalog.ActiveThemeChanged -= OnEnvironmentOrThemeChanged;

			if (_mainWindow != null)
			{
				_mainWindow.SourceInitialized -= OnMainWindowSourceInitialized;
				_mainWindow = null;
			}

			if (ReferenceEquals(_activeInstance, this))
				_activeInstance = null;
		}

		private static void EnsureDetachedWindowClassHandler()
		{
			if (_classHandlerRegistered)
				return;

			_classHandlerRegistered = true;
			EventManager.RegisterClassHandler(
				typeof(DetachedAnchorableWindow),
				FrameworkElement.LoadedEvent,
				new RoutedEventHandler(OnDetachedAnchorableWindowLoaded));
		}

		private static void OnDetachedAnchorableWindowLoaded(object sender, RoutedEventArgs e)
		{
			_activeInstance?.ApplyCurrentModeToAllWindows();
		}

		private static void ApplyToWindow(Window window, int buildNumber, bool useDarkTitleBar)
		{
			var hwnd = new WindowInteropHelper(window).Handle;
			if (hwnd == IntPtr.Zero)
				return;

			var attribute = buildNumber >= 18985
				? NativeMethods.DwmwaUseImmersiveDarkMode
				: NativeMethods.DwmwaUseImmersiveDarkModeLegacy;
			var flag = useDarkTitleBar ? 1 : 0;

			try
			{
				_ = NativeMethods.DwmSetWindowAttribute(hwnd, attribute, ref flag, sizeof(int));
			}
			catch (DllNotFoundException)
			{
			}
			catch (EntryPointNotFoundException)
			{
			}
		}

		private bool EvaluateShouldUseDarkTitleBar(WindowsOsThemeSnapshot snapshot)
		{
			switch (_mode)
			{
				case DwmTitleBarMode.MatchWindowsOsMode:
					return snapshot.AppsThemeMode == OsLightDarkState.ExplicitDark;
				case DwmTitleBarMode.MatchActiveAvalonDockTheme:
					return _themeCatalog.ActiveTheme?.Polarity == ThemePolarity.Dark;
				default:
					return false;
			}
		}

		private void OnMainWindowSourceInitialized(object? sender, EventArgs e)
		{
			ApplyCurrentModeToAllWindows();
		}

		private void OnEnvironmentOrThemeChanged(object? sender, EventArgs e)
		{
			ApplyCurrentModeToAllWindows();
		}
	}
}