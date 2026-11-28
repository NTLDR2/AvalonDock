using System;

namespace AvalonDock.Themes.Type3New
{
	/// <summary>
	/// VS2026 Light theme. Built from the VS2022 Light base palette with a VS2026 JSON
	/// override that applies the Fluent accent and the new independent header/tab colors.
	/// </summary>
	public class VS2026LightTheme : VsTheme
	{
		private static readonly Uri VS2026GenericXamlUri =
			new Uri("/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml", UriKind.Relative);

		/// <summary>
		/// Initializes a new instance of the <see cref="VS2026LightTheme"/> class.
		/// </summary>
		public VS2026LightTheme()
			: base(Vs2026ThemeFactory.Build(
				"AvalonDock.Themes.Type3New.Resources.vs2022light.vstheme",
				"AvalonDock.Themes.Type3New.Resources.vs2026light.json",
				VS2026GenericXamlUri))
		{
		}
	}
}