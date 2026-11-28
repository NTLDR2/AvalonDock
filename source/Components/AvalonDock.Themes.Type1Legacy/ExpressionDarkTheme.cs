using System;

namespace AvalonDock.Themes.Type1Legacy
{
	/// <inheritdoc/>
	public class ExpressionDarkTheme : Theme
	{
		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return new Uri(
				"/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/DarkTheme.xaml",
				UriKind.Relative);
		}
	}
}