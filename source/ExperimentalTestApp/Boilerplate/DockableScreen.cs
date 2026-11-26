// BOILERPLATE - exists only because no Caliburn.Micro <-> AvalonDock base class library ships today.
// Sample-internal; not a supported API. An optional future BCL (ROADMAP.md, Phase 3) may replace this folder.
// The names deliberately differ from the BCL names sketched in ROADMAP.md so the two are never confused.
using AvalonDock.Core;
using Caliburn.Micro;

namespace ExperimentalTestApp.Boilerplate
{
	/// <summary>
	/// A Caliburn.Micro <see cref="Screen"/> that is also an AvalonDock <see cref="IDockable"/>.
	/// </summary>
	/// <remarks>
	/// <para><b>Title vs DisplayName.</b> Caliburn.Micro binds <see cref="Screen.DisplayName"/>; AvalonDock binds
	/// <see cref="IDockable.Title"/>. Both share one backing field and both raise change notification.</para>
	/// <para><b>Two different "IsActive" flags.</b> <see cref="Screen.IsActive"/> is Caliburn.Micro's lifecycle state
	/// (private setter, driven by conductors). <see cref="IDockable.IsActive"/> means "has AvalonDock focus"; it is
	/// implemented explicitly and surfaces as <see cref="IsDockActive"/>. Never bind <c>LayoutItem.IsActive</c> to
	/// <c>Model.IsActive</c> - that path resolves to the Caliburn.Micro property.</para>
	/// <para><b>Who calls the IDockable hooks?</b> AvalonDock's <c>DockingManager</c>/<c>LayoutSyncBridge</c> never calls
	/// <see cref="IDockable.OnClose"/> or <see cref="IDockable.OnSelected"/> and never sets <see cref="IDockable.IsActive"/>.
	/// <see cref="DockLayoutConductor{T}"/> maintains the focus flag and <see cref="IDockable.OnSelected"/>; closing goes
	/// through Caliburn.Micro's <see cref="Screen.CanCloseAsync"/> pipeline instead of <see cref="IDockable.OnClose"/>.</para>
	/// </remarks>
	public abstract class DockableScreen : Screen, IDockable
	{
		private string _id = string.Empty;
		private string _title = string.Empty;
		private object? _context;
		private IDockable? _owner;
		private IFactory? _factory;
		private bool _canClose = true;
		private bool _canPin = true;
		private bool _canFloat = true;
		private bool _canDrag = true;
		private bool _canDrop = true;
		private bool _isModified;
		private bool _isDockActive;
		private DockState _dockState = DockState.Docked;

		/// <summary>
		/// Gets or sets the stable identifier. Bound to <c>LayoutItem.ContentId</c> and used to match layout XML on restore.
		/// </summary>
		public string Id
		{
			get => _id;
			set => Set(ref _id, value ?? string.Empty);
		}

		/// <summary>
		/// Gets or sets the display name. Shares its value with <see cref="Title"/>.
		/// </summary>
		public override string DisplayName
		{
			get => _title;
			set
			{
				if (Set(ref _title, value ?? string.Empty))
					NotifyOfPropertyChange(nameof(Title));
			}
		}

		/// <summary>
		/// Gets or sets the AvalonDock title (an alias of <see cref="DisplayName"/>).
		/// </summary>
		public string Title
		{
			get => DisplayName;
			set => DisplayName = value;
		}

		/// <inheritdoc/>
		public object? Context
		{
			get => _context;
			set => Set(ref _context, value);
		}

		/// <inheritdoc/>
		public IDockable? Owner
		{
			get => _owner;
			set => Set(ref _owner, value);
		}

		/// <inheritdoc/>
		public IFactory? Factory
		{
			get => _factory;
			set => Set(ref _factory, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether AvalonDock may offer a close button. Bound to <c>LayoutItem.CanClose</c>.
		/// </summary>
		/// <remarks>This is independent of <see cref="Screen.CanCloseAsync"/>, which can still veto a close.</remarks>
		public bool CanClose
		{
			get => _canClose;
			set => Set(ref _canClose, value);
		}

		/// <inheritdoc/>
		public bool CanPin
		{
			get => _canPin;
			set => Set(ref _canPin, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the item may float. Bound to <c>LayoutItem.CanFloat</c>.
		/// </summary>
		public bool CanFloat
		{
			get => _canFloat;
			set => Set(ref _canFloat, value);
		}

		/// <inheritdoc/>
		public bool CanDrag
		{
			get => _canDrag;
			set => Set(ref _canDrag, value);
		}

		/// <inheritdoc/>
		public bool CanDrop
		{
			get => _canDrop;
			set => Set(ref _canDrop, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the item has unsaved changes (shown as a dot in the document tab).
		/// </summary>
		public bool IsModified
		{
			get => _isModified;
			set => Set(ref _isModified, value);
		}

		/// <summary>
		/// Gets a value indicating whether this item currently has AvalonDock focus (<see cref="IDockable.IsActive"/>).
		/// </summary>
		public bool IsDockActive
		{
			get => _isDockActive;
			private set => Set(ref _isDockActive, value);
		}

		/// <inheritdoc/>
		public DockState DockState
		{
			get => _dockState;
			set => Set(ref _dockState, value);
		}

		/// <inheritdoc/>
		bool IDockable.IsActive
		{
			get => IsDockActive;
			set => IsDockActive = value;
		}

		/// <inheritdoc/>
		/// <remarks>Not called by <c>DockingManager</c>; use <see cref="Screen.CanCloseAsync"/> to guard a close.</remarks>
		bool IDockable.OnClose() => CanClose;

		/// <inheritdoc/>
		void IDockable.OnSelected() => OnDockSelected();

		/// <inheritdoc/>
		/// <remarks>Caliburn.Micro logs screens with <c>ToString()</c> ("Activating ..."); show the kind and title.</remarks>
		public override string ToString() => $"{GetType().Name} \"{Title}\"";

		/// <summary>
		/// Called by <see cref="DockLayoutConductor{T}"/> when this item receives AvalonDock focus.
		/// </summary>
		protected virtual void OnDockSelected()
		{
			// Intentionally empty: derived screens may react to gaining AvalonDock focus.
		}
	}
}
