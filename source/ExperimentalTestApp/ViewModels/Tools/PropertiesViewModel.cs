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
	/// Shows the properties of the active document, including both "active" flags:
	/// Caliburn.Micro's <see cref="Screen.IsActive"/> and AvalonDock's focus (<see cref="DockableScreen.IsDockActive"/>).
	/// </summary>
	public sealed class PropertiesViewModel : ToolboxScreen, IHandle<DocumentsChangedMessage>
	{
		private readonly IEventAggregator _events;
		private DocumentScreen? _activeDocument;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertiesViewModel"/> class.
		/// </summary>
		/// <param name="events">The event aggregator.</param>
		public PropertiesViewModel(IEventAggregator events)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			Id = "tool.properties";
			DisplayName = "Properties";
			ToolTipText = "Properties of the active document";
			Zone = DockZone.RightTop;
			Shortcut = "Ctrl+Shift+P";
			IsOpenByDefault = true;
		}

		/// <summary>Gets the active document, if any.</summary>
		public DocumentScreen? ActiveDocument
		{
			get => _activeDocument;
			private set
			{
				if (Set(ref _activeDocument, value))
					NotifyOfPropertyChange(nameof(HasActiveDocument));
			}
		}

		/// <summary>Gets a value indicating whether a document is active.</summary>
		public bool HasActiveDocument => ActiveDocument != null;

		/// <inheritdoc/>
		public Task HandleAsync(DocumentsChangedMessage message, CancellationToken cancellationToken)
		{
			ActiveDocument = message.ActiveDocument;
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
