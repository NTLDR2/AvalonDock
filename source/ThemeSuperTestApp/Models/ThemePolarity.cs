namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Categorizes the visual luminance/color family of an AvalonDock theme for OS mode comparison.
	/// </summary>
	public enum ThemePolarity
	{
		/// <summary>Light background palette.</summary>
		Light,

		/// <summary>Dark background palette.</summary>
		Dark,

		/// <summary>Visual Studio Blue chrome palette with light content surfaces.</summary>
		Blue,

		/// <summary>Classic/system-inspired palette (<c>Aero</c>, <c>VS2010</c>, <c>Metro</c>, <c>Generic</c>).</summary>
		ClassicSystem,
	}
}