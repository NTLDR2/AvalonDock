using System;
using System.Windows;
using AvalonDock;
using ExperimentalTestApp.Services;

namespace ExperimentalTestApp.Views
{
	/// <summary>
	/// The main window. Created through the container (Caliburn.Micro's <c>ViewLocator</c> asks
	/// <c>IoC.GetAllInstances(typeof(ShellView))</c> before falling back to <c>Activator</c>), which lets it receive the
	/// WPF-only services that must be attached to its <see cref="DockingManager"/>.
	/// </summary>
	public partial class ShellView : Window
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ShellView"/> class.
		/// </summary>
		/// <param name="themeManager">The AvalonDock theme manager; the view attaches its docking manager to it.</param>
		/// <param name="layoutPersistence">The layout persistence service; the view attaches its docking manager to it.</param>
		public ShellView(ThemeManager themeManager, LayoutPersistenceService layoutPersistence)
		{
			if (themeManager == null) throw new ArgumentNullException(nameof(themeManager));
			if (layoutPersistence == null) throw new ArgumentNullException(nameof(layoutPersistence));

			InitializeComponent();

			themeManager.Attach(DockManager);
			if (themeManager.CurrentThemeName != null)
				themeManager.ApplyTheme(themeManager.CurrentThemeName); // apply the start-up theme before the first render

			layoutPersistence.Attach(DockManager);
		}
	}
}
