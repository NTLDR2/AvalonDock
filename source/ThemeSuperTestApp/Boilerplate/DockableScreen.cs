using System.Windows.Media;
using AvalonDock.Core;
using Caliburn.Micro;

namespace ThemeSuperTestApp.Boilerplate
{
	/// <summary>
	/// A Caliburn.Micro <see cref="Screen"/> that is also an AvalonDock <see cref="IDockable"/>.
	/// </summary>
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
		private ImageSource? _iconSource;
		private DockState _dockState = DockState.Docked;

		/// <summary>
		/// Gets or sets the stable identifier bound to <c>LayoutItem.ContentId</c>.
		/// </summary>
		public string Id
		{
			get => _id;
			set => Set(ref _id, value ?? string.Empty);
		}

		/// <summary>
		/// Gets or sets the display name (shared with <see cref="Title"/>).
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
		/// Gets or sets the AvalonDock title (alias of <see cref="DisplayName"/>).
		/// </summary>
		public string Title
		{
			get => DisplayName;
			set => DisplayName = value;
		}

		/// <summary>
		/// Gets or sets the optional WPF icon bound to <c>LayoutItem.IconSource</c> and custom tab headers.
		/// </summary>
		public ImageSource? IconSource
		{
			get => _iconSource;
			set => Set(ref _iconSource, value);
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

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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
		/// Gets or sets a value indicating whether the item has unsaved changes.
		/// </summary>
		public bool IsModified
		{
			get => _isModified;
			set => Set(ref _isModified, value);
		}

		/// <summary>
		/// Gets a value indicating whether this item currently has AvalonDock focus.
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
		bool IDockable.OnClose() => CanClose;

		/// <inheritdoc/>
		void IDockable.OnSelected() => OnDockSelected();

		/// <inheritdoc/>
		public override string ToString() => $"{GetType().Name} \"{Title}\"";

		/// <summary>
		/// Invoked when this item receives AvalonDock focus.
		/// </summary>
		protected virtual void OnDockSelected()
		{
		}
	}
}