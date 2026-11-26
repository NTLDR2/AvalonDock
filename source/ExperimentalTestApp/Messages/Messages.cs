using System;
using System.Collections.Generic;
using System.Linq;
using ExperimentalTestApp.Boilerplate;

namespace ExperimentalTestApp.Messages
{
	/// <summary>
	/// Published (for example by the Explorer) to ask the shell to open or activate a workspace file.
	/// </summary>
	public sealed class OpenDocumentMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDocumentMessage"/> class.
		/// </summary>
		/// <param name="fileKey">The key of the workspace file.</param>
		public OpenDocumentMessage(string fileKey)
		{
			FileKey = fileKey ?? throw new ArgumentNullException(nameof(fileKey));
		}

		/// <summary>Gets the key of the workspace file.</summary>
		public string FileKey { get; }

		/// <inheritdoc/>
		public override string ToString() => $"OpenDocument \"{FileKey}\"";
	}

	/// <summary>
	/// Published (for example by "Open Documents") to ask the shell to activate an open document.
	/// </summary>
	public sealed class ActivateDocumentMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ActivateDocumentMessage"/> class.
		/// </summary>
		/// <param name="document">The document to activate.</param>
		public ActivateDocumentMessage(DocumentScreen document)
		{
			Document = document ?? throw new ArgumentNullException(nameof(document));
		}

		/// <summary>Gets the document to activate.</summary>
		public DocumentScreen Document { get; }

		/// <inheritdoc/>
		public override string ToString() => $"ActivateDocument \"{Document.Title}\"";
	}

	/// <summary>
	/// Published by the shell whenever the set of open documents or the active document changes.
	/// </summary>
	public sealed class DocumentsChangedMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentsChangedMessage"/> class.
		/// </summary>
		/// <param name="documents">The open documents.</param>
		/// <param name="activeDocument">The active document, if any.</param>
		public DocumentsChangedMessage(IEnumerable<DocumentScreen> documents, DocumentScreen? activeDocument)
		{
			Documents = (documents ?? Enumerable.Empty<DocumentScreen>()).ToList().AsReadOnly();
			ActiveDocument = activeDocument;
		}

		/// <summary>Gets the open documents.</summary>
		public IReadOnlyList<DocumentScreen> Documents { get; }

		/// <summary>Gets the active document, if any.</summary>
		public DocumentScreen? ActiveDocument { get; }

		/// <inheritdoc/>
		public override string ToString() => $"DocumentsChanged ({Documents.Count} open, active: \"{ActiveDocument?.Title ?? "none"}\")";
	}
}
