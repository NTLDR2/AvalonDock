using System;
using Caliburn.Micro;

namespace ExperimentalTestApp.Infrastructure
{
	/// <summary>Severity of a <see cref="LogEntry"/>.</summary>
	public enum LogLevel
	{
		/// <summary>Informational message.</summary>
		Info,

		/// <summary>Warning.</summary>
		Warn,

		/// <summary>Error.</summary>
		Error,
	}

	/// <summary>
	/// One line written through Caliburn.Micro's logging pipeline (<see cref="LogManager"/>).
	/// </summary>
	public sealed class LogEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LogEntry"/> class.
		/// </summary>
		/// <param name="timestamp">When the entry was written.</param>
		/// <param name="level">The severity.</param>
		/// <param name="source">The logging type name.</param>
		/// <param name="message">The formatted message.</param>
		public LogEntry(DateTimeOffset timestamp, LogLevel level, string source, string message)
		{
			Timestamp = timestamp;
			Level = level;
			Source = source;
			Message = message;
		}

		/// <summary>Gets when the entry was written.</summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>Gets the severity.</summary>
		public LogLevel Level { get; }

		/// <summary>Gets the logging type name.</summary>
		public string Source { get; }

		/// <summary>Gets the formatted message.</summary>
		public string Message { get; }
	}

	/// <summary>
	/// Process-wide, bounded store of log entries shown by the "Caliburn Log" toolbox.
	/// </summary>
	/// <remarks>
	/// A static instance is used on purpose: <see cref="LogManager.GetLog"/> is assigned before the container exists
	/// (see <see cref="Bootstrapper"/>), so the sink cannot be container-managed.
	/// </remarks>
	public sealed class LogSink
	{
		private const int Capacity = 500;

		private LogSink()
		{
		}

		/// <summary>Gets the process-wide sink.</summary>
		public static LogSink Instance { get; } = new LogSink();

		/// <summary>Gets the retained entries (newest last). Mutated on the UI thread only.</summary>
		public BindableCollection<LogEntry> Entries { get; } = new BindableCollection<LogEntry>();

		/// <summary>
		/// Gets or sets a value indicating whether Caliburn.Micro's verbose binding/action diagnostics
		/// (<c>ViewModelBinder</c> and <c>ActionMessage</c> informational messages) are retained.
		/// </summary>
		/// <remarks>
		/// Caliburn.Micro 6 logs every convention and action-resolution step at Info level; left on, they push the
		/// interesting lifecycle messages (Activating / Deactivating / Closed) out of the bounded list within seconds.
		/// Warnings and errors are always retained.
		/// </remarks>
		public bool IncludeDiagnostics { get; set; }

		/// <summary>
		/// Appends an entry. Safe to call from any thread: the mutation is posted to the UI thread.
		/// </summary>
		/// <param name="entry">The entry to append.</param>
		public void Add(LogEntry entry)
		{
			if (entry == null || (!IncludeDiagnostics && IsDiagnostic(entry)))
				return;

			Execute.BeginOnUIThread(() =>
			{
				Entries.Add(entry);
				while (Entries.Count > Capacity)
					Entries.RemoveAt(0);
			});
		}

		/// <summary>Removes all entries.</summary>
		public void Clear() => Execute.BeginOnUIThread(() => Entries.Clear());

		private static bool IsDiagnostic(LogEntry entry) =>
			entry.Level == LogLevel.Info && (entry.Source == "ViewModelBinder" || entry.Source == "ActionMessage");
	}
}
