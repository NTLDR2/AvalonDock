using System.Windows;
using System.Windows.Controls;
using ThemeSuperTestApp.Boilerplate;

namespace ThemeSuperTestApp.Docking
{
	/// <summary>
	/// Selects the <c>LayoutItem</c> container style by ViewModel type (<see cref="ToolboxScreen"/> or <see cref="DocumentScreen"/>).
	/// </summary>
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