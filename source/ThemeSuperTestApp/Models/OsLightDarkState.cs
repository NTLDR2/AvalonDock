namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Represents whether Windows 10/11 Personalize registry keys explicitly configure Light or Dark mode.
	/// </summary>
	public enum OsLightDarkState
	{
		/// <summary>Explicitly configured to Light mode (<c>DWORD = 1</c>).</summary>
		ExplicitLight,

		/// <summary>Explicitly configured to Dark mode (<c>DWORD = 0</c>).</summary>
		ExplicitDark,

		/// <summary>Registry value is not present (unconfigured profile or pre-Windows 10 1809/1903 OS).</summary>
		NotConfiguredOrLegacyOs,
	}
}