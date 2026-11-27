namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Categorizes the kind of key used by an entry in a WPF <c>ResourceDictionary</c>.
	/// </summary>
	public enum ResourceEntryCategory
	{
		/// <summary>A <see cref="System.Windows.ComponentResourceKey"/> token (e.g., <c>ResourceKeys.Background</c>).</summary>
		ComponentResourceKey,

		/// <summary>A <see cref="System.Type"/> key representing an implicit <c>Style</c>.</summary>
		ImplicitTypeStyle,

		/// <summary>A <see cref="System.Windows.DataTemplateKey"/> representing an implicit <c>DataTemplate</c>.</summary>
		DataTemplateKey,

		/// <summary>A string, <see cref="System.Windows.ResourceKey"/>, or other custom key object.</summary>
		StringOrOther,
	}
}