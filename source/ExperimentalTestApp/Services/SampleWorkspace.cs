using System;
using System.Linq;
using Caliburn.Micro;

namespace ExperimentalTestApp.Services
{
	/// <summary>
	/// A text file of the in-memory <see cref="SampleWorkspace"/>.
	/// </summary>
	public sealed class SampleFile : PropertyChangedBase
	{
		private string _text;

		/// <summary>
		/// Initializes a new instance of the <see cref="SampleFile"/> class.
		/// </summary>
		/// <param name="key">The unique key (also the file name).</param>
		/// <param name="text">The saved content.</param>
		public SampleFile(string key, string text)
		{
			Key = key ?? throw new ArgumentNullException(nameof(key));
			_text = text ?? string.Empty;
		}

		/// <summary>Gets the unique key, used as file name.</summary>
		public string Key { get; }

		/// <summary>Gets or sets the saved content.</summary>
		public string Text
		{
			get => _text;
			set => Set(ref _text, value ?? string.Empty);
		}
	}

	/// <summary>
	/// In-memory "workspace" listed by the Explorer tool. Keeps the sample free of file dialogs and disk I/O
	/// (the only file written is the layout XML).
	/// </summary>
	public sealed class SampleWorkspace
	{
		private readonly BindableCollection<SampleFile> _files;
		private int _untitledCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="SampleWorkspace"/> class with a few sample files.
		/// </summary>
		public SampleWorkspace()
		{
			_files = new BindableCollection<SampleFile>
			{
				new SampleFile("readme.txt", SampleTexts.Readme),
				new SampleFile("caliburn-micro.txt", SampleTexts.CaliburnMicro),
				new SampleFile("avalondock.txt", SampleTexts.AvalonDock),
				new SampleFile("todo.txt", SampleTexts.Todo),
			};
		}

		/// <summary>Gets the files of the workspace.</summary>
		public IObservableCollection<SampleFile> Files => _files;

		/// <summary>
		/// Finds a file by key.
		/// </summary>
		/// <param name="key">The file key.</param>
		/// <returns>The file, or <see langword="null"/>.</returns>
		public SampleFile? Find(string key) => _files.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

		/// <summary>
		/// Adds a new, empty file named <c>untitled-N.txt</c>.
		/// </summary>
		/// <returns>The new file.</returns>
		public SampleFile CreateUntitled()
		{
			SampleFile file;
			do
			{
				_untitledCount++;
				file = new SampleFile($"untitled-{_untitledCount}.txt", string.Empty);
			}
			while (Find(file.Key) != null);

			_files.Add(file);
			return file;
		}

		private static class SampleTexts
		{
			public const string Readme =
				"ExperimentalTestApp\n" +
				"===================\n\n" +
				"A reference sample for AvalonDock v5 (classic DockingManager) hosted by Caliburn.Micro.\n\n" +
				"Try this:\n" +
				"  * Edit this text - a dot appears in the tab. Close the tab and answer the Save/Discard/Cancel prompt.\n" +
				"  * Drag tabs and tool windows around, tear them off into floating windows, pin/unpin tool windows.\n" +
				"  * Toggle tool windows with their shortcuts (View menu) - e.g. Ctrl+Shift+E for the Explorer.\n" +
				"  * Switch themes with the combo box (VS2010 is the default).\n" +
				"  * Layout > Save, rearrange everything, then Layout > Load or Reset.\n\n" +
				"Watch the 'Caliburn Log' and 'Event Bus' tool windows while you do it.\n";

			public const string CaliburnMicro =
				"Caliburn.Micro patterns used here\n" +
				"---------------------------------\n" +
				"  * BootstrapperBase + SimpleContainer, logger assigned before Initialize().\n" +
				"  * async OnStartup -> await DisplayRootViewForAsync<ShellViewModel>().\n" +
				"  * Conductor<T>.Collection.OneActive for documents; CanCloseAsync guards with a modal dialog.\n" +
				"  * OnInitializedAsync / OnActivatedAsync / OnDeactivateAsync (no obsolete overrides).\n" +
				"  * Actions instead of ICommand: cal:Message.Attach, x:Name conventions, Can* guards, KeyTrigger.\n" +
				"  * IEventAggregator: SubscribeOnUIThread / PublishOnUIThreadAsync / IHandle<T>.\n";

			public const string AvalonDock =
				"AvalonDock v5 patterns used here\n" +
				"--------------------------------\n" +
				"  * DockingManager.DockLayout bound to IDockLayoutService.Layout (an IRootDock tree).\n" +
				"  * ViewModels implement IDockable / IToolbox (see the Boilerplate folder).\n" +
				"  * Views: an implicit DataTemplate for DockableScreen hosting <ContentControl cal:View.Model=\"{Binding}\" />\n" +
				"    (LayoutItemTemplate is left unset: themes apply it with a different DataContext per host).\n" +
				"  * Container styles bind Title <- Model.Title and ContentId <- Model.Id (needed for layout restore).\n" +
				"  * Window policy (floating / detached windows) is set once, on the IRootDock.\n" +
				"  * XmlLayoutSerializer saves and restores the layout.\n";

			public const string Todo =
				"TODO\n" +
				"----\n" +
				"[ ] Make some changes and save them with Ctrl+S\n" +
				"[ ] Create a new document with Ctrl+N\n" +
				"[ ] Close all documents and cancel one of the prompts\n";
		}
	}
}
