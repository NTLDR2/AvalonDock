using Caliburn.Micro;
using CaliburnDockTestApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;

namespace CaliburnDockTestApp
{
	public class Bootstrapper : BootstrapperBase
	{
		private readonly SimpleContainer _container = new SimpleContainer();

		public Bootstrapper()
		{
			LogManager.GetLog = type => new CaliburnLogger(type);
			Initialize();
		}

		protected override void Configure()
		{
			base.Configure();

			// Services & root shell are Singletons; child/dialog ViewModels are PerRequest so SimpleContainer
			// automatically synthesizes zero-arg Func<T> factories (Func<TestViewModel>, Func<AboutViewModel>).
			// Runtime-parameterized prompts (ConfirmCloseViewModel(string)) are constructed at the call site because
			// SimpleContainer only synthesizes parameterless Func<T> delegates. WPF Views are intentionally left
			// unregistered so ViewLocator constructs each view once without triggering SimpleContainer.GetAllInstances
			// double-enumeration.
			_container.Instance(_container);
			_container.Singleton<IWindowManager, WindowManager>();
			_container.Singleton<MainWindowViewModel>();
			_container.PerRequest<TestViewModel>();
			_container.PerRequest<AboutViewModel>();

			Coroutine.Completed += (_, e) =>
			{
				if (e.Error != null)
					LogManager.GetLog(typeof(Bootstrapper)).Error(e.Error);
			};

			// Dual-host View.Context fallback: Caliburn.Micro's ViewAware caches one UIElement per context key.
			// DockManager resolves TestView on the default (null) context while TabManager specifies
			// cal:View.Context="TabControl"; falling back to context == null here lets both controls materialize
			// independent TestView instances simultaneously without stealing visuals from one another.
			// (For Mode-A DockLayout, 18-theme switching, and DetachedAnchorableWindow tool panes, see ExperimentalTestApp and ThemeSuperTestApp.)
			var defaultLocateTypeForModelType = ViewLocator.LocateTypeForModelType;
			ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
				defaultLocateTypeForModelType(modelType, displayLocation, context)
				?? (context != null ? defaultLocateTypeForModelType(modelType, displayLocation, null) : null);
		}

		protected override IEnumerable<Assembly> SelectAssemblies() => new[] { typeof(Bootstrapper).Assembly };

		protected override object GetInstance(Type service, string key) => _container.GetInstance(service, key);

		protected override IEnumerable<object> GetAllInstances(Type service) => _container.GetAllInstances(service);

		protected override void BuildUp(object instance) => _container.BuildUp(instance);

		protected override async void OnStartup(object sender, StartupEventArgs e)
		{
			base.OnStartup(sender, e);
			await DisplayRootViewForAsync<MainWindowViewModel>();
		}
	}
}