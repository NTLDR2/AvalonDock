using System;
using System.Linq;
using System.Threading.Tasks;
using AvalonDock;
using AvalonDock.Core;
using Caliburn.Micro;
using Microsoft.Xaml.Behaviors;

namespace ThemeSuperTestApp.Docking
{
	/// <summary>
	/// Routes AvalonDock document close gestures through Caliburn.Micro's asynchronous <see cref="Screen.CanCloseAsync"/> pipeline.
	/// </summary>
	public sealed class DocumentCloseBehavior : Behavior<DockingManager>
	{
		private Task _pending = Task.CompletedTask;

		/// <inheritdoc/>
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.DocumentClosing += OnDocumentClosing;
		}

		/// <inheritdoc/>
		protected override void OnDetaching()
		{
			AssociatedObject.DocumentClosing -= OnDocumentClosing;
			base.OnDetaching();
		}

		private static async Task CloseAfterAsync(Task previous, IConductor conductor, object document)
		{
			await previous;

			if (!conductor.GetChildren().Cast<object>().Contains(document))
				return;

			try
			{
				await conductor.CloseItemAsync(document);
			}
			catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
			{
				LogManager.GetLog(typeof(DocumentCloseBehavior)).Error(ex);
			}
		}

		private void OnDocumentClosing(object? sender, DocumentClosingEventArgs e)
		{
			if (!(e.Document?.Content is IChild child) || !(child.Parent is IConductor conductor))
				return;

			e.Cancel = true;
			if (child is IDockable { CanClose: false })
				return;

			_pending = CloseAfterAsync(_pending, conductor, child);
		}
	}
}