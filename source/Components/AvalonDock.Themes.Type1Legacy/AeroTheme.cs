using System;

namespace AvalonDock.Themes.Type1Legacy
{
	/// <inheritdoc/>
	public class AeroTheme : Theme
	{
		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return new Uri(
				"/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Theme.xaml",
				UriKind.Relative);
		}
	}
}