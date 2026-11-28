using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.Mvvm;
using AvalonDock.Themes;
using AvalonDock.Themes.Type1Legacy;
using AvalonDock.Themes.Type2Hybrid;
using AvalonDock.Themes.Type3New;
using Caliburn.Micro;
using ExperimentalTestApp.Infrastructure;
using ExperimentalTestApp.Services;
using ExperimentalTestApp.ViewModels;
using ExperimentalTestApp.ViewModels.Tools;
using ExperimentalTestApp.Views;

namespace ExperimentalTestApp
{
	/// <summary>
	/// Caliburn.Micro bootstrapper: composes the application with <see cref="SimpleContainer"/> and shows the shell.
	/// </summary>
	/// <remarks>
	/// <para>Start-up order (Caliburn.Micro <c>BootstrapperBase.StartRuntime</c>): <see cref="Configure"/> runs inside
	/// <see cref="BootstrapperBase.Initialize"/>, before the <c>IoC</c> delegates are assigned - so resolve from
	/// <see cref="_container"/> directly here, never through <c>IoC.Get</c>.</para>
	/// <para>AvalonDock window policy (<see cref="IRootDock.AllowFloatingWindows"/> / <see cref="IRootDock.AllowDetachedWindows"/>)
	/// is configured in exactly one place - on the <see cref="IRootDock"/> - because <c>LayoutSyncBridge</c> pushes it
	/// onto the <see cref="DockingManager"/> and overwrites any XAML value.</para>
	/// </remarks>
	public sealed class Bootstrapper : BootstrapperBase
	{
		/// <summary>The AvalonDock theme applied at start-up (<c>AvalonDock.Themes.Type1Legacy.VS2010Theme</c>).</summary>
		public const string DefaultThemeName = "VS2010";

		private readonly SimpleContainer _container = new SimpleContainer();

		/// <summary>
		/// Initializes a new instance of the <see cref="Bootstrapper"/> class.
		/// </summary>
		public Bootstrapper()
		{
			// Must run before Initialize(): Caliburn.Micro classes cache their ILog in static fields.
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
				.Singleton<SampleWorkspace>()
				.Singleton<LayoutPersistenceService>()
				.Singleton<ShellViewModel>()
				// Views can be container-built too: ViewLocator asks IoC.GetAllInstances(viewType) first,
				// which lets ShellView receive WPF-only services through its constructor.
				// Singleton, NOT PerRequest: SimpleContainer.GetAllInstances enumerates its lazy result twice,
				// so a PerRequest view would be constructed twice - leaving an orphan Window in
				// Application.Windows that keeps the process alive after the main window closes.
				.Singleton<ShellView>();

			// Toolboxes are singletons registered under their concrete type AND under IToolbox
			// (a Handler forwards to the same instance; RegisterSingleton twice would create two objects).
			RegisterToolbox<ExplorerViewModel>();
			RegisterToolbox<OpenDocumentsViewModel>();
			RegisterToolbox<PropertiesViewModel>();
			RegisterToolbox<CaliburnLogViewModel>();
			RegisterToolbox<EventBusViewModel>();

			var themeManager = new ThemeManager(new Theme[]
			{
				new VS2010Theme(),
				new Vs2013LightTheme(),
				new Vs2013BlueTheme(),
				new Vs2013DarkTheme(),
				new VS2015LightTheme(),
				new VS2015BlueTheme(),
				new VS2015DarkTheme(),
				new VS2022LightTheme(),
				new VS2022BlueTheme(),
				new VS2022DarkTheme(),
				new VS2026LightTheme(),
				new VS2026BlueTheme(),
				new VS2026DarkTheme(),
				new MetroTheme(),
				new ExpressionLightTheme(),
				new ExpressionDarkTheme(),
				new AeroTheme(),
				new GenericTheme(),
			});
			themeManager.ApplyTheme(DefaultThemeName); // records the choice; ShellView re-applies it once attached
			_container.Instance(themeManager);
			_container.Instance<IThemeManager>(themeManager);

			// AvalonDock.Mvvm builds the IRootDock tree (Documents + one ToolDock per side) from the IToolbox list.
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
		protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			LogManager.GetLog(typeof(Bootstrapper)).Error(e.Exception);
			MessageBox.Show(e.Exception.Message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}

		private static void OnCoroutineCompleted(object? sender, ResultCompletionEventArgs e)
		{
			// Async action methods (Task-returning cal:Message.Attach targets) run as coroutines:
			// their exceptions surface here instead of on the dispatcher.
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
