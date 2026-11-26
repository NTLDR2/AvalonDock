using System;
using System.Linq;
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.Layout;

namespace ExperimentalTestApp.Docking
{
	/// <summary>
	/// <see cref="ILayoutUpdateStrategy"/> that places each <see cref="IToolbox"/> into the anchorable pane named after its
	/// <see cref="IToolbox.Zone"/> (<c>Zone_LeftTop</c> ... <c>Zone_BottomRight</c>, declared in <c>ShellView.xaml</c>).
	/// </summary>
	/// <remarks>
	/// <para>Assigning a strategy in XAML also stops <see cref="DockingManager"/> from installing its default
	/// <c>DockAlignmentStrategy</c> for <c>DockLayout</c>. That strategy only knows the side (left/right/bottom) and falls back
	/// to auto-hide groups when no docked pane is visible on that side yet, which is the case for an empty start-up layout.</para>
	/// <para>Fallbacks: another <c>Zone_*</c> pane on the same side, then AvalonDock's default placement.</para>
	/// </remarks>
	public sealed class ZonePlacementStrategy : ILayoutUpdateStrategy
	{
		/// <summary>The prefix of the anchorable pane names the strategy looks for.</summary>
		public const string PaneNamePrefix = "Zone_";

		/// <summary>
		/// Gets the name of the anchorable pane that hosts toolboxes of <paramref name="zone"/>.
		/// </summary>
		/// <param name="zone">The toolbox zone.</param>
		/// <returns>The pane name, for example <c>Zone_LeftTop</c>.</returns>
		public static string GetPaneName(DockZone zone) => PaneNamePrefix + zone;

		/// <inheritdoc/>
		public bool BeforeInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableToShow, ILayoutContainer destinationContainer)
		{
			if (layout == null || !(anchorableToShow?.Content is IToolbox toolbox))
				return false;

			// AvalonDock is inserting into a floating window: let it.
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
			// Nothing to adjust after insertion.
		}

		/// <inheritdoc/>
		public bool BeforeInsertDocument(LayoutRoot layout, LayoutDocument anchorableToShow, ILayoutContainer destinationContainer) => false;

		/// <inheritdoc/>
		public void AfterInsertDocument(LayoutRoot layout, LayoutDocument anchorableShown)
		{
			// Documents use AvalonDock's default placement (the last focused document pane).
		}
	}
}
