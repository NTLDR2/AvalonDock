using System;
using System.Linq;
using System.Threading.Tasks;
using AvalonDock;
using AvalonDock.Core;
using Caliburn.Micro;
using Microsoft.Xaml.Behaviors;

namespace ExperimentalTestApp.Docking
{
	/// <summary>
	/// Routes every AvalonDock document close (tab X, middle click, "Close", "Close All", "Close All But This")
	/// through Caliburn.Micro's close pipeline so <see cref="Screen.CanCloseAsync"/> can veto asynchronously.
	/// </summary>
	/// <remarks>
	/// <para>AvalonDock raises <see cref="DockingManager.DocumentClosing"/> synchronously and expects an immediate answer,
	/// while Caliburn.Micro's guard is asynchronous (it may show a dialog). The behavior therefore always cancels the
	/// AvalonDock close and asks the document's conductor (<see cref="IConductor"/>) to close it instead. If the
	/// conductor removes the document from its items, the layout service removes the tab - silently, without raising
	/// <see cref="DockingManager.DocumentClosing"/> again.</para>
	/// <para>Requests are queued so that "Close All" over several modified documents asks one question at a time.</para>
	/// <para>Only Caliburn.Micro's public API is used (<see cref="IChild.Parent"/>, <see cref="IConductor"/>,
	/// <c>ScreenExtensions.CloseItemAsync</c>), so the behavior works with any conductor.</para>
	/// </remarks>
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

			// The same document may have been queued twice (e.g. X clicked, then "Close All").
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
				return; // not a conducted Caliburn.Micro screen: let AvalonDock close it as usual

			e.Cancel = true;
			if (child is IDockable { CanClose: false })
				return; // the ViewModel does not allow closing from the UI

			_pending = CloseAfterAsync(_pending, conductor, child);
		}
	}
}
