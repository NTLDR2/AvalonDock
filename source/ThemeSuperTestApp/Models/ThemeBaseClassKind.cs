namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Identifies which AvalonDock base-class architecture a theme uses.
	/// </summary>
	public enum ThemeBaseClassKind
	{
		/// <summary>
		/// Subclasses <see cref="AvalonDock.Themes.Theme"/> directly and returns a compiled BAML pack URI from <c>GetResourceUri()</c>.
		/// </summary>
		UriTheme,

		/// <summary>
		/// Subclasses <see cref="AvalonDock.Themes.DictionaryTheme"/> directly (e.g., <c>Vs2013*Theme</c>) with an in-memory <c>ThemeResourceDictionary</c>.
		/// </summary>
		DictionaryTheme,

		/// <summary>
		/// Subclasses or instantiates <see cref="AvalonDock.Themes.Type3New.VsTheme"/> (<c>: DictionaryTheme</c>) in <c>AvalonDock.Themes.Type3New</c>.
		/// </summary>
		VsTheme,
	}
}