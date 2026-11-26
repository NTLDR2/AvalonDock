using System;
using System.Threading;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Messages;

namespace ExperimentalTestApp.ViewModels.Tools
{
	/// <summary>
	/// Lists the open documents. It never references the shell: it listens to <see cref="DocumentsChangedMessage"/> and
	/// asks for activation with <see cref="ActivateDocumentMessage"/>.
	/// </summary>
	/// <remarks>Subscribes while active only (shown), unsubscribes when hidden - the usual Caliburn.Micro pattern.</remarks>
	public sealed class OpenDocumentsViewModel : ToolboxScreen, IHandle<DocumentsChangedMessage>
	{
		private readonly IEventAggregator _events;
		private DocumentScreen? _selectedDocument;
		private bool _isRefreshing;

		/// <summary>
		/// Initializes a new instance of the <see cref="OpenDocumentsViewModel"/> class.
		/// </summary>
		/// <param name="events">The event aggregator.</param>
		public OpenDocumentsViewModel(IEventAggregator events)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			Id = "tool.openDocuments";
			DisplayName = "Open Documents";
			ToolTipText = "Documents conducted by the shell";
			Zone = DockZone.LeftBottom;
			Shortcut = "Ctrl+Shift+D";
			IsOpenByDefault = true;
		}

		/// <summary>Gets the open documents (bound by convention to the <c>ListBox</c> named <c>Documents</c>).</summary>
		public BindableCollection<DocumentScreen> Documents { get; } = new BindableCollection<DocumentScreen>();

		/// <summary>Gets or sets the selected document (bound by convention: <c>Documents</c> -&gt; <c>SelectedDocument</c>).</summary>
		public DocumentScreen? SelectedDocument
		{
			get => _selectedDocument;
			set => Set(ref _selectedDocument, value);
		}

		/// <summary>Activates the selected document (list <c>SelectionChanged</c>).</summary>
		/// <returns>A task that completes when the message is published.</returns>
		public Task ActivateSelected()
		{
			return _isRefreshing || SelectedDocument == null
				? Task.CompletedTask
				: _events.PublishOnUIThreadAsync(new ActivateDocumentMessage(SelectedDocument));
		}

		/// <inheritdoc/>
		public Task HandleAsync(DocumentsChangedMessage message, CancellationToken cancellationToken)
		{
			_isRefreshing = true;
			try
			{
				Documents.Clear();
				Documents.AddRange(message.Documents);
				SelectedDocument = message.ActiveDocument;
			}
			finally
			{
				_isRefreshing = false;
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
		{
			await base.OnActivatedAsync(cancellationToken);
			_events.SubscribeOnUIThread(this);
		}

		/// <inheritdoc/>
		protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			_events.Unsubscribe(this);
			await base.OnDeactivateAsync(close, cancellationToken);
		}
	}
}
