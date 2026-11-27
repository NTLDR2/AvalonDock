using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.Layout;
using Microsoft.Xaml.Behaviors;

namespace ThemeSuperTestApp.Docking
{
	/// <summary>
	/// Synchronizes <see cref="IToolbox.IsOpen"/> with the visibility of the matching <see cref="LayoutAnchorable"/> on classic <see cref="DockingManager"/>.
	/// </summary>
	public sealed class ToolboxVisibilityBehavior : Behavior<DockingManager>
	{
		private readonly Dictionary<LayoutAnchorable, IToolbox> _tracked = new Dictionary<LayoutAnchorable, IToolbox>();
		private readonly HashSet<IToolbox> _observedToolboxes = new HashSet<IToolbox>();
		private LayoutRoot? _layout;
		private bool _isSyncing;
		private bool _isScanPending;

		/// <inheritdoc/>
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.LayoutChanging += OnLayoutChanging;
			AssociatedObject.LayoutChanged += OnLayoutChanged;
			HookLayout(AssociatedObject.Layout);
			ScheduleScan();
		}

		/// <inheritdoc/>
		protected override void OnDetaching()
		{
			AssociatedObject.LayoutChanging -= OnLayoutChanging;
			AssociatedObject.LayoutChanged -= OnLayoutChanged;
			UnhookLayout();
			UntrackAll();

			foreach (var toolbox in _observedToolboxes.OfType<INotifyPropertyChanged>())
				toolbox.PropertyChanged -= OnToolboxPropertyChanged;
			_observedToolboxes.Clear();

			base.OnDetaching();
		}

		private void OnLayoutChanging(object? sender, EventArgs e)
		{
			UnhookLayout();
			UntrackAll();
		}

		private void OnLayoutChanged(object? sender, EventArgs e)
		{
			HookLayout(AssociatedObject?.Layout);
			Scan(layoutWins: true);
		}

		private void HookLayout(LayoutRoot? layout)
		{
			_layout = layout;
			if (_layout != null)
				_layout.ElementAdded += OnElementAdded;
		}

		private void UnhookLayout()
		{
			if (_layout != null)
				_layout.ElementAdded -= OnElementAdded;
			_layout = null;
		}

		private void OnElementAdded(object? sender, LayoutElementEventArgs e)
		{
			if (e.Element is LayoutAnchorable)
				ScheduleScan();
		}

		private void ScheduleScan()
		{
			if (_isScanPending)
				return;

			_isScanPending = true;
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
			{
				_isScanPending = false;
				Scan(layoutWins: false);
			}));
		}

		private void Scan(bool layoutWins)
		{
			var layout = AssociatedObject?.Layout;
			if (layout == null)
				return;

			var untracked = layout.Descendents()
				.OfType<LayoutAnchorable>()
				.Where(a => a.Content is IToolbox && !_tracked.ContainsKey(a))
				.ToList();

			foreach (var anchorable in untracked)
			{
				var toolbox = (IToolbox)anchorable.Content;
				_tracked.Add(anchorable, toolbox);
				anchorable.IsVisibleChanged += OnAnchorableIsVisibleChanged;
				if (_observedToolboxes.Add(toolbox) && toolbox is INotifyPropertyChanged notifier)
					notifier.PropertyChanged += OnToolboxPropertyChanged;

				if (layoutWins)
					SyncToolbox(toolbox, anchorable.IsVisible);
				else
					SyncAnchorable(anchorable, toolbox.IsOpen);
			}
		}

		private void UntrackAll()
		{
			foreach (var anchorable in _tracked.Keys)
				anchorable.IsVisibleChanged -= OnAnchorableIsVisibleChanged;
			_tracked.Clear();
		}

		private void OnAnchorableIsVisibleChanged(object? sender, EventArgs e)
		{
			if (_isSyncing || !(sender is LayoutAnchorable anchorable) || !_tracked.TryGetValue(anchorable, out var toolbox))
				return;

			SyncToolbox(toolbox, anchorable.IsVisible);
		}

		private void OnToolboxPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (_isSyncing || e.PropertyName != nameof(IToolbox.IsOpen) || !(sender is IToolbox toolbox))
				return;

			foreach (var anchorable in _tracked.Where(p => ReferenceEquals(p.Value, toolbox)).Select(p => p.Key).ToList())
				SyncAnchorable(anchorable, toolbox.IsOpen);
		}

		private void SyncToolbox(IToolbox toolbox, bool isOpen)
		{
			if (toolbox.IsOpen == isOpen)
				return;

			_isSyncing = true;
			try
			{
				toolbox.IsOpen = isOpen;
			}
			finally
			{
				_isSyncing = false;
			}
		}

		private void SyncAnchorable(LayoutAnchorable anchorable, bool isOpen)
		{
			_isSyncing = true;
			try
			{
				if (isOpen && !anchorable.IsVisible && anchorable.IsHidden)
					anchorable.Show();
				else if (!isOpen && anchorable.IsVisible)
					anchorable.Hide();
			}
			finally
			{
				_isSyncing = false;
			}
		}
	}
}