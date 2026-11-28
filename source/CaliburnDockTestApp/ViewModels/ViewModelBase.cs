using Caliburn.Micro;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaliburnDockTestApp.ViewModels
{
	public abstract class ViewModelBase : Screen
	{
		private readonly IWindowManager _windowManager;
		private readonly ILog _log;

		protected ViewModelBase(IWindowManager windowManager)
		{
			_windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
			_log = LogManager.GetLog(GetType());
			_id = GetType().Name;
			DisplayName = GetType().Name;
		}

		private string _id;

		public string Id
		{
			get => _id;
			set => Set(ref _id, value);
		}

		private bool _isDirty;

		public bool IsDirty
		{
			get => _isDirty;
			set => Set(ref _isDirty, value);
		}

		private string _customText;

		public string CustomText
		{
			get => _customText;
			set
			{
				if (Set(ref _customText, value))
					IsDirty = true;
			}
		}

		public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
		{
			bool close = true;
			if (IsDirty)
			{
				bool? result = await _windowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName));
				close = result == true;
			}
			_log.Info("CanCloseAsync: close={0}", close);
			return close;
		}

		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			_log.Info("OnDeactivateAsync: close={0}", close);
			return base.OnDeactivateAsync(close, cancellationToken);
		}
	}
}