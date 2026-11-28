using Caliburn.Micro;
using CaliburnDockTestApp.ViewModels;
using System.Windows;

namespace CaliburnDockTestApp
{
	public class Bootstrapper : Caliburn.Micro.BootstrapperBase
	{
		public Bootstrapper()
		{
			LogManager.GetLog = type => new CaliburnLogger(type);
			Initialize();
		}

		protected override void Configure()
		{
			base.Configure();

			// Verified on Caliburn.Micro 6.0.15-beta; stress-test again once full AvalonDock v5 features
			// (Themes.Type3New, DockLayout, and DetachedAnchorableWindow tool panes) are enabled: rapidly switch,
			// split, swap themes, float, and detach tabs while typing in both panes to confirm zero view-theft
			// or stale ViewAware cache references.
			var defaultLocateTypeForModelType = ViewLocator.LocateTypeForModelType;
			ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
				defaultLocateTypeForModelType(modelType, displayLocation, context)
				?? (context != null ? defaultLocateTypeForModelType(modelType, displayLocation, null) : null);
		}

		protected override async void OnStartup(object sender, StartupEventArgs e)
		{
			base.OnStartup(sender, e);
			await DisplayRootViewForAsync<MainWindowViewModel>();
		}
	}
}
