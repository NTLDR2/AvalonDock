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
				"CaliburnDockTestApp is a side-by-side diagnostic and reference harness for integrating AvalonDock with Caliburn.Micro in WPF (.NET Framework 4.8).\r\n\r\n" +
				"Why Two Tab Controls Side by Side?\r\n" +
				"Both the left pane (AvalonDock DockingManager) and the right pane (a standard WPF TabControl acting as a baseline \"control group\") bind simultaneously to the exact same MainWindowViewModel (Conductor<Screen>.Collection.OneActive):\r\n" +
				"• Collection Sync: DocumentsSource (AvalonDock) and ItemsSource (TabControl) both observe Conductor.Items, verifying that adding, closing, reopening (Window -> Window A–D), and resetting documents stay in lockstep.\r\n" +
				"• Active Selection Sync: ActiveContent (AvalonDock) and SelectedItem (TabControl) bind two-way to Conductor.ActiveItem, confirming that tab switches, tear-off floating windows, and split tab groups activate and deactivate Screen lifecycles identically.\r\n" +
				"• Live View & Dirty-State Sync: Typing in \"Custom Text\" or toggling \"Is Dirty\" in either pane immediately updates the shared TestViewModel, mirroring text across both panes and toggling the red dirty indicator dots in both tab headers.\r\n" +
				"• Cancellable Async Close Guards: Closing a dirty tab from either pane (or closing a floating window or the main window) invokes Screen.CanCloseAsync, cancelling the close in both controls if the user declines.\r\n\r\n" +
				"Dual-Host Architecture Note\r\n" +
				"Because Caliburn.Micro's ViewAware caches one UIElement view per context, rendering the same Screen in two controls at once requires distinct contexts. DockManager uses an implicit DataTemplate on the default context, while TabManager specifies cal:View.Context=\"TabControl\" (backed by a ViewLocator fallback in Bootstrapper) so each host maintains its own TestView instance without stealing the visual from the other.";
		}

		public string AboutTitle { get; }

		public string AboutText { get; }

		public Task Close() => TryCloseAsync(true);
	}
}
