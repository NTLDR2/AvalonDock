using System;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Messages;
using ExperimentalTestApp.Services;

namespace ExperimentalTestApp.ViewModels.Tools
{
	/// <summary>
	/// Lists the workspace files; opening one publishes an <see cref="OpenDocumentMessage"/> (decoupled from the shell).
	/// </summary>
	public sealed class ExplorerViewModel : ToolboxScreen
	{
		private readonly IEventAggregator _events;
		private SampleFile? _selectedFile;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExplorerViewModel"/> class.
		/// </summary>
		/// <param name="workspace">The workspace whose files are listed.</param>
		/// <param name="events">The event aggregator.</param>
		public ExplorerViewModel(SampleWorkspace workspace, IEventAggregator events)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			Files = (workspace ?? throw new ArgumentNullException(nameof(workspace))).Files;
			Id = "tool.explorer";
			DisplayName = "Explorer";
			ToolTipText = "Workspace files (double-click to open)";
			Zone = DockZone.LeftTop;
			Shortcut = "Ctrl+Shift+E";
			IsOpenByDefault = true;
		}

		/// <summary>Gets the workspace files (bound by convention to the <c>ListBox</c> named <c>Files</c>).</summary>
		public IObservableCollection<SampleFile> Files { get; }

		/// <summary>Gets or sets the selected file (bound by convention: <c>Files</c> -&gt; <c>SelectedFile</c>).</summary>
		public SampleFile? SelectedFile
		{
			get => _selectedFile;
			set
			{
				if (Set(ref _selectedFile, value))
					NotifyOfPropertyChange(nameof(CanOpenSelected));
			}
		}

		/// <summary>Gets a value indicating whether a file is selected (guard of <see cref="OpenSelected"/>).</summary>
		public bool CanOpenSelected => SelectedFile != null;

		/// <summary>Opens the selected file (button <c>OpenSelected</c> and list double-click).</summary>
		/// <returns>A task that completes when the message is published.</returns>
		public Task OpenSelected()
		{
			return SelectedFile == null
				? Task.CompletedTask
				: _events.PublishOnUIThreadAsync(new OpenDocumentMessage(SelectedFile.Key));
		}
	}
}
