using Caliburn.Micro;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaliburnDockTestApp.ViewModels
{
	// AvalonDock v5 supports two official MVVM binding modes:
	//   - Mode A (DockLayout + IDockLayoutService + IDockable/IToolbox in AvalonDock.Mvvm): full IDE workspace
	//     with documents + 6-zone tool windows, demonstrated in ExperimentalTestApp and ThemeSuperTestApp.
	//   - Mode B (Direct DocumentsSource + ActiveContent on DockingManager): lightweight document-only binding
	//     where an existing collection/conductor drives DockingManager directly with zero IDockable wrappers.
	//
	// CaliburnDockTestApp intentionally uses Mode B so DockingManager (DocumentsSource/ActiveContent) and
	// standard WPF TabControl (ItemsSource/SelectedItem) bind 1:1 to the exact same Conductor.Items and
	// Conductor.ActiveItem properties. Note: binding ActiveContent TwoWay directly to Conductor.ActiveItem is
	// safe here ONLY because no AnchorablesSource tool panes are hosted (otherwise focusing a tool pane would
	// push a non-document ViewModel into ActiveItem and Conductor.EnsureItem would add it to Items).
	public class MainWindowViewModel : Conductor<Screen>.Collection.OneActive
	{
		private readonly IWindowManager _windowManager;
		private readonly Func<TestViewModel> _testViewModelFactory;
		private readonly Func<AboutViewModel> _aboutViewModelFactory;
		private readonly ILog _log;
		private int _createCount;

		public MainWindowViewModel(
			IWindowManager windowManager,
			Func<TestViewModel> testViewModelFactory,
			Func<AboutViewModel> aboutViewModelFactory)
		{
			_windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
			_testViewModelFactory = testViewModelFactory ?? throw new ArgumentNullException(nameof(testViewModelFactory));
			_aboutViewModelFactory = aboutViewModelFactory ?? throw new ArgumentNullException(nameof(aboutViewModelFactory));
			_log = LogManager.GetLog(GetType());
			DisplayName = "Caliburn AvalonDock Test";
		}

		public Task OpenDocumentAsync(string displayName)
		{
			if (string.IsNullOrWhiteSpace(displayName))
				return Task.CompletedTask;

			string id = displayName.Replace(" ", string.Empty);
			var item = Items.OfType<TestViewModel>().FirstOrDefault(x => x.Id == id);
			if (item == null)
			{
				item = _testViewModelFactory();
				item.Id = id;
				item.DisplayName = displayName;
				item.IsDirty = ++_createCount % 2 > 0;
			}
			return ActivateItemAsync(item, CancellationToken.None);
		}

		public Task CloseDocumentAsync(Screen document)
			=> document != null ? this.CloseItemAsync(document, CancellationToken.None) : Task.CompletedTask;

		public Task ShowAboutAsync() => _windowManager.ShowDialogAsync(_aboutViewModelFactory());

		private static string GetListItemName(object obj)
		{
			if (obj == null)
				return "null";
			if (obj is IHaveDisplayName named)
				return named.DisplayName;
			return obj.GetType().Name;
		}

		private static string GetListTypes(IList list)
		{
			if (list == null)
				return "null";
			return "[" + string.Join(",", list.Cast<object>().Select(GetListItemName)) + "]";
		}

		private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			_log.Info(
				"Items_CollectionChanged: Action={0}, OldItems={1}, OldStartingIndex={2}, NewItems={3}, NewStartingIndex={4}",
				e.Action,
				GetListTypes(e.OldItems),
				e.OldStartingIndex,
				GetListTypes(e.NewItems),
				e.NewStartingIndex);
		}

		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			_log.Info("OnDeactivateAsync: close={0}", close);
			return base.OnDeactivateAsync(close, cancellationToken);
		}

		protected override Task OnInitializedAsync(CancellationToken cancellationToken)
		{
			_log.Info("OnInitializedAsync");

			Items.CollectionChanged += Items_CollectionChanged;
			_ = Execute.OnUIThreadAsync(StartInitialDocumentsAsync);

			return base.OnInitializedAsync(cancellationToken);
		}

		protected async Task StartInitialDocumentsAsync()
		{
			await Task.Delay(100);
			await OpenDocumentAsync("Test A");
			await Task.Delay(100);
			await OpenDocumentAsync("Test B");
			await Task.Delay(100);
			await OpenDocumentAsync("Test C");
			await Task.Delay(100);
			await OpenDocumentAsync("Test D");
		}
	}
}