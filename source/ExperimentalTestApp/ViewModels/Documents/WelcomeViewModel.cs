using System;
using System.Threading.Tasks;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Messages;

namespace ExperimentalTestApp.ViewModels.Documents
{
	/// <summary>
	/// The start page. It cannot be closed from its tab (<see cref="DockableScreen.CanClose"/> is <see langword="false"/>).
	/// </summary>
	public sealed class WelcomeViewModel : DocumentScreen
	{
		/// <summary>The stable document id (persisted in the layout XML).</summary>
		public const string DocumentId = "welcome";

		private readonly IEventAggregator _events;

		/// <summary>
		/// Initializes a new instance of the <see cref="WelcomeViewModel"/> class.
		/// </summary>
		/// <param name="events">The event aggregator used by the quick links.</param>
		public WelcomeViewModel(IEventAggregator events)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			Id = DocumentId;
			DisplayName = "Welcome";
			ToolTipText = "Start page";
			CanClose = false;
		}

		/// <summary>Opens <c>readme.txt</c> (bound by convention to the button named <c>OpenReadme</c>).</summary>
		/// <returns>A task that completes when the message is published.</returns>
		public Task OpenReadme() => _events.PublishOnUIThreadAsync(new OpenDocumentMessage("readme.txt"));

		/// <summary>Opens <c>caliburn-micro.txt</c>.</summary>
		/// <returns>A task that completes when the message is published.</returns>
		public Task OpenCaliburnNotes() => _events.PublishOnUIThreadAsync(new OpenDocumentMessage("caliburn-micro.txt"));

		/// <summary>Opens <c>avalondock.txt</c>.</summary>
		/// <returns>A task that completes when the message is published.</returns>
		public Task OpenAvalonDockNotes() => _events.PublishOnUIThreadAsync(new OpenDocumentMessage("avalondock.txt"));
	}
}
