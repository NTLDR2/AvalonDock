using System;

namespace AvalonDock.Themes.Type1Legacy
{
	/// <inheritdoc/>
	public class VS2010Theme : Theme
	{
		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return new Uri(
				"/AvalonDock.Themes.Type1Legacy;component/Themes/VS2010/Theme.xaml",
				UriKind.Relative);
		}
	}
}