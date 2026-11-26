using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;
using ExperimentalTestApp.Boilerplate;
using ExperimentalTestApp.Infrastructure;
using ExperimentalTestApp.Messages;
using ExperimentalTestApp.Services;
using ExperimentalTestApp.ViewModels.Dialogs;
using ExperimentalTestApp.ViewModels.Documents;

namespace ExperimentalTestApp.ViewModels
{
	/// <summary>
	/// The main window: conducts the documents (through <see cref="DockLayoutConductor{T}"/>), drives the tool windows'
	/// lifecycle and exposes the menu actions.
	/// </summary>
	/// <remarks>
	/// All commands are Caliburn.Micro actions (<c>cal:Message.Attach</c> / <c>KeyTrigger</c>) with <c>Can*</c> guards -
	/// there is no <c>ICommand</c> in this sample.
	/// </remarks>
	public sealed class ShellViewModel : DockLayoutConductor<DocumentScreen>,
		IHandle<OpenDocumentMessage>,
		IHandle<ActivateDocumentMessage>
	{
		private readonly IEventAggregator _events;
		private readonly IWindowManager _windowManager;
		private readonly IThemeManager _themeManager;
		private readonly LayoutPersistenceService _layoutPersistence;
		private readonly SampleWorkspace _workspace;
		private readonly ILog _log = LogManager.GetLog(typeof(ShellViewModel));
		private string _statusText = "Ready";
		private string? _selectedTheme;

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellViewModel"/> class.
		/// </summary>
		/// <param name="layoutService">The AvalonDock layout service (documents + toolboxes).</param>
		/// <param name="events">The event aggregator.</param>
		/// <param name="windowManager">The window manager used for dialogs.</param>
		/// <param name="themeManager">The AvalonDock theme manager.</param>
		/// <param name="layoutPersistence">Saves and restores the layout.</param>
		/// <param name="workspace">The in-memory sample files.</param>
		public ShellViewModel(
			IDockLayoutService layoutService,
			IEventAggregator events,
			IWindowManager windowManager,
			IThemeManager themeManager,
			LayoutPersistenceService layoutPersistence,
			SampleWorkspace workspace)
			: base(layoutService)
		{
			_events = events ?? throw new ArgumentNullException(nameof(events));
			_windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
			_themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
			_layoutPersistence = layoutPersistence ?? throw new ArgumentNullException(nameof(layoutPersistence));
			_workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));

			DisplayName = "ExperimentalTestApp - Caliburn.Micro + AvalonDock v5";
			Tools = layoutService.Anchorables.OfType<ToolboxScreen>().ToList().AsReadOnly();
			Themes = themeManager.AvailableThemes.Select(t => t.Name).ToList().AsReadOnly();
			_selectedTheme = themeManager.CurrentThemeName;

			foreach (var tool in Tools)
			{
				tool.IsOpen = tool.IsOpenByDefault;
				tool.PropertyChanged += OnToolPropertyChanged;
			}

			Items.CollectionChanged += OnDocumentsCollectionChanged;
		}

		/// <summary>Gets the tool windows (bound to the View menu and the shortcut behavior).</summary>
		public IReadOnlyList<ToolboxScreen> Tools { get; }

		/// <summary>Gets the names of the available AvalonDock themes (bound by convention to the <c>Themes</c> combo box).</summary>
		public IReadOnlyList<string> Themes { get; }

		/// <summary>
		/// Gets or sets the selected theme (bound by convention: <c>Themes</c> -&gt; <c>SelectedTheme</c>).
		/// </summary>
		public string? SelectedTheme
		{
			get => _selectedTheme;
			set
			{
				if (value == null || !_themeManager.ApplyTheme(value))
					return;

				Set(ref _selectedTheme, value);
				StatusText = $"Theme: {value}";
			}
		}

		/// <summary>Gets or sets the status bar text (bound by convention to <c>StatusText</c>).</summary>
		public string StatusText
		{
			get => _statusText;
			set => Set(ref _statusText, value);
		}

		/// <summary>Gets a value indicating whether the active document can be saved.</summary>
		public bool CanSave => ActiveItem is TextDocumentViewModel { IsModified: true };

		/// <summary>Gets a value indicating whether any document has unsaved changes.</summary>
		public bool CanSaveAll => Items.OfType<TextDocumentViewModel>().Any(d => d.IsModified);

		/// <summary>Gets a value indicating whether the active document can be closed.</summary>
		public bool CanCloseActiveDocument => ActiveItem?.CanClose == true;

		/// <summary>Gets a value indicating whether a saved layout exists.</summary>
		public bool CanLoadLayout => _layoutPersistence.HasSavedLayout;

		/// <summary>Creates and activates a new, empty text document (Ctrl+N).</summary>
		/// <returns>A task that completes when the document is active.</returns>
		public Task NewDocument()
		{
			var file = _workspace.CreateUntitled();
			StatusText = $"Created {file.Key}";
			return ActivateItemAsync(CreateTextDocument(file), CancellationToken.None);
		}

		/// <summary>Saves the active document (Ctrl+S).</summary>
		public void Save()
		{
			if (ActiveItem is TextDocumentViewModel document && document.IsModified)
			{
				document.Save();
				StatusText = $"Saved {document.Title}";
			}
		}

		/// <summary>Saves every modified document (Ctrl+Shift+S).</summary>
		public void SaveAll()
		{
			var modified = Items.OfType<TextDocumentViewModel>().Where(d => d.IsModified).ToList();
			modified.ForEach(d => d.Save());
			StatusText = $"Saved {modified.Count} document(s)";
		}

		/// <summary>Closes the active document through Caliburn.Micro's guard (Ctrl+W).</summary>
		/// <returns>A task that completes when the close attempt has finished.</returns>
		public async Task CloseActiveDocument()
		{
			if (ActiveItem is { CanClose: true } document)
				await DeactivateItemAsync(document, true, CancellationToken.None);
		}

		/// <summary>Closes every closable document; stops at the first "Cancel".</summary>
		/// <returns>A task that completes when the close attempts have finished.</returns>
		public async Task CloseAllDocuments()
		{
			StatusText = await CloseAllDocumentsAsync() ? "All documents closed" : "Close cancelled";
		}

		/// <summary>Saves the current layout.</summary>
		public void SaveLayout()
		{
			StatusText = _layoutPersistence.Save() ? $"Layout saved to {_layoutPersistence.LayoutFilePath}" : "Layout could not be saved";
			NotifyOfPropertyChange(nameof(CanLoadLayout));
		}

		/// <summary>Restores the saved layout.</summary>
		public void LoadLayout()
		{
			StatusText = _layoutPersistence.TryLoad(ResolveLayoutContent) ? "Layout restored" : "No layout restored";
		}

		/// <summary>Restores the start-up layout and the default tool window visibility.</summary>
		public void ResetLayout()
		{
			if (!_layoutPersistence.TryReset(ResolveLayoutContent))
				return;

			foreach (var tool in Tools)
				tool.IsOpen = tool.IsOpenByDefault;
			StatusText = "Layout reset";
		}

		/// <summary>Closes the main window (Caliburn.Micro runs <see cref="Screen.CanCloseAsync"/> first).</summary>
		/// <returns>A task that completes when the close attempt has finished.</returns>
		public Task Exit() => TryCloseAsync();

		/// <summary>Shows the About dialog.</summary>
		/// <returns>A task that completes when the dialog is closed.</returns>
		public Task About() => _windowManager.ShowDialogAsync(new AboutViewModel(_themeManager), null, DialogSettings.CreateFixedDialog());

		/// <inheritdoc/>
		public async Task HandleAsync(OpenDocumentMessage message, CancellationToken cancellationToken)
		{
			var file = _workspace.Find(message.FileKey);
			if (file == null)
				return;

			var id = TextDocumentViewModel.GetId(file);
			await OpenOrActivateAsync(d => d.Id == id, () => CreateTextDocument(file));
		}

		/// <inheritdoc/>
		public Task HandleAsync(ActivateDocumentMessage message, CancellationToken cancellationToken)
		{
			return Items.Contains(message.Document)
				? ActivateItemAsync(message.Document, cancellationToken)
				: Task.CompletedTask;
		}

		/// <inheritdoc/>
		protected override async Task OnInitializedAsync(CancellationToken cancellationToken)
		{
			await base.OnInitializedAsync(cancellationToken);
			_events.SubscribeOnUIThread(this);
			await ActivateItemAsync(new WelcomeViewModel(_events), cancellationToken);
		}

		/// <inheritdoc/>
		protected override async Task OnActivatedAsync(CancellationToken cancellationToken)
		{
			await base.OnActivatedAsync(cancellationToken);
			foreach (var tool in Tools.Where(t => t.IsOpen))
				await ScreenExtensions.TryActivateAsync(tool, cancellationToken);
		}

		/// <inheritdoc/>
		protected override void OnViewLoaded(object view)
		{
			base.OnViewLoaded(view);

			// ShellView has attached its DockingManager by now: remember the pristine layout, then restore the saved one.
			_layoutPersistence.CaptureDefaultLayout();
			if (_layoutPersistence.HasSavedLayout && _layoutPersistence.TryLoad(ResolveLayoutContent))
				StatusText = "Layout restored";
		}

		/// <inheritdoc/>
		protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
			{
				// Save before the conductor closes (and therefore removes) the documents.
				if (_layoutPersistence.IsAttached)
					_layoutPersistence.Save();

				_events.Unsubscribe(this);
				foreach (var tool in Tools)
				{
					tool.PropertyChanged -= OnToolPropertyChanged;
					await ScreenExtensions.TryDeactivateAsync(tool, true, cancellationToken);
				}
			}

			await base.OnDeactivateAsync(close, cancellationToken);
		}

		/// <inheritdoc/>
		protected override void OnActivationProcessed(DocumentScreen item, bool success)
		{
			base.OnActivationProcessed(item, success);
			if (success)
				OnDocumentsChanged();
		}

		private TextDocumentViewModel CreateTextDocument(SampleFile file) => new TextDocumentViewModel(file, _windowManager);

		/// <summary>
		/// Supplies content for a <c>ContentId</c> found in layout XML that is not currently open.
		/// </summary>
		private object? ResolveLayoutContent(string contentId)
		{
			var toolbox = LayoutService.Anchorables.FirstOrDefault(t => t.Id == contentId);
			if (toolbox != null)
				return toolbox;

			var document = Items.FirstOrDefault(d => d.Id == contentId) ?? CreateDocument(contentId);
			if (document != null)
				Adopt(document);
			return document;
		}

		private DocumentScreen? CreateDocument(string contentId)
		{
			if (contentId == WelcomeViewModel.DocumentId)
				return new WelcomeViewModel(_events);

			var key = TextDocumentViewModel.GetFileKey(contentId);
			var file = key == null ? null : _workspace.Find(key);
			return file == null ? null : CreateTextDocument(file);
		}

		private void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			foreach (var document in e.OldItems?.OfType<DocumentScreen>() ?? Enumerable.Empty<DocumentScreen>())
				document.PropertyChanged -= OnDocumentPropertyChanged;
			foreach (var document in e.NewItems?.OfType<DocumentScreen>() ?? Enumerable.Empty<DocumentScreen>())
				document.PropertyChanged += OnDocumentPropertyChanged;

			OnDocumentsChanged();
		}

		private void OnDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(DockableScreen.IsModified))
			{
				NotifyOfPropertyChange(nameof(CanSave));
				NotifyOfPropertyChange(nameof(CanSaveAll));
			}
		}

		private void OnDocumentsChanged()
		{
			NotifyOfPropertyChange(nameof(CanSave));
			NotifyOfPropertyChange(nameof(CanSaveAll));
			NotifyOfPropertyChange(nameof(CanCloseActiveDocument));
			_ = PublishDocumentsChangedAsync();
		}

		private Task PublishDocumentsChangedAsync() =>
			_events.PublishOnUIThreadAsync(new DocumentsChangedMessage(Items, ActiveItem));

		private void OnToolPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(ToolboxScreen.IsOpen) || !(sender is ToolboxScreen tool))
				return;

			_ = tool.IsOpen ? ShowToolAsync(tool) : HideToolAsync(tool);
		}

		private async Task ShowToolAsync(ToolboxScreen tool)
		{
			if (!IsActive)
				return;

			await ScreenExtensions.TryActivateAsync(tool);
			_log.Info("Tool window '{0}' shown", tool.Title);

			// Late subscribers (tools activated after the last change) get the current state.
			await PublishDocumentsChangedAsync();
		}

		private async Task HideToolAsync(ToolboxScreen tool)
		{
			await ScreenExtensions.TryDeactivateAsync(tool, false);
			_log.Info("Tool window '{0}' hidden", tool.Title);
		}
	}
}
