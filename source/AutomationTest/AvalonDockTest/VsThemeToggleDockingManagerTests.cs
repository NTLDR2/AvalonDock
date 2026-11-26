namespace AvalonDockTest
{
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using NUnit.Framework;

	using AvalonDock;
	using AvalonDock.Controls;
	using AvalonDock.Themes;

	/// <summary>
	/// Regression tests for #683: <see cref="VS2010Theme"/> must provide the
	/// <see cref="ToggleDockingManager"/> specific styles and resolve control templates cleanly.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class VsThemeToggleDockingManagerTests
	{
		[Test]
		public void VS2010Theme_ProvidesToggleDockingManagerStyles()
		{
			_ = new Border();
			var dict = new ResourceDictionary { Source = new VS2010Theme().GetResourceUri() };

			Assert.That(dict["ToggleAnchorablePaneControlStyle"], Is.InstanceOf<Style>());
			Assert.That(dict[ToggleDockButton.ForegroundBrushKey], Is.InstanceOf<Brush>());
			Assert.That(dict[typeof(ToggleDockButton)], Is.InstanceOf<Style>());
			Assert.That(dict[typeof(ToggleDockingManager)], Is.InstanceOf<Style>());
		}

		[Test]
		public void VS2010Theme_ToggleAnchorablePaneTitle_TemplateResolvesAllResources()
		{
			var host = new Border();
			host.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new VS2010Theme().GetResourceUri() });
			var title = new ToggleAnchorablePaneTitle();
			host.Child = title;

			Assert.DoesNotThrow(() => title.ApplyTemplate());
			Assert.That(title.Template, Is.Not.Null);
			Assert.That(title.Template.FindName("PART_MinimizeButton", title), Is.InstanceOf<Button>());
		}
	}
}
