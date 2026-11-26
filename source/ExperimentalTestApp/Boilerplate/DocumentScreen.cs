// BOILERPLATE - exists only because no Caliburn.Micro <-> AvalonDock base class library ships today.
// Sample-internal; not a supported API. An optional future BCL (ROADMAP.md, Phase 3) may replace this folder.

namespace ExperimentalTestApp.Boilerplate
{
	/// <summary>
	/// Base class for screens hosted as AvalonDock documents (the tabbed document area).
	/// </summary>
	/// <remarks>
	/// Documents are conducted by <see cref="DockLayoutConductor{T}"/>, which mirrors them into the
	/// <c>IDocumentDock</c> of the <c>IDockLayoutService</c> layout.
	/// </remarks>
	public abstract class DocumentScreen : DockableScreen
	{
		private string? _toolTipText;

		/// <summary>
		/// Gets or sets the tooltip of the document tab. Bound to <c>LayoutItem.ToolTip</c>.
		/// </summary>
		public string? ToolTipText
		{
			get => _toolTipText;
			set => Set(ref _toolTipText, value);
		}
	}
}
