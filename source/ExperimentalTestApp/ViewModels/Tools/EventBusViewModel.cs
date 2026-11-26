using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;

namespace ExperimentalTestApp.ViewModels.Tools
{
	/// <summary>
	/// Monitors every message published on the <see cref="IEventAggregator"/> (<c>IHandle&lt;object&gt;</c> receives all of them,
	/// because Caliburn.Micro dispatches polymorphically).
	/// </summary>
	/// <remarks>Hidden by default (<see cref="ToolboxScreen.IsOpenByDefault"/> is <see langword="false"/>); toggle it with Ctrl+Shift+B.</remarks>
	public sealed class EventBusViewModel : ToolboxScreen, IHandle<object>
	{
		private const int Capacity = 200;

		private readonly IEventAggregator _events;

		/// <summary>
		/// Initializes a new instance of the <see cref="EventBusViewModel"/> class.
		/// </summary>
		/// <param name="events">The event aggregator.</param>
		public EventBusViewModel(IEventAggregator events)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			Id = "tool.eventBus";
			DisplayName = "Event Bus";
			ToolTipText = "Messages published on the IEventAggregator";
			Zone = DockZone.BottomRight;
			Shortcut = "Ctrl+Shift+B";
			IsOpenByDefault = false;
		}

		/// <summary>Gets the received messages, newest last (bound by convention to the list named <c>Messages</c>).</summary>
		public BindableCollection<string> Messages { get; } = new BindableCollection<string>();

		/// <summary>Clears the list (button <c>Clear</c>).</summary>
		public void Clear() => Messages.Clear();

		/// <inheritdoc/>
		public Task HandleAsync(object message, CancellationToken cancellationToken)
		{
			Messages.Add(string.Format(CultureInfo.InvariantCulture, "{0:HH:mm:ss.fff}  {1}", DateTimeOffset.Now, message));
			while (Messages.Count > Capacity)
				Messages.RemoveAt(0);
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
