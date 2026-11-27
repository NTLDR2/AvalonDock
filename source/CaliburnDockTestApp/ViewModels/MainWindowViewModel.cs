using Caliburn.Micro;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaliburnDockTestApp.ViewModels
{
	public class MainWindowViewModel : Conductor<Screen>.Collection.OneActive
	{
		private readonly IWindowManager _windowManager;
		private RelayCommand _showACommand;
		private RelayCommand _showBCommand;
		private RelayCommand _showCCommand;
		private RelayCommand _showDCommand;

		public MainWindowViewModel()
			: this(null)
		{
		}

		public MainWindowViewModel(IWindowManager windowManager)
		{
			_windowManager = windowManager ?? new WindowManager();
			SetShowCommand<TestViewModel>(ref _showACommand, "Test A");
			SetShowCommand<TestViewModel>(ref _showBCommand, "Test B");
			SetShowCommand<TestViewModel>(ref _showCCommand, "Test C");
			SetShowCommand<TestViewModel>(ref _showDCommand, "Test D");
		}

		public RelayCommand ShowACommand { get { return _showACommand; } }
		public RelayCommand ShowBCommand { get { return _showBCommand; } }
		public RelayCommand ShowCCommand { get { return _showCCommand; } }
		public RelayCommand ShowDCommand { get { return _showDCommand; } }

		public Task ShowAbout() => _windowManager.ShowDialogAsync(new AboutViewModel());

		private void SetShowCommand<T>(ref RelayCommand command, string displayName)
			where T : ViewModelBase
		{
			command = new RelayCommand(_ => ActivateOrCreate<T>(displayName));
		}

		private int _createCount;

		public Task ActivateOrCreate<T>(string displayName)
			where T : ViewModelBase
		{
			var item = Items.OfType<T>().FirstOrDefault(x => x.DisplayName == displayName);
			if (item == null)
			{
				item = (T)Activator.CreateInstance(typeof(T));
				item.DisplayName = displayName;
				item.IsDirty = ++_createCount % 2 > 0;
			}
			return ActivateItemAsync(item, CancellationToken.None);
		}

		private string GetListItemName(object obj)
		{
			if (obj == null)
				return "null";
			else if (obj is IHaveDisplayName)
				return ((IHaveDisplayName)obj).DisplayName;
			else
				return obj.GetType().Name;
		}
		private string GetListTypes(System.Collections.IList list)
		{
			if (list == null)
				return "null";
			else
				return "[" + string.Join(",", list.Cast<object>().Select(x => GetListItemName(x))) + "]";
		}

		private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			Trace.TraceInformation($"{GetType().Name}.Items_CollectionChanged: Action={e.Action}, OldItems={GetListTypes(e.OldItems)}, OldStartingIndex={e.OldStartingIndex}, NewItems={GetListTypes(e.NewItems)}, NewStartingIndex={e.NewStartingIndex}");
		}

		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			Trace.TraceInformation($"{GetType().Name}.OnDeactivateAsync: close={close}");
			return base.OnDeactivateAsync(close, cancellationToken);
		}

		protected override Task OnInitializedAsync(CancellationToken cancellationToken)
		{
			Trace.TraceInformation($"{GetType().Name}.OnInitializedAsync");

			Items.CollectionChanged += Items_CollectionChanged;
			_ = Execute.OnUIThreadAsync(StartInitialDocuments);

			return base.OnInitializedAsync(cancellationToken);
		}

		protected async Task StartInitialDocuments()
		{
			await Task.Delay(100);
			await ActivateOrCreate<TestViewModel>("Test A");
			await Task.Delay(100);
			await ActivateOrCreate<TestViewModel>("Test B");
			await Task.Delay(100);
			await ActivateOrCreate<TestViewModel>("Test C");
			await Task.Delay(100);
			await ActivateOrCreate<TestViewModel>("Test D");
		}
	}
}
