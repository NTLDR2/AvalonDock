using System.Windows;
using System.Windows.Media;

namespace ThemeSuperTestApp.Infrastructure
{
	/// <summary>
	/// Builds frozen vector <see cref="DrawingImage"/> icons for specimen documents and toolboxes so tab icon rendering
	/// can be inspected cleanly across all 20 AvalonDock themes.
	/// </summary>
	internal static class SpecimenIconFactory
	{
		internal static ImageSource CreateBadgeIcon(Color fillColor, Color accentColor)
		{
			var fillBrush = new SolidColorBrush(fillColor);
			fillBrush.Freeze();

			var accentBrush = new SolidColorBrush(accentColor);
			accentBrush.Freeze();

			var borderPen = new Pen(accentBrush, 1.25);
			borderPen.Freeze();

			var outerRect = new RectangleGeometry(new Rect(1, 1, 14, 14), 3, 3);
			outerRect.Freeze();

			var innerDot = new EllipseGeometry(new Point(8, 8), 3.2, 3.2);
			innerDot.Freeze();

			var group = new DrawingGroup();
			group.Children.Add(new GeometryDrawing(fillBrush, borderPen, outerRect));
			group.Children.Add(new GeometryDrawing(accentBrush, null, innerDot));
			group.Freeze();

			var image = new DrawingImage(group);
			image.Freeze();
			return image;
		}
	}
}