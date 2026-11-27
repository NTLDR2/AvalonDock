using System;
using System.Windows;
using ThemeSuperTestApp.Services;
using ThemeSuperTestApp.ViewModels;

namespace ThemeSuperTestApp.Views
{
	/// <summary>
	/// Main application window resolved as a singleton from <see cref="Bootstrapper"/> so WPF and Win32 services can attach to <c>DockManager</c> and its HWND.
	/// </summary>
	public partial class ShellView : Window
	{
		private readonly DataTemplate? _iconDocumentHeaderTemplate;
		private ShellViewModel? _boundShellViewModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellView"/> class.
		/// </summary>
		/// <param name="themeCatalog">The theme catalog service attached to <c>DockManager</c>.</param>
		/// <param name="osThemeService">The Windows OS theme detector attached to this window's HWND.</param>
		/// <param name="dwmChromeHelper">The opt-in DWM title-bar helper attached to this window.</param>
		/// <param name="agentBridge">The bidirectional Antigravity file-mailbox service attached to this window.</param>
		/// <param name="layoutPersistence">The layout persistence service attached to <c>DockManager</c>.</param>
		public ShellView(
			ThemeCatalogService themeCatalog,
			WindowsOsThemeService osThemeService,
			DwmWindowChromeHelper dwmChromeHelper,
			AgentBridgeService agentBridge,
			LayoutPersistenceService layoutPersistence)
		{
			if (themeCatalog == null) throw new ArgumentNullException(nameof(themeCatalog));
			if (osThemeService == null) throw new ArgumentNullException(nameof(osThemeService));
			if (dwmChromeHelper == null) throw new ArgumentNullException(nameof(dwmChromeHelper));
			if (agentBridge == null) throw new ArgumentNullException(nameof(agentBridge));
			if (layoutPersistence == null) throw new ArgumentNullException(nameof(layoutPersistence));

			InitializeComponent();

			_iconDocumentHeaderTemplate = TryFindResource("IconDocumentHeaderTemplate") as DataTemplate;
			DataContextChanged += OnShellDataContextChanged;

			osThemeService.AttachWindow(this);
			dwmChromeHelper.AttachMainWindow(this);

			themeCatalog.Attach(DockManager);
			themeCatalog.ApplyTheme(ThemeCatalogService.DefaultThemeId);
			layoutPersistence.Attach(DockManager);

			agentBridge.AttachVisualRoot(this);
			agentBridge.Start();
		}

		private void OnShellDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (_boundShellViewModel != null)
				_boundShellViewModel.DocumentHeaderTemplateOptionChanged -= OnDocumentHeaderTemplateOptionChanged;

			_boundShellViewModel = e.NewValue as ShellViewModel;
			if (_boundShellViewModel != null)
			{
				_boundShellViewModel.DocumentHeaderTemplateOptionChanged += OnDocumentHeaderTemplateOptionChanged;
				SyncDocumentHeaderTemplate(_boundShellViewModel.UseCustomDocumentHeaderTemplate);
			}
		}

		private void OnDocumentHeaderTemplateOptionChanged(object? sender, EventArgs e)
		{
			if (_boundShellViewModel != null)
				SyncDocumentHeaderTemplate(_boundShellViewModel.UseCustomDocumentHeaderTemplate);
		}

		private void SyncDocumentHeaderTemplate(bool useCustomTemplate)
		{
			DockManager.DocumentHeaderTemplate = useCustomTemplate ? _iconDocumentHeaderTemplate : null;
		}
	}
}