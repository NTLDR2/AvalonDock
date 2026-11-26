using AvalonDock.Core;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Infrastructure;

namespace ExperimentalTestApp.ViewModels.Tools
{
	/// <summary>
	/// Shows what Caliburn.Micro itself logs through <see cref="LogManager"/>: screen activation/deactivation/close,
	/// action invocations and guard evaluation, plus the sample's own messages.
	/// </summary>
	public sealed class CaliburnLogViewModel : ToolboxScreen
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CaliburnLogViewModel"/> class.
		/// </summary>
		public CaliburnLogViewModel()
		{
			Id = "tool.caliburnLog";
			DisplayName = "Caliburn Log";
			ToolTipText = "Caliburn.Micro log output (ILog)";
			Zone = DockZone.BottomLeft;
			Shortcut = "Ctrl+Shift+L";
			IsOpenByDefault = true;
		}

		/// <summary>Gets the log entries (bound by convention to the list named <c>Entries</c>).</summary>
		public BindableCollection<LogEntry> Entries => LogSink.Instance.Entries;

		/// <summary>
		/// Gets or sets a value indicating whether Caliburn.Micro's verbose binder/action diagnostics are shown
		/// (bound by convention to the check box named <c>IncludeDiagnostics</c>).
		/// </summary>
		public bool IncludeDiagnostics
		{
			get => LogSink.Instance.IncludeDiagnostics;
			set
			{
				if (LogSink.Instance.IncludeDiagnostics == value)
					return;

				LogSink.Instance.IncludeDiagnostics = value;
				NotifyOfPropertyChange();
			}
		}

		/// <summary>Clears the log (button <c>Clear</c>).</summary>
		public void Clear() => LogSink.Instance.Clear();
	}
}
