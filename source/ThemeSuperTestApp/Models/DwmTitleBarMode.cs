namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Controls how <c>ThemeSuperTestApp</c> applies <c>DWMWA_USE_IMMERSIVE_DARK_MODE</c> to native Win32 window chrome.
	/// </summary>
	public enum DwmTitleBarMode
	{
		/// <summary>
		/// Leaves Win32/WPF <c>net48</c> title bars at their default out-of-the-box behavior without dark-mode overrides.
		/// </summary>
		DefaultUntouched,

		/// <summary>
		/// Sets immersive dark title bars on the main window and <c>DetachedAnchorableWindow</c>s when Windows OS Apps Mode is Dark.
		/// </summary>
		MatchWindowsOsMode,

		/// <summary>
		/// Sets immersive dark title bars on the main window and <c>DetachedAnchorableWindow</c>s when the active AvalonDock theme is Dark.
		/// </summary>
		MatchActiveAvalonDockTheme,
	}
}