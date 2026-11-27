using System;
using System.Diagnostics;
using System.Globalization;
using Caliburn.Micro;

namespace ThemeSuperTestApp.Infrastructure
{
	/// <summary>
	/// Caliburn.Micro <see cref="ILog"/> that writes to <see cref="Trace"/> and to <see cref="LogSink"/>.
	/// </summary>
	public sealed class TraceLog : ILog
	{
		private readonly string _source;

		/// <summary>
		/// Initializes a new instance of the <see cref="TraceLog"/> class.
		/// </summary>
		/// <param name="type">The type that owns the log.</param>
		public TraceLog(Type type)
		{
			_source = type?.Name ?? "?";
		}

		/// <inheritdoc/>
		public void Info(string format, params object[] args) => Write(LogLevel.Info, Format(format, args));

		/// <inheritdoc/>
		public void Warn(string format, params object[] args) => Write(LogLevel.Warn, Format(format, args));

		/// <inheritdoc/>
		public void Error(Exception exception) => Write(LogLevel.Error, exception?.ToString() ?? "(null exception)");

		private static string Format(string format, object[] args)
		{
			if (format == null)
				return string.Empty;
			if (args == null || args.Length == 0)
				return format;

			try
			{
				return string.Format(CultureInfo.InvariantCulture, format, args);
			}
			catch (FormatException)
			{
				return format;
			}
		}

		private void Write(LogLevel level, string message)
		{
			Trace.WriteLine($"[{level}] {_source}: {message}");
			LogSink.Instance.Add(new LogEntry(DateTimeOffset.Now, level, _source, message));
		}
	}
}