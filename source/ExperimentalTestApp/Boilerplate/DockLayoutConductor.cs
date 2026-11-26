// BOILERPLATE - exists only because no Caliburn.Micro <-> AvalonDock base class library ships today.
// Sample-internal; not a supported API. An optional future BCL (ROADMAP.md, Phase 3) may replace this folder.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;

namespace ExperimentalTestApp.Boilerplate
{
	/// <summary>
	/// A Caliburn.Micro <c>Conductor&lt;T&gt;.Collection.OneActive</c> whose documents live in an AvalonDock
	/// <see cref="IDockLayoutService"/> layout.
	/// </summary>
	/// <typeparam name="T">The document screen type.</typeparam>
	/// <remarks>
	/// <para>Synchronization rules (the conductor's <see cref="Conductor{T}.Collection.OneActive.Items"/> is the single source of truth):</para>
	/// <list type="bullet">
	/// <item><b>Open documents.</b> <c>Items</c> changes are mirrored into the layout's <see cref="IDocumentDock"/>.
	/// Nothing else mutates <see cref="IDock.VisibleDockables"/>. <c>LayoutSyncBridge</c> turns those changes into its
	/// <c>DocumentsSource</c>; removing a document there drops its tab silently (no <c>DocumentClosing</c>).</item>
	/// <item><b>Active document.</b> <see cref="ConductorBaseWithActiveItem{T}.ActiveItem"/> is pushed to
	/// <see cref="IDockLayoutService.ActiveDockable"/>; AvalonDock focus arriving through <see cref="IRootDock.ActiveDockable"/>
	/// activates the matching document. Toolboxes and <see langword="null"/> never enter <c>Items</c>. Do not bind
	/// <c>DockingManager.ActiveContent</c> in XAML: with <c>DockLayout</c> the bridge owns it.</item>
	/// <item><b>Focus flag.</b> <see cref="IDockable.IsActive"/> / <see cref="IDockable.OnSelected"/> follow
	/// <see cref="IRootDock.ActiveDockable"/> for documents and toolboxes alike.</item>
	/// <item><b>Closing.</b> Always through Caliburn.Micro (<c>CloseItemAsync</c> / <c>TryCloseAsync</c> / <see cref="CloseAllDocumentsAsync"/>)
	/// so <see cref="Screen.CanCloseAsync"/> can veto. <c>Docking/DocumentCloseBehavior</c> routes AvalonDock's tab close here.</item>
	/// </list>
	/// </remarks>
	public abstract class DockLayoutConductor<T> : Conductor<T>.Collection.OneActive
		where T : DockableScreen
	{
		private readonly IDockLayoutService _layoutService;
		private readonly IDocumentDock _documentDock;
		private readonly IList<IDockable> _documentDockables;
		private IDockable? _focused;
		private bool _isSyncing;

		/// <summary>
		/// Initializes a new instance of the <see cref="DockLayoutConductor{T}"/> class.
		/// </summary>
		/// <param name="layoutService">The AvalonDock layout service whose document dock hosts the conducted items.</param>
		protected DockLayoutConductor(IDockLayoutService layoutService)
		{
			_layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));
			_documentDock = FindDocumentDock(layoutService.Layout)
				?? throw new InvalidOperationException("The IDockLayoutService layout contains no IDocumentDock to host documents.");
			_documentDockables = _documentDock.VisibleDockables
				?? throw new InvalidOperationException("The IDocumentDock has no VisibleDockables collection.");

			Items.CollectionChanged += OnItemsCollectionChanged;
			if (layoutService.Layout is INotifyPropertyChanged rootDock)
				rootDock.PropertyChanged += OnRootDockPropertyChanged;
		}

		/// <summary>
		/// Gets the AvalonDock layout tree. Bind <c>DockingManager.DockLayout</c> to this property.
		/// </summary>
		public IRootDock DockLayout => _layoutService.Layout;

		/// <summary>
		/// Gets the AvalonDock layout service.
		/// </summary>
		protected IDockLayoutService LayoutService => _layoutService;

		/// <summary>
		/// Activates the first document matching <paramref name="predicate"/>, or creates one with <paramref name="factory"/>.
		/// </summary>
		/// <param name="predicate">Identifies an already open document.</param>
		/// <param name="factory">Creates the document when none is open.</param>
		/// <returns>The activated document.</returns>
		public async Task<T> OpenOrActivateAsync(Func<T, bool> predicate, Func<T> factory)
		{
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (factory == null) throw new ArgumentNullException(nameof(factory));

			var document = Items.FirstOrDefault(predicate) ?? factory();
			await ActivateItemAsync(document, CancellationToken.None);
			return document;
		}

		/// <summary>
		/// Closes every closable document, one at a time, honouring <see cref="Screen.CanCloseAsync"/>.
		/// </summary>
		/// <returns><see langword="false"/> if a document vetoed; documents before it stay closed.</returns>
		public async Task<bool> CloseAllDocumentsAsync()
		{
			foreach (var document in Items.Where(d => d.CanClose).ToList())
			{
				await DeactivateItemAsync(document, true, CancellationToken.None);
				if (Items.Contains(document))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Adds a document to <c>Items</c> (and therefore to the layout) without activating it.
		/// </summary>
		/// <remarks>Used while a saved layout is being restored, before AvalonDock decides which document is active.</remarks>
		/// <param name="document">The document to adopt.</param>
		public void Adopt(T document)
		{
			if (document == null) throw new ArgumentNullException(nameof(document));
			if (!Items.Contains(document))
				Items.Add(document);
		}

		/// <inheritdoc/>
		protected override void OnActivationProcessed(T item, bool success)
		{
			base.OnActivationProcessed(item, success);
			if (success)
				PushActiveItemToLayout();
		}

		/// <inheritdoc/>
		protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			await base.OnDeactivateAsync(close, cancellationToken);
			if (!close)
				return;

			Items.CollectionChanged -= OnItemsCollectionChanged;
			if (_layoutService.Layout is INotifyPropertyChanged rootDock)
				rootDock.PropertyChanged -= OnRootDockPropertyChanged;
		}

		private static IDocumentDock? FindDocumentDock(IDockable node)
		{
			if (node is IDocumentDock documentDock)
				return documentDock;

			if (node is IDock dock && dock.VisibleDockables != null)
			{
				foreach (var child in dock.VisibleDockables)
				{
					var found = FindDocumentDock(child);
					if (found != null)
						return found;
				}
			}

			return null;
		}

		private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					AddToLayout(e.NewItems);
					break;
				case NotifyCollectionChangedAction.Remove:
					RemoveFromLayout(e.OldItems);
					break;
				case NotifyCollectionChangedAction.Replace:
					RemoveFromLayout(e.OldItems);
					AddToLayout(e.NewItems);
					break;
				case NotifyCollectionChangedAction.Reset:
					RemoveFromLayout(_documentDockables.OfType<T>().Where(d => !Items.Contains(d)).ToList());
					AddToLayout(Items.ToList());
					break;
				default:
					// Move: document order inside the AvalonDock pane is owned by the user, not by Items.
					break;
			}
		}

		private void AddToLayout(IEnumerable? documents)
		{
			if (documents == null)
				return;

			foreach (var document in documents.OfType<T>().Where(d => !_documentDockables.Contains(d)))
			{
				document.Owner = _documentDock;
				_documentDockables.Add(document);
			}
		}

		private void RemoveFromLayout(IEnumerable? documents)
		{
			if (documents == null)
				return;

			_isSyncing = true;
			try
			{
				foreach (var document in documents.OfType<T>().ToList())
				{
					_layoutService.CloseDocument(document);
					document.Owner = null;
				}
			}
			finally
			{
				_isSyncing = false;
			}

			// CloseDocument may have picked a different active document than Caliburn.Micro did; Caliburn.Micro wins.
			PushActiveItemToLayout();
		}

		private void PushActiveItemToLayout()
		{
			var active = ActiveItem;
			if (active == null || _isSyncing)
				return;

			if (!ReferenceEquals(_layoutService.ActiveDockable, active))
			{
				_isSyncing = true;
				try
				{
					_layoutService.ActiveDockable = active;
				}
				finally
				{
					_isSyncing = false;
				}
			}

			UpdateFocus(active);
		}

		private void OnRootDockPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(IRootDock.ActiveDockable))
				return;

			var dockable = _layoutService.ActiveDockable;
			UpdateFocus(dockable);

			if (_isSyncing || dockable is not T document || !Items.Contains(document) || ReferenceEquals(document, ActiveItem))
				return;

			_ = ActivateFromLayoutAsync(document);
		}

		private async Task ActivateFromLayoutAsync(T document)
		{
			try
			{
				await ActivateItemAsync(document, CancellationToken.None);
			}
			catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
			{
				LogManager.GetLog(GetType()).Error(ex);
			}
		}

		private void UpdateFocus(IDockable? dockable)
		{
			if (ReferenceEquals(dockable, _focused))
				return;

			if (_focused != null)
				_focused.IsActive = false;

			_focused = dockable;
			if (dockable == null)
				return;

			dockable.IsActive = true;
			dockable.OnSelected();
		}
	}
}
