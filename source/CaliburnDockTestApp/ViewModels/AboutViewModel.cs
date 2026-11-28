using Caliburn.Micro;
using System.Threading.Tasks;

namespace CaliburnDockTestApp.ViewModels
{
	public class AboutViewModel : Screen
	{
		public AboutViewModel()
		{
			DisplayName = "What's This App About — CaliburnDockTestApp";
			AboutTitle = "CaliburnDockTestApp — Side-by-Side Synchronization Harness";
			AboutText =
				"CaliburnDockTestApp is a side-by-side diagnostic and reference harness for AvalonDock 5.1.0 and Caliburn.Micro 6.0.15-beta on WPF (.NET Framework 4.8), composed with SimpleContainer (Singleton shell/services, PerRequest ViewModels with Func<T> auto-factories, and zero ICommand or MessageBox.Show boilerplate).\r\n\r\n" +
				"Why Two Tab Controls Side by Side?\r\n" +
				"Both the left pane (AvalonDock DockingManager) and the right pane (a standard WPF TabControl \"control group\") bind to the same MainWindowViewModel (Conductor<Screen>.Collection.OneActive):\r\n" +
				"• Collection & Selection Sync: DocumentsSource and ItemsSource observe Conductor.Items, while ActiveContent, SelectedItem, and the single radio-exclusive checkmark in Window -> Window A–D (OpenDocumentAsync) track Conductor.ActiveItem across tab switches, split tab groups, and floating windows.\r\n" +
				"• Conventions & Dirty-State Sync: TestView, ConfirmCloseView, and AboutView use pure x:Name conventions (PropertyChanged text sync, Async-suffix action stripping, and automatic Window.Title <-> DisplayName binding), while cal:Message.Attach handles parameterized menu items and item DataTemplates. Editing \"Custom Text\" or \"Is Dirty\" updates both views and tab-header dots in lockstep.\r\n" +
				"• Unified Async Close Guards: DocumentCloseBehavior cancels DockingManager.DocumentClosing and FIFO-queues IConductor.CloseItemAsync alongside TabManager's CloseDocumentAsync($dataContext), awaiting IWindowManager.ShowDialogAsync(ConfirmCloseViewModel) in Screen.CanCloseAsync.\r\n\r\n" +
				"Dual-Host Architecture Note\r\n" +
				"Because Caliburn.Micro's ViewAware caches one UIElement per context, DockManager uses an implicit DataTemplate on the default context while TabManager sets cal:View.Context=\"TabControl\" (backed by a ViewLocator fallback in Bootstrapper), keeping two live TestView instances without visual theft.";
		}

		public string AboutTitle { get; }

		public string AboutText { get; }

		public Task OkAsync() => TryCloseAsync(true);
	}
}