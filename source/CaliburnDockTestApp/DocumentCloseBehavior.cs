using AvalonDock;
using Caliburn.Micro;
using Microsoft.Xaml.Behaviors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CaliburnDockTestApp
{
	/// <summary>
	/// Routes AvalonDock document closes (tab X, middle-click, context menu Close / Close All / Close All But This,
	/// and floating document window close) through the parent Caliburn.Micro <see cref="IConductor"/> so
	/// <see cref="Screen.CanCloseAsync"/> can prompt asynchronously.
	/// </summary>
	public sealed class DocumentCloseBehavior : Behavior<DockingManager>
	{
		private Task _pending = Task.CompletedTask;

		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.DocumentClosing += OnDocumentClosing;
		}

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

		private void OnDocumentClosing(object sender, DocumentClosingEventArgs e)
		{
			if (!(e.Document?.Content is IChild child) || !(child.Parent is IConductor conductor))
				return;

			e.Cancel = true;
			_pending = CloseAfterAsync(_pending, conductor, child);
		}
	}
}