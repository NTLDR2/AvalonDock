# CaliburnDockTestApp

A focused side-by-side synchronization and diagnostic reference sample for **AvalonDock v5** (NTLDR2 fork, `v5.1.0`) and **Caliburn.Micro 6.0.15-beta** on **`.NET Framework 4.8` (`net48`)**.

It binds an AvalonDock **`DockingManager`** (left pane) and a standard WPF **`TabControl`** (right pane, acting as a "control group") simultaneously to the exact same `Conductor<Screen>.Collection.OneActive` (`MainWindowViewModel`), proving 1:1 collection, active-tab, dirty-state, convention-binding, and async close-confirmation parity.

> [!NOTE]
> **How this sample differs from its siblings:**
> - **`CaliburnDockTestApp` (this project):** Minimal document-only **Mode-B (`DocumentsSource`)** side-by-side harness comparing `DockingManager` directly against inbox `TabControl` with zero extra theme/MVVM/serializer packages.
> - **[ExperimentalTestApp](../ExperimentalTestApp/README.md):** Full IDE-style **Mode-A (`DockLayout` + `IDockLayoutService`)** reference with 6-zone `IToolbox` panes, `IEventAggregator`, 18-theme switching, and `XmlLayoutSerializer` persistence.
> - **[ThemeSuperTestApp](../ThemeSuperTestApp/README.md):** 18-theme visual debugger, 6-tool profiler/auditor, dual-mode WPF/DWM screenshot stitcher, and Antigravity XML `AgentBridge`.

| | |
|:--|:---|
| **Target framework** | `net48` (singular `<TargetFramework>net48</TargetFramework>`, `Microsoft.NET.Sdk`) |
| **Caliburn.Micro** | NuGet `Caliburn.Micro` **6.0.15-beta** (`SimpleContainer`, actions, conventions, async `Screen`/`Conductor`, zero `System.Text.Json`) |
| **AvalonDock** | `AvalonDock` (`5.1.0`, `https://github.com/NTLDR2/AvalonDock` XAML namespace, built-in `Generic` theme) |
| **UI chrome policy** | Strictly WPF / Windows OS system-default fonts, font sizes, and colors on all dialogs and child views |
| **Automated tests** | `CaliburnDockTestAppTests` in `source/AutomationTest/AvalonDockTest/` (`Category!=FlaUI`) |

## AvalonDock v5 MVVM Modes: Mode A vs. Mode B (Why This Sample Uses Mode B)

AvalonDock v5 supports two first-class MVVM binding architectures on `DockingManager`:

| Dimension | Mode A: `DockLayout` + `IDockLayoutService` | Mode B: Direct `DocumentsSource` + `ActiveContent` *(Used Here)* |
|:----------|:--------------------------------------------|:-----------------------------------------------------------------|
| **XAML Binding Surface** | `DockLayout="{Binding DockLayout}"` (`LayoutSyncBridge` owns `DocumentsSource`, `AnchorablesSource`, and `ActiveContent`) | `DocumentsSource="{Binding Items}"` + `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` |
| **Packages Required** | `AvalonDock` + `AvalonDock.Core` + `AvalonDock.Mvvm` | `AvalonDock` (+ transitive `AvalonDock.Core` only) |
| **ViewModel Contracts** | ViewModels implement `IDockable` / `IToolbox` (bridged onto `Caliburn.Micro.Screen`) | Plain `Caliburn.Micro.Screen` ViewModels (`zero` AvalonDock interfaces in ViewModels) |
| **Tool Windows (`Anchorables`)** | First-class (`IToolDock` sides, `IToolbox` 6-zone placement, `AllowDetachedWindows` policy sync) | Not needed for document-only shells (requires manual `ActiveContent` type filtering if `AnchorablesSource` is added) |
| **Reference Sample** | [ExperimentalTestApp](../ExperimentalTestApp/README.md) & [ThemeSuperTestApp](../ThemeSuperTestApp/README.md) | **`CaliburnDockTestApp`** *(this project)* |

### Why Mode B (`DocumentsSource`) Is the Right Engineering Choice Here

1. **1:1 Control-Group Symmetry:** `CaliburnDockTestApp` exists to prove that `DockingManager` synchronizes identically to a standard WPF `TabControl` when bound to the exact same `Conductor<Screen>.Collection.OneActive`. Binding `DockManager.DocumentsSource` / `ActiveContent` alongside `TabManager.ItemsSource` / `SelectedItem` tests that contract directly without an intermediate `LayoutSyncBridge` layer.
2. **Zero Wrapper Boilerplate for Document-Only Shells:** Applications that only need tabbed, split, and floatable documents on an existing `Conductor<Screen>.Collection.OneActive` can adopt AvalonDock v5 directly in XAML without adding `AvalonDock.Mvvm` or implementing `IDockable` adapters.
3. **Clear Graduation Boundary to Mode A:** Unfiltered `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` is safe here **specifically because `CaliburnDockTestApp` hosts documents only** (no `AnchorablesSource` tool panes). As soon as an application adds dockable tool windows alongside documents, focusing a tool pane changes `DockingManager.ActiveContent` to that tool ViewModel—which an unfiltered two-way binding would push into `Conductor.ActiveItem` (causing `Conductor.EnsureItem` to add the tool into `Items`). That is the exact inflection point to either filter `ActiveContent` or graduate to **Mode A (`DockLayout` + `IDockLayoutService`)** as shown in [ExperimentalTestApp](../ExperimentalTestApp/README.md).

## Building, Running & Testing

```powershell
# Build with warnings treated as errors
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj -c Release --warnaserror

# Launch desktop app
dotnet run --project source/CaliburnDockTestApp/CaliburnDockTestApp.csproj -c Release

# Run automated STA regression tests
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj -c Release --no-build --framework net48 --filter "FullyQualifiedName~CaliburnDockTestAppTests" -m:1
```

## What Each File Demonstrates

| File | Demonstrates |
|:-----|:-------------|
| [App.xaml](App.xaml) | Hosts `Bootstrapper` in merged resources with **no `StartupUri`**. |
| [ActiveDocumentConverter.cs](ActiveDocumentConverter.cs) | Pure `IValueConverter` comparing `Conductor.ActiveItem`'s `Id` against `ConverterParameter` (`TestA`–`TestD`) so the `_Window` menu displays at most `1` native OS checkmark (`0` when all tabs are closed). |
| [Bootstrapper.cs](Bootstrapper.cs) | `BootstrapperBase` + `SimpleContainer` (`Singleton` `IWindowManager` & `MainWindowViewModel`, `PerRequest` `TestViewModel` & `AboutViewModel` so `SimpleContainer` auto-synthesizes `Func<T>` factories; Views left unregistered to avoid `SimpleContainer.GetAllInstances` double-instantiation), `SelectAssemblies()`, `Coroutine.Completed` error logging, `ViewLocator.LocateTypeForModelType` `"TabControl"` context fallback, and `await DisplayRootViewForAsync<MainWindowViewModel>()`. |
| [CaliburnLogger.cs](CaliburnLogger.cs) | Brace-safe `ILog` implementation forwarding to `System.Diagnostics.Trace` without `.NET Framework 4.8` `FormatException`s when log messages contain `{` or `}`. |
| [DocumentCloseBehavior.cs](DocumentCloseBehavior.cs) | `Behavior<DockingManager>` cancelling synchronous `DocumentClosing` (`e.Cancel = true`) and FIFO-queuing `IConductor.CloseItemAsync(child)` so tab `X`, middle-click, `Close All`, and floating-window closes await `CanCloseAsync` one prompt at a time. |
| [ViewModels/ViewModelBase.cs](ViewModels/ViewModelBase.cs) | Pure UI-agnostic `Screen` base (`0` `System.Windows` references) with `Id`, `IsDirty`, `CustomText`, `ILog` lifecycle logging, and async `CanCloseAsync` awaiting `IWindowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName))`. |
| [ViewModels/TestViewModel.cs](ViewModels/TestViewModel.cs) | `PerRequest` document ViewModel receiving `IWindowManager` via constructor injection. |
| [ViewModels/MainWindowViewModel.cs](ViewModels/MainWindowViewModel.cs) | `Conductor<Screen>.Collection.OneActive` shell injected with `IWindowManager`, `Func<TestViewModel>`, and `Func<AboutViewModel>`, exposing `OpenDocumentAsync` (`Id` `"TestA"`–`"TestD"` vs. `DisplayName` `"Test A"`–`"Test D"`), `CloseDocumentAsync`, `ShowAboutAsync`, and staggered UI-thread seeding (`Execute.OnUIThreadAsync(StartInitialDocumentsAsync)`). |
| [ViewModels/ConfirmCloseViewModel.cs](ViewModels/ConfirmCloseViewModel.cs), [Views/ConfirmCloseView.xaml](Views/ConfirmCloseView.xaml) | System-default `CenterOwner` modal dirty-close confirmation dialog using 100% pure `x:Name` conventions (`Window.Title` ↔ `DisplayName`, `Message`, `Confirm` → `ConfirmAsync`, `Cancel` → `CancelAsync`). |
| [ViewModels/AboutViewModel.cs](ViewModels/AboutViewModel.cs), [Views/AboutView.xaml](Views/AboutView.xaml) | System-default `CenterOwner` modal `"What's This App About"` dialog (`< 300` words) using pure `x:Name` conventions (`AboutTitle`, read-only `AboutText` `OneWay`, `Ok` → `OkAsync` avoiding `Window.Close()` `CS0108` hiding). |
| [Views/TestView.xaml](Views/TestView.xaml) | Document editor `UserControl` using pure `x:Name` conventions (`IsDirty` `TwoWay` + `CustomText` `TwoWay` with `UpdateSourceTrigger.PropertyChanged`). |
| [Views/MainWindowView.xaml](Views/MainWindowView.xaml) | Side-by-side `<avalonDock:DockingManager>` (`https://github.com/NTLDR2/AvalonDock`) + `<TabControl>` (`http://caliburnmicro.com`), concise `cal:Message.Attach` shorthand for menu literals and item `DataTemplate`s (`CloseDocumentAsync($dataContext)`), implicit `ViewModelBase` `DataTemplate`, `Model.Id` → `ContentId` + `Model.DisplayName` → `Title`, and teardown-safe `{Binding Title}` headers. |

## Core Synchronization & Convention Takeaways

1. **Dual-Host `ViewAware` Isolation (`cal:View.Context`):** Caliburn.Micro caches one `UIElement` per context key in `ViewAware.Views`. `DockManager` uses the default (`null`) context via an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">`, while `TabManager` sets `cal:View.Context="TabControl"` backed by a `ViewLocator.LocateTypeForModelType` fallback in `Bootstrapper.Configure()`, keeping two live `TestView` instances synchronized without visual theft.
2. **Pure `x:Name` Conventions vs. `cal:Message.Attach` Boundaries:** Top-level views (`TestView`, `ConfirmCloseView`, `AboutView`) use 100% `x:Name` conventions (including `Async`-suffix action stripping and automatic `Window.Title` ↔ `DisplayName` binding), while `cal:Message.Attach` is reserved for literal arguments (`OpenDocumentAsync('Test A')`) and repeating item `DataTemplate`s (`CloseDocumentAsync($dataContext)`).
3. **Unified Async Close Pipeline (`Zero ICommand`):** Leaving `LayoutItem.CloseCommand` unset lets AvalonDock raise `DocumentClosing` across all close gestures, where `DocumentCloseBehavior` cancels synchronously and FIFO-queues `IConductor.CloseItemAsync`—eliminating custom `RelayCommand` boilerplate and avoiding `Screen.TryCloseAsync`'s spurious `TryClose requires a parent IConductor...` warning on child `UserControl` tabs.
4. **Identity (`Id`) vs. Presentation (`DisplayName`) & Teardown-Safe Headers:** `LayoutItemContainerStyle` maps `Model.Id` (`"TestA"`) → `ContentId` and `Model.DisplayName` (`"Test A"`) → `Title`. `DocumentHeaderTemplate` binds `{Binding Title}` directly on `LayoutDocument` so headers never flash `#ERROR#` when AvalonDock clears `LayoutDocument.Content = null` prior to visual teardown.
5. **Radio-Exclusive `_Window` Menu Checkmark (`IsCheckable="False"`):** Each `_Window` `MenuItem` binds `IsChecked` (`Mode=OneWay`) to `MainWindowViewModel.ActiveItem` via `ActiveDocumentConverter` while leaving `IsCheckable="False"` (so clicking the already-active item never toggles its checkmark off). Because `ActiveItem` holds a single reference, at most `1` item is ever checked—even across side-by-side split tab groups or floating windows—and `0` when all tabs close, independent of the parent menu's header name. *(For dynamic $N$-item menus, bind `<MenuItem ItemsSource="{Binding ...}">` with `ItemContainerStyle` as shown in `ExperimentalTestApp`.)*

## Companion Documentation

- **Scoped AI Coding Rules:** [AGENTS.md](AGENTS.md)
- **Technical Spec & Applied Fixes (Iterations 1–10):** [99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md](99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md)
- **AvalonDock v5 Guides:** [documentation/guides/caliburn-micro.md](../../documentation/guides/caliburn-micro.md), [documentation/guides/mvvm.md](../../documentation/guides/mvvm.md), [AGENTS.md](../../AGENTS.md)
- **Caliburn.Micro Reference:** [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro) (`AGENTS.md` and `docs/`)