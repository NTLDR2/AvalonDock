using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Detects and live-monitors Windows 10/11 Dark Mode, Light Mode, High Contrast, and DWM Accent Color on <c>.NET Framework 4.8</c>.
	/// </summary>
	public sealed class WindowsOsThemeService : IDisposable
	{
		private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
		private const string DwmKeyPath = @"Software\Microsoft\Windows\DWM";
		private const string HighContrastKeyPath = @"Control Panel\Accessibility\HighContrast";

		private Window? _attachedWindow;
		private HwndSource? _hwndSource;
		private Dispatcher? _dispatcher;
		private bool _refreshPending;
		private bool _isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsOsThemeService"/> class and captures the initial OS theme snapshot.
		/// </summary>
		public WindowsOsThemeService()
		{
			CurrentSnapshot = CaptureSnapshot();
			SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
		}

		/// <summary>
		/// Raised on the UI thread whenever Windows OS theme, high-contrast, or accent color settings change.
		/// </summary>
		public event EventHandler? OsThemeChanged;

		/// <summary>
		/// Gets the latest captured Windows OS theme snapshot.
		/// </summary>
		public WindowsOsThemeSnapshot CurrentSnapshot { get; private set; }

		/// <summary>
		/// Captures an immutable snapshot of the current Windows OS theme, high-contrast, and accent color configuration.
		/// </summary>
		/// <returns>A populated <see cref="WindowsOsThemeSnapshot"/>.</returns>
		public static WindowsOsThemeSnapshot CaptureSnapshot()
		{
			ReadRealOsVersion(out var major, out var minor, out var build);
			var osCaption = FormatOsCaption(major, minor, build);

			var appsMode = ReadPersonalizeLightDarkValue("AppsUseLightTheme");
			var systemMode = ReadPersonalizeLightDarkValue("SystemUsesLightTheme");
			// Windows 10/11 stores "Show accent color on title bars and window borders" at DWM\ColorPrevalence.
			var colorPrevalence = ReadRegistryDword(DwmKeyPath, "ColorPrevalence") == 1
				|| ReadRegistryDword(PersonalizeKeyPath, "ColorPrevalence") == 1;

			var highContrast = SystemParameters.HighContrast;
			var hcScheme = highContrast
				? (ReadRegistryString(HighContrastKeyPath, "High Contrast Scheme") ?? string.Empty)
				: string.Empty;

			var accentColor = ReadOsAccentColor();

			return new WindowsOsThemeSnapshot(
				osCaption,
				major,
				build,
				appsMode,
				systemMode,
				colorPrevalence,
				highContrast,
				hcScheme,
				accentColor,
				DateTimeOffset.Now);
		}

		/// <summary>
		/// Attaches a Win32 <see cref="HwndSourceHook"/> to <paramref name="window"/> so live OS Dark/Light/Accent broadcasts update <see cref="CurrentSnapshot"/>.
		/// </summary>
		/// <param name="window">The main shell window.</param>
		public void AttachWindow(Window window)
		{
			if (window == null) throw new ArgumentNullException(nameof(window));
			if (_isDisposed)
				return;

			DetachWindow();

			_attachedWindow = window;
			_dispatcher = window.Dispatcher;
			window.Closed += OnAttachedWindowClosed;

			var handle = new WindowInteropHelper(window).Handle;
			if (handle != IntPtr.Zero)
				HookHwnd(handle);
			else
				window.SourceInitialized += OnWindowSourceInitialized;
		}

		/// <summary>
		/// Forces an immediate re-read of the Windows OS theme state and notifies subscribers.
		/// </summary>
		public void Refresh()
		{
			CurrentSnapshot = CaptureSnapshot();
			OsThemeChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
			DetachWindow();
		}

		private static void ReadRealOsVersion(out int major, out int minor, out int build)
		{
			var info = new NativeMethods.OsVersionInfoEx
			{
				OsVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OsVersionInfoEx)),
			};

			try
			{
				if (NativeMethods.RtlGetVersion(ref info) == 0 && info.MajorVersion > 0)
				{
					major = info.MajorVersion;
					minor = info.MinorVersion;
					build = info.BuildNumber;
					return;
				}
			}
			catch (DllNotFoundException)
			{
			}
			catch (EntryPointNotFoundException)
			{
			}

			var fallback = Environment.OSVersion.Version;
			major = fallback.Major;
			minor = fallback.Minor;
			build = fallback.Build;
		}

		private static string FormatOsCaption(int major, int minor, int build)
		{
			if (major == 10 && build >= 22000)
				return $"Windows 11 (Build {build})";
			if (major == 10)
				return $"Windows 10 (Build {build})";
			if (major == 6 && minor == 3)
				return $"Windows 8.1 (Build {build})";
			if (major == 6 && minor == 2)
				return $"Windows 8 (Build {build})";
			if (major == 6 && minor == 1)
				return $"Windows 7 (Build {build})";

			return $"Windows {major}.{minor} (Build {build})";
		}

		private static OsLightDarkState ReadPersonalizeLightDarkValue(string valueName)
		{
			var raw = ReadRegistryDword(PersonalizeKeyPath, valueName);
			if (!raw.HasValue)
				return OsLightDarkState.NotConfiguredOrLegacyOs;

			return raw.Value == 0
				? OsLightDarkState.ExplicitDark
				: OsLightDarkState.ExplicitLight;
		}

		private static Color ReadOsAccentColor()
		{
			// Windows 10/11 stores DWM\AccentColor in ABGR byte order (0xAABBGGRR).
			var abgr = ReadRegistryDword(DwmKeyPath, "AccentColor");
			if (abgr.HasValue)
			{
				var raw = unchecked((uint)abgr.Value);
				var r = (byte)(raw & 0xFF);
				var g = (byte)((raw >> 8) & 0xFF);
				var b = (byte)((raw >> 16) & 0xFF);
				return Color.FromRgb(r, g, b);
			}

			// Fallback: DWM\ColorizationColor is stored in ARGB byte order (0xAARRGGBB).
			var argb = ReadRegistryDword(DwmKeyPath, "ColorizationColor");
			if (argb.HasValue)
			{
				var raw = unchecked((uint)argb.Value);
				var r = (byte)((raw >> 16) & 0xFF);
				var g = (byte)((raw >> 8) & 0xFF);
				var b = (byte)(raw & 0xFF);
				return Color.FromRgb(r, g, b);
			}

			try
			{
				return SystemParameters.WindowGlassColor;
			}
			catch (InvalidOperationException)
			{
				return Color.FromRgb(0x00, 0x78, 0xD4);
			}
		}

		private static int? ReadRegistryDword(string subKeyPath, string valueName)
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: false);
				var value = key?.GetValue(valueName);
				if (value is int intValue)
					return intValue;
			}
			catch (SecurityException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (ObjectDisposedException)
			{
			}

			return null;
		}

		private static string? ReadRegistryString(string subKeyPath, string valueName)
		{
			try
			{
				using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: false);
				return key?.GetValue(valueName) as string;
			}
			catch (SecurityException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (ObjectDisposedException)
			{
			}

			return null;
		}

		private void OnWindowSourceInitialized(object? sender, EventArgs e)
		{
			if (_attachedWindow == null)
				return;

			_attachedWindow.SourceInitialized -= OnWindowSourceInitialized;
			var handle = new WindowInteropHelper(_attachedWindow).Handle;
			if (handle != IntPtr.Zero)
				HookHwnd(handle);
		}

		private void HookHwnd(IntPtr handle)
		{
			_hwndSource = HwndSource.FromHwnd(handle);
			_hwndSource?.AddHook(WndProc);
		}

		private void OnAttachedWindowClosed(object? sender, EventArgs e)
		{
			DetachWindow();
		}

		private void DetachWindow()
		{
			if (_hwndSource != null)
			{
				_hwndSource.RemoveHook(WndProc);
				_hwndSource = null;
			}

			if (_attachedWindow != null)
			{
				_attachedWindow.SourceInitialized -= OnWindowSourceInitialized;
				_attachedWindow.Closed -= OnAttachedWindowClosed;
				_attachedWindow = null;
			}
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == NativeMethods.WmSettingChange ||
				msg == NativeMethods.WmDwmColorizationColorChanged ||
				msg == NativeMethods.WmSysColorChange ||
				msg == NativeMethods.WmThemeChanged)
			{
				ScheduleRefresh();
			}

			return IntPtr.Zero;
		}

		private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			ScheduleRefresh();
		}

		private void ScheduleRefresh()
		{
			if (_isDisposed)
				return;

			var app = Application.Current;
			var dispatcher = _dispatcher ?? ((app != null && app.CheckAccess()) ? app.Dispatcher : null);
			if (dispatcher == null)
			{
				Refresh();
				return;
			}

			if (_refreshPending)
				return;

			_refreshPending = true;
			dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
			{
				_refreshPending = false;
				if (!_isDisposed)
					Refresh();
			}));
		}
	}
}