using System;

namespace AvalonDock.Themes.Type1Legacy
{
	/// <inheritdoc/>
	public class ExpressionLightTheme : Theme
	{
		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return new Uri(
				"/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/LightTheme.xaml",
				UriKind.Relative);
		}
	}
}