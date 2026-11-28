using System;
using System.IO;
using System.Reflection;

namespace AvalonDock.Themes.Type3New
{
	/// <summary>
	/// VS2022 Dark theme loaded from an embedded .vstheme resource file.
	/// Uses VS2022-specific control templates with rounded corners and modern tab design.
	/// </summary>
	public class VS2022DarkTheme : VsTheme
	{
		private static readonly Uri VS2022GenericXamlUri =
			new Uri("/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml", UriKind.Relative);

		/// <summary>
		/// Initializes a new instance of the <see cref="VS2022DarkTheme"/> class.
		/// </summary>
		public VS2022DarkTheme()
			: base(LoadEmbeddedResource("AvalonDock.Themes.Type3New.Resources.vs2022dark.vstheme"), VS2022GenericXamlUri)
		{
		}

		private static Stream LoadEmbeddedResource(string name)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
		}
	}
}