// BOILERPLATE - exists only because no Caliburn.Micro <-> AvalonDock base class library ships today.
// Sample-internal; not a supported API. An optional future BCL (ROADMAP.md, Phase 3) may replace this folder.
using AvalonDock.Core;

namespace ExperimentalTestApp.Boilerplate
{
	/// <summary>
	/// A Caliburn.Micro screen that is an AvalonDock <see cref="IToolbox"/> (a tool window / anchorable).
	/// </summary>
	/// <remarks>
	/// <para>The classic <c>DockingManager</c> consumes only part of <see cref="IToolbox"/> natively (the
	/// <c>DockLayoutService</c> groups toolboxes by side from <see cref="Zone"/>). The sample honours the rest:</para>
	/// <list type="bullet">
	/// <item><see cref="Zone"/> - <c>Docking/ZonePlacementStrategy</c> places the tool in the pane named after its zone.</item>
	/// <item><see cref="IsOpen"/> - <c>Docking/ToolboxVisibilityBehavior</c> keeps it in sync with the anchorable's visibility.</item>
	/// <item><see cref="IsOpenByDefault"/> - the shell applies it at start-up and on "Reset Layout".</item>
	/// <item><see cref="Shortcut"/> - <c>Docking/ToolboxShortcutBehavior</c> toggles <see cref="IsOpen"/>.</item>
	/// <item><see cref="ToolTipText"/> - bound to the anchorable's tooltip.</item>
	/// </list>
	/// <para>Tool windows hide rather than close, so <see cref="DockableScreen.CanClose"/> defaults to <see langword="false"/>.</para>
	/// </remarks>
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
		/// <remarks>Not used by this sample (an icon would have to be a WPF <c>ImageSource</c> to bind to <c>LayoutItem.IconSource</c>).</remarks>
		public object? Icon
		{
			get => _icon;
			set => Set(ref _icon, value);
		}
	}
}
