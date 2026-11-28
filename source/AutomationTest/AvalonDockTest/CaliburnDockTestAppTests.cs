// WPF STA tests intentionally resume on TestHost's DispatcherSynchronizationContext after every await.
#pragma warning disable CA2007

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;
using AvalonDockTest.TestHelpers;
using Caliburn.Micro;
using CaliburnDockTestApp;
using CaliburnDockTestApp.ViewModels;
using CaliburnDockTestApp.Views;
using NUnit.Framework;
using static AvalonDockTest.CaliburnTestHarness;

namespace AvalonDockTest
{
	/// <summary>
	/// STA integration and regression tests for <c>CaliburnDockTestApp</c> on <c>net48</c>:
	/// <see cref="SimpleContainer"/> composition, pure <c>x:Name</c> convention bindings, strict OS/WPF system-default
	/// dialogs, dual-host <c>cal:View.Context</c> isolation, <c>Model.Id</c> -&gt; <see cref="LayoutContent.ContentId"/>
	/// vs. <c>Model.DisplayName</c> -&gt; <see cref="LayoutContent.Title"/> mapping, teardown-safe header bindings,
	/// and serialized async close confirmation via <see cref="DocumentCloseBehavior"/>.
	/// </summary>
	[TestFixture]
	public class CaliburnDockTestAppTests : AutomationTestBase
	{
		private Func<Type, ILog> _savedGetLog;
		private Func<Type, string, object> _savedGetInstance;
		private Func<Type, IEnumerable<object>> _savedGetAllInstances;
		private Action<object> _savedBuildUp;
		private Func<Type, DependencyObject, object, Type> _savedLocateTypeForModelType;
		private List<Assembly> _savedAssemblies;

		[OneTimeSetUp]
		public async Task CaliburnOneTimeSetUp()
		{
			await TestHost.SwitchToAppThread();

			_savedGetLog = LogManager.GetLog;
			_savedGetInstance = IoC.GetInstance;
			_savedGetAllInstances = IoC.GetAllInstances;
			_savedBuildUp = IoC.BuildUp;
			_savedLocateTypeForModelType = ViewLocator.LocateTypeForModelType;
			_savedAssemblies = AssemblySource.Instance.ToList();

			_ = new Bootstrapper();
		}

		[OneTimeTearDown]
		public async Task CaliburnOneTimeTearDown()
		{
			await TestHost.SwitchToAppThread();

			if (_savedGetLog != null)
				LogManager.GetLog = _savedGetLog;
			if (_savedGetInstance != null)
				IoC.GetInstance = _savedGetInstance;
			if (_savedGetAllInstances != null)
				IoC.GetAllInstances = _savedGetAllInstances;
			if (_savedBuildUp != null)
				IoC.BuildUp = _savedBuildUp;
			if (_savedLocateTypeForModelType != null)
				ViewLocator.LocateTypeForModelType = _savedLocateTypeForModelType;

			if (_savedAssemblies != null)
			{
				AssemblySource.Instance.Clear();
				AssemblySource.Instance.AddRange(_savedAssemblies);
			}
		}

		[Test]
		public async Task Bootstrapper_SimpleContainer_ResolvesSingletonShellAndPerRequestViewModels()
		{
			await TestHost.SwitchToAppThread();

			var wm1 = IoC.Get<IWindowManager>();
			var wm2 = IoC.Get<IWindowManager>();
			Assert.That(wm1, Is.Not.Null);
			Assert.That(wm2, Is.SameAs(wm1), "IWindowManager must be registered as a Singleton.");

			var shell1 = IoC.Get<MainWindowViewModel>();
			var shell2 = IoC.Get<MainWindowViewModel>();
			Assert.That(shell1, Is.Not.Null);
			Assert.That(shell2, Is.SameAs(shell1), "MainWindowViewModel must be registered as a Singleton.");
			Assert.That(shell1.DisplayName, Is.EqualTo("Caliburn AvalonDock Test"));

			var testVm1 = IoC.Get<TestViewModel>();
			var testVm2 = IoC.Get<TestViewModel>();
			Assert.That(testVm1, Is.Not.Null);
			Assert.That(testVm2, Is.Not.SameAs(testVm1), "TestViewModel must be registered as PerRequest.");

			var aboutVm1 = IoC.Get<AboutViewModel>();
			var aboutVm2 = IoC.Get<AboutViewModel>();
			Assert.That(aboutVm1, Is.Not.Null);
			Assert.That(aboutVm2, Is.Not.SameAs(aboutVm1), "AboutViewModel must be registered as PerRequest.");
		}

		[Test]
		public async Task ModalDialogs_BindPureConventionsAndHonorStrictSystemDefaults()
		{
			await TestHost.SwitchToAppThread();

			var windowManager = new TestableWindowManager();

			var confirmVm = new ConfirmCloseViewModel("Test A");
			var confirmWin = await ShowOffScreenAsync(windowManager, confirmVm, isDialog: true);
			try
			{
				Assert.That(confirmWin, Is.TypeOf<ConfirmCloseView>());
				Assert.That(confirmWin.Title, Is.EqualTo("Test A"));

				var msgBlock = (TextBlock)confirmWin.FindName("Message");
				var confirmBtn = (Button)confirmWin.FindName("Confirm");
				var cancelBtn = (Button)confirmWin.FindName("Cancel");

				Assert.That(msgBlock.Text, Is.EqualTo("Are you sure you want to close 'Test A'?"));
				Assert.That(confirmBtn.Content, Is.EqualTo("_Yes"));
				Assert.That(cancelBtn.Content, Is.EqualTo("_No"));
				AssertZeroLocalFontColorOverrides(confirmWin);
			}
			finally
			{
				confirmWin.Close();
			}

			var aboutVm = IoC.Get<AboutViewModel>();
			var aboutWin = await ShowOffScreenAsync(windowManager, aboutVm, isDialog: true);
			try
			{
				Assert.That(aboutWin, Is.TypeOf<AboutView>());
				Assert.That(aboutWin.Title, Is.EqualTo(aboutVm.DisplayName));

				var aboutTitleBlock = (TextBlock)aboutWin.FindName("AboutTitle");
				var aboutTextBox = (TextBox)aboutWin.FindName("AboutText");
				var okBtn = (Button)aboutWin.FindName("Ok");

				Assert.That(aboutTitleBlock.Text, Is.EqualTo(aboutVm.AboutTitle));
				Assert.That(aboutTextBox.Text, Does.Contain("SimpleContainer"));
				Assert.That(aboutTextBox.Text, Does.Contain("DocumentCloseBehavior"));
				Assert.That(aboutTextBox.Text, Does.Contain("CloseDocumentAsync($dataContext)"));
				Assert.That(aboutTextBox.Text, Does.Contain("Dual-Host Architecture Note"));
				Assert.That(okBtn.Content, Is.EqualTo("OK"));

				var wordCount = aboutTextBox.Text
					.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
					.Length;
				Assert.That(wordCount, Is.LessThan(300));
				AssertZeroLocalFontColorOverrides(aboutWin);
			}
			finally
			{
				aboutWin.Close();
			}
		}

		[Test]
		public async Task MainWindow_DualHostSync_IsolatesViewsMapsIdsAndKeepsHeaderTitleSafeOnNullContent()
		{
			await TestHost.SwitchToAppThread();

			using var bindingErrors = new RecordingTraceListener();
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

			var fakeWm = new FakeWindowManager();
			var mainVm = new MainWindowViewModel(
				fakeWm,
				() => new TestViewModel(fakeWm),
				() => new AboutViewModel());

			var windowManager = new TestableWindowManager();
			var mainWin = await ShowOffScreenAsync(windowManager, mainVm, isDialog: false);
			try
			{
				await WaitUntilAsync(() => mainVm.Items.Count == 4);
				await YieldDispatcherAsync();

				Assert.That(mainWin.Title, Is.EqualTo("Caliburn AvalonDock Test"));
				Assert.That(mainVm.Items, Has.Count.EqualTo(4));

				var dockManager = (DockingManager)mainWin.FindName("DockManager");
				var tabManager = (TabControl)mainWin.FindName("TabManager");

				VerifyDistinctIdsAndTitles(mainVm, dockManager);
				await VerifyNullContentHeaderSafetyAsync(dockManager);
				await VerifyDualHostViewIsolationAndLiveSyncAsync(mainVm, dockManager, tabManager);

				AssertNoUnexpectedBindingErrors(bindingErrors);
			}
			finally
			{
				PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
				mainWin.Close();
			}
		}

		[Test]
		public async Task DocumentClosePipeline_EnforcesDirtyVetoConfirmAndSerializesCloseAllPrompts()
		{
			await TestHost.SwitchToAppThread();

			using var bindingErrors = new RecordingTraceListener();
			using var generalTrace = new RecordingTraceListener();
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
			Trace.Listeners.Add(generalTrace);

			var fakeWm = new FakeWindowManager();
			var mainVm = new MainWindowViewModel(
				fakeWm,
				() => new TestViewModel(fakeWm),
				() => new AboutViewModel());

			var windowManager = new TestableWindowManager();
			var mainWin = await ShowOffScreenAsync(windowManager, mainVm, isDialog: false);
			try
			{
				await WaitUntilAsync(() => mainVm.Items.Count == 4);
				await YieldDispatcherAsync();

				var dockManager = (DockingManager)mainWin.FindName("DockManager");

				// 1. Clean close via AvalonDock's default LayoutItem.CloseCommand -> DocumentClosing -> DocumentCloseBehavior
				await mainVm.OpenDocumentAsync("Test B");
				var docB = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestB");
				dockManager.GetLayoutItemFromModel(docB).CloseCommand.Execute(null);
				await WaitUntilAsync(() => mainVm.Items.Count == 3);
				Assert.That(fakeWm.ShownPrompts, Is.Empty);

				// 2. Reopen Test B via OpenDocumentAsync and verify ContentId/Title mapping restores cleanly
				await mainVm.OpenDocumentAsync("Test B");
				await YieldDispatcherAsync();
				var reopenedDocB = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestB");
				Assert.That(reopenedDocB.Title, Is.EqualTo("Test B"));

				// 3. Dirty close veto (false) keeps Test A open in both controls
				fakeWm.ScriptedDialogResponses.Enqueue(false);
				var docA = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestA");
				dockManager.GetLayoutItemFromModel(docA).CloseCommand.Execute(null);
				await WaitUntilAsync(() => fakeWm.CompletedDialogs == 1);
				await YieldDispatcherAsync();
				Assert.That(mainVm.Items, Has.Count.EqualTo(4));

				// 4. Dirty close confirm (true) via TabManager's CloseDocumentAsync removes Test A cleanly
				fakeWm.ScriptedDialogResponses.Enqueue(true);
				var testA = mainVm.Items.OfType<TestViewModel>().Single(x => x.Id == "TestA");
				await mainVm.CloseDocumentAsync(testA);
				await YieldDispatcherAsync();
				Assert.That(mainVm.Items, Has.Count.EqualTo(3));

				// 5. Close All via LayoutItem.CloseAllCommand serializes remaining dirty prompts FIFO (MaxConcurrentDialogs == 1)
				fakeWm.ScriptedDialogResponses.Enqueue(true);
				fakeWm.ScriptedDialogResponses.Enqueue(true);
				var anyDoc = dockManager.Layout.Descendents().OfType<LayoutDocument>().First();
				dockManager.GetLayoutItemFromModel(anyDoc).CloseAllCommand.Execute(null);
				await WaitUntilAsync(() => mainVm.Items.Count == 0);
				await YieldDispatcherAsync();

				Assert.That(mainVm.Items, Is.Empty);
				Assert.That(fakeWm.MaxConcurrentDialogs, Is.EqualTo(1));

				var spuriousTryCloseLogs = generalTrace.Lines.Count(l => l.Contains("TryClose requires a parent IConductor"));
				Assert.That(spuriousTryCloseLogs, Is.EqualTo(0));
				AssertNoUnexpectedBindingErrors(bindingErrors);
			}
			finally
			{
				PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
				Trace.Listeners.Remove(generalTrace);
				mainWin.Close();
			}
		}

		[Test]
		public async Task WindowMenu_RadioCheckmarks_TrackSingleActiveDocumentAcrossPanesSplitsAndCloseAll()
		{
			await TestHost.SwitchToAppThread();

			using var bindingErrors = new RecordingTraceListener();
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;

			var fakeWm = new FakeWindowManager();
			var mainVm = new MainWindowViewModel(
				fakeWm,
				() => new TestViewModel(fakeWm),
				() => new AboutViewModel());

			var windowManager = new TestableWindowManager();
			var mainWin = await ShowOffScreenAsync(windowManager, mainVm, isDialog: false);
			try
			{
				await WaitUntilAsync(() => mainVm.Items.Count == 4);
				await YieldDispatcherAsync();

				var dockManager = (DockingManager)mainWin.FindName("DockManager");
				var tabManager = (TabControl)mainWin.FindName("TabManager");
				var topMenu = WalkVisuals(mainWin).OfType<Menu>().Single();
				var windowMenu = topMenu.Items.OfType<MenuItem>().Single(m => Equals(m.Header, "_Window"));
				var menuItems = windowMenu.Items.OfType<MenuItem>().ToList();

				Assert.That(menuItems, Has.Count.EqualTo(4));
				Assert.That(menuItems.All(mi => !mi.IsCheckable), Is.True, "IsCheckable must stay false so clicking an active item never unchecks it.");

				// 1. Initial seeded active item is Test D -> only Window D is checked
				AssertSingleCheckedWindowMenuItem(menuItems, "Window D");

				// 2. Switching via AvalonDock, TabControl, and OpenDocumentAsync updates the single checkmark in lockstep
				var docA = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestA");
				docA.IsActive = true;
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window A");

				var testC = mainVm.Items.OfType<TestViewModel>().Single(x => x.Id == "TestC");
				tabManager.SelectedItem = testC;
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window C");

				await mainVm.OpenDocumentAsync("Test B");
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window B");

				// 3. Clicking the already-active Window B menu item must keep Window B checked (never toggle off),
				// and its attached Caliburn.Micro trigger must target OpenDocumentAsync('Test B').
				var windowBItem = menuItems.Single(mi => Equals(mi.Header, "Window B"));
				var windowBTrigger = Microsoft.Xaml.Behaviors.Interaction.GetTriggers(windowBItem)
					.OfType<Microsoft.Xaml.Behaviors.EventTrigger>()
					.Single();
				var windowBAction = windowBTrigger.Actions.OfType<ActionMessage>().Single();
				Assert.That(windowBTrigger.EventName, Is.EqualTo("Click"));
				Assert.That(windowBAction.MethodName, Is.EqualTo("OpenDocumentAsync"));
				Assert.That(windowBAction.Parameters.Single().Value, Is.EqualTo("Test B"));

				windowBItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, windowBItem));
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window B");

				// 4. Splitting AvalonDock into 2 side-by-side panes (2 visible IsSelected tabs) STILL keeps <= 1 checkmark
				dockManager.GetLayoutItemFromModel(docA).NewVerticalTabGroupCommand.Execute(null);
				await YieldDispatcherAsync();
				Assert.That(dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().Count(), Is.EqualTo(2));
				Assert.That(dockManager.Layout.Descendents().OfType<LayoutDocument>().Count(d => d.IsSelected), Is.EqualTo(2));
				AssertSingleCheckedWindowMenuItem(menuItems, "Window A");

				var docB = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestB");
				docB.IsActive = true;
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window B");

				// 5. Closing all tabs leaves 0 checkmarks while keeping all 4 menu items available to reopen
				fakeWm.ScriptedDialogResponses.Enqueue(true);
				fakeWm.ScriptedDialogResponses.Enqueue(true);
				dockManager.GetLayoutItemFromModel(docB).CloseAllCommand.Execute(null);
				await WaitUntilAsync(() => mainVm.Items.Count == 0);
				await YieldDispatcherAsync();

				Assert.That(mainVm.ActiveItem, Is.Null);
				Assert.That(menuItems, Has.Count.EqualTo(4));
				Assert.That(menuItems.Count(mi => mi.IsChecked), Is.EqualTo(0));

				// 6. Reopening Test B via OpenDocumentAsync('Test B') after Close All restores the single Window B checkmark
				await mainVm.OpenDocumentAsync("Test B");
				await WaitUntilAsync(() => mainVm.Items.Count == 1);
				await YieldDispatcherAsync();
				AssertSingleCheckedWindowMenuItem(menuItems, "Window B");

				AssertNoUnexpectedBindingErrors(bindingErrors);
			}
			finally
			{
				PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
				mainWin.Close();
			}
		}
	}

	internal static class CaliburnTestHarness
	{
		internal static void AssertSingleCheckedWindowMenuItem(IReadOnlyList<MenuItem> menuItems, string expectedHeader)
		{
			var checkedItems = menuItems.Where(mi => mi.IsChecked).ToList();
			Assert.That(checkedItems, Has.Count.EqualTo(1), "At most 1 Window menu item may be checked at any time.");
			Assert.That(checkedItems[0].Header, Is.EqualTo(expectedHeader));
		}

		internal static void AssertNoUnexpectedBindingErrors(RecordingTraceListener bindingErrors)
		{
			// Exclude inbox WPF PresentationFramework.Aero2 TabItem's internal Trigger detach trace
			// (RelativeSource FindAncestor TabControl.TabStripPlacement when a TabItem leaves the visual tree).
			var unexpected = bindingErrors.Lines
				.Where(line => !(line.Contains("Path=TabStripPlacement") && line.Contains("target element is 'TabItem'")))
				.ToList();

			Assert.That(unexpected, Is.Empty, () => "WPF DataBinding errors:\n" + string.Join("\n", unexpected));
		}

		internal static void VerifyDistinctIdsAndTitles(MainWindowViewModel mainVm, DockingManager dockManager)
		{
			var expectedPairs = new (string Id, string Title)[]
			{
				("TestA", "Test A"),
				("TestB", "Test B"),
				("TestC", "Test C"),
				("TestD", "Test D"),
			};

			var layoutDocs = dockManager.Layout.Descendents().OfType<LayoutDocument>().ToList();
			Assert.That(layoutDocs, Has.Count.EqualTo(4));

			foreach (var pair in expectedPairs)
			{
				var vm = mainVm.Items.OfType<TestViewModel>().SingleOrDefault(x => x.Id == pair.Id);
				Assert.That(vm, Is.Not.Null);
				Assert.That(vm.DisplayName, Is.EqualTo(pair.Title));

				var doc = layoutDocs.SingleOrDefault(d => ReferenceEquals(d.Content, vm));
				Assert.That(doc, Is.Not.Null);
				Assert.That(doc.ContentId, Is.EqualTo(pair.Id));
				Assert.That(doc.Title, Is.EqualTo(pair.Title));
			}
		}

		internal static async Task VerifyNullContentHeaderSafetyAsync(DockingManager dockManager)
		{
			var tabItems = WalkVisuals(dockManager).OfType<LayoutDocumentTabItem>().ToList();
			Assert.That(tabItems, Has.Count.EqualTo(4));

			var docB = dockManager.Layout.Descendents().OfType<LayoutDocument>().Single(d => d.ContentId == "TestB");
			var tabItemB = tabItems.Single(t => ReferenceEquals(t.Model, docB));
			var titleBlockB = WalkVisuals(tabItemB).OfType<TextBlock>().Single(tb => tb.Name == "TitleText");

			var savedContent = docB.Content;
			try
			{
				docB.Content = null;
				await YieldDispatcherAsync();
				Assert.That(titleBlockB.Text, Is.EqualTo("Test B"));
			}
			finally
			{
				docB.Content = savedContent;
				await YieldDispatcherAsync();
			}
		}

		internal static async Task VerifyDualHostViewIsolationAndLiveSyncAsync(
			MainWindowViewModel mainVm,
			DockingManager dockManager,
			TabControl tabManager)
		{
			var dockTestView = WalkVisuals(dockManager).OfType<TestView>().First(v => v.IsVisible);
			var tabTestView = WalkVisuals(tabManager).OfType<TestView>().First(v => v.IsVisible);
			Assert.That(dockTestView, Is.Not.SameAs(tabTestView));

			var activeVm = (TestViewModel)mainVm.ActiveItem;
			Assert.That(activeVm.IsDirty, Is.False);

			var dockTextBox = (TextBox)dockTestView.FindName("CustomText");
			var tabTextBox = (TextBox)tabTestView.FindName("CustomText");
			var dockCheckBox = (CheckBox)dockTestView.FindName("IsDirty");
			var tabCheckBox = (CheckBox)tabTestView.FindName("IsDirty");

			dockTextBox.Text = "Live convention sync!";
			await YieldDispatcherAsync();

			Assert.That(activeVm.CustomText, Is.EqualTo("Live convention sync!"));
			Assert.That(tabTextBox.Text, Is.EqualTo("Live convention sync!"));
			Assert.That(activeVm.IsDirty, Is.True);
			Assert.That(dockCheckBox.IsChecked, Is.True);
			Assert.That(tabCheckBox.IsChecked, Is.True);

			tabCheckBox.IsChecked = false;
			await YieldDispatcherAsync();
			Assert.That(activeVm.IsDirty, Is.False);
			Assert.That(dockCheckBox.IsChecked, Is.False);
		}

		internal static async Task<Window> ShowOffScreenAsync(TestableWindowManager windowManager, object viewModel, bool isDialog)
		{
			var window = await windowManager.BuildWindowAsync(viewModel, isDialog);
			window.WindowStartupLocation = WindowStartupLocation.Manual;
			window.Left = -10000;
			window.Top = -10000;
			window.ShowActivated = false;
			window.Show();
			await YieldDispatcherAsync();
			return window;
		}

		internal static async Task WaitUntilAsync(Func<bool> predicate, int timeoutMs = 4000)
		{
			var sw = Stopwatch.StartNew();
			while (!predicate() && sw.ElapsedMilliseconds < timeoutMs)
			{
				await Task.Delay(25);
			}

			Assert.That(predicate(), Is.True, "Timed out waiting for asynchronous UI condition.");
		}

		internal static async Task YieldDispatcherAsync()
		{
			await Dispatcher.Yield(DispatcherPriority.ContextIdle);
		}

		internal static IEnumerable<DependencyObject> WalkVisuals(DependencyObject root)
		{
			if (root == null)
				yield break;

			yield return root;
			var count = VisualTreeHelper.GetChildrenCount(root);
			for (var i = 0; i < count; i++)
			{
				foreach (var child in WalkVisuals(VisualTreeHelper.GetChild(root, i)))
					yield return child;
			}
		}

		internal static void AssertZeroLocalFontColorOverrides(Window window)
		{
			var propsToCheck = new[]
			{
				Control.FontFamilyProperty,
				Control.FontSizeProperty,
				Control.FontWeightProperty,
				Control.FontStyleProperty,
				Control.ForegroundProperty,
				Control.BackgroundProperty,
				Control.BorderBrushProperty,
				TextBlock.FontFamilyProperty,
				TextBlock.FontSizeProperty,
				TextBlock.FontWeightProperty,
				TextBlock.ForegroundProperty,
				TextBlock.BackgroundProperty,
				Border.BackgroundProperty,
				Border.BorderBrushProperty,
				Panel.BackgroundProperty,
			};

			var localOverrides = new List<string>();
			foreach (var vis in WalkVisuals(window))
			{
				var isDirectShellElement =
					ReferenceEquals(vis, window)
					|| (vis is Grid && VisualTreeHelper.GetParent(vis) is ContentPresenter)
					|| (vis is TextBlock && VisualTreeHelper.GetParent(vis) is Grid)
					|| (vis is TextBox && VisualTreeHelper.GetParent(vis) is Grid)
					|| vis is StackPanel
					|| vis is Button;

				if (!isDirectShellElement)
					continue;

				foreach (var dp in propsToCheck)
				{
					var valueSource = System.Windows.DependencyPropertyHelper.GetValueSource(vis, dp);
					if (valueSource.BaseValueSource == BaseValueSource.Local)
						localOverrides.Add($"{window.GetType().Name}->{vis.GetType().Name}.{dp.Name}");
				}
			}

			Assert.That(localOverrides, Is.Empty, () => "Local font/color overrides found: " + string.Join(", ", localOverrides));
		}
	}

	internal sealed class TestableWindowManager : WindowManager
	{
		public Task<Window> BuildWindowAsync(object rootModel, bool isDialog)
			=> CreateWindowAsync(rootModel, isDialog, null, null);
	}

	internal sealed class FakeWindowManager : IWindowManager
	{
		public Queue<bool?> ScriptedDialogResponses { get; } = new Queue<bool?>();

		public List<string> ShownPrompts { get; } = new List<string>();

		public int MaxConcurrentDialogs { get; private set; }

		public int CompletedDialogs { get; private set; }

		private int _activeDialogs;

		public async Task<bool?> ShowDialogAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
		{
			_activeDialogs++;
			if (_activeDialogs > MaxConcurrentDialogs)
				MaxConcurrentDialogs = _activeDialogs;

			try
			{
				ShownPrompts.Add(rootModel is ConfirmCloseViewModel confirm
					? confirm.Message
					: rootModel?.GetType().Name ?? "null");
				await Task.Delay(20);
				return ScriptedDialogResponses.Count > 0 ? ScriptedDialogResponses.Dequeue() : true;
			}
			finally
			{
				_activeDialogs--;
				CompletedDialogs++;
			}
		}

		public Task ShowWindowAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
			=> Task.CompletedTask;

		public Task ShowPopupAsync(object rootModel, object context = null, IDictionary<string, object> settings = null)
			=> Task.CompletedTask;
	}

	internal sealed class RecordingTraceListener : TraceListener
	{
		public List<string> Lines { get; } = new List<string>();

		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
			if (message != null)
				Lines.Add(message);
		}
	}
}