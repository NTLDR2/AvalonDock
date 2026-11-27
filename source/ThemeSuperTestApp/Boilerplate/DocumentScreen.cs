namespace ThemeSuperTestApp.Boilerplate
{
	/// <summary>
	/// Base class for screens hosted as AvalonDock documents in the tabbed document well.
	/// </summary>
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