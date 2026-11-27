using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AvalonDock.Core;
using Microsoft.Xaml.Behaviors;

namespace ThemeSuperTestApp.Docking
{
	/// <summary>
	/// Toggles <see cref="IToolbox.IsOpen"/> when the key gesture in <see cref="IToolbox.Shortcut"/> is pressed in the host window.
	/// </summary>
	public sealed class ToolboxShortcutBehavior : Behavior<Window>
	{
		/// <summary>Identifies the <see cref="Toolboxes"/> dependency property.</summary>
		public static readonly DependencyProperty ToolboxesProperty = DependencyProperty.Register(
			nameof(Toolboxes),
			typeof(IEnumerable),
			typeof(ToolboxShortcutBehavior),
			new PropertyMetadata(null));

		private readonly Dictionary<string, KeyGesture?> _gestures = new Dictionary<string, KeyGesture?>(StringComparer.OrdinalIgnoreCase);

		/// <summary>Gets or sets the toolboxes whose shortcuts are handled.</summary>
		public IEnumerable? Toolboxes
		{
			get => (IEnumerable?)GetValue(ToolboxesProperty);
			set => SetValue(ToolboxesProperty, value);
		}

		/// <inheritdoc/>
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
		}

		/// <inheritdoc/>
		protected override void OnDetaching()
		{
			AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
			base.OnDetaching();
		}

		private static KeyGesture? Parse(string shortcut)
		{
			try
			{
				return new KeyGestureConverter().ConvertFromInvariantString(shortcut) as KeyGesture;
			}
			catch (NotSupportedException)
			{
				return null;
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
		{
			var toolboxes = Toolboxes;
			if (toolboxes == null)
				return;

			var toolbox = toolboxes.OfType<IToolbox>().FirstOrDefault(t => GetGesture(t.Shortcut)?.Matches(AssociatedObject, e) == true);
			if (toolbox == null)
				return;

			toolbox.IsOpen = !toolbox.IsOpen;
			e.Handled = true;
		}

		private KeyGesture? GetGesture(string? shortcut)
		{
			if (shortcut == null || shortcut.Trim().Length == 0)
				return null;

			if (!_gestures.TryGetValue(shortcut, out var gesture))
			{
				gesture = Parse(shortcut);
				_gestures[shortcut] = gesture;
			}

			return gesture;
		}
	}
}