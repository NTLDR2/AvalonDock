# CaliburnDockTestApp — Updated Technical Documentation & Applied Fixes

> **Status:** Technical Specification & Incremental Modernization Log (Iterations `1`–`10`)  
> **Companion Quick-Start Guide:** [README.md](README.md)  
> **Location:** `source/CaliburnDockTestApp/`  
> **Target Framework:** `.NET Framework 4.8` (`<TargetFramework>net48</TargetFramework>`, singular)  
> **MSBuild SDK:** `Microsoft.NET.Sdk` (`<UseWPF>true</UseWPF>`)  
> **Pinned Frameworks:** `Caliburn.Micro` `6.0.15-beta` + ProjectReference to `AvalonDock` (`5.1.0`)

This document specifies the architecture and runtime behavior of **`CaliburnDockTestApp`** alongside the complete log of incremental modernization fixes applied across Iterations `1`–`10`.

---

## Table of Contents

- [Applied Fixes Log](#applied-fixes-log)
1. [Purpose & Historical Provenance](#1-purpose--historical-provenance)
2. [Project Configuration & Dependencies](#2-project-configuration--dependencies)
3. [Complete File-by-File Inventory](#3-complete-file-by-file-inventory)
4. [Current Runtime Behavior](#4-current-runtime-behavior)
   - [4.1 Startup & Bootstrapper Sequence](#41-startup--bootstrapper-sequence)
   - [4.2 Asynchronous UI-Thread Document Seeding (`StartInitialDocumentsAsync`)](#42-asynchronous-ui-thread-document-seeding-startinitialdocumentsasync)
   - [4.3 Side-by-Side Dual-Host Rendering & `View.Context` Isolation](#43-side-by-side-dual-host-rendering--viewcontext-isolation)
   - [4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem`)](#44-active-tab-synchronization-activecontent--activeitem--selecteditem)
   - [4.5 Dirty-State Tracking & Bidirectional Live Editing](#45-dirty-state-tracking--bidirectional-live-editing)
   - [4.6 Document Closing, Async Confirmation Dialogs, Floating Windows & App Exit](#46-document-closing-async-confirmation-dialogs-floating-windows--app-exit)
   - [4.7 Reopening Documents via the `_Window` Menu (`Window A`–`Window D`)](#47-reopening-documents-via-the-_window-menu-window-awindow-d)
   - [4.8 `Help -> What's This App About...` Modal Dialog](#48-help---whats-this-app-about-modal-dialog)
   - [4.9 Diagnostic Trace Output](#49-diagnostic-trace-output)
5. [Resolved Caliburn.Micro Deprecations & MVVM Anti-Patterns](#5-resolved-caliburnmicro-deprecations--mvvm-anti-patterns)
6. [Remaining Legacy, Misused & Superseded AvalonDock Usage](#6-remaining-legacy-misused--superseded-avalondock-usage)
7. [Summary Matrix of Current Behavior & Remaining Quirks](#7-summary-matrix-of-current-behavior--remaining-quirks)

---

## Applied Fixes Log

| Iteration | File(s) Modified | Change Applied | Rationale & Verification |
|:----------|:-----------------|:---------------|:-------------------------|
| **1** | `CaliburnDockTestApp.csproj` (line 5) | Changed plural `<TargetFrameworks>net48</TargetFrameworks>` (and `HEAD`'s `net8.0-windows;net48`) to explicitly singular `<TargetFramework>net48</TargetFramework>`. | Prevents accidental multi-targeting in `CaliburnDockTestApp` and eliminates MSBuild's outer multi-target dispatch pass. Verified via `dotnet msbuild -getProperty:TargetFramework -getProperty:TargetFrameworks` (`"TargetFramework": "net48"`, `"TargetFrameworks": ""`) and `Release --warnaserror` build (`0 Warning(s), 0 Error(s)`). |
| **2.1** | `CaliburnDockTestApp.csproj` (line 1) | Updated MSBuild SDK moniker from `<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">` to `<Project Sdk="Microsoft.NET.Sdk">` (retaining `<TargetFramework>net48</TargetFramework>` and `<UseWPF>true</UseWPF>`). | Replaces the legacy .NET Core 3.x `WindowsDesktop` SDK moniker with the standard .NET 5+ / .NET 8 SDK convention where `Microsoft.NET.Sdk` automatically imports WPF desktop targets whenever `<UseWPF>true</UseWPF>` is set (including on `net48`). |
| **2.2** | `ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Exposed `public RelayCommand ShowDCommand { get { return _showDCommand; } }` for the existing `_showDCommand` field and added `<MenuItem Header="Window D" Command="{Binding ShowDCommand}"/>` under `_Window`. | Completes unfinished baseline wiring so all four seeded documents (`Test A`–`Test D`) can be activated and reopened from the `_Window` menu after being closed, eliminating the dead `_showDCommand` field. |
| **2.3** | `ViewModels/MainWindowViewModel.cs` | Removed redundant `item.Parent = this;` and `item.ConductWith(this);` inside `ActivateOrCreate<T>`. | `MainWindowViewModel` (`Conductor<Screen>.Collection.OneActive`) already sets `item.Parent = this` and conducts items via `ActivateItemAsync(item, ...)`. Removing `ConductWith(this)` eliminates duplicate activation/deactivation wiring and fixes the event-handler memory leak where `MainWindowViewModel.Activated` and `Deactivated` retained every closed `TestViewModel` instance for the lifetime of the shell. |
| **3.1** | `CaliburnDockTestApp.csproj`<br>`ViewModels/ViewModelBase.cs`<br>`ViewModels/MainWindowViewModel.cs` | Removed custom `<DefineConstants>` overrides (`CALIBURN_ASYNC`) from `CaliburnDockTestApp.csproj`, unwrapped `#if CALIBURN_ASYNC`, and deleted the dead synchronous Caliburn.Micro 3.2 `#else` branches in `ViewModelBase` and `MainWindowViewModel`. | Restores standard `Microsoft.NET.Sdk` preprocessor symbols (`TRACE;DEBUG;NETFRAMEWORK;NET48;` in Debug and `TRACE;RELEASE;NETFRAMEWORK;NET48;` in Release) that were previously overwritten by hardcoded `<DefineConstants>` tags, and removes ~45 lines of uncompilable Caliburn.Micro 3.x legacy code. |
| **3.2** | `ViewModels/MainWindowViewModel.cs` | Replaced `Task.Run(() => StartInitialDocuments());` with `_ = Execute.OnUIThreadAsync(StartInitialDocuments);` inside `OnInitializeAsync`. | Eliminates off-UI-thread `Items` queries, unsynchronized `_createCount` mutations, background `TestViewModel` allocations, and cross-thread `ActivateItemAsync` calls while preserving the non-blocking staggered tab seeding (`+100 ms`..`+400 ms`) on the STA UI `Dispatcher` thread. |
| **4.1** | `Views/MainWindowView.xaml` | Replaced `<DockingManager.LayoutItemTemplate>` (`cal:View.Model="{Binding Content}"`) with an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` inside `<DockingManager.Resources>` using `<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>`. | Fixes the blank `AvalonDock` document body (`BindingExpression path error: 'Content' property not found on 'object' ''TestViewModel''`) across both `Generic` and non-Generic AvalonDock themes by leaving `LayoutItemTemplate` unset so `LayoutItem.View` resolves the implicit `DataTemplate` with `DataContext = TestViewModel`. |
| **4.2** | `Views/MainWindowView.xaml`<br>`Bootstrapper.cs` | Added `cal:View.Context="TabControl"` to `TabManager`'s `ContentTemplate` and overrode `Bootstrapper.Configure()` to add a default-view fallback to `ViewLocator.LocateTypeForModelType` (with future AvalonDock v5 + Caliburn.Micro v6-beta stress-test notes). | Prevents `ViewAware` view-theft between `DockManager` (which keeps `View.DefaultContext`, preserving parameterless `Screen.GetView()`) and `TabManager` (`Views["TabControl"]`), enabling both controls to render live, independent, synchronized `TestView` instances simultaneously—even when tabs are floated. |
| **5** | `ViewModels/AboutViewModel.cs` *(new)*<br>`Views/AboutView.xaml` *(new)*<br>`Views/AboutView.xaml.cs` *(new)*<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Added `Help -> What's This App About...` menu item wired via Caliburn.Micro v5/v6-compatible leaf `cal:Message.Attach="[Event Click] = [Action ShowAbout]"` to `IWindowManager.ShowDialogAsync(new AboutViewModel())`, opening a `CenterOwner` modal `AboutView` window (~234-word architectural summary). | Makes the side-by-side diagnostic purpose and dual-host `View.Context` design self-evident both interactively for humans and in-source for AI coding agents, while modeling idiomatic Caliburn.Micro v5/v6 dialog & action patterns (constructor-injectable `IWindowManager`, `Screen.TryCloseAsync`, leaf `cal:Message.Attach`, zero `MessageBox.Show` or `RelayCommand`). |
| **6.1** | `CaliburnDockTestApp.csproj` (line 10) | Upgraded `<PackageReference Include="Caliburn.Micro" Version="4.0.136-rc" />` to `Version="6.0.15-beta"`. | Replaces the pre-release 2020 `4.0.136-rc` package with `Caliburn.Micro` `6.0.15-beta` (`Caliburn.Micro.Core` `6.0.15-beta`, `Caliburn.Micro.Platform` `6.0.15-beta`, `Caliburn.Micro.Platform.Core` `6.0.15-beta`, and `Microsoft.Xaml.Behaviors.Wpf` `1.1.135`), verified on `net48` with **zero** `System.Text.Json` assemblies in the build output. |
| **6.2** | `Bootstrapper.cs` (lines 19–33) | Changed `protected override void OnStartup` + unawaited `DisplayRootViewFor<MainWindowViewModel>()` to `protected override async void OnStartup` + `await DisplayRootViewForAsync<MainWindowViewModel>()`, and updated `Configure()`'s inline comment to note verification on `Caliburn.Micro 6.0.15-beta`. | Migrates off `DisplayRootViewFor<T>()` (removed after `4.0.136-rc`) and properly awaits root window activation so startup exceptions are observed on the UI `DispatcherSynchronizationContext`. |
| **6.3** | `ViewModels/MainWindowViewModel.cs` (lines 88–96) | Replaced `protected override Task OnInitializeAsync(CancellationToken)` (and its `base.OnInitializeAsync` call and trace string) with `protected override Task OnInitializedAsync(CancellationToken)` (`base.OnInitializedAsync`). | Eliminates `[Obsolete("Override OnInitializedAsync")]` (`CS0672`), compiling cleanly with `0 Warning(s), 0 Error(s)` under `Directory.Build.props`'s `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. |
| **7** | `Views/AboutView.xaml` | Removed all custom colors, brushes, border overrides, font sizes, and font weights (`Background="#F0F2F5"`, `Background="#F6F8FA"`, `BorderBrush="#D0D7DE"`, `Background="Transparent"`, `BorderThickness="0"`, `FontSize="14"`, `FontWeight="SemiBold"`, `FontSize="12.5"`, explicit `Window.Background`, and fixed `Button` `Height="26"`), replacing them with a 3-row `<Grid Margin="12">` using pure WPF/OS system defaults (`TextBlock`, native read-only `TextBox` with `Padding="8"` and `TextWrapping="Wrap"`, and `MinWidth="75"` `Padding="12,3"` `OK` `Button`). | Ensures `AboutView` strictly honors OS system fonts, system font sizes/scaling, system colors, and Windows High Contrast themes with zero hard-coded brushes or font overrides. |
| **8.1** | `Bootstrapper.cs`<br>`CaliburnLogger.cs`<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Configured `SimpleContainer` in `Bootstrapper` (`Singleton<IWindowManager, WindowManager>()`, `Singleton<MainWindowViewModel>()`, `GetInstance`/`GetAllInstances`/`BuildUp`, plus `Coroutine.Completed` error logging, keeping Views unregistered to avoid `SimpleContainer.GetAllInstances` double-instantiation), added brace-safe `FormatMessage` in `CaliburnLogger`, and replaced `_showACommand`–`_showDCommand` with `public Task OpenDocument(string displayName)` wired via leaf `cal:Message.Attach="[Event Click] = [Action OpenDocument('Test A')]"`..`'Test D'`. | Replaces fire-and-forget menu `ICommand` fields with observed `TaskResult` coroutines (`Coroutine.BeginExecute`), wires inbox Caliburn.Micro DI (`SimpleContainer`) with zero external packages, and prevents `.NET Framework 4.8` `DefaultTraceListener` `FormatException`s when action/binding log strings contain `{` or `}`. |
| **8.2** | `DocumentCloseBehavior.cs` *(new)*<br>`ViewModels/MainWindowViewModel.cs`<br>`ViewModels/ViewModelBase.cs`<br>`Views/MainWindowView.xaml`<br>`RelayCommand.cs` *(deleted)* | Added `DocumentCloseBehavior : Behavior<DockingManager>` (cancelling synchronous `DocumentClosing` with `e.Cancel = true` and FIFO-queuing `IConductor.CloseItemAsync(child)` via `_pending`), removed `<Setter Property="CloseCommand" Value="{Binding Model.CloseCommand}"/>` from `LayoutItemContainerStyle`, replaced `TabManager` `CloseTabButton`'s `Command` with `cal:Message.Attach="[Event Click] = [Action CloseDocument($dataContext)]"`, removed `ViewModelBase.CloseCommand`, and deleted `RelayCommand.cs`. | Completely eliminates custom `ICommand` / global-namespace `RelayCommand` boilerplate, keeps ViewModels 100% UI-control-agnostic, restores AvalonDock's `DocumentClosing` lifecycle across tab `X`, middle-click, `Close All`, and floating windows (serialized one prompt at a time), and eliminates `Screen.TryCloseAsync`'s spurious `TryClose requires a parent IConductor...` trace warning on tab closes. |
| **8.3** | `ViewModels/ConfirmCloseViewModel.cs` *(new)*<br>`Views/ConfirmCloseView.xaml` *(new)*<br>`Views/ConfirmCloseView.xaml.cs` *(new)*<br>`ViewModels/ViewModelBase.cs`<br>`ViewModels/TestViewModel.cs`<br>`ViewModels/MainWindowViewModel.cs` | Added modal `ConfirmCloseViewModel` / `ConfirmCloseView` (`CenterOwner` `Window` using strictly WPF/OS system defaults with `_Yes` / `_No` action buttons), injected `IWindowManager` into `ViewModelBase` and `TestViewModel`, replaced synchronous `MessageBox.Show(...)` in `ViewModelBase.CanCloseAsync` with `await _windowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName))`, and removed `using System.Windows;` from `ViewModelBase.cs`. | Decouples all ViewModels from `System.Windows`, centers dirty-close prompts over whichever window is active (`MainWindowView` or torn-off `LayoutDocumentFloatingWindowControl` via `WindowManager.InferOwnerOf`), honors strict OS/WPF system fonts and colors (`0` `BaseValueSource.Local` overrides), and makes `CanCloseAsync` 100% unit-testable with a mock `IWindowManager`. |
| **8.4** | `Bootstrapper.cs`<br>`ViewModels/MainWindowViewModel.cs`<br>`ViewModels/ViewModelBase.cs`<br>`ViewModels/TestViewModel.cs` | Registered `PerRequest<TestViewModel>()` and `PerRequest<AboutViewModel>()` in `Bootstrapper.Configure()`, replaced `Activator.CreateInstance` and containerless `?? new WindowManager()` constructors with explicit constructor-injected `Func<TestViewModel>` and `Func<AboutViewModel>` auto-factories (synthesized by `SimpleContainer`), set `DisplayName = "Caliburn AvalonDock Test"` in `MainWindowViewModel`, and routed all ViewModel lifecycle logging through `LogManager.GetLog(...)` (`ILog`) instead of raw `System.Diagnostics.Trace`. | Eliminates `Activator.CreateInstance`, `new WindowManager()`, and `System.Diagnostics` from `ViewModels/`, modeling textbook Caliburn.Micro `SimpleContainer` DI (`Singleton` services/shell + `PerRequest` transient ViewModels + `Func<T>` factories, while keeping runtime-parameterized `ConfirmCloseViewModel(string)` directly constructed). |
| **8.5** | `ViewModels/MainWindowViewModel.cs`<br>`ViewModels/ConfirmCloseViewModel.cs`<br>`ViewModels/AboutViewModel.cs`<br>`Views/TestView.xaml`<br>`Views/ConfirmCloseView.xaml`<br>`Views/AboutView.xaml`<br>`Views/MainWindowView.xaml` | Renamed `Task`-returning actions to `*Async` (`OpenDocumentAsync`, `CloseDocumentAsync`, `ShowAboutAsync`, `ConfirmAsync`, `CancelAsync`, `OkAsync`, `StartInitialDocumentsAsync`), replaced manual `{Binding}`s and verbose `[Event Click] = [Action ...]` markup in `TestView.xaml`, `ConfirmCloseView.xaml`, and `AboutView.xaml` with pure `x:Name` conventions (`IsDirty`, `CustomText`, `Message`, `Confirm`, `Cancel`, `AboutTitle`, `AboutText`, `Ok`), omitted explicit `Window.Title` attributes so `WindowManager.CreateWindowAsync` binds `DisplayName` across all 3 windows, switched `MainWindowView.xaml` to `xmlns:cal="http://caliburnmicro.com"` with concise `cal:Message.Attach` shorthand (`OpenDocumentAsync('Test A')`..`'Test D'`, `ShowAboutAsync`, `CloseDocumentAsync($dataContext)`), and added inline AI-reference comments explaining each convention boundary. | Showcases Caliburn.Micro's core convention-over-configuration design (`TwoWay` + `UpdateSourceTrigger.PropertyChanged` on `TextBox`, `OneWay` on get-only properties, `BindActions` `Async`-suffix stripping before `BindProperties`, avoiding `x:Name="Close"` `CS0108` collisions on `Window` subclasses, and where `cal:Message.Attach` is needed for literal arguments and item `DataTemplate`s). |
| **8.6** | `ViewModels/AboutViewModel.cs` | Refreshed `AboutText` (~208 words, strictly `< 300` words) to document the modernized `AvalonDock 5.1.0` + `Caliburn.Micro 6.0.15-beta` stack, `SimpleContainer` (`Singleton` shell/services + `PerRequest` ViewModels with `Func<T>` auto-factories, zero `ICommand` or `MessageBox.Show`), pure `x:Name` conventions (`PropertyChanged` text sync, `Async`-suffix stripping, `Window.Title` ↔ `DisplayName`) vs. `cal:Message.Attach` shorthand, and the unified `DocumentCloseBehavior` + `CloseDocumentAsync($dataContext)` → `IWindowManager.ShowDialogAsync(ConfirmCloseViewModel)` close pipeline alongside the dual-host `View.Context` note. | Keeps the interactive `Help -> What's This App About...` modal dialog 100% synchronized with the live codebase for both human developers and AI coding agents. |
| **9.1** | `ViewModels/ViewModelBase.cs`<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Declared the canonical fork XAML namespace `xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"` (`<avalonDock:DockingManager>`, `<avalonDock:LayoutItem>`), flattened `<Window.Resources>` (removing redundant nested `MergedDictionaries` and empty `IsFocused` trigger), updated `DocumentHeaderTemplate` `TitleText` to bind `{Binding Title}` (`ToolTip="{Binding Title}"`) and removed `FallbackValue=#ERROR#` across both panes, added observable `Id` on `ViewModelBase` (`"TestA"`–`"TestD"` in `OpenDocumentAsync`, distinct from `DisplayName` `"Test A"`–`"Test D"`), and mapped `<Setter Property="ContentId" Value="{Binding Model.Id, Mode=OneWay}"/>` in `LayoutItemContainerStyle` with AI-reference XML comments. | Eliminates implicit WPF-namespace piggybacking, fixes transient `#ERROR#` header flashes when AvalonDock sets `LayoutDocument.Content = null` prior to visual teardown, and models AvalonDock v5's canonical separation between logical identity (`Model.Id` → `LayoutItem.ContentId` → `LayoutDocument.ContentId`) and presentation (`Model.DisplayName` → `LayoutItem.Title` → `LayoutDocument.Title`). |
| **9.2** | `Bootstrapper.cs`<br>`README.md` *(new)*<br>`../AvalonDock.sln`<br>`../AutomationTest/AvalonDockTest/AvalonDockTest.csproj`<br>`../AutomationTest/AvalonDockTest/CaliburnDockTestAppTests.cs` *(new)*<br>`../../ROADMAP.md`<br>`../../REPO_MAP.md`<br>`../../AGENTS.md` | Added `CaliburnDockTestApp.csproj` to `source/AvalonDock.sln` under `"Test Projects"`, overrode `Bootstrapper.SelectAssemblies()` to return `typeof(Bootstrapper).Assembly` for test-runner `AppDomain` safety, promoted the STA verification harness into `CaliburnDockTestAppTests` (`AutomationTestBase` + `TestHost.SwitchToAppThread()` + Caliburn.Micro static-hook restore), added `source/CaliburnDockTestApp/README.md`, and synchronized `ROADMAP.md`, `REPO_MAP.md`, and `AGENTS.md`. | Ensures `dotnet build source/AvalonDock.sln --warnaserror` and `dotnet test ... --filter "Category!=FlaUI" -m:1` continuously guard `CaliburnDockTestApp` without `Application.Current` or Caliburn.Micro static-state leaks, completing `CaliburnDockTestApp`'s Phase 4 modernization. |
| **9.3** | `App.xaml`<br>`App.xaml.cs`<br>`Bootstrapper.cs`<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml`<br>`Views/MainWindowView.xaml.cs`<br>`Views/TestView.xaml.cs`<br>`README.md` | Added inline `//` and XML architectural comments in `MainWindowViewModel.cs` and `MainWindowView.xaml` plus a dedicated **Mode A (`DockLayout`) vs. Mode B (`DocumentsSource`)** section in `README.md` explaining why Mode B is intentionally chosen here (and the exact `AnchorablesSource` graduation boundary to Mode A), replaced the stale Iteration 4.2 comment in `Bootstrapper.cs`, added the `No StartupUri` comment in `App.xaml`, and trimmed unused 2020 Visual Studio template `using` directives in `App.xaml.cs`, `MainWindowView.xaml.cs`, and `TestView.xaml.cs`. | Completes AI-training-ready architectural documentation right inside the source files and `README.md` while eliminating the last cosmetic boilerplate remnants. |
| **10** | `ActiveDocumentConverter.cs` *(new)*<br>`Views/MainWindowView.xaml`<br>`ViewModels/AboutViewModel.cs`<br>`README.md`<br>`../AutomationTest/AvalonDockTest/CaliburnDockTestAppTests.cs` | Added `ActiveDocumentConverter : IValueConverter` (`(value as ViewModelBase)?.Id == parameter`) and bound `IsChecked="{Binding ActiveItem, Converter={StaticResource ActiveDocumentConverter}, ConverterParameter=TestA..TestD, Mode=OneWay}"` (`IsCheckable="False"` default) on `Window A`–`Window D` in `MainWindowView.xaml`, updated `AboutText` and `README.md`, and added `WindowMenu_RadioCheckmarks_TrackSingleActiveDocumentAcrossPanesSplitsAndCloseAll` in `CaliburnDockTestAppTests.cs`. | Synchronizes the `_Window` menu visually with `Conductor.ActiveItem` / `DockManager.ActiveContent` / `TabManager.SelectedItem` using native OS menu checkmarks, guaranteeing `<= 1` checked item across tab switches, side-by-side split tab groups, and floating windows (`0` when all tabs close) without unchecking on repeat clicks or coupling to parent menu header names. |

---

## 1. Purpose & Historical Provenance

`CaliburnDockTestApp` was originally created in August 2020 (`commit c1be099`, merged in PR `#185` — *"Fix ObservableCollection Reset notification handling"*) during the AvalonDock v4 cycle as a minimal side-by-side diagnostic harness.

Its goal is to compare **AvalonDock's `DockingManager`** (left half of the window) directly against a standard **WPF `TabControl`** (right half of the window) when **both controls are bound simultaneously to the exact same Caliburn.Micro `Conductor<Screen>.Collection.OneActive` ViewModel (`MainWindowViewModel`)**:

- **`DocumentsSource="{Binding Items}"`** (on `DockingManager`) vs **`ItemsSource="{Binding Items}"`** (on `TabControl`)
- **`ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`** (on `DockingManager`) vs **`SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`** (on `TabControl`)
- Simultaneous side-by-side `TestView` rendering (`IsDirty` checkbox + `Custom Text` textbox) synchronized in real time across both panes *(restored in Iteration 4)*
- Per-tab dirty state (`IsDirty`) rendered as a red dot (`Ellipse`) in both tab headers
- Cancellable async close confirmation (`IWindowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName))` inside `Screen.CanCloseAsync`) routed through `DocumentCloseBehavior` (`DockingManager.DocumentClosing`) and `CloseDocumentAsync($dataContext)` (`TabControl`) with zero `ICommand` / `RelayCommand` or `MessageBox.Show` boilerplate *(modernized in Iteration 8)*
- Re-activating or re-creating tabs from a top `_Window` menu (`OpenDocumentAsync('Test A')`–`'Test D'`) and inspecting the app's architectural rationale from **`Help -> What's This App About...`** *(added in Iteration 5; modernized in Iterations 8.1–8.5)*

`CaliburnDockTestApp` is included in `source/AvalonDock.sln` (under `"Test Projects"`), compiles cleanly (`0 Warning(s), 0 Error(s)`) against the repository's live `AvalonDock` (`5.1.0`) assembly and `Caliburn.Micro` (`6.0.15-beta`) on `net48` (with zero `System.Text.Json` dependencies), is guarded by `CaliburnDockTestAppTests` in `source/AutomationTest/AvalonDockTest/`, and intentionally exercises classic AvalonDock `DocumentsSource`/`ActiveContent` bindings alongside `TabControl` (rather than `AvalonDock.Mvvm` / `DockLayout`).

---

## 2. Project Configuration & Dependencies

Defined in `CaliburnDockTestApp.csproj` (inheriting repository-wide settings from `source/Directory.Build.props`):

| Setting / Dependency | Value | Notes |
|:---------------------|:------|:------|
| **MSBuild SDK** | `Microsoft.NET.Sdk` *(Fixed — Iteration 2.1)* | Updated from legacy `Microsoft.NET.Sdk.WindowsDesktop`; imports WPF desktop build targets via `<UseWPF>true</UseWPF>` on `net48`. |
| **Output Type** | `WinExe` | Desktop WPF executable (`CaliburnDockTestApp.exe`). |
| **Target Framework** | `net48` *(Fixed — Iteration 1)* | Explicitly singular `<TargetFramework>net48</TargetFramework>` (updated from legacy plural `<TargetFrameworks>` so MSBuild runs a single-target `net48` build with no multi-target wrapper). |
| **Preprocessor Constants** | Standard SDK defaults *(Fixed — Iteration 3.1)* | Custom `<DefineConstants>` blocks (`CALIBURN_ASYNC`) removed so `Microsoft.NET.Sdk` provides standard `TRACE;DEBUG;NETFRAMEWORK;NET48;` (Debug) and `TRACE;RELEASE;NETFRAMEWORK;NET48;` (Release) constants. |
| **`Caliburn.Micro`** | `6.0.15-beta` (NuGet) *(Upgraded — Iteration 6.1)* | Pulls `Caliburn.Micro.Core` (`6.0.15-beta`, `netstandard2.0`, including `SimpleContainer`), `Caliburn.Micro.Platform` + `Caliburn.Micro.Platform.Core` (`6.0.15-beta`, `net462`), and `Microsoft.Xaml.Behaviors.Wpf` (`1.1.135`)—with **zero** `System.Text.Json` dependencies. |
| **`AvalonDock`** | `..\Components\AvalonDock\AvalonDock.csproj` | Brings `AvalonDock.dll` (`5.1.0`) and transitively `AvalonDock.Core.dll`. |
| **Not Referenced** | `AvalonDock.Themes.*`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, `AvalonDock.Serializer.Xml` | Uses AvalonDock's built-in fallback `Themes/generic.xaml`, `DocumentsSource` binding, Caliburn.Micro's built-in `SimpleContainer` *(configured in Iterations 8.1 & 8.4)*, and no XML layout persistence. |
| **Static Analysis** | `Directory.Build.props` (`LangVersion 12`, `TreatWarningsAsErrors=true`) | Runs `NetAnalyzers` and `SonarAnalyzer.CSharp`, but **skips `StyleCop.Analyzers`** because `Directory.Build.props` exempts projects whose name contains `"Test"`. |

To build and launch from the repository root:

```powershell
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Debug --warnaserror
.\source\CaliburnDockTestApp\bin\Debug\net48\CaliburnDockTestApp.exe
```

---

## 3. Complete File-by-File Inventory

`CaliburnDockTestApp` consists of **22 files** (`21` source/configuration files + `README.md`; `RelayCommand.cs` deleted in Iteration 8.2):

```text
source/CaliburnDockTestApp/
├── ActiveDocumentConverter.cs      # IValueConverter matching ActiveItem.Id for single-active Window menu checkmarks
├── App.xaml                        # Application resources; instantiates Bootstrapper (no StartupUri)
├── App.xaml.cs                     # Empty Application code-behind partial class
├── AssemblyInfo.cs                 # WPF [assembly: ThemeInfo(None, SourceAssembly)]
├── Bootstrapper.cs                 # BootstrapperBase + SimpleContainer, Coroutine error log, ViewLocator fallback & MainWindowViewModel
├── CaliburnDockTestApp.csproj      # Microsoft.NET.Sdk WPF project targeting singular net48 with Caliburn.Micro 6.0.15-beta
├── CaliburnLogger.cs               # Brace-safe Caliburn.Micro.ILog forwarding to System.Diagnostics.Trace
├── DocumentCloseBehavior.cs        # Behavior<DockingManager> cancelling DocumentClosing and FIFO-queuing IConductor.CloseItemAsync
├── ViewModels/
│   ├── AboutViewModel.cs           # PerRequest modal Screen presenting the <300-word architectural summary (OkAsync)
│   ├── ConfirmCloseViewModel.cs    # Modal Screen presenting the Yes/No dirty-close confirmation prompt (ConfirmAsync / CancelAsync)
│   ├── MainWindowViewModel.cs      # Singleton Conductor<Screen>.Collection.OneActive with Func<T> factories & *Async actions
│   ├── TestViewModel.cs            # PerRequest document ViewModel subclassing ViewModelBase (injected IWindowManager)
│   └── ViewModelBase.cs            # Pure UI-agnostic Screen base with Id, IsDirty, CustomText, ILog & async CanCloseAsync
└── Views/
    ├── AboutView.xaml              # Pure x:Name convention CenterOwner system-default modal Window (AboutTitle, AboutText, Ok)
    ├── AboutView.xaml.cs           # Standard InitializeComponent() Window code-behind
    ├── ConfirmCloseView.xaml       # Pure x:Name convention CenterOwner system-default modal Window (Message, Confirm, Cancel)
    ├── ConfirmCloseView.xaml.cs    # Standard InitializeComponent() Window code-behind
    ├── MainWindowView.xaml         # 800x450 split window (NTLDR2/AvalonDock + caliburnmicro.com): Menus + DockingManager + TabControl
    ├── MainWindowView.xaml.cs      # Standard InitializeComponent() Window code-behind
    ├── TestView.xaml               # Pure x:Name convention UserControl (IsDirty CheckBox + CustomText TextBox)
    └── TestView.xaml.cs            # Standard InitializeComponent() UserControl code-behind
```

### Detailed Class & XAML Breakdown

1. **`App.xaml` & `App.xaml.cs`**
   - Omits `StartupUri` so Caliburn.Micro controls window creation.
   - Places `<local:Bootstrapper x:Key="Bootstrapper"/>` inside `<ResourceDictionary.MergedDictionaries>`.
2. **`Bootstrapper.cs`** *(updated in Iterations 4.2, 6.2, 8.1 & 8.4)*
   - Subclasses `Caliburn.Micro.BootstrapperBase` backed by `SimpleContainer`.
   - Constructor assigns `LogManager.GetLog = type => new CaliburnLogger(type);` before calling `Initialize()`.
   - **`Configure()`**:
     - Registers `_container.Instance(_container)`, `_container.Singleton<IWindowManager, WindowManager>()`, `_container.Singleton<MainWindowViewModel>()`, `_container.PerRequest<TestViewModel>()`, and `_container.PerRequest<AboutViewModel>()` (enabling `SimpleContainer` to auto-synthesize `Func<TestViewModel>` and `Func<AboutViewModel>`, while leaving WPF Views unregistered so `ViewLocator` constructs each view once without triggering `SimpleContainer.GetAllInstances` lazy double-enumeration).
     - Subscribes `Coroutine.Completed` so any faulted async action `Task` (`e.Error != null`) is logged via `ILog.Error`.
     - Wraps `ViewLocator.LocateTypeForModelType` so non-null `cal:View.Context` values (such as `"TabControl"` on `TabManager`) fall back to the model's default view (`TestView`).
   - Overrides `GetInstance`, `GetAllInstances`, and `BuildUp` to delegate to `_container`, and `async void OnStartup` awaits `DisplayRootViewForAsync<MainWindowViewModel>()`.
3. **`CaliburnLogger.cs`** *(updated in Iteration 8.1)*
   - Package-internal `class CaliburnLogger : ILog`.
   - Formats messages via `FormatMessage(format, args)` (only calling `string.Format` when `args.Length > 0` and catching `FormatException`) before forwarding to single-argument `Trace.TraceInformation` / `Trace.TraceWarning`.
4. **`DocumentCloseBehavior.cs`** *(added in Iteration 8.2)*
   - `public sealed class DocumentCloseBehavior : Behavior<DockingManager>` (`Microsoft.Xaml.Behaviors.Wpf`).
   - Subscribes to `DockingManager.DocumentClosing`: when `e.Document?.Content` is a conducted `IChild` (`Parent is IConductor conductor`), sets `e.Cancel = true` synchronously and chains `_pending = CloseAfterAsync(_pending, conductor, child)` so single-tab closes, floating window closes, and multi-tab `Close All` operations await `conductor.CloseItemAsync(document)` sequentially one prompt at a time.
5. **`ViewModels/ViewModelBase.cs`** *(updated in Iterations 3.1, 8.2, 8.3, 8.4 & 9.1)*
   - `public abstract class ViewModelBase : Screen` with **zero `System.Windows` or `System.Diagnostics` references**.
   - Requires constructor-injected `IWindowManager windowManager`, initializes `ILog` via `LogManager.GetLog(GetType())`, and seeds `_id = GetType().Name` and `DisplayName = GetType().Name`.
   - **`Id` (`string`):** Logical persistence/lookup identifier (`Set(ref _id, value)`), mapped to `LayoutItem.ContentId` (`"TestA"`–`"TestD"`).
   - **`IsDirty` (`bool`):** Notifies via `Set(ref _isDirty, value)`.
   - **`CustomText` (`string`):** Notifies via `Set(ref _customText, value)` and **automatically sets `IsDirty = true`** whenever the value changes.
   - **`CanCloseAsync(CancellationToken)`:** When `IsDirty == true`, awaits `_windowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName))` and returns `result == true`; when `IsDirty == false`, returns `true` immediately without opening a dialog.
   - **`OnDeactivateAsync(bool close, CancellationToken)`:** Logs `close` via `ILog` and delegates to `base.OnDeactivateAsync`.
6. **`ViewModels/TestViewModel.cs`** *(updated in Iterations 8.3 & 8.4)*
   - `public class TestViewModel : ViewModelBase` resolved as `PerRequest` with constructor-injected `IWindowManager`.
7. **`ViewModels/ConfirmCloseViewModel.cs`** *(added in Iteration 8.3; updated in Iteration 8.5)*
   - `public sealed class ConfirmCloseViewModel : Screen` accepting `string documentName`, setting `DisplayName = documentName` and `Message = $"Are you sure you want to close '{documentName}'?"`, and exposing `ConfirmAsync()` (`TryCloseAsync(true)`) and `CancelAsync()` (`TryCloseAsync(false)`).
8. **`ViewModels/AboutViewModel.cs`** *(added in Iteration 5; updated in Iterations 8.5 & 8.6)*
   - `public class AboutViewModel : Screen` setting `DisplayName`, `AboutTitle`, and the ~201-word `AboutText` summary, with `public Task OkAsync() => TryCloseAsync(true);`.
9. **`ViewModels/MainWindowViewModel.cs`** *(updated through Iteration 9.1)*
   - `public class MainWindowViewModel : Conductor<Screen>.Collection.OneActive` injected with `IWindowManager`, `Func<TestViewModel>`, and `Func<AboutViewModel>`, setting `DisplayName = "Caliburn AvalonDock Test"` and logging through `LogManager.GetLog(GetType())`.
   - **`OpenDocumentAsync(string displayName)`**: Computes space-free `id = displayName.Replace(" ", string.Empty)` (`"TestA"`–`"TestD"`), looks up an existing `TestViewModel` in `Items` matching `x.Id == id`, or invokes `_testViewModelFactory()`, sets `item.Id = id` and `item.DisplayName = displayName`, assigns `item.IsDirty = ++_createCount % 2 > 0`, and awaits `ActivateItemAsync(item, CancellationToken.None)`.
   - **`CloseDocumentAsync(Screen document)`**: Calls `this.CloseItemAsync(document, CancellationToken.None)`.
   - **`ShowAboutAsync()`**: Calls `_windowManager.ShowDialogAsync(_aboutViewModelFactory())`.
   - **`OnInitializedAsync` & `StartInitialDocumentsAsync()`**: Subscribes `Items.CollectionChanged` and schedules `_ = Execute.OnUIThreadAsync(StartInitialDocumentsAsync);` on the STA UI thread.
10. **`Views/MainWindowView.xaml` & `Views/MainWindowView.xaml.cs`** *(updated through Iteration 9.1)*
    - `800 × 450` `Window` (`xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"`, `xmlns:cal="http://caliburnmicro.com"`, flattened `<Window.Resources>`, and `Window.Title` omitted so `WindowManager` binds `DisplayName` automatically).
    - **Top Menu:**
      - `_Window` menu with `Window A`–`Window D` wired via shorthand `cal:Message.Attach="OpenDocumentAsync('Test A')"`..`'Test D'`.
      - `_Help` menu with `_What's This App About...` wired via shorthand `cal:Message.Attach="ShowAboutAsync"`.
    - **Left Column (`"AvalonDock"`):**
      - `<avalonDock:DockingManager x:Name="DockManager">` bound to `DocumentsSource="{Binding Items}"` and `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`, with `<i:Interaction.Behaviors><local:DocumentCloseBehavior/></i:Interaction.Behaviors>`.
      - `DocumentHeaderTemplate` (`DataContext = LayoutDocument`) showing the `6×6` red dirty `Ellipse` (`FallbackValue=Collapsed`) + teardown-safe `{Binding Title}` `TextBlock`.
      - `DockingManager.Resources` declaring the implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` (`<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>`).
      - `LayoutItemContainerStyle` (`TargetType="{x:Type avalonDock:LayoutItem}"`) mapping `ContentId="{Binding Model.Id, Mode=OneWay}"` and `Title="{Binding Model.DisplayName, Mode=OneWay}"` (`CloseCommand` left unset so AvalonDock's default close pipeline raises `DocumentClosing`).
    - **Right Column (`"TabControl"`):**
      - `TabControl` (`x:Name="TabManager"`) bound to `ItemsSource="{Binding Items}"` and `SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`.
      - `ItemTemplate` showing the `6×6` red dirty `Ellipse`, `{Binding DisplayName}`, and the Marlett `"r"` (`✕`) `Button` wired via `cal:Message.Attach="CloseDocumentAsync($dataContext)"`.
      - `ContentTemplate` hosting `<ContentControl cal:View.Model="{Binding}" cal:View.Context="TabControl" IsTabStop="False"/>`.
11. **`Views/ConfirmCloseView.xaml` & `Views/ConfirmCloseView.xaml.cs`** *(added in Iteration 8.3; updated in Iteration 8.5)*
    - `420`-px wide, `SizeToContent="Height"`, `NoResize`, `CenterOwner` modal `Window` (`ShowInTaskbar="False"`) using **100% Caliburn.Micro conventions** (`Window.Title` <- `DisplayName`, `<TextBlock x:Name="Message"/>`, `<Button x:Name="Confirm" Content="_Yes"/>` -> `ConfirmAsync()`, `<Button x:Name="Cancel" Content="_No"/>` -> `CancelAsync()`) and **strictly WPF/OS system defaults** (`0` custom brushes, hex colors, or `Font*` overrides).
12. **`Views/AboutView.xaml` & `Views/AboutView.xaml.cs`** *(added in Iteration 5; refined in Iterations 7 & 8.5)*
    - `560`-px wide, `SizeToContent="Height"`, `NoResize`, `CenterOwner` modal `Window` using **100% Caliburn.Micro conventions** (`Window.Title` <- `DisplayName`, `<TextBlock x:Name="AboutTitle"/>`, `<TextBox x:Name="AboutText" IsReadOnly="True"/>`, `<Button x:Name="Ok" Content="OK"/>` -> `OkAsync()`, avoiding `x:Name="Close"` `CS0108` collision on `Window`) and **strictly WPF/OS system defaults**.
13. **`Views/TestView.xaml` & `Views/TestView.xaml.cs`** *(updated in Iteration 8.5)*
    - 2-row, 2-column `UserControl` using pure `x:Name` conventions: `<CheckBox x:Name="IsDirty" Content="Is Dirty"/>` (`TwoWay`) and `<TextBox x:Name="CustomText"/>` (`TwoWay` with automatic `UpdateSourceTrigger.PropertyChanged` on WPF).

---

## 4. Current Runtime Behavior

### 4.1 Startup & Bootstrapper Sequence

1. WPF loads `App.xaml`, instantiating `Bootstrapper` as a resource dictionary entry.
2. `Bootstrapper` configures `LogManager.GetLog` to emit brace-safe `CaliburnLogger` trace messages and calls `Initialize()`, which invokes `Configure()` to set up `SimpleContainer` (`Singleton<IWindowManager, WindowManager>()`, `Singleton<MainWindowViewModel>()`, `PerRequest<TestViewModel>()`, `PerRequest<AboutViewModel>()`), attach `Coroutine.Completed` error logging, and install the `ViewLocator.LocateTypeForModelType` `"TabControl"` context fallback.
3. On `Application.Startup`, `Bootstrapper.OnStartup` awaits `DisplayRootViewForAsync<MainWindowViewModel>()`.
4. `SimpleContainer` constructs `MainWindowViewModel` with the singleton `WindowManager` and auto-synthesized `Func<TestViewModel>` and `Func<AboutViewModel>` delegates; `ViewLocator` instantiates `MainWindowView`; `ViewModelBinder.Bind` wires `Action.Target` and conventions; `WindowManager.CreateWindowAsync` binds `MainWindowView.Title` `TwoWay` to `DisplayName` (`"Caliburn AvalonDock Test"`); `WindowConductor` activates `MainWindowViewModel` (`OnInitializedAsync` → `OnActivatedAsync`); and `MainWindowView` is displayed.

### 4.2 Asynchronous UI-Thread Document Seeding (`StartInitialDocumentsAsync`)

Inside `MainWindowViewModel.OnInitializedAsync`:

```csharp
Items.CollectionChanged += Items_CollectionChanged;
_ = Execute.OnUIThreadAsync(StartInitialDocumentsAsync);
```

`StartInitialDocumentsAsync()` executes non-blockingly on the **STA UI `Dispatcher` thread**, pausing 100 ms before creating and activating each of the four initial documents (`Test A` dirty, `Test B` clean, `Test C` dirty, `Test D` clean and active) via `_testViewModelFactory()`.

### 4.3 Side-by-Side Dual-Host Rendering & `View.Context` Isolation

Once loaded, both panes simultaneously render live, independent `TestView` instances (`Views[View.DefaultContext]` in `DockManager` via its implicit `ViewModelBase` `DataTemplate`, and `Views["TabControl"]` in `TabManager` via `cal:View.Context="TabControl"` + `Bootstrapper.Configure()`'s `ViewLocator` fallback) with zero `ViewAware` visual theft.

### 4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem` + `_Window` Checkmark)

Selection is synchronized across all three UI surfaces through `MainWindowViewModel.ActiveItem`:
1. Clicking a tab in `DockManager` updates `ActiveContent` → `MainWindowViewModel.ActiveItem` → `TabManager.SelectedItem` and moves the single `_Window` menu checkmark (`ActiveDocumentConverter`).
2. Clicking a tab in `TabManager` updates `SelectedItem` → `MainWindowViewModel.ActiveItem` → `DockManager.ActiveContent` and moves the single `_Window` menu checkmark.
3. Clicking **`Window -> Window A / B / C / D`** triggers `ActionMessage` (`OpenDocumentAsync('Test X')`), wrapped in a `TaskResult` coroutine on the UI `Dispatcher`, activating or recreating the target document in both controls and moving the single checkmark (`IsCheckable="False"` ensures clicking the already-active item keeps it checked, and `Close All` leaves `0` items checked).

### 4.5 Dirty-State Tracking & Bidirectional Live Editing

`TestView.xaml`'s `<TextBox x:Name="CustomText"/>` is bound by `ConventionManager` (`TwoWay` + `UpdateSourceTrigger.PropertyChanged` on WPF) and `<CheckBox x:Name="IsDirty"/>` (`TwoWay`). Typing or toggling in either pane immediately updates both `TestView` instances and both `6×6` red dirty-indicator dots in lockstep.

### 4.6 Document Closing, Async Confirmation Dialogs, Floating Windows & App Exit

Every UI close path across both controls and the OS window frame funnels through `MainWindowViewModel` (`Conductor<Screen>.Collection.OneActive`) and `ViewModelBase.CanCloseAsync`:

- **Left Pane (`DockManager` — `DocumentCloseBehavior`):**
  - Because `LayoutItem.CloseCommand` is left unset, AvalonDock's default `_defaultCloseCommand` handles tab `X` clicks, middle-clicks, context menu **Close** / **Close All** / **Close All But This**, and floating document window (`LayoutDocumentFloatingWindowControl`) `X` / `Alt+F4` / `WM_CLOSE`, raising `DockingManager.DocumentClosing`.
  - `DocumentCloseBehavior` sets `e.Cancel = true` synchronously and appends `CloseAfterAsync(_pending, conductor, child)` to its FIFO `_pending` `Task` chain, calling `await conductor.CloseItemAsync(document)` one document at a time (preventing overlapping dialogs during **Close All**).
- **Right Pane (`TabManager` — `CloseDocumentAsync($dataContext)`):**
  - Clicking the Marlett `"r"` (`✕`) button fires `cal:Message.Attach="CloseDocumentAsync($dataContext)"`, which bubbles up the visual tree to `MainWindowViewModel.CloseDocumentAsync(Screen)` and executes `this.CloseItemAsync(document, CancellationToken.None)`.
- **Confirmation Execution Flow (`ConfirmCloseViewModel` / `ConfirmCloseView`):**
  1. `Conductor` invokes `TestViewModel.CanCloseAsync`.
  2. If **`IsDirty == false`**, `CanCloseAsync` returns `true` immediately without opening a dialog.
  3. If **`IsDirty == true`**, `CanCloseAsync` awaits `_windowManager.ShowDialogAsync(new ConfirmCloseViewModel(DisplayName))`, opening `ConfirmCloseView` centered over whichever window is currently active (`MainWindowView` or a torn-off floating window via `WindowManager.InferOwnerOf`).
     - Clicking **No** (`x:Name="Cancel"` → `CancelAsync()`, or pressing `Esc` / closing the dialog `X`) returns `false`, aborting the close so the tab stays open in both `DockManager` and `TabManager`.
     - Clicking **Yes** (`x:Name="Confirm"` → `ConfirmAsync()`, or pressing `Enter`) returns `true`: `Conductor` switches `ActiveItem` to `DetermineNextItemToActivate`, awaits `DeactivateAsync(close: true)` (clearing `ViewAware.Views`), and removes the item from `Items`.
  4. `Items.CollectionChanged` (`Remove`) removes the `TabItem` from `TabManager` and silently detaches the `LayoutDocument` in `DockingManager.DocumentsSourceElementsChanged` (without re-raising `DocumentClosing`), running `Layout.CollectGarbage()` to close any emptied floating window automatically.
- **Closing the Main Application Window (`MainWindowView`):**
  - Clicking the OS window `X` button or pressing `Alt+F4` triggers Caliburn.Micro's `WindowConductor.Closing` (`e.Cancel = true` → `await Task.Yield()` → `await MainWindowViewModel.CanCloseAsync()`), sequentially prompting with `ConfirmCloseView` for each dirty document via `DefaultCloseStrategy<Screen>`. Clicking **No** on any prompt cancels closing `MainWindowView`.

### 4.7 Reopening Documents via the `_Window` Menu (`Window A`–`Window D`)

Selecting **`Window -> Window A`..`Window D`** invokes `MainWindowViewModel.OpenDocumentAsync('Test X')`, either focusing the existing `TestViewModel` or invoking `_testViewModelFactory()` (assigning `IsDirty = ++_createCount % 2 > 0`) and activating it via `ActivateItemAsync`.

### 4.8 `Help -> What's This App About...` Modal Dialog

Selecting **`Help -> What's This App About...`** invokes `MainWindowViewModel.ShowAboutAsync()` → `_windowManager.ShowDialogAsync(_aboutViewModelFactory())`, opening the convention-bound, system-default `CenterOwner` `AboutView` modal window (`x:Name="Ok"` → `OkAsync()`).

### 4.9 Diagnostic Trace Output

When run with a `TraceListener` attached, `CaliburnDockTestApp` produces clean trace output with:
- **`0` WPF `System.Windows.Data` binding errors** *(fixed in Iteration 4.1)*.
- **`0` spurious `Screen: TryClose requires a parent IConductor...` warnings** *(eliminated in Iteration 8.2 by closing conducted tabs via `IConductor.CloseItemAsync` instead of `Screen.TryCloseAsync`)*.
- Brace-safe `CaliburnLogger` messages (including ViewModel `ILog` lifecycle traces) and AvalonDock `#if TRACE` `ConsoleDump` layout trees.

---

## 5. Resolved Caliburn.Micro Deprecations & MVVM Anti-Patterns

All legacy pre-v5 Caliburn.Micro deprecations, threading hazards, view-caching bugs, manual binding boilerplate, and custom `ICommand` / `MessageBox` anti-patterns have been resolved across Iterations `1`–`10`:

- Singular `<TargetFramework>net48</TargetFramework>` & `Microsoft.NET.Sdk` *(Iterations 1 & 2.1)*
- `Window D` menu completion & `ConductWith(this)` memory-leak removal *(Iterations 2.2 & 2.3)*
- Removal of `<DefineConstants>` clobbering, `#if CALIBURN_ASYNC` / 3.2 shims, and off-UI-thread `Task.Run` seeding *(Iterations 3.1 & 3.2)*
- Dual-host `ViewAware` isolation (`cal:View.Context="TabControl"` + `ViewLocator` fallback) *(Iterations 4.1 & 4.2)*
- Upgrade to `Caliburn.Micro 6.0.15-beta`, `await DisplayRootViewForAsync<MainWindowViewModel>()`, and `OnInitializedAsync` *(Iterations 6.1–6.3)*
- `SimpleContainer` DI (`Singleton` + `PerRequest` + auto-synthesized `Func<T>` factories), `ILog` lifecycle logging, brace-safe `CaliburnLogger`, pure `x:Name` conventions (`*Async` suffix stripping + `Window.Title` ↔ `DisplayName` binding), concise `cal:Message.Attach` shorthand on `http://caliburnmicro.com`, `DocumentCloseBehavior`, complete removal of `RelayCommand.cs`, and replacement of `MessageBox.Show` with the system-default async `ConfirmCloseViewModel` / `ConfirmCloseView` dialog *(Iterations 5, 7 & 8.1–8.5)*

---

## 6. Intentional Mode-B Scope & Architectural Notes

Because `CaliburnDockTestApp`'s core diagnostic purpose is comparing `DockingManager.DocumentsSource` side-by-side against `TabControl.ItemsSource` on the same `Conductor.Items` collection, two choices remain deliberately scoped to classic document-only hosting:

| File & Line | AvalonDock Pattern Used in `CaliburnDockTestApp` | Architectural Rationale | Companion v5 Reference if Extended |
|:------------|:-------------------------------------------------|:------------------------|:-----------------------------------|
| `MainWindowView.xaml:61–64` | `DocumentsSource="{Binding Items}"` + unfiltered `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` | Safe here because `CaliburnDockTestApp` hosts only `TestViewModel` documents (zero tool anchorables); unfiltered `ActiveContent` bindings only break when `AnchorablesSource` tools are added alongside documents. | Guard/filter `ActiveContent` synchronization if tool anchorables are introduced, or use `DockLayout` via `IDockLayoutService` as shown in `source/ExperimentalTestApp`. |
| `MainWindowView.xaml:61` | Unset `DockingManager.Theme`, `AllowFloatingWindows`, and `AllowDetachedWindows` | Uses built-in `Themes/generic.xaml` and default window flags to keep `CaliburnDockTestApp.csproj` zero-extra-package minimal. *(If `DetachedAnchorableWindow` tool panes are ever added, move the implicit `ViewModelBase` `DataTemplate` from `<avalonDock:DockingManager.Resources>` to `App.xaml` `<Application.Resources>`.)* | See `source/ThemeSuperTestApp` and `source/ExperimentalTestApp` for runtime switching across `AvalonDock.Themes.{Type1Legacy,Type2Hybrid,Type3New}` and `XmlLayoutSerializer` persistence. |

---

## 7. Summary Matrix of Current Behavior & Remaining Quirks

1. **Both `AvalonDock` (left) and `TabControl` (right) render live, independent `TestView` instances simultaneously** (`Views[DefaultContext]` vs `Views["TabControl"]`), with **zero** WPF data-binding errors and zero view-theft—even when tabs are torn off into floating windows *(fixed in Iteration 4)*.
2. **Startup populates 4 tabs (`Test A`, `Test B`, `Test C`, `Test D`) asynchronously over ~400 ms on the STA UI thread** (`Execute.OnUIThreadAsync(StartInitialDocumentsAsync)`), leaving `Test D` selected *(fixed in Iterations 3.2 & 8.5)*.
3. **Initial dirty flags alternate by creation counter (`_createCount % 2 > 0`):** `Test A` (dirty), `Test B` (clean), `Test C` (dirty), `Test D` (clean). Reopening any closed tab (`Test A`–`Test D`) via `_Window -> Window A/B/C/D` (`OpenDocumentAsync` action, matched by `Id` `"TestA"`–`"TestD"`) increments `_createCount` and flips whether it starts dirty or clean *(modernized in Iterations 2.2, 8.1, 8.5 & 9.1)*.
4. **Bidirectional live editing & teardown-safe headers:** Typing into `Custom Text` or toggling `Is Dirty` in **either** `AvalonDock` or `TabControl` immediately updates both `TestView` instances and both red dirty-indicator dots in lockstep, while `DocumentHeaderTemplate` binds `{Binding Title}` and `LayoutItemContainerStyle` maps `Model.Id` → `ContentId` and `Model.DisplayName` → `Title` under `xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"` *(polished in Iteration 9.1)*.
5. **Async system-default close confirmation & FIFO close queuing:** Closing a dirty tab (from `AvalonDock`, `Close All`, a floating `AvalonDock` document window, `TabControl`, or `MainWindowView` exit) awaits `ConfirmCloseViewModel` / `ConfirmCloseView` (`IWindowManager.ShowDialogAsync`, strictly WPF/OS system defaults, centered on the active window) one prompt at a time with **zero** `RelayCommand`/`ICommand`, **zero** `MessageBox.Show`, and **zero** spurious `Screen.TryCloseAsync` trace warnings *(modernized in Iteration 8)*.
6. **`Help -> What's This App About...` opens a `CenterOwner` modal `AboutView` dialog** (~201 words, selectable/copyable text, strictly WPF/OS system defaults) summarizing the side-by-side synchronization design, conventions, async close pipeline, and `View.Context` architecture *(added in Iteration 5; refined in Iterations 7, 8.5 & 8.6)*.
7. **Runs cleanly on `Caliburn.Micro 6.0.15-beta` (`SimpleContainer`) + `AvalonDock 5.1.0` (`net48`)** via `await DisplayRootViewForAsync<MainWindowViewModel>()` and `OnInitializedAsync`, compiling with `0 Warning(s), 0 Error(s)` under `--warnaserror` and **zero** `System.Text.Json` dependencies *(upgraded in Iterations 6, 8 & 9.1)*.