using System;
using System.Globalization;
using System.Windows.Media;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Immutable point-in-time snapshot of the host Windows operating system's theme, high-contrast, and DWM accent settings.
	/// </summary>
	public sealed class WindowsOsThemeSnapshot
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowsOsThemeSnapshot"/> class.
		/// </summary>
		/// <param name="osVersionCaption">Human-readable OS and real build number caption.</param>
		/// <param name="majorVersion">Real OS major version from <c>RtlGetVersion</c>.</param>
		/// <param name="buildNumber">Real OS build number from <c>RtlGetVersion</c>.</param>
		/// <param name="appsThemeMode">State of <c>Personalize\AppsUseLightTheme</c>.</param>
		/// <param name="systemThemeMode">State of <c>Personalize\SystemUsesLightTheme</c>.</param>
		/// <param name="isTitleBarAccentColorEnabled">Whether <c>Personalize\ColorPrevalence</c> is enabled.</param>
		/// <param name="isHighContrastEnabled">Whether Windows High Contrast mode is active.</param>
		/// <param name="highContrastSchemeName">Active High Contrast scheme name when enabled.</param>
		/// <param name="osAccentColor">Decoded Windows DWM accent color.</param>
		/// <param name="capturedAt">Timestamp when the snapshot was read.</param>
		public WindowsOsThemeSnapshot(
			string osVersionCaption,
			int majorVersion,
			int buildNumber,
			OsLightDarkState appsThemeMode,
			OsLightDarkState systemThemeMode,
			bool isTitleBarAccentColorEnabled,
			bool isHighContrastEnabled,
			string highContrastSchemeName,
			Color osAccentColor,
			DateTimeOffset capturedAt)
		{
			OsVersionCaption = osVersionCaption ?? throw new ArgumentNullException(nameof(osVersionCaption));
			MajorVersion = majorVersion;
			BuildNumber = buildNumber;
			AppsThemeMode = appsThemeMode;
			SystemThemeMode = systemThemeMode;
			IsTitleBarAccentColorEnabled = isTitleBarAccentColorEnabled;
			IsHighContrastEnabled = isHighContrastEnabled;
			HighContrastSchemeName = highContrastSchemeName ?? string.Empty;
			OsAccentColor = osAccentColor;
			CapturedAt = capturedAt;

			OsAccentHex = string.Format(
				CultureInfo.InvariantCulture,
				"#{0:X2}{1:X2}{2:X2}",
				osAccentColor.R,
				osAccentColor.G,
				osAccentColor.B);

			var brush = new SolidColorBrush(Color.FromRgb(osAccentColor.R, osAccentColor.G, osAccentColor.B));
			brush.Freeze();
			OsAccentBrush = brush;

			SupportsDwmImmersiveDarkMode = majorVersion > 10 || (majorVersion == 10 && buildNumber >= 17763);
		}

		/// <summary>Gets the human-readable Windows version and build caption (e.g., <c>"Windows 11 (Build 26100)"</c>).</summary>
		public string OsVersionCaption { get; }

		/// <summary>Gets the real Windows major version number.</summary>
		public int MajorVersion { get; }

		/// <summary>Gets the real Windows build number.</summary>
		public int BuildNumber { get; }

		/// <summary>Gets the explicit state of <c>AppsUseLightTheme</c> ("Choose your default app mode").</summary>
		public OsLightDarkState AppsThemeMode { get; }

		/// <summary>Gets the explicit state of <c>SystemUsesLightTheme</c> ("Choose your default Windows mode").</summary>
		public OsLightDarkState SystemThemeMode { get; }

		/// <summary>Gets a value indicating whether Windows is configured to show accent color on title bars and window borders.</summary>
		public bool IsTitleBarAccentColorEnabled { get; }

		/// <summary>Gets a value indicating whether Windows High Contrast accessibility mode is enabled.</summary>
		public bool IsHighContrastEnabled { get; }

		/// <summary>Gets the active High Contrast scheme name when enabled.</summary>
		public string HighContrastSchemeName { get; }

		/// <summary>Gets the decoded Windows OS accent color.</summary>
		public Color OsAccentColor { get; }

		/// <summary>Gets the <c>#RRGGBB</c> hexadecimal representation of <see cref="OsAccentColor"/>.</summary>
		public string OsAccentHex { get; }

		/// <summary>Gets a frozen <see cref="SolidColorBrush"/> for <see cref="OsAccentColor"/>.</summary>
		public SolidColorBrush OsAccentBrush { get; }

		/// <summary>Gets a value indicating whether the OS supports <c>DWMWA_USE_IMMERSIVE_DARK_MODE</c> (Windows 10 1809+).</summary>
		public bool SupportsDwmImmersiveDarkMode { get; }

		/// <summary>Gets when this snapshot was captured.</summary>
		public DateTimeOffset CapturedAt { get; }

		/// <summary>Gets the concise label for <see cref="AppsThemeMode"/>.</summary>
		public string AppsThemeModeText => FormatMode(AppsThemeMode);

		/// <summary>Gets the concise label for <see cref="SystemThemeMode"/>.</summary>
		public string SystemThemeModeText => FormatMode(SystemThemeMode);

		/// <summary>Gets the concise High Contrast status text.</summary>
		public string HighContrastDisplayText => IsHighContrastEnabled
			? (string.IsNullOrWhiteSpace(HighContrastSchemeName) ? "ON" : $"ON ({HighContrastSchemeName})")
			: "Off";

		private static string FormatMode(OsLightDarkState state)
		{
			switch (state)
			{
				case OsLightDarkState.ExplicitDark:
					return "Explicit Dark";
				case OsLightDarkState.ExplicitLight:
					return "Explicit Light";
				default:
					return "Unset / Legacy OS";
			}
		}
	}
}