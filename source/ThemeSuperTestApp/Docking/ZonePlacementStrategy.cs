using System;
using System.Linq;
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.Layout;

namespace ThemeSuperTestApp.Docking
{
	/// <summary>
	/// Places each <see cref="IToolbox"/> into the anchorable pane named after its <see cref="IToolbox.Zone"/>.
	/// </summary>
	public sealed class ZonePlacementStrategy : ILayoutUpdateStrategy
	{
		/// <summary>Prefix of zone anchorable pane names declared in <c>ShellView.xaml</c>.</summary>
		public const string PaneNamePrefix = "Zone_";

		/// <summary>
		/// Gets the anchorable pane name for <paramref name="zone"/>.
		/// </summary>
		/// <param name="zone">The toolbox zone.</param>
		/// <returns>The pane name, e.g., <c>Zone_LeftTop</c>.</returns>
		public static string GetPaneName(DockZone zone) => PaneNamePrefix + zone;

		/// <inheritdoc/>
		public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
		{
			if (layout == null || !(anchorableToShow?.Content is IToolbox toolbox))
				return false;

			if (destinationContainer?.FindParent<LayoutFloatingWindow>() != null)
				return false;

			var panes = layout.Descendents()
				.OfType<LayoutAnchorablePane>()
				.Where(p => !p.IsHostedInFloatingWindow)
				.ToList();

			var zoneName = GetPaneName(toolbox.Zone);
			var sidePrefix = PaneNamePrefix + toolbox.Zone.ToSide();
			var target = panes.Find(p => p.Name == zoneName)
				?? panes.Find(p => p.Name != null && p.Name.StartsWith(sidePrefix, StringComparison.Ordinal));
			if (target == null)
				return false;

			target.Children.Add(anchorableToShow);
			return true;
		}

		/// <inheritdoc/>
		public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown)
		{
		}

		/// <inheritdoc/>
		public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer) => false;

		/// <inheritdoc/>
		public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
		{
		}
	}
}