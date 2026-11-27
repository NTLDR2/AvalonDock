using System;
using System.Diagnostics;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Single WPF XAML binding, resource-dictionary, or markup diagnostic event captured from <c>PresentationTraceSources</c>.
	/// </summary>
	public sealed class WpfTraceEventRecord
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WpfTraceEventRecord"/> class.
		/// </summary>
		/// <param name="sequenceNumber">Monotonic event sequence number.</param>
		/// <param name="timestamp">Local timestamp when captured.</param>
		/// <param name="category">Trace source category (<c>DataBinding</c>, <c>ResourceDictionary</c>, <c>Markup</c>).</param>
		/// <param name="severity">Trace event severity (<c>Warning</c>, <c>Error</c>, <c>Critical</c>).</param>
		/// <param name="eventId">WPF trace event ID (e.g., <c>40</c> for binding path error).</param>
		/// <param name="themeName">Active AvalonDock theme name when the event occurred.</param>
		/// <param name="baseClassBadge">Active AvalonDock theme base-class badge.</param>
		/// <param name="message">Full WPF diagnostic message.</param>
		public WpfTraceEventRecord(
			int sequenceNumber,
			DateTime timestamp,
			string category,
			TraceEventType severity,
			int eventId,
			string themeName,
			string baseClassBadge,
			string message)
		{
			SequenceNumber = sequenceNumber;
			Timestamp = timestamp;
			Category = category ?? throw new ArgumentNullException(nameof(category));
			Severity = severity;
			EventId = eventId;
			ThemeName = themeName ?? string.Empty;
			BaseClassBadge = baseClassBadge ?? string.Empty;
			Message = message ?? string.Empty;
		}

		/// <summary>Gets the monotonic sequence number.</summary>
		public int SequenceNumber { get; }

		/// <summary>Gets when the event was recorded.</summary>
		public DateTime Timestamp { get; }

		/// <summary>Gets the formatted time string (<c>HH:mm:ss.fff</c>).</summary>
		public string TimestampText => Timestamp.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>Gets the WPF trace category.</summary>
		public string Category { get; }

		/// <summary>Gets the trace severity.</summary>
		public TraceEventType Severity { get; }

		/// <summary>Gets the WPF trace event ID.</summary>
		public int EventId { get; }

		/// <summary>Gets the active AvalonDock theme display name at capture time.</summary>
		public string ThemeName { get; }

		/// <summary>Gets the active AvalonDock theme base-class badge at capture time.</summary>
		public string BaseClassBadge { get; }

		/// <summary>Gets the full diagnostic message.</summary>
		public string Message { get; }
	}
}