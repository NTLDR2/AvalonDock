using System.Windows;
using System.Windows.Controls;
using ExperimentalTestApp.Boilerplate;

namespace ExperimentalTestApp.Docking
{
	/// <summary>
	/// Picks the <c>LayoutItem</c> container style by ViewModel kind (<see cref="ToolboxScreen"/> or <see cref="DocumentScreen"/>).
	/// </summary>
	/// <remarks>
	/// The styles bind <c>Title</c> and <c>ContentId</c> live. Nothing in AvalonDock copies <c>IDockable.Title</c>/<c>Id</c>
	/// onto documents, so without these bindings tabs would be untitled and layouts could not be restored.
	/// </remarks>
	public sealed class PanesStyleSelector : StyleSelector
	{
		/// <summary>Gets or sets the style for tool windows (<c>LayoutAnchorableItem</c>).</summary>
		public Style? ToolboxStyle { get; set; }

		/// <summary>Gets or sets the style for documents (<c>LayoutDocumentItem</c>).</summary>
		public Style? DocumentStyle { get; set; }

		/// <inheritdoc/>
		public override Style SelectStyle(object item, DependencyObject container)
		{
			switch (item)
			{
				case ToolboxScreen _ when ToolboxStyle != null:
					return ToolboxStyle;
				case DocumentScreen _ when DocumentStyle != null:
					return DocumentStyle;
				default:
					return base.SelectStyle(item, container);
			}
		}
	}
}
