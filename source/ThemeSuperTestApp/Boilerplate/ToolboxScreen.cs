using AvalonDock.Core;

namespace ThemeSuperTestApp.Boilerplate
{
	/// <summary>
	/// A Caliburn.Micro screen that is also an AvalonDock <see cref="IToolbox"/> (tool window / anchorable).
	/// </summary>
	public abstract class ToolboxScreen : DockableScreen, IToolbox
	{
		private string? _toolTipText;
		private DockZone _zone = DockZone.LeftTop;
		private string? _shortcut;
		private bool _isOpenByDefault;
		private bool _isOpen;
		private object? _icon;

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolboxScreen"/> class.
		/// </summary>
		protected ToolboxScreen()
		{
			CanClose = false;
		}

		/// <inheritdoc/>
		public string? ToolTipText
		{
			get => _toolTipText;
			set => Set(ref _toolTipText, value);
		}

		/// <inheritdoc/>
		public DockZone Zone
		{
			get => _zone;
			set => Set(ref _zone, value);
		}

		/// <inheritdoc/>
		public string? Shortcut
		{
			get => _shortcut;
			set => Set(ref _shortcut, value);
		}

		/// <inheritdoc/>
		public bool IsOpenByDefault
		{
			get => _isOpenByDefault;
			set => Set(ref _isOpenByDefault, value);
		}

		/// <inheritdoc/>
		public bool IsOpen
		{
			get => _isOpen;
			set => Set(ref _isOpen, value);
		}

		/// <inheritdoc/>
		public object? Icon
		{
			get => _icon;
			set => Set(ref _icon, value);
		}
	}
}