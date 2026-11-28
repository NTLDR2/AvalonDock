using System;

namespace AvalonDock.Themes.Type1Legacy
{
	/// <inheritdoc/>
	public class MetroTheme : Theme
	{
		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return new Uri(
				"/AvalonDock.Themes.Type1Legacy;component/Themes/Metro/Theme.xaml",
				UriKind.Relative);
		}
	}
}