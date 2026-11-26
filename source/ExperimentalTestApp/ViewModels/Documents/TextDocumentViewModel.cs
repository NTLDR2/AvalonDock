using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Infrastructure;
using ExperimentalTestApp.Services;
using ExperimentalTestApp.ViewModels.Dialogs;

namespace ExperimentalTestApp.ViewModels.Documents
{
	/// <summary>
	/// An editable text document backed by a <see cref="SampleFile"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="CanCloseAsync"/> asks Save / Discard / Cancel through <see cref="IWindowManager.ShowDialogAsync"/> when the
	/// document is modified. The same guard runs for the tab's close button, Ctrl+W, "Close All" and application exit.
	/// </remarks>
	public sealed class TextDocumentViewModel : DocumentScreen
	{
		private const string IdPrefix = "file:";

		private readonly SampleFile _file;
		private readonly IWindowManager _windowManager;
		private string _text;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextDocumentViewModel"/> class.
		/// </summary>
		/// <param name="file">The workspace file.</param>
		/// <param name="windowManager">The window manager used for the save prompt.</param>
		public TextDocumentViewModel(SampleFile file, IWindowManager windowManager)
		{
			_file = file ?? throw new ArgumentNullException(nameof(file));
			_windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
			_text = file.Text;
			Id = GetId(file);
			DisplayName = file.Key;
			ToolTipText = $"workspace://{file.Key}";
		}

		/// <summary>
		/// Gets or sets the edited text (bound by convention to the <c>TextBox</c> named <c>Text</c>).
		/// </summary>
		public string Text
		{
			get => _text;
			set
			{
				if (!Set(ref _text, value ?? string.Empty))
					return;

				IsModified = !string.Equals(_text, _file.Text, StringComparison.Ordinal);
				NotifyOfPropertyChange(nameof(LineCount));
				NotifyOfPropertyChange(nameof(CharacterCount));
			}
		}

		/// <summary>Gets the number of lines.</summary>
		public int LineCount => _text.Count(c => c == '\n') + 1;

		/// <summary>Gets the number of characters.</summary>
		public int CharacterCount => _text.Length;

		/// <summary>
		/// Gets the document id for a workspace file.
		/// </summary>
		/// <param name="file">The workspace file.</param>
		/// <returns>The document id.</returns>
		public static string GetId(SampleFile file) => IdPrefix + (file ?? throw new ArgumentNullException(nameof(file))).Key;

		/// <summary>
		/// Gets the workspace file key from a document id.
		/// </summary>
		/// <param name="documentId">A document id.</param>
		/// <returns>The file key, or <see langword="null"/> if the id does not belong to a text document.</returns>
		public static string? GetFileKey(string documentId) =>
			documentId != null && documentId.StartsWith(IdPrefix, StringComparison.Ordinal) ? documentId.Substring(IdPrefix.Length) : null;

		/// <summary>Writes the text back to the workspace file.</summary>
		public void Save()
		{
			_file.Text = _text;
			IsModified = false;
		}

		/// <inheritdoc/>
		public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
		{
			if (!IsModified)
				return true;

			var prompt = new SaveChangesViewModel(Title);
			await _windowManager.ShowDialogAsync(prompt, null, DialogSettings.CreateFixedDialog());

			switch (prompt.Choice)
			{
				case SaveChangesChoice.Save:
					Save();
					return true;
				case SaveChangesChoice.Discard:
					Text = _file.Text;
					return true;
				default:
					return false;
			}
		}
	}
}
