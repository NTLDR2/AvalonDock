using System;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace ExperimentalTestApp.ViewModels.Dialogs
{
	/// <summary>The answer given in the <see cref="SaveChangesViewModel"/> dialog.</summary>
	public enum SaveChangesChoice
	{
		/// <summary>Keep the document open.</summary>
		Cancel,

		/// <summary>Save, then close.</summary>
		Save,

		/// <summary>Close without saving.</summary>
		Discard,
	}

	/// <summary>
	/// Modal "Save changes?" prompt shown by <c>TextDocumentViewModel.CanCloseAsync</c> through <see cref="IWindowManager.ShowDialogAsync"/>.
	/// </summary>
	public sealed class SaveChangesViewModel : Screen
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SaveChangesViewModel"/> class.
		/// </summary>
		/// <param name="documentTitle">The title of the modified document.</param>
		public SaveChangesViewModel(string documentTitle)
		{
			DisplayName = "Unsaved changes";
			Message = $"Do you want to save the changes to \"{documentTitle ?? throw new ArgumentNullException(nameof(documentTitle))}\"?";
		}

		/// <summary>Gets the question shown to the user.</summary>
		public string Message { get; }

		/// <summary>Gets the user's answer. <see cref="SaveChangesChoice.Cancel"/> if the window was closed.</summary>
		public SaveChangesChoice Choice { get; private set; } = SaveChangesChoice.Cancel;

		/// <summary>Answers "Save" (button <c>Save</c>).</summary>
		/// <returns>A task that completes when the dialog is closed.</returns>
		public Task Save() => Close(SaveChangesChoice.Save);

		/// <summary>Answers "Don't save" (button <c>Discard</c>).</summary>
		/// <returns>A task that completes when the dialog is closed.</returns>
		public Task Discard() => Close(SaveChangesChoice.Discard);

		/// <summary>Answers "Cancel" (button <c>Cancel</c>, also Esc).</summary>
		/// <returns>A task that completes when the dialog is closed.</returns>
		public Task Cancel() => Close(SaveChangesChoice.Cancel);

		private Task Close(SaveChangesChoice choice)
		{
			Choice = choice;

			// Passing a dialog result is valid here because the view is shown with ShowDialogAsync (modal).
			return TryCloseAsync(choice != SaveChangesChoice.Cancel);
		}
	}
}
