using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AvalonDock.Core;
using AvalonDock.Mvvm;
using Caliburn.Micro;
using ThemeSuperTestApp.Infrastructure;
using ThemeSuperTestApp.Services;
using ThemeSuperTestApp.ViewModels;
using ThemeSuperTestApp.ViewModels.Documents;
using ThemeSuperTestApp.ViewModels.Tools;
using ThemeSuperTestApp.Views;

namespace ThemeSuperTestApp
{
	/// <summary>
	/// Caliburn.Micro bootstrapper composing <c>ThemeSuperTestApp</c> via <see cref="SimpleContainer"/>.
	/// </summary>
	public sealed class Bootstrapper : BootstrapperBase
	{
		private readonly SimpleContainer _container = new SimpleContainer();

		/// <summary>
		/// Initializes a new instance of the <see cref="Bootstrapper"/> class.
		/// </summary>
		public Bootstrapper()
		{
			LogManager.GetLog = type => new TraceLog(type);
			Initialize();
		}

		/// <inheritdoc/>
		protected override void Configure()
		{
			_container.Instance(_container);

			_container
				.Singleton<IWindowManager, WindowManager>()
				.Singleton<IEventAggregator, EventAggregator>()
				.Singleton<ThemeCatalogService>()
				.Singleton<ResourceDictionaryAuditService>()
				.Singleton<WindowDictionaryTrackerService>()
				.Singleton<WpfTraceCaptureService>()
				.Singleton<WindowsOsThemeService>()
				.Singleton<DwmWindowChromeHelper>()
				.Singleton<DiagnosticReportExporterService>()
				.Singleton<AgentBridgePaths>()
				.Singleton<WindowScreenshotService>()
				.Singleton<AgentBridgeService>()
				.Singleton<LayoutPersistenceService>()
				.Singleton<ThemeOverviewDocumentViewModel>()
				.Singleton<ControlGalleryDocumentViewModel>()
				.Singleton<ShellViewModel>()
				// Register ShellView as Singleton (never PerRequest) so SimpleContainer.GetAllInstances never creates an orphan Window.
				.Singleton<ShellView>();

			// Force WpfTraceCaptureService construction early so startup XAML binding/resource diagnostics are captured too.
			_ = _container.GetInstance(typeof(WpfTraceCaptureService), null);

			RegisterToolbox<RuntimeThemeLoaderViewModel>();
			RegisterToolbox<AgentBridgeToolboxViewModel>();
			RegisterToolbox<StressSpecimenToolboxViewModel>();
			RegisterToolbox<ThemeProfilerViewModel>();
			RegisterToolbox<ResourceAuditorViewModel>();
			RegisterToolbox<WindowSharingTrackerViewModel>();
			RegisterToolbox<WpfTraceListenerViewModel>();

			var toolboxes = _container.GetAllInstances(typeof(IToolbox)).Cast<IToolbox>();
			var layoutService = new DockLayoutService(toolboxes);
			layoutService.Layout.AllowFloatingWindows = true;
			layoutService.Layout.AllowDetachedWindows = true;
			_container.Instance<IDockLayoutService>(layoutService);

			Coroutine.Completed += OnCoroutineCompleted;
		}

		/// <inheritdoc/>
		protected override object GetInstance(Type service, string key) => _container.GetInstance(service, key);

		/// <inheritdoc/>
		protected override IEnumerable<object> GetAllInstances(Type service) => _container.GetAllInstances(service);

		/// <inheritdoc/>
		protected override void BuildUp(object instance) => _container.BuildUp(instance);

		/// <inheritdoc/>
		protected override async void OnStartup(object sender, StartupEventArgs e)
		{
			await DisplayRootViewForAsync<ShellViewModel>();
		}

		/// <inheritdoc/>
		protected override void OnExit(object sender, EventArgs e)
		{
			(_container.GetInstance(typeof(AgentBridgeService), null) as IDisposable)?.Dispose();
			(_container.GetInstance(typeof(WpfTraceCaptureService), null) as IDisposable)?.Dispose();
			(_container.GetInstance(typeof(DwmWindowChromeHelper), null) as IDisposable)?.Dispose();
			(_container.GetInstance(typeof(WindowsOsThemeService), null) as IDisposable)?.Dispose();
			base.OnExit(sender, e);
		}

		/// <inheritdoc/>
		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			LogManager.GetLog(typeof(Bootstrapper)).Error(e.Exception);
			MessageBox.Show(e.Exception.Message, "ThemeSuperTestApp - Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}

		private static void OnCoroutineCompleted(object? sender, ResultCompletionEventArgs e)
		{
			if (e.Error != null)
				LogManager.GetLog(typeof(Bootstrapper)).Error(e.Error);
		}

		private void RegisterToolbox<TToolbox>()
			where TToolbox : class, IToolbox
		{
			_container.Singleton<TToolbox>();
			_container.RegisterHandler(typeof(IToolbox), null, container => container.GetInstance(typeof(TToolbox), null));
		}
	}
}