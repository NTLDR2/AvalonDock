# CaliburnDockTestApp — Updated Technical Documentation & Applied Fixes

> **Status:** Living Technical Specification & Incremental Modernization Log  
> **Baseline Reference (Frozen):** `00_CaliburnDockTestApp- Baseline-Technicals-and-Deprecation-Audit.md`  
> **Location:** `source/CaliburnDockTestApp/`  
> **Target Framework:** `.NET Framework 4.8` (`<TargetFramework>net48</TargetFramework>`, singular)  
> **MSBuild SDK:** `Microsoft.NET.Sdk` (`<UseWPF>true</UseWPF>`)  
> **Pinned Frameworks:** `Caliburn.Micro` `6.0.15-beta` + ProjectReference to `AvalonDock` (`5.1.0`)

This living document tracks the current technical behavior of **`CaliburnDockTestApp`** alongside every focused, incremental fix applied from the frozen baseline in `00_CaliburnDockTestApp- Baseline-Technicals-and-Deprecation-Audit.md`.

---

## Table of Contents

- [Applied Fixes Log](#applied-fixes-log)
1. [Purpose & Historical Provenance](#1-purpose--historical-provenance)
2. [Project Configuration & Dependencies](#2-project-configuration--dependencies)
3. [Complete File-by-File Inventory](#3-complete-file-by-file-inventory)
4. [Current Runtime Behavior](#4-current-runtime-behavior)
   - [4.1 Startup & Bootstrapper Sequence](#41-startup--bootstrapper-sequence)
   - [4.2 Asynchronous UI-Thread Document Seeding (`StartInitialDocuments`)](#42-asynchronous-ui-thread-document-seeding-startinitialdocuments)
   - [4.3 Side-by-Side Dual-Host Rendering & `View.Context` Isolation](#43-side-by-side-dual-host-rendering--viewcontext-isolation)
   - [4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem`)](#44-active-tab-synchronization-activecontent--activeitem--selecteditem)
   - [4.5 Dirty-State Tracking & Bidirectional Live Editing](#45-dirty-state-tracking--bidirectional-live-editing)
   - [4.6 Document Closing, Confirmation Prompts, Floating Windows & App Exit](#46-document-closing-confirmation-prompts-floating-windows--app-exit)
   - [4.7 Reopening Documents via the `_Window` Menu (`Window A`–`Window D`)](#47-reopening-documents-via-the-_window-menu-window-awindow-d)
   - [4.8 `Help -> What's This App About...` Modal Dialog](#48-help---whats-this-app-about-modal-dialog)
   - [4.9 Diagnostic Trace Output](#49-diagnostic-trace-output)
5. [Remaining Deprecated, Obsolete & Broken Caliburn.Micro Usage](#5-remaining-deprecated-obsolete--broken-caliburnmicro-usage)
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
| **3.2** | `ViewModels/MainWindowViewModel.cs` | Replaced `Task.Run(() => StartInitialDocuments());` with `_ = Execute.OnUIThreadAsync(StartInitialDocuments);` inside `OnInitializeAsync`. | Eliminates off-UI-thread `Items` queries, unsynchronized `_createCount` mutations, background `TestViewModel` construction, and cross-thread `ActivateItemAsync` calls while preserving the non-blocking staggered tab seeding (`+100 ms`..`+400 ms`) on the STA UI `Dispatcher` thread. |
| **4.1** | `Views/MainWindowView.xaml` | Replaced `<DockingManager.LayoutItemTemplate>` (`cal:View.Model="{Binding Content}"`) with an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` inside `<DockingManager.Resources>` using `<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>`. | Fixes the blank `AvalonDock` document body (`BindingExpression path error: 'Content' property not found on 'object' ''TestViewModel''`) across both `Generic` and non-Generic AvalonDock themes by leaving `LayoutItemTemplate` unset so `LayoutItem.View` resolves the implicit `DataTemplate` with `DataContext = TestViewModel`. |
| **4.2** | `Views/MainWindowView.xaml`<br>`Bootstrapper.cs` | Added `cal:View.Context="TabControl"` to `TabManager`'s `ContentTemplate` and overrode `Bootstrapper.Configure()` to add a default-view fallback to `ViewLocator.LocateTypeForModelType` (with future AvalonDock v5 + Caliburn.Micro v6-beta stress-test notes). | Prevents `ViewAware` view-theft between `DockManager` (which keeps `View.DefaultContext`, preserving parameterless `Screen.GetView()`) and `TabManager` (`Views["TabControl"]`), enabling both controls to render live, independent, synchronized `TestView` instances simultaneously—even when tabs are floated. |
| **5** | `ViewModels/AboutViewModel.cs` *(new)*<br>`Views/AboutView.xaml` *(new)*<br>`Views/AboutView.xaml.cs` *(new)*<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Added `Help -> What's This App About...` menu item wired via Caliburn.Micro v5/v6-compatible leaf `cal:Message.Attach="[Event Click] = [Action ShowAbout]"` to `IWindowManager.ShowDialogAsync(new AboutViewModel())`, opening a `CenterOwner` modal `AboutView` window (~234-word architectural summary). | Makes the side-by-side diagnostic purpose and dual-host `View.Context` design self-evident both interactively for humans and in-source for AI coding agents, while modeling idiomatic Caliburn.Micro v5/v6 dialog & action patterns (constructor-injectable `IWindowManager`, `Screen.TryCloseAsync`, leaf `cal:Message.Attach`, zero `MessageBox.Show` or `RelayCommand`). |
| **6.1** | `CaliburnDockTestApp.csproj` (line 10) | Upgraded `<PackageReference Include="Caliburn.Micro" Version="4.0.136-rc" />` to `Version="6.0.15-beta"`. | Replaces the pre-release 2020 `4.0.136-rc` package with `Caliburn.Micro` `6.0.15-beta` (`Caliburn.Micro.Core` `6.0.15-beta`, `Caliburn.Micro.Platform` `6.0.15-beta`, `Caliburn.Micro.Platform.Core` `6.0.15-beta`, and `Microsoft.Xaml.Behaviors.Wpf` `1.1.135`), verified on `net48` with **zero** `System.Text.Json` assemblies in the build output. |
| **6.2** | `Bootstrapper.cs` (lines 19–33) | Changed `protected override void OnStartup` + unawaited `DisplayRootViewFor<MainWindowViewModel>()` to `protected override async void OnStartup` + `await DisplayRootViewForAsync<MainWindowViewModel>()`, and updated `Configure()`'s inline comment to note verification on `Caliburn.Micro 6.0.15-beta`. | Migrates off `DisplayRootViewFor<T>()` (removed after `4.0.136-rc`) and properly awaits root window activation so startup exceptions are observed on the UI `DispatcherSynchronizationContext`. |
| **6.3** | `ViewModels/MainWindowViewModel.cs` (lines 88–96) | Replaced `protected override Task OnInitializeAsync(CancellationToken)` (and its `base.OnInitializeAsync` call and trace string) with `protected override Task OnInitializedAsync(CancellationToken)` (`base.OnInitializedAsync`). | Eliminates `[Obsolete("Override OnInitializedAsync")]` (`CS0672`), compiling cleanly with `0 Warning(s), 0 Error(s)` under `Directory.Build.props`'s `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. |
| **7** | `Views/AboutView.xaml` | Removed all custom colors, brushes, border overrides, font sizes, and font weights (`Background="#F0F2F5"`, `Background="#F6F8FA"`, `BorderBrush="#D0D7DE"`, `Background="Transparent"`, `BorderThickness="0"`, `FontSize="14"`, `FontWeight="SemiBold"`, `FontSize="12.5"`, explicit `Window.Background`, and fixed `Button` `Height="26"`), replacing them with a 3-row `<Grid Margin="12">` using pure WPF/OS system defaults (`TextBlock`, native read-only `TextBox` with `Padding="8"` and `TextWrapping="Wrap"`, and `MinWidth="75"` `Padding="12,3"` `OK` `Button`). | Ensures `AboutView` strictly honors OS system fonts, system font sizes/scaling, system colors, and Windows High Contrast themes with zero hard-coded brushes or font overrides. |

---

## 1. Purpose & Historical Provenance

`CaliburnDockTestApp` was originally created in August 2020 (`commit c1be099`, merged in PR `#185` — *"Fix ObservableCollection Reset notification handling"*) during the AvalonDock v4 cycle as a minimal side-by-side diagnostic harness.

Its goal is to compare **AvalonDock's `DockingManager`** (left half of the window) directly against a standard **WPF `TabControl`** (right half of the window) when **both controls are bound simultaneously to the exact same Caliburn.Micro `Conductor<Screen>.Collection.OneActive` ViewModel (`MainWindowViewModel`)**:

- **`DocumentsSource="{Binding Items}"`** (on `DockingManager`) vs **`ItemsSource="{Binding Items}"`** (on `TabControl`)
- **`ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`** (on `DockingManager`) vs **`SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`** (on `TabControl`)
- Simultaneous side-by-side `TestView` rendering (`IsDirty` checkbox + `Custom Text` textbox) synchronized in real time across both panes *(restored in Iteration 4)*
- Per-tab dirty state (`IsDirty`) rendered as a red dot (`Ellipse`) in both tab headers
- Cancellable close confirmation (`MessageBox.Show` inside `Screen.CanCloseAsync`) routed via a custom `RelayCommand` (`CloseCommand`)
- Re-activating or re-creating tabs from a top `_Window` menu (`ShowACommand`, `ShowBCommand`, `ShowCCommand`, `ShowDCommand`) and inspecting the app's architectural rationale from **`Help -> What's This App About...`** *(added in Iteration 5)*

`CaliburnDockTestApp` compiles cleanly (`0 Warning(s), 0 Error(s)`) against the repository's live `AvalonDock` (`5.1.0`) assembly and `Caliburn.Micro` (`6.0.15-beta`) on `net48` (with zero `System.Text.Json` dependencies), though it is **not currently included in `source/AvalonDock.sln`** and still uses classic AvalonDock v4-style XAML bindings (`DocumentsSource`/`ActiveContent` rather than `AvalonDock.Mvvm` / `DockLayout` or v5 theme packages).

---

## 2. Project Configuration & Dependencies

Defined in `CaliburnDockTestApp.csproj` (inheriting repository-wide settings from `source/Directory.Build.props`):

| Setting / Dependency | Value | Notes |
|:---------------------|:------|:------|
| **MSBuild SDK** | `Microsoft.NET.Sdk` *(Fixed — Iteration 2.1)* | Updated from legacy `Microsoft.NET.Sdk.WindowsDesktop`; imports WPF desktop build targets via `<UseWPF>true</UseWPF>` on `net48`. |
| **Output Type** | `WinExe` | Desktop WPF executable (`CaliburnDockTestApp.exe`). |
| **Target Framework** | `net48` *(Fixed — Iteration 1)* | Explicitly singular `<TargetFramework>net48</TargetFramework>` (updated from legacy plural `<TargetFrameworks>` so MSBuild runs a single-target `net48` build with no multi-target wrapper). |
| **Preprocessor Constants** | Standard SDK defaults *(Fixed — Iteration 3.1)* | Custom `<DefineConstants>` blocks (`CALIBURN_ASYNC`) removed so `Microsoft.NET.Sdk` provides standard `TRACE;DEBUG;NETFRAMEWORK;NET48;` (Debug) and `TRACE;RELEASE;NETFRAMEWORK;NET48;` (Release) constants. |
| **`Caliburn.Micro`** | `6.0.15-beta` (NuGet) *(Upgraded — Iteration 6.1)* | Pulls `Caliburn.Micro.Core` (`6.0.15-beta`, `netstandard2.0`), `Caliburn.Micro.Platform` + `Caliburn.Micro.Platform.Core` (`6.0.15-beta`, `net462`), and `Microsoft.Xaml.Behaviors.Wpf` (`1.1.135`)—with **zero** `System.Text.Json` dependencies. |
| **`AvalonDock`** | `..\Components\AvalonDock\AvalonDock.csproj` | Brings `AvalonDock.dll` (`5.1.0`) and transitively `AvalonDock.Core.dll`. |
| **Not Referenced** | `AvalonDock.Themes.*`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, `AvalonDock.Serializer.Xml` | Uses AvalonDock's built-in fallback `Themes/generic.xaml`, raw `DocumentsSource` binding, no layout persistence, and no IoC container. |
| **Static Analysis** | `Directory.Build.props` (`LangVersion 12`, `TreatWarningsAsErrors=true`) | Runs `NetAnalyzers` and `SonarAnalyzer.CSharp`, but **skips `StyleCop.Analyzers`** because `Directory.Build.props` exempts projects whose name contains `"Test"`. |

To build and launch from the repository root:

```powershell
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Debug
.\source\CaliburnDockTestApp\bin\Debug\net48\CaliburnDockTestApp.exe
```

---

## 3. Complete File-by-File Inventory

`CaliburnDockTestApp` consists of **17 source and configuration files**:

```text
source/CaliburnDockTestApp/
├── App.xaml                        # Application resources; instantiates Bootstrapper (no StartupUri)
├── App.xaml.cs                     # Empty Application code-behind partial class
├── AssemblyInfo.cs                 # WPF [assembly: ThemeInfo(None, SourceAssembly)]
├── Bootstrapper.cs                 # Caliburn.Micro BootstrapperBase; hooks CaliburnLogger, ViewLocator fallback & MainWindowViewModel
├── CaliburnDockTestApp.csproj      # Microsoft.NET.Sdk WPF project targeting singular net48 with Caliburn.Micro 6.0.15-beta
├── CaliburnLogger.cs               # Implements Caliburn.Micro.ILog forwarding to System.Diagnostics.Trace
├── RelayCommand.cs                 # Global-namespace ICommand / RelayCommand<T> implementation
├── ViewModels/
│   ├── AboutViewModel.cs           # Modal Screen presenting the <300-word architectural summary via IWindowManager
│   ├── MainWindowViewModel.cs      # Root Conductor<Screen>.Collection.OneActive managing TestViewModel items & ShowAbout()
│   ├── TestViewModel.cs            # Empty concrete document ViewModel subclassing ViewModelBase
│   └── ViewModelBase.cs            # Abstract Screen base with IsDirty, CustomText, CloseCommand & CanClosePrompt()
└── Views/
    ├── AboutView.xaml              # CenterOwner modal dialog Window displaying AboutTitle, copyable AboutText & OK button
    ├── AboutView.xaml.cs           # Standard InitializeComponent() Window code-behind
    ├── MainWindowView.xaml         # 800x450 split window: _Window + _Help Menus + DockingManager (left) + TabControl (right)
    ├── MainWindowView.xaml.cs      # Standard InitializeComponent() Window code-behind
    ├── TestView.xaml               # UserControl with IsDirty CheckBox + CustomText TextBox
    └── TestView.xaml.cs            # Standard InitializeComponent() UserControl code-behind
```

### Detailed Class & XAML Breakdown

1. **`App.xaml` & `App.xaml.cs`**
   - Omits `StartupUri` so Caliburn.Micro controls window creation.
   - Places `<local:Bootstrapper x:Key="Bootstrapper"/>` inside `<ResourceDictionary.MergedDictionaries>`.
2. **`Bootstrapper.cs`**
   - Subclasses `Caliburn.Micro.BootstrapperBase` without an IoC container (uses Caliburn.Micro's default `Activator.CreateInstance` factory).
   - Constructor assigns `LogManager.GetLog = type => new CaliburnLogger(type);` before calling `Initialize()`.
   - **`Configure()`** *(added in Iteration 4.2; updated in Iteration 6.2)*: Wraps `ViewLocator.LocateTypeForModelType` so that non-null `cal:View.Context` values (such as `"TabControl"` on `TabManager`) fall back to the model's default view (`TestView`) when no specialized context sub-view exists. Verified on `Caliburn.Micro 6.0.15-beta` and includes inline stress-test notes for enabling full AvalonDock v5 features (`Themes.Arc`, `DockLayout`, and `DetachedAnchorableWindow` tool panes).
   - **`OnStartup(object sender, StartupEventArgs e)`** *(updated in Iteration 6.2)*: `async void` override calling `await DisplayRootViewForAsync<MainWindowViewModel>()`.
3. **`CaliburnLogger.cs`**
   - Package-internal `class CaliburnLogger : ILog`.
   - Routes `Info` → `Trace.TraceInformation`, `Warn` → `Trace.TraceWarning`, and `Error` → `Trace.TraceError`, prefixing every message with `"{_type.Name}: "`.
4. **`RelayCommand.cs`**
   - Declared in the **global namespace** (no `namespace` block).
   - Defines `RelayCommand<T> : ICommand` and `RelayCommand : RelayCommand<object>`, bridging WPF `ICommand` properties (`MenuItem.Command`, `LayoutItem.CloseCommand`, `Button.Command`) to ViewModel delegates.
   - Enforces runtime parameter assignability in `ICommand.CanExecute` (throwing `ArgumentException` on mismatched non-null parameter types) and exposes `RaiseCanExecuteChanged()`.
5. **`ViewModels/ViewModelBase.cs`**
   - `public abstract class ViewModelBase : Screen`.
   - Constructor sets `DisplayName = GetType().Name;`.
   - **`IsDirty` (`bool`):** Notifies via `Set(ref _isDirty, value)`.
   - **`CustomText` (`string`):** Notifies via `Set(ref _customText, value)` and **automatically sets `IsDirty = true`** whenever the value changes.
   - **`CloseCommand` (`RelayCommand`):** Lazily instantiated as `new RelayCommand(_ => TryCloseAsync(null))`.
   - **`CanClosePrompt()` (`protected bool`):** If `IsDirty == true`, blocks on `MessageBox.Show($"Are you sure you want to close '{DisplayName}'?", DisplayName, MessageBoxButton.YesNo, MessageBoxImage.Question)` and returns `true` only when the user clicks `Yes`. If `IsDirty == false`, immediately returns `true`.
   - **Async Lifecycle Overrides** *(legacy `#if CALIBURN_ASYNC` / `#else` removed in Iteration 3.1)*: Overrides `CanCloseAsync(CancellationToken)` (calling `CanClosePrompt()` synchronously and wrapping in `Task.FromResult`) and `OnDeactivateAsync(bool close, CancellationToken)` (tracing `close`).
6. **`ViewModels/TestViewModel.cs`**
   - Empty marker class `public class TestViewModel : ViewModelBase { }` resolved by Caliburn.Micro's naming convention (`TestViewModel` → `TestView`).
7. **`ViewModels/AboutViewModel.cs`** *(added in Iteration 5)*
   - `public class AboutViewModel : Screen` (written strictly against Caliburn.Micro v5/v6 idioms with zero v4 cruft).
   - Sets `DisplayName`, `AboutTitle`, and the ~234-word `AboutText` summary in its constructor, and exposes `public Task Close() => TryCloseAsync(true);`.
8. **`ViewModels/MainWindowViewModel.cs`**
   - `public class MainWindowViewModel : Conductor<Screen>.Collection.OneActive`.
   - Provides both a parameterless constructor (`public MainWindowViewModel() : this(null)`) for `Activator.CreateInstance` and an `IWindowManager`-accepting constructor (`_windowManager = windowManager ?? new WindowManager();`) for DI/testing *(added in Iteration 5)*.
   - Initializes four private `RelayCommand` fields (`_showACommand`..`_showDCommand`) for `"Test A"` through `"Test D"` and exposes `ShowACommand`, `ShowBCommand`, `ShowCCommand`, and `ShowDCommand` *(completed in Iteration 2.2)*.
   - **`ShowAbout()`** *(added in Iteration 5)*: `public Task ShowAbout() => _windowManager.ShowDialogAsync(new AboutViewModel());`.
   - **`ActivateOrCreate<T>(string displayName)`:**
     - Looks up an existing item in `Items` matching type `T` and `DisplayName == displayName`.
     - If none exists, instantiates `T` via `Activator.CreateInstance`, sets `item.DisplayName = displayName`, assigns **`item.IsDirty = ++_createCount % 2 > 0`** (alternating `true` on odd creations, `false` on even creations), and returns `ActivateItemAsync(item, CancellationToken.None)` *(redundant/leaking `ConductWith` removed in Iteration 2.3)*.
   - **`OnInitializedAsync` & `StartInitialDocuments()`** *(updated in Iterations 3.1, 3.2 & 6.3)*:
     - Subscribes `Items.CollectionChanged += Items_CollectionChanged;` (logging collection `Action`, `OldItems`, and `NewItems` to `Trace`).
     - Schedules `_ = Execute.OnUIThreadAsync(StartInitialDocuments);`, non-blockingly creating `"Test A"`, `"Test B"`, `"Test C"`, and `"Test D"` at 100 ms intervals directly on the STA UI `Dispatcher` thread.
9. **`Views/MainWindowView.xaml` & `Views/MainWindowView.xaml.cs`**
   - `800 × 450` `Window` (`Title="Caliburn AvalonDock Test"`).
   - **Top Menu (`Grid.Row="0"`, `Grid.ColumnSpan="2"`):**
     - `_Window` menu containing `Window A` (`ShowACommand`), `Window B` (`ShowBCommand`), `Window C` (`ShowCCommand`), and `Window D` (`ShowDCommand`) *(added in Iteration 2.2)*.
     - `_Help` menu *(added in Iteration 5)* containing `_What's This App About...` wired via leaf `cal:Message.Attach="[Event Click] = [Action ShowAbout]"`.
   - **Left Column (`Grid.Column="0"` — `"AvalonDock"`):**
     - `DockingManager` (`x:Name="DockManager"`) bound to `DocumentsSource="{Binding Items}"` and `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`.
     - Custom `DocumentHeaderTemplate` showing a `6×6` red `Ellipse` (`Content.IsDirty`) + `TextBlock` (`Content.DisplayName`, `FallbackValue=#ERROR#`).
     - **`DockingManager.Resources`** *(fixed in Iteration 4.1)*: Declares an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` hosting `<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>` (`DockingManager.LayoutItemTemplate` left unset).
     - `LayoutItemContainerStyle` (`TargetType="{x:Type LayoutItem}"`) binding `Title="{Binding Model.DisplayName, Mode=OneWay}"` and `CloseCommand="{Binding Model.CloseCommand}"`.
   - **Right Column (`Grid.Column="1"` — `"TabControl"`):**
     - Standard WPF `TabControl` (`x:Name="TabManager"`) bound to `ItemsSource="{Binding Items}"` and `SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`.
     - Custom `ItemTemplate` showing an identical `6×6` red `Ellipse` (`IsDirty`), `TextBlock` (`DisplayName`), and a flat `Marlett`-font `"r"` (`✕`) close `Button` (`Command="{Binding CloseCommand}"`) visible when the tab is selected or hovered.
     - Custom `ContentTemplate` *(updated in Iteration 4.2)* containing `<ContentControl cal:View.Model="{Binding}" cal:View.Context="TabControl" IsTabStop="False"/>`.
10. **`Views/AboutView.xaml` & `Views/AboutView.xaml.cs`** *(added in Iteration 5; refined in Iteration 7)*
    - `560`-px wide, `SizeToContent="Height"`, `NoResize`, `CenterOwner` modal `Window` (`ShowInTaskbar="False"`) using **strictly WPF/OS system defaults** for all fonts, font sizes, and colors (zero custom brushes, hex colors, or `Font*` overrides).
    - Uses a 3-row `<Grid Margin="12">` displaying `AboutTitle` in a wrapped `TextBlock`, `AboutText` in a native system-themed read-only `TextBox` (`Padding="8"`, `TextWrapping="Wrap"`, `VerticalScrollBarVisibility="Auto"`, allowing users to select and copy text while honoring OS themes and High Contrast), and a leaf `OK` `Button` (`MinWidth="75"`, `Padding="12,3"`, `IsDefault="True"`, `IsCancel="True"`, `cal:Message.Attach="[Event Click] = [Action Close]"`).
11. **`Views/TestView.xaml` & `Views/TestView.xaml.cs`**
    - Simple 2-row, 2-column `UserControl` containing:
      - Row 0: `Label` (`"IsDirty"`) + `CheckBox` (`"Is Dirty"`, `IsChecked="{Binding IsDirty}"`)
      - Row 1: `Label` (`"Custom Text"`) + `TextBox` (`Text="{Binding CustomText,UpdateSourceTrigger=PropertyChanged}"`)

---

## 4. Current Runtime Behavior

### 4.1 Startup & Bootstrapper Sequence

1. WPF loads `App.xaml`, instantiating `Bootstrapper` as a resource dictionary entry.
2. `Bootstrapper` configures `LogManager.GetLog` to emit `CaliburnLogger` trace messages and calls `Initialize()`, which invokes `Configure()` to install the `ViewLocator.LocateTypeForModelType` context fallback.
3. On `Application.Startup`, `Bootstrapper.OnStartup` awaits `DisplayRootViewForAsync<MainWindowViewModel>()` *(updated in Iteration 6.2)*.
4. Caliburn.Micro instantiates `MainWindowViewModel` (via its parameterless constructor), resolves `MainWindowView` by naming convention, sets `MainWindowView.DataContext = MainWindowViewModel`, attaches `WindowConductor` (wiring `Window.Closing` to `MainWindowViewModel.CanCloseAsync`), activates `MainWindowViewModel` (`OnInitializedAsync` → `OnActivatedAsync`), and displays the window—initially empty for the first ~100 ms.

### 4.2 Asynchronous UI-Thread Document Seeding (`StartInitialDocuments`)

Inside `MainWindowViewModel.OnInitializedAsync` *(updated in Iterations 3.2 & 6.3)*:

```csharp
Items.CollectionChanged += Items_CollectionChanged;
_ = Execute.OnUIThreadAsync(StartInitialDocuments);
```

`StartInitialDocuments()` executes non-blockingly on the **STA UI `Dispatcher` thread** (`DispatcherSynchronizationContext` captures each `await Task.Delay(100)` continuation on the UI thread), pausing 100 ms before creating and activating each of the four initial documents:

| Approx. Time | Document Created | `_createCount` | Initial `IsDirty` (`_createCount % 2 > 0`) | Active Tab Transition |
|:-------------|:-----------------|:---------------|:-------------------------------------------|:----------------------|
| `+100 ms` | **`Test A`** | `1` | **`true`** *(red dot visible)* | `null` → `Test A` |
| `+200 ms` | **`Test B`** | `2` | **`false`** *(red dot collapsed)* | `Test A` deactivated (`close=false`) → `Test B` |
| `+300 ms` | **`Test C`** | `3` | **`true`** *(red dot visible)* | `Test B` deactivated (`close=false`) → `Test C` |
| `+400 ms` | **`Test D`** | `4` | **`false`** *(red dot collapsed)* | `Test C` deactivated (`close=false`) → `Test D` |

Once seeding completes (~400 ms), **`Test D` is the active/selected tab in both panes**, and all `Items` lookups, `_createCount` updates, `TestViewModel` allocations, and `ActivateItemAsync` calls have occurred strictly on the STA UI thread.

### 4.3 Side-by-Side Dual-Host Rendering & `View.Context` Isolation

Once all four documents have loaded, the window is split 50/50 horizontally, and **both panes simultaneously render live, interactive `TestView` instances** *(fixed in Iterations 4.1 & 4.2)*:

- **Left Pane (`AvalonDock` / `DockManager`):**
  - Displays the 4 tabs (`Test A` ●, `Test B`, `Test C` ●, `Test D`) with working headers, tooltips, close buttons, tab drag-reordering, tear-off floating windows (`LayoutDocumentFloatingWindowControl`), horizontal/vertical split tab groups (`OverlayWindow` drop targets and right-click `DocumentContextMenu`), the top-right pane overflow dropdown menu, and the `Ctrl+Tab` `NavigatorWindow`.
  - **Body Rendering:** Because `DockingManager.LayoutItemTemplate` is left unset, `LayoutItem.View` (`ContentPresenter`) resolves the implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` from `<DockingManager.Resources>` with `DataContext = TestViewModel` across both `Generic` and non-Generic themes. Its `<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>` binds using Caliburn.Micro's default (`null`) context, caching `TestView` #1 under `Views[View.DefaultContext]` (so parameterless `Screen.GetView()` returns the `DockManager` view).
  - *(Future v5 note: If standalone `DetachedAnchorableWindow` tool panes are later added to this app, moving the implicit `DataTemplate` from `<DockingManager.Resources>` to `App.xaml`'s `<Application.Resources>` ensures ownerless detached windows—whose `LayoutItem.View` is temporarily removed from `DockingManager`'s logical tree—also resolve the template.)*
- **Right Pane (`TabControl` / `TabManager`):**
  - Displays the same 4 tabs (`Test A` ●, `Test B`, `Test C` ●, `Test D`) with custom `Flat` hover/selection close buttons.
  - **Body Rendering:** Uses `<ContentControl cal:View.Model="{Binding}" cal:View.Context="TabControl" IsTabStop="False"/>`. Through `Bootstrapper.Configure()`'s `ViewLocator.LocateTypeForModelType` fallback, Caliburn.Micro resolves `TestView` for context `"TabControl"` and caches a **separate** `TestView` #2 instance under `Views["TabControl"]`, completely eliminating `ViewAware` visual theft between `DockManager` and `TabManager` (even when tabs are floated out of `DockManager`).

### 4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem`)

Selection is synchronized three ways through `MainWindowViewModel.ActiveItem`:

1. **Clicking a tab in `DockManager`:** Sets `LayoutDocument.IsActive = true` → updates `DockingManager.ActiveContent` → two-way binding sets `MainWindowViewModel.ActiveItem` → `Conductor<Screen>.Collection.OneActive` deactivates the previous `TestViewModel` (`OnDeactivateAsync(close: false)`), activates the newly selected `TestViewModel`, and updates `TabManager.SelectedItem`.
2. **Clicking a tab in `TabManager`:** Sets `TabManager.SelectedItem` → sets `MainWindowViewModel.ActiveItem` → sets `DockingManager.ActiveContent` → `InternalSetActiveContent` activates the matching `LayoutDocument` tab on the left.
3. **Clicking `Window -> Window A / B / C / D`:** Executes `ShowACommand` / `ShowBCommand` / `ShowCCommand` / `ShowDCommand` → `ActivateOrCreate<TestViewModel>(...)` → `ActivateItemAsync(item)` → updates `ActiveItem`, selecting the tab in both `DockManager` and `TabManager`.

### 4.5 Dirty-State Tracking & Bidirectional Live Editing

Inside **either** `DockManager`'s `TestView` (docked or floating) **or** `TabManager`'s `TestView`:

- **Typing into `Custom Text` (`TextBox`):** Because `UpdateSourceTrigger=PropertyChanged` is set on `Text="{Binding CustomText, UpdateSourceTrigger=PropertyChanged}"`, every keystroke in either pane immediately updates `ViewModelBase.CustomText`, mirrors the text into the opposite pane's `TextBox`, and sets `IsDirty = true`.
- **Toggling `Is Dirty` (`CheckBox`):** Directly toggles `ViewModelBase.IsDirty` (`true` / `false`) across both panes.
- **Visual Feedback:** Changing `IsDirty` immediately updates **four UI elements in lockstep**:
  1. The `Is Dirty` `CheckBox` inside `DockManager`'s `TestView`
  2. The `Is Dirty` `CheckBox` inside `TabManager`'s `TestView`
  3. The `6×6` red `DirtyIndicator` `Ellipse` in `DockManager`'s `DocumentHeaderTemplate` (`{Binding Content.IsDirty}`)
  4. The `6×6` red `DirtyIndicator` `Ellipse` in `TabManager`'s `ItemTemplate` (`{Binding IsDirty}`)

### 4.6 Document Closing, Confirmation Prompts, Floating Windows & App Exit

Because `MainWindowView.xaml` overrides `LayoutItem.CloseCommand` in `LayoutItemContainerStyle`:

```xml
<DockingManager.LayoutItemContainerStyle>
    <Style TargetType="{x:Type LayoutItem}">
        <Setter Property="Title" Value="{Binding Model.DisplayName, Mode=OneWay}"/>
        <Setter Property="CloseCommand" Value="{Binding Model.CloseCommand}"/>
    </Style>
</DockingManager.LayoutItemContainerStyle>
```

all UI-initiated document closes in both controls route through **`ViewModelBase.CloseCommand`** (`TryCloseAsync(null)`):

- **All UI Close Triggers Using This Path:**
  - Clicking the `X` close button on a `DockManager` tab (`LayoutDocumentTabItem`)
  - Middle-clicking a `DockManager` tab (`LayoutDocumentTabItem.OnMouseDown`)
  - Choosing **Close**, **Close All But This**, or **Close All** from `DockManager`'s tab context menu (`DockingManager.Close(LayoutContent)` checks `layoutItem.CloseCommand != null` first)
  - Dragging a tab out into a floating document window (`LayoutDocumentFloatingWindowControl`) and clicking its title-bar `X` button or pressing `Alt+F4` (`CloseWindowCommand` invokes `documentLayoutItem.CloseCommand`)
  - Clicking the Marlett `"r"` (`✕`) button on a `TabManager` tab
- **Close Execution Flow:**
  1. `CloseCommand` calls `TryCloseAsync(null)` on the `TestViewModel`.
  2. `MainWindowViewModel` (`Conductor<Screen>.Collection.OneActive`) invokes `TestViewModel.CanCloseAsync`.
  3. If **`IsDirty == false`**, `CanClosePrompt()` returns `true` immediately without showing a dialog.
  4. If **`IsDirty == true`**, `CanClosePrompt()` displays a modal `Yes`/`No` `MessageBox`:
     > *"Are you sure you want to close 'Test A'?"*
     - Clicking **No** returns `false`: the close is aborted and the tab remains open in both `DockManager` and `TabManager`.
     - Clicking **Yes** returns `true`: `Conductor` deactivates the item (`OnDeactivateAsync(close: true)`), selects the adjacent item in `Items` as the new `ActiveItem`, and removes the closed `TestViewModel` from `Items`.
  5. `Items.CollectionChanged` (`NotifyCollectionChangedAction.Remove`) fires:
     - `TabManager` removes its `TabItem`.
     - `DockingManager.DocumentsSourceElementsChanged` sets `documentToRemove.Content = null`, removes the `LayoutDocument` from its parent pane, detaches its `LayoutItem.View` from `DockingManager`'s logical children, and runs `Layout.CollectGarbage()` (closing any emptied floating window automatically).
- **Closing the Main Application Window (`MainWindowView`):**
  - Clicking the OS window `X` button or pressing `Alt+F4` on `MainWindowView` triggers Caliburn.Micro's `WindowConductor.Closing` handler, which evaluates `MainWindowViewModel.CanCloseAsync`.
  - `Conductor<Screen>.Collection.OneActive` iterates through `Items` and calls `CanCloseAsync` on each open `TestViewModel`, prompting with a `MessageBox` for every dirty document (`Test A`, `Test C`, etc.). Clicking **No** on any prompt cancels closing the main window!

### 4.7 Reopening Documents via the `_Window` Menu (`Window A`–`Window D`)

- Selecting **`Window -> Window A`**, **`Window B`**, **`Window C`**, or **`Window D`** *(completed in Iteration 2.2)*:
  - If the corresponding `TestViewModel` is still in `Items`, `ActivateOrCreate` simply activates/focuses it.
  - If it was previously closed, `ActivateOrCreate` creates a **new** `TestViewModel` with that `DisplayName`, increments `_createCount`, assigns `IsDirty = ++_createCount % 2 > 0` (meaning a recreated tab's initial dirty state flips depending on total tabs created so far!), adds it to `Items`, and activates it cleanly via `ActivateItemAsync` without leaking event subscriptions onto `MainWindowViewModel` *(fixed in Iteration 2.3)*.

### 4.8 `Help -> What's This App About...` Modal Dialog

- Selecting **`Help -> What's This App About...`** *(added in Iteration 5)* triggers Caliburn.Micro's `ActionMessage` (`cal:Message.Attach="[Event Click] = [Action ShowAbout]"`), which invokes and observes `MainWindowViewModel.ShowAbout()`.
- `ShowAbout()` calls `_windowManager.ShowDialogAsync(new AboutViewModel())`, resolving `AboutView` by naming convention, centering it over `MainWindowView` (`WindowStartupLocation="CenterOwner"`), and displaying the ~234-word summary of `CaliburnDockTestApp`'s side-by-side purpose and `View.Context` architecture.
- Clicking **OK** (or pressing `Enter` or `Esc`) executes `AboutViewModel.Close()` (`TryCloseAsync(true)`), closing the modal dialog cleanly.

### 4.9 Diagnostic Trace Output

When run under a debugger or with a `System.Diagnostics.TraceListener` attached, `CaliburnDockTestApp` produces verbose trace output from four sources (and **zero** WPF `System.Windows.Data` binding errors following Iteration 4.1):

1. **`CaliburnLogger` (`ILog`):** Logs `ViewLocator`, `ViewModelBinder` convention checks, and `Screen` activation/deactivation events.
2. **Custom ViewModel Traces:** `MainWindowViewModel.OnInitializedAsync`, `MainWindowViewModel.OnDeactivateAsync`, `MainWindowViewModel.Items_CollectionChanged`, `ViewModelBase.CanCloseAsync`, and `ViewModelBase.OnDeactivateAsync`.
3. **Spurious `Caliburn.Micro` `Screen.TryCloseAsync` Close Log (Present in `4.0.136-rc` through `6.0.15-beta`):** Every time a `TestViewModel` closes via `TryCloseAsync(null)`, `Caliburn.Micro` logs:
   ```text
   Screen: TryClose requires a parent IConductor or a view with a Close method or IsOpen property.
   ```
   because after `await conductor.CloseItemAsync(this, ...)` removes the item and clears `Parent = null`, `Screen.TryCloseAsync` continues into its view-closing fallback inspection instead of returning immediately.
4. **AvalonDock `#if TRACE` Layout Dumps:** Because the standard SDK defines `TRACE` in `Debug` and `Release`, AvalonDock's `LayoutRoot.CollectGarbage()` invokes `RootPanel.ConsoleDump(4)` after layout mutations, writing indented `Panel(Horizontal)` / `DocumentPaneGroup(Horizontal)` / `DocumentPane()` / `Document()` trees to `Trace`.

---

## 5. Remaining Deprecated, Obsolete & Broken Caliburn.Micro Usage

*(Items resolved in Iterations 1–7—singular `net48`, `Microsoft.NET.Sdk`, `ShowDCommand` / `Window D`, `ConductWith` memory leak, `#if CALIBURN_ASYNC` / Caliburn.Micro 3.2 shims, off-UI-thread `Task.Run` seeding, `ViewAware` dual-host view-theft, the v5/v6-ready system-default `AboutViewModel` / `AboutView` dialog, and upgrading to `Caliburn.Micro 6.0.15-beta` with `await DisplayRootViewForAsync<MainWindowViewModel>()` and `OnInitializedAsync`—are recorded in the [Applied Fixes Log](#applied-fixes-log).)*

`CaliburnDockTestApp` currently retains the following custom MVVM patterns that bypass Caliburn.Micro's action and dialog infrastructure:

| File & Line | Legacy / Non-Idiomatic Pattern Used | Status & Why It Is Problematic | Modern Caliburn.Micro Pattern |
|:------------|:------------------------------------|:-------------------------------|:------------------------------|
| `RelayCommand.cs:1–77`<br>`ViewModelBase.cs:39–46`<br>`MainWindowViewModel.cs:39–43` | Hand-rolled global-namespace `RelayCommand` wrapping async methods (`_ => TryCloseAsync(null)`, `_ => ActivateOrCreate<T>(...)`) | Bypasses Caliburn.Micro actions/conventions, pollutes the global namespace, discards returned `Task`s (`fire-and-forget`), and fails to hook `CommandManager.RequerySuggested` to `CanExecuteChanged`. | Use Caliburn.Micro actions (`cal:Message.Attach`, `x:Name` conventions) returning `Task` so Caliburn.Micro observes exceptions and manages guard availability (as demonstrated on `Help -> What's This App About...`). |
| `ViewModelBase.cs:48–57` | Direct `MessageBox.Show(...)` inside `ViewModelBase.CanClosePrompt()` | Couples a ViewModel directly to `System.Windows.MessageBox`, blocks the UI dispatcher synchronously inside async `CanCloseAsync`, and prevents headless unit testing (causing automated `Window.Close()` calls to hang on modal Win32 dialogs). | Inject `IWindowManager` or a dialog service and `await` an async confirmation dialog inside `CanCloseAsync`. |

---

## 6. Remaining Legacy, Misused & Superseded AvalonDock Usage

*(The broken `DockingManager.LayoutItemTemplate` / `{Binding Content}` pattern was resolved in Iteration 4.1 via an implicit `DataTemplate`.)*

`CaliburnDockTestApp` currently retains these AvalonDock v4-style patterns:

| File & Line | AvalonDock Pattern Used in `CaliburnDockTestApp` | Problem / Why It Is Broken or Superseded in AvalonDock v5 | Current AvalonDock v5 Best Practice |
|:------------|:-------------------------------------------------|:----------------------------------------------------------|:------------------------------------|
| `MainWindowView.xaml:61–62` | `DocumentsSource="{Binding Items}"` + unfiltered `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` | **Fragile v4 Mode-B binding:** `DockingManager.ActiveContent` is `object` (any active document, tool anchorable, or `null`). An unfiltered two-way binding to `Conductor<Screen>.Collection.OneActive.ActiveItem` breaks or corrupts `Conductor.Items` as soon as tool windows (`AnchorablesSource`) are clicked, and triggers fire-and-forget `set_ActiveItem` re-entrancy on layout transitions. | Bind `DockLayout="{Binding DockLayout}"` via `IDockLayoutService` (`AvalonDock.Mvvm`), or guard/filter `ActiveContent` synchronization so non-document anchorables and transient `null` states do not overwrite conductor state. |
| `MainWindowView.xaml:86` | `<Setter Property="CloseCommand" Value="{Binding Model.CloseCommand}"/>` on `LayoutItem` | **Bypasses AvalonDock's close lifecycle:** Replacing `LayoutItem.CloseCommand` prevents `DockingManager.ExecuteCloseCommand(LayoutDocument)` from running, so **`DocumentClosing` and `DocumentClosed` events never fire** on UI closes. Conversely, any programmatic `LayoutDocument.Close()` bypasses `Model.CloseCommand` / `CanCloseAsync` and leaves `Conductor.Items` out of sync with `DockingManager.Layout`. | Keep AvalonDock's default `CloseCommand` and handle `DockingManager.DocumentClosing`: set `e.Cancel = true` and route asynchronously through `IConductor.CloseItemAsync`, letting collection removal detach the `LayoutDocument` cleanly without re-entrancy. |
| `MainWindowView.xaml:83–88` | `LayoutItemContainerStyle` sets `Title` and `CloseCommand` only (no `ContentId`) | Every generated `LayoutDocument` has `ContentId == null` (`""`). Without `ContentId`, `XmlLayoutSerializer` (`Dirkster.AvalonDock.Serializer.Xml`) cannot match or persist documents across layout save/restore. | Implement `IDockable` (`Id`) on ViewModels and bind `<Setter Property="ContentId" Value="{Binding Model.Id}"/>` in `LayoutItemContainerStyle`. |
| `MainWindowView.xaml:63–77` | `DocumentHeaderTemplate` binds `{Binding Content.IsDirty}` and `{Binding Content.DisplayName, FallbackValue=#ERROR#}` | Tightly couples tab headers to `LayoutDocument.Content` (`TestViewModel`). During document close (`DockingManager.cs`), AvalonDock sets `document.Content = null` before visual teardown, transiently tripping `FallbackValue=#ERROR#`. | Bind `{Binding Title}` on the `LayoutContent` model (already populated via `LayoutItem.Title`), or guard custom header templates against `Content == null`. |
| `MainWindowView.xaml:59, 84` | Unprefixed `<DockingManager>` and `<LayoutItem>` tags in the default WPF XML namespace | Works only because `AvalonDock`'s `AssemblyInfo.cs` maps `http://schemas.microsoft.com/winfx/2006/xaml/presentation` alongside `https://github.com/Dirkster99/AvalonDock`, making AvalonDock types indistinguishable from built-in WPF controls in XAML. | Declare an explicit XAML namespace prefix: `xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"`. |
| `MainWindowView.xaml:59` | Unset `DockingManager.Theme`, `AllowFloatingWindows`, and `AllowDetachedWindows` | Falls back to the legacy unthemed `Themes/generic.xaml` dictionary and leaves v5 multi-window policies unconfigured. *(Note: moving the implicit `DataTemplate` from `<DockingManager.Resources>` to `Application.Resources` is recommended if `AllowDetachedWindows` tool panes are added later.)* | Reference a v5 theme package (e.g., `Dirkster.AvalonDock.Themes.Arc` with `ArcDarkTheme` / `ArcLightTheme`) and configure window policies explicitly. |

---

## 7. Summary Matrix of Current Behavior & Remaining Quirks

1. **Both `AvalonDock` (left) and `TabControl` (right) render live, independent `TestView` instances simultaneously** (`Views[DefaultContext]` vs `Views["TabControl"]`), with **zero** WPF data-binding errors and zero view-theft—even when tabs are torn off into floating windows *(fixed in Iteration 4)*.
2. **Startup populates 4 tabs (`Test A`, `Test B`, `Test C`, `Test D`) asynchronously over ~400 ms on the STA UI thread** (`Execute.OnUIThreadAsync(StartInitialDocuments)`), leaving `Test D` selected *(fixed in Iteration 3.2)*.
3. **Initial dirty flags alternate by creation counter (`_createCount % 2 > 0`):** `Test A` (dirty), `Test B` (clean), `Test C` (dirty), `Test D` (clean). Reopening any closed tab (`Test A`–`Test D`) via `_Window -> Window A/B/C/D` increments `_createCount` and flips whether it starts dirty or clean *(Window D menu completed in Iteration 2.2)*.
4. **Bidirectional live editing:** Typing into `Custom Text` or toggling `Is Dirty` in **either** `AvalonDock` or `TabControl` immediately updates both `TestView` instances and both red dirty-indicator dots in lockstep.
5. **Closing a dirty tab (from `AvalonDock`, a floating `AvalonDock` document window, or `TabControl`) prompts with a `Yes`/`No` `MessageBox`**; clicking `No` cancels the close in both controls.
6. **`Help -> What's This App About...` opens a `CenterOwner` modal `AboutView` dialog** (~234 words, selectable/copyable text) summarizing the side-by-side synchronization design and `View.Context` architecture *(added in Iteration 5)*.
7. **Runs cleanly on `Caliburn.Micro 6.0.15-beta` + `AvalonDock 5.1.0` (`net48`)** via `await DisplayRootViewForAsync<MainWindowViewModel>()` and `OnInitializedAsync`, compiling with `0 Warning(s), 0 Error(s)` under `--warnaserror` and **zero** `System.Text.Json` dependencies *(upgraded in Iteration 6)*.
