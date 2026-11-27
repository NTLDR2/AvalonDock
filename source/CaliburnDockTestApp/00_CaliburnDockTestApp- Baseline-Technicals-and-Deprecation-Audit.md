# CaliburnDockTestApp — Baseline Technical Documentation & Deprecation Audit

> **Status:** Legacy / Historical Diagnostic Baseline (AvalonDock v4 era, August 2020)  
> **Location:** `source/CaliburnDockTestApp/`  
> **Target Framework:** `.NET Framework 4.8` (`net48`)  
> **Pinned Frameworks:** `Caliburn.Micro` `4.0.136-rc` + ProjectReference to `AvalonDock` (`5.1.0`)

This document captures an exact, empirically verified baseline of what **`CaliburnDockTestApp`** does—file by file, binding by binding, and runtime step by runtime step—along with a complete audit of the deprecated, removed, broken, and superseded **Caliburn.Micro** and **AvalonDock** APIs and patterns it uses.

---

## Table of Contents

1. [Purpose & Historical Provenance](#1-purpose--historical-provenance)
2. [Project Configuration & Dependencies](#2-project-configuration--dependencies)
3. [Complete File-by-File Inventory](#3-complete-file-by-file-inventory)
4. [Exact Runtime Behavior (Step-by-Step Baseline)](#4-exact-runtime-behavior-step-by-step-baseline)
   - [4.1 Startup & Bootstrapper Sequence](#41-startup--bootstrapper-sequence)
   - [4.2 Background Document Seeding (`StartInitialDocuments`)](#42-background-document-seeding-startinitialdocuments)
   - [4.3 Visual Layout & The Blank Document Body Bug in `DockManager`](#43-visual-layout--the-blank-document-body-bug-in-dockmanager)
   - [4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem`)](#44-active-tab-synchronization-activecontent--activeitem--selecteditem)
   - [4.5 Dirty-State Tracking & Live Editing](#45-dirty-state-tracking--live-editing)
   - [4.6 Document Closing, Confirmation Prompts, Floating Windows & App Exit](#46-document-closing-confirmation-prompts-floating-windows--app-exit)
   - [4.7 Reopening Documents via the `_Window` Menu (and the Missing `Test D` Menu Item)](#47-reopening-documents-via-the-_window-menu-and-the-missing-test-d-menu-item)
   - [4.8 Diagnostic Trace Output](#48-diagnostic-trace-output)
5. [Deprecated, Obsolete & Broken Caliburn.Micro Usage](#5-deprecated-obsolete--broken-caliburnmicro-usage)
6. [Legacy, Misused & Superseded AvalonDock Usage](#6-legacy-misused--superseded-avalondock-usage)
7. [Summary Matrix of Baseline Quirks & Bugs](#7-summary-matrix-of-baseline-quirks--bugs)

---

## 1. Purpose & Historical Provenance

`CaliburnDockTestApp` was originally created in August 2020 (`commit c1be099`, merged in PR `#185` — *"Fix ObservableCollection Reset notification handling"*) during the AvalonDock v4 cycle as a minimal side-by-side diagnostic harness.

Its goal was to compare **AvalonDock's `DockingManager`** (left half of the window) directly against a standard **WPF `TabControl`** (right half of the window) when **both controls are bound simultaneously to the exact same Caliburn.Micro `Conductor<Screen>.Collection.OneActive` ViewModel (`MainWindowViewModel`)**:

- **`DocumentsSource="{Binding Items}"`** (on `DockingManager`) vs **`ItemsSource="{Binding Items}"`** (on `TabControl`)
- **`ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`** (on `DockingManager`) vs **`SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`** (on `TabControl`)
- Per-tab dirty state (`IsDirty`) rendered as a red dot (`Ellipse`) in both tab headers
- Cancellable close confirmation (`MessageBox.Show` inside `Screen.CanCloseAsync`) routed via a custom `RelayCommand` (`CloseCommand`)
- Re-activating or re-creating tabs from a top `_Window` menu (`ShowACommand`, `ShowBCommand`, `ShowCCommand`)

Although `CaliburnDockTestApp` compiles cleanly (`0 Warning(s), 0 Error(s)`) as a standalone project on `net48`, it is **not currently included in `source/AvalonDock.sln`**, pins an early 2020 release candidate of Caliburn.Micro (`4.0.136-rc`), contains dead Caliburn.Micro 3.2 `#else` conditional code, and exhibits several significant runtime bugs—most notably that **document views fail to render inside the `AvalonDock` pane (appearing completely blank) while rendering only in the right-hand `TabControl`**.

---

## 2. Project Configuration & Dependencies

Defined in `CaliburnDockTestApp.csproj` (inheriting repository-wide settings from `source/Directory.Build.props`):

| Setting / Dependency | Value | Notes |
|:---------------------|:------|:------|
| **MSBuild SDK** | `Microsoft.NET.Sdk.WindowsDesktop` | Legacy .NET Core 3.x WPF SDK moniker (superseded by `Microsoft.NET.Sdk` with `<UseWPF>true</UseWPF>`). |
| **Output Type** | `WinExe` | Desktop WPF executable (`CaliburnDockTestApp.exe`). |
| **Target Framework** | `net48` | Uses plural `<TargetFrameworks>net48</TargetFrameworks>` left over from earlier multi-TFM edits. |
| **Debug Constants** | `DEBUG;TRACE;CALIBURN_ASYNC` | Enables async Caliburn.Micro 4.x code paths (`#if CALIBURN_ASYNC`) and `Trace` logging. |
| **Release Constants** | `TRACE;CALIBURN_ASYNC` | Keeps `TRACE` enabled in Release, which also keeps AvalonDock's `#if TRACE` `ConsoleDump` active. |
| **`Caliburn.Micro`** | `4.0.136-rc` (NuGet) | Pulls `Caliburn.Micro.Core` (`4.0.136-rc`), `Caliburn.Micro.Platform` (`4.0.136-rc`), and `Microsoft.Xaml.Behaviors.Wpf` (`1.1.19`). |
| **`AvalonDock`** | `..\Components\AvalonDock\AvalonDock.csproj` | Brings `AvalonDock.dll` and transitively `AvalonDock.Core.dll`. |
| **Not Referenced** | `AvalonDock.Themes.*`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, `AvalonDock.Serializer.Xml` | Uses AvalonDock's built-in fallback `Themes/generic.xaml`, raw `DocumentsSource` binding, no layout persistence, and no IoC container. |
| **Static Analysis** | `Directory.Build.props` (`LangVersion 12`, `TreatWarningsAsErrors=true`) | Runs `NetAnalyzers` and `SonarAnalyzer.CSharp`, but **skips `StyleCop.Analyzers`** because `Directory.Build.props` exempts projects whose name contains `"Test"`. |

To build and launch from the repository root:

```powershell
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Debug
.\source\CaliburnDockTestApp\bin\Debug\net48\CaliburnDockTestApp.exe
```

---

## 3. Complete File-by-File Inventory

`CaliburnDockTestApp` consists of **14 source and configuration files**:

```text
source/CaliburnDockTestApp/
├── App.xaml                        # Application resources; instantiates Bootstrapper (no StartupUri)
├── App.xaml.cs                     # Empty Application code-behind partial class
├── AssemblyInfo.cs                 # WPF [assembly: ThemeInfo(None, SourceAssembly)]
├── Bootstrapper.cs                 # Caliburn.Micro BootstrapperBase; hooks CaliburnLogger & launches MainWindowViewModel
├── CaliburnDockTestApp.csproj      # SDK-style WPF project targeting net48 with Caliburn.Micro 4.0.136-rc
├── CaliburnLogger.cs               # Implements Caliburn.Micro.ILog forwarding to System.Diagnostics.Trace
├── RelayCommand.cs                 # Global-namespace ICommand / RelayCommand<T> implementation
├── ViewModels/
│   ├── MainWindowViewModel.cs      # Root Conductor<Screen>.Collection.OneActive managing TestViewModel items
│   ├── TestViewModel.cs            # Empty concrete document ViewModel subclassing ViewModelBase
│   └── ViewModelBase.cs            # Abstract Screen base with IsDirty, CustomText, CloseCommand & CanClosePrompt()
└── Views/
    ├── MainWindowView.xaml         # 800x450 split window: Menu + DockingManager (left) + TabControl (right)
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
   - `OnStartup(object sender, StartupEventArgs e)` calls `DisplayRootViewFor<MainWindowViewModel>()` without awaiting the returned `Task`.
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
   - **`#if CALIBURN_ASYNC` vs `#else`:**
     - `#if CALIBURN_ASYNC` *(active)*: Overrides `CanCloseAsync(CancellationToken)` (calling `CanClosePrompt()` synchronously and wrapping in `Task.FromResult`) and `OnDeactivateAsync(bool close, CancellationToken)` (tracing `close`).
     - `#else` *(inactive Caliburn.Micro 3.2 shim)*: Adds a `TryCloseAsync(bool?)` shim calling `TryClose(dialogResult)`, plus synchronous `CanClose(Action<bool>)` and `OnDeactivate(bool)` overrides.
6. **`ViewModels/TestViewModel.cs`**
   - Empty marker class `public class TestViewModel : ViewModelBase { }` resolved by Caliburn.Micro's naming convention (`TestViewModel` → `TestView`).
7. **`ViewModels/MainWindowViewModel.cs`**
   - `public class MainWindowViewModel : Conductor<Screen>.Collection.OneActive`.
   - Initializes four private `RelayCommand` fields in its constructor (`_showACommand`, `_showBCommand`, `_showCCommand`, `_showDCommand`) for `"Test A"` through `"Test D"`, **but only exposes public properties `ShowACommand`, `ShowBCommand`, and `ShowCCommand`** (`_showDCommand` is never exposed).
   - **`ActivateOrCreate<T>(string displayName)`:**
     - Looks up an existing item in `Items` matching type `T` and `DisplayName == displayName`.
     - If none exists, instantiates `T` via `Activator.CreateInstance`, sets `item.Parent = this`, calls `item.ConductWith(this)`, sets `item.DisplayName = displayName`, assigns **`item.IsDirty = ++_createCount % 2 > 0`** (alternating `true` on odd creations, `false` on even creations), and returns `ActivateItemAsync(item, CancellationToken.None)`.
   - **`OnInitializeAsync` & `StartInitialDocuments()`:**
     - Subscribes `Items.CollectionChanged += Items_CollectionChanged;` (logging collection `Action`, `OldItems`, and `NewItems` to `Trace`).
     - Fires `Task.Run(() => StartInitialDocuments())` without awaiting it, creating `"Test A"`, `"Test B"`, `"Test C"`, and `"Test D"` at 100 ms intervals on a background ThreadPool thread.
8. **`Views/MainWindowView.xaml` & `Views/MainWindowView.xaml.cs`**
   - `800 × 450` `Window` (`Title="Caliburn AvalonDock Test"`).
   - **Top Menu (`Grid.Row="0"`, `Grid.ColumnSpan="2"`):** `_Window` menu containing `Window A` (`ShowACommand`), `Window B` (`ShowBCommand`), and `Window C` (`ShowCCommand`).
   - **Left Column (`Grid.Column="0"` — `"AvalonDock"`):**
     - `DockingManager` (`x:Name="DockManager"`) bound to `DocumentsSource="{Binding Items}"` and `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"`.
     - Custom `DocumentHeaderTemplate` showing a `6×6` red `Ellipse` (`Content.IsDirty`) + `TextBlock` (`Content.DisplayName`, `FallbackValue=#ERROR#`).
     - Custom `LayoutItemTemplate` containing `<ContentControl cal:View.Model="{Binding Content}" IsTabStop="False"/>`.
     - `LayoutItemContainerStyle` (`TargetType="{x:Type LayoutItem}"`) binding `Title="{Binding Model.DisplayName, Mode=OneWay}"` and `CloseCommand="{Binding Model.CloseCommand}"`.
   - **Right Column (`Grid.Column="1"` — `"TabControl"`):**
     - Standard WPF `TabControl` (`x:Name="TabManager"`) bound to `ItemsSource="{Binding Items}"` and `SelectedItem="{Binding ActiveItem, Mode=TwoWay}"`.
     - Custom `ItemTemplate` showing an identical `6×6` red `Ellipse` (`IsDirty`), `TextBlock` (`DisplayName`), and a flat `Marlett`-font `"r"` (`✕`) close `Button` (`Command="{Binding CloseCommand}"`) visible when the tab is selected or hovered.
     - Custom `ContentTemplate` containing `<ContentControl cal:View.Model="{Binding}" IsTabStop="False"/>`.
9. **`Views/TestView.xaml` & `Views/TestView.xaml.cs`**
   - Simple 2-row, 2-column `UserControl` containing:
     - Row 0: `Label` (`"IsDirty"`) + `CheckBox` (`"Is Dirty"`, `IsChecked="{Binding IsDirty}"`)
     - Row 1: `Label` (`"Custom Text"`) + `TextBox` (`Text="{Binding CustomText,UpdateSourceTrigger=PropertyChanged}"`)

---

## 4. Exact Runtime Behavior (Step-by-Step Baseline)

### 4.1 Startup & Bootstrapper Sequence

1. WPF loads `App.xaml`, instantiating `Bootstrapper` as a resource dictionary entry.
2. `Bootstrapper` configures `LogManager.GetLog` to emit `CaliburnLogger` trace messages and calls `Initialize()`.
3. On `Application.Startup`, `Bootstrapper.OnStartup` invokes `DisplayRootViewFor<MainWindowViewModel>()`.
4. Caliburn.Micro instantiates `MainWindowViewModel`, resolves `MainWindowView` by naming convention, sets `MainWindowView.DataContext = MainWindowViewModel`, attaches `WindowConductor` (wiring `Window.Closing` to `MainWindowViewModel.CanCloseAsync`), activates `MainWindowViewModel` (`OnInitializeAsync` → `OnActivateAsync`), and displays the window—initially empty for the first ~100 ms.

### 4.2 Background Document Seeding (`StartInitialDocuments`)

Inside `MainWindowViewModel.OnInitializeAsync`:

```csharp
Items.CollectionChanged += Items_CollectionChanged;
Task.Run(() => StartInitialDocuments());
```

`StartInitialDocuments()` executes on a **ThreadPool background thread** (`SynchronizationContext.Current == null`), pausing 100 ms before creating and activating each of the four initial documents:

| Approx. Time | Document Created | `_createCount` | Initial `IsDirty` (`_createCount % 2 > 0`) | Active Tab Transition |
|:-------------|:-----------------|:---------------|:-------------------------------------------|:----------------------|
| `+100 ms` | **`Test A`** | `1` | **`true`** *(red dot visible)* | `null` → `Test A` |
| `+200 ms` | **`Test B`** | `2` | **`false`** *(red dot collapsed)* | `Test A` deactivated (`close=false`) → `Test B` |
| `+300 ms` | **`Test C`** | `3` | **`true`** *(red dot visible)* | `Test B` deactivated (`close=false`) → `Test C` |
| `+400 ms` | **`Test D`** | `4` | **`false`** *(red dot collapsed)* | `Test C` deactivated (`close=false`) → `Test D` |

> **Why cross-thread collection updates do not immediately crash WPF:** Even though `ActivateOrCreate` runs on a ThreadPool worker thread, `Conductor.Items` is a Caliburn.Micro `BindableCollection<Screen>`, which marshals `CollectionChanged` and `PropertyChanged` notifications onto the UI `Dispatcher` via `Execute.OnUIThread`. Both `DockManager` (`DocumentsSourceElementsChanged`) and `TabManager` (`ItemsSource`) therefore receive the `Add` notifications on the STA UI thread. Once seeding completes (~400 ms), **`Test D` is the active/selected tab in both panes**.

### 4.3 Visual Layout & The Blank Document Body Bug in `DockManager`

Once all four documents have loaded, the window is split 50/50 horizontally:

- **Right Pane (`TabControl` / `TabManager`):** Displays 4 tabs (`Test A` ●, `Test B`, `Test C` ●, `Test D`) **and renders `TestView` inside the tab body** (`IsDirty` checkbox and `Custom Text` textbox).
- **Left Pane (`AvalonDock` / `DockManager`):** Displays the same 4 tabs (`Test A` ●, `Test B`, `Test C` ●, `Test D`) with working headers, tooltips, close buttons, tab drag-reordering, tear-off floating windows (`LayoutDocumentFloatingWindowControl`), horizontal/vertical split tab groups (`OverlayWindow` drop targets and right-click `DocumentContextMenu`), the top-right pane overflow dropdown menu (which inherits `DocumentHeaderTemplate` including the red dirty dot via `DocumentPaneMenuItemHeaderTemplate` coercion), and the `Ctrl+Tab` `NavigatorWindow`—**but the document content area underneath the tab strip is 100% blank!**
  - *(Note: When a single tab is torn off into a floating document window, the floating window chrome uses `DocumentTitleTemplate` bound to `LayoutDocument.Title` rather than `DocumentHeaderTemplate`, so the floating window title bar shows `"Test A"` without the red dirty indicator dot.)*
  - *(Note: On the right `TabControl`, `CloseTabButton` uses a custom `Flat` style with ordered `DataTrigger`s on the ancestor `TabItem`—`IsSelected == False` → `Hidden`, overridden by `IsMouseOver == True` → `Visible`—specifically crafted to mimic `AvalonDock`'s `LayoutDocumentTabItem` close-button hover/selection behavior.)*

#### Root Cause 1: `LayoutItemTemplate` `DataContext` Mismatch (`{Binding Content}` on `TestViewModel`)

In `MainWindowView.xaml` (lines 72–76):

```xml
<DockingManager.LayoutItemTemplate>
    <DataTemplate>
        <ContentControl cal:View.Model="{Binding Content}" IsTabStop="False"/>
    </DataTemplate>
</DockingManager.LayoutItemTemplate>
```

Because `CaliburnDockTestApp` does **not** assign a `Theme` to `DockingManager`, AvalonDock uses `Themes/generic.xaml`. In `generic.xaml` (`LayoutDocumentControl` template), the outer `ContentPresenter` hosts `LayoutItem.View` directly without setting `ContentTemplate`:

```xml
<!-- Themes/generic.xaml: LayoutDocumentControl -->
<ContentPresenter Content="{Binding LayoutItem.View, RelativeSource={RelativeSource TemplatedParent}}" />
```

Inside `LayoutItem.View` (an inner `ContentPresenter`), `Content` is bound to `LayoutDocument.Content` (`TestViewModel`) and `ContentTemplate` is bound to `DockingManager.LayoutItemTemplate`. Consequently, WPF instantiates `LayoutItemTemplate` with **`DataContext = TestViewModel`**. Evaluating `{Binding Content}` on `TestViewModel` fails at runtime for every tab:

```text
System.Windows.Data Error: 40 : BindingExpression path error: 'Content' property not found on 'object' ''TestViewModel''.
BindingExpression:Path=Content; DataItem='TestViewModel'; target element is 'ContentControl' (Name=''); target property is 'Model' (type 'Object')
```

`cal:View.Model` receives `null`, leaving `ContentControl.Content` empty inside `DockManager`.

*(Conversely, in AvalonDock's non-Generic theme dictionaries such as `Aero`, `VS2010`, `Expression`, `Metro`, and `Arc`, `LayoutDocumentControl`'s outer `ContentPresenter` binds **both** `Content="{Binding LayoutItem.View}"` and `ContentTemplate="{Binding LayoutItem.View.ContentTemplate}"`, which causes WPF to instantiate `LayoutItemTemplate` with `DataContext = LayoutItem.View` (`ContentPresenter`) rather than `TestViewModel`—the historical quirk that originally led the author to write `{Binding Content}`!)*

#### Root Cause 2: Hidden `ViewAware` Single-View Caching Collision Between Left & Right Panes

Even if `DockManager`'s binding were changed so `cal:View.Model` received `TestViewModel` (for example, `cal:View.Model="{Binding}"` under `Generic` theme, or applying `AeroTheme`), **both panes still cannot display `TestView` simultaneously** as written:

- `TestViewModel` inherits `Screen` → `ViewAware` (`CacheViews = true` by default).
- Neither `DockManager` nor `TabManager` sets `cal:View.Context` (both use `View.DefaultContext`).
- `ViewLocator.LocateForModel` returns the **exact same `TestView` `UserControl` instance** for a given `TestViewModel`. Because a WPF `Visual` can have only one parent, Caliburn.Micro detaches `TestView` from the first `ContentControl` (`Content = null`) when assigning it to the second `ContentControl`, causing one side to steal the visual from the other!

### 4.4 Active Tab Synchronization (`ActiveContent` ↔ `ActiveItem` ↔ `SelectedItem`)

Selection is synchronized three ways through `MainWindowViewModel.ActiveItem`:

1. **Clicking a tab in `DockManager`:** Sets `LayoutDocument.IsActive = true` → updates `DockingManager.ActiveContent` → two-way binding sets `MainWindowViewModel.ActiveItem` → `Conductor<Screen>.Collection.OneActive` deactivates the previous `TestViewModel` (`OnDeactivateAsync(close: false)`), activates the newly selected `TestViewModel`, and updates `TabManager.SelectedItem` (which renders that item's `TestView` on the right).
2. **Clicking a tab in `TabManager`:** Sets `TabManager.SelectedItem` → sets `MainWindowViewModel.ActiveItem` → sets `DockingManager.ActiveContent` → `InternalSetActiveContent` activates the matching `LayoutDocument` tab on the left.
3. **Clicking `Window -> Window A / B / C`:** Executes `ShowACommand` / `ShowBCommand` / `ShowCCommand` → `ActivateOrCreate<TestViewModel>(...)` → `ActivateItemAsync(item)` → updates `ActiveItem`, selecting the tab in both `DockManager` and `TabManager`.

### 4.5 Dirty-State Tracking & Live Editing

Inside `TabManager`'s rendered `TestView`:

- **Typing into `Custom Text` (`TextBox`):** Because `UpdateSourceTrigger=PropertyChanged` is set on `Text="{Binding CustomText, UpdateSourceTrigger=PropertyChanged}"`, the very first keystroke updates `ViewModelBase.CustomText`, which executes `if (Set(ref _customText, value)) IsDirty = true;`.
- **Toggling `Is Dirty` (`CheckBox`):** Directly toggles `ViewModelBase.IsDirty` (`true` / `false`).
- **Visual Feedback:** Changing `IsDirty` immediately updates **three UI elements in lockstep**:
  1. The `Is Dirty` `CheckBox` inside `TestView`
  2. The `6×6` red `DirtyIndicator` `Ellipse` in `DockManager`'s `DocumentHeaderTemplate` (`{Binding Content.IsDirty}`)
  3. The `6×6` red `DirtyIndicator` `Ellipse` in `TabManager`'s `ItemTemplate` (`{Binding IsDirty}`)

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

### 4.7 Reopening Documents via the `_Window` Menu (and the Missing `Test D` Menu Item)

- Selecting **`Window -> Window A`**, **`Window B`**, or **`Window C`**:
  - If the corresponding `TestViewModel` is still in `Items`, `ActivateOrCreate` simply activates/focuses it.
  - If it was previously closed, `ActivateOrCreate` creates a **new** `TestViewModel` with that `DisplayName`, increments `_createCount`, assigns `IsDirty = ++_createCount % 2 > 0` (meaning a recreated tab's initial dirty state flips depending on total tabs created so far!), adds it to `Items`, and activates it.
- **Dead `_showDCommand` Bug:** `MainWindowViewModel`'s constructor initializes `_showDCommand` for `"Test D"`, **but omits a public `ShowDCommand` property**, and `MainWindowView.xaml` omits a `<MenuItem Header="Window D" .../>`. Once the user closes `Test D`, there is **no way in the UI to reopen `Test D`**.

### 4.8 Diagnostic Trace Output

When run under a debugger or with a `System.Diagnostics.TraceListener` attached, `CaliburnDockTestApp` produces verbose trace output from four sources:

1. **`CaliburnLogger` (`ILog`):** Logs every `ViewLocator`, `ViewModelBinder` convention miss (`Action Convention Not Applied`, `Binding Convention Not Applied: Element DockManager did not match a property`), and `Screen` activation/deactivation event.
2. **Custom ViewModel Traces:** `MainWindowViewModel.OnInitializeAsync`, `MainWindowViewModel.OnDeactivateAsync`, `MainWindowViewModel.Items_CollectionChanged`, `ViewModelBase.CanCloseAsync`, and `ViewModelBase.OnDeactivateAsync`.
3. **Spurious `Caliburn.Micro 4.0.136-rc` Close Log:** Every time a `TestViewModel` closes via `TryCloseAsync(null)`, `Caliburn.Micro 4.0.136-rc` logs:
   ```text
   Screen: TryClose requires a parent IConductor or a view with a Close method or IsOpen property.
   ```
   because in `4.0.136-rc`, after `await conductor.CloseItemAsync(this, ...)` removes the item and clears `Parent = null`, `Screen.TryCloseAsync` continues into its view-closing fallback inspection instead of returning immediately.
4. **AvalonDock `#if TRACE` Layout Dumps:** Because `TRACE` is defined in both `Debug` and `Release`, AvalonDock's `LayoutRoot.CollectGarbage()` invokes `RootPanel.ConsoleDump(4)` after layout mutations, writing indented `Panel(Horizontal)` / `DocumentPaneGroup(Horizontal)` / `DocumentPane()` / `Document()` trees to `Trace`.

---

## 5. Deprecated, Obsolete & Broken Caliburn.Micro Usage

`CaliburnDockTestApp` relies on transitional `Caliburn.Micro 4.0.136-rc` APIs, dead 3.x shims, and several MVVM anti-patterns that either fail to compile or misbehave in modern Caliburn.Micro (`4.0.212` / `5.x` / `6.x`):

| File & Line | Legacy / Deprecated Pattern Used | Status & Why It Is Obsolete or Broken | Modern Caliburn.Micro Pattern |
|:------------|:---------------------------------|:--------------------------------------|:------------------------------|
| `CaliburnDockTestApp.csproj:18` | `<PackageReference Include="Caliburn.Micro" Version="4.0.136-rc" />` | Pre-release 2020 RC build with unfinished API renames and the `Screen.TryCloseAsync` fallthrough log quirk. | Stable `Caliburn.Micro` (`5.0.258` or `6.0.15-beta`). |
| `Bootstrapper.cs:15–19` | `protected override void OnStartup(...) { DisplayRootViewFor<MainWindowViewModel>(); }` | **Removed in Caliburn.Micro 4.0 final / 5.x / 6.x** (`CS0103: DisplayRootViewFor does not exist`). Also drops the returned `Task` unawaited (`fire-and-forget`), hiding async startup exceptions. | `protected override async void OnStartup(object sender, StartupEventArgs e) { await DisplayRootViewForAsync<MainWindowViewModel>(); }` |
| `MainWindowViewModel.cs:82` | `protected override Task OnInitializeAsync(CancellationToken cancellationToken)` | **Marked `[Obsolete("Override OnInitializedAsync")]`** in Caliburn.Micro 4.0 final / 5.x / 6.x (fails build with `CS0672` when `TreatWarningsAsErrors=true`; same applies to `OnActivateAsync` → `OnActivatedAsync`). | Override `OnInitializedAsync(CancellationToken)` and `OnActivatedAsync(CancellationToken)`. |
| `CaliburnDockTestApp.csproj:10,14`<br>`ViewModelBase.cs:59–93`<br>`MainWindowViewModel.cs:74–113` | `#if CALIBURN_ASYNC` / `#else` synchronous shims (`TryClose`, `CanClose(Action<bool>)`, `OnDeactivate(bool)`, `ActivateItem`, `OnInitialize`) | **All synchronous lifecycle methods in `#else` were removed in Caliburn.Micro 4.0+.** Additionally, `#else`'s `OnInitialize()` forgot `Items.CollectionChanged += Items_CollectionChanged;`. | Remove `#if CALIBURN_ASYNC` and dead `#else` branches; use pure async lifecycle overrides. |
| `RelayCommand.cs:1–77`<br>`ViewModelBase.cs:39–46`<br>`MainWindowViewModel.cs:29–33` | Hand-rolled global-namespace `RelayCommand` wrapping async methods (`_ => TryCloseAsync(null)`, `_ => ActivateOrCreate<T>(...)`) | Bypasses Caliburn.Micro actions/conventions, pollutes the global namespace, discards returned `Task`s (`fire-and-forget`), and fails to hook `CommandManager.RequerySuggested` to `CanExecuteChanged`. | Use Caliburn.Micro actions (`cal:Message.Attach`, `x:Name` conventions) returning `Task` so Caliburn.Micro observes exceptions and manages guard availability. |
| `MainWindowViewModel.cs:44–45` | `item.Parent = this;` followed by `item.ConductWith(this);` on items added to `Conductor.Items` | **Redundant & leaks memory:** `Conductor.ActivateItemAsync` already sets `item.Parent = this` and conducts items in `Items`. `ConductWith` attaches `Activated`/`Deactivated` handlers onto `MainWindowViewModel` that **never unsubscribe when individual tabs close**, keeping closed `TestViewModel` instances rooted in memory for the lifetime of the shell! | Remove `item.Parent = this;` and `item.ConductWith(this);`—let `Conductor<Screen>.Collection.OneActive` conduct its own `Items`. |
| `MainWindowViewModel.cs:87, 115–125` | `Task.Run(() => StartInitialDocuments());` mutating `Conductor` from a ThreadPool thread | Unawaited background task reading `Items`, incrementing `_createCount` without synchronization, and constructing/activating screens off the STA UI thread. | `await` UI-thread async initialization (or marshal screen creation/activation explicitly onto the UI thread). |
| `ViewModelBase.cs:48–57` | Direct `MessageBox.Show(...)` inside `ViewModelBase.CanClosePrompt()` | Couples a ViewModel directly to `System.Windows.MessageBox`, blocks the UI dispatcher synchronously inside async `CanCloseAsync`, and prevents headless unit testing (causing automated `Window.Close()` calls to hang on modal Win32 dialogs). | Inject `IWindowManager` or a dialog service and `await` an async confirmation dialog inside `CanCloseAsync`. |
| `MainWindowView.xaml:74, 142` | Binding two `ContentControl`s with `cal:View.Model` to the same `Screen` without `cal:View.Context` | `Screen` (`ViewAware`) caches a single view instance under `View.DefaultContext`, causing dual hosts to steal the single `TestView` visual from one another. | Avoid hosting the same `ViewAware` instance twice, or specify distinct `cal:View.Context` values / `CacheViews = false`. |
| `CaliburnDockTestApp.csproj:1` | `<Project Sdk="Microsoft.NET.Sdk.WindowsDesktop">` | Legacy .NET Core 3.0 SDK name. | `<Project Sdk="Microsoft.NET.Sdk">` with `<UseWPF>true</UseWPF>`. |

---

## 6. Legacy, Misused & Superseded AvalonDock Usage

`CaliburnDockTestApp` reflects raw AvalonDock v4 experimentation before AvalonDock v5 introduced `AvalonDock.Core` (`IDockable`, `IRootDock`), `AvalonDock.Mvvm` (`IDockLayoutService`, `LayoutSyncBridge`), `DetachedAnchorableWindow`, and modernized theme/serialization contracts:

| File & Line | AvalonDock Pattern Used in `CaliburnDockTestApp` | Problem / Why It Is Broken or Superseded in AvalonDock v5 | Current AvalonDock v5 Best Practice |
|:------------|:-------------------------------------------------|:----------------------------------------------------------|:------------------------------------|
| `MainWindowView.xaml:72–76` | `<DockingManager.LayoutItemTemplate>` with `<ContentControl cal:View.Model="{Binding Content}" />` | **Broken in `Generic` theme & `DetachedAnchorableWindow`:** `LayoutItemTemplate`'s `DataContext` is the **ViewModel** in `Generic` `LayoutDocumentControl` and in v5's `DetachedAnchorableWindow` (so `{Binding Content}` fails and renders blank!), whereas in themed `Layout*Control`s (`Arc`, `Aero`, `VS2010`, `Expression`, `Metro`) the outer `ContentPresenter` re-applies `ContentTemplate` onto `LayoutItem.View`, making `DataContext = ContentPresenter` (where `{Binding}` fails!). | Leave `DockingManager.LayoutItemTemplate` **unset** and register an **implicit `<DataTemplate DataType="{x:Type vm:MyViewModel}">`** in resources so `DataContext` is always the ViewModel across every theme and window mode. |
| `MainWindowView.xaml:55–56` | `DocumentsSource="{Binding Items}"` + unfiltered `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` | **Fragile v4 Mode-B binding:** `DockingManager.ActiveContent` is `object` (any active document, tool anchorable, or `null`). An unfiltered two-way binding to `Conductor<Screen>.Collection.OneActive.ActiveItem` breaks or corrupts `Conductor.Items` as soon as tool windows (`AnchorablesSource`) are clicked, and triggers fire-and-forget `set_ActiveItem` re-entrancy on layout transitions. | Bind `DockLayout="{Binding DockLayout}"` via `IDockLayoutService` (`AvalonDock.Mvvm`), or guard/filter `ActiveContent` synchronization so non-document anchorables and transient `null` states do not overwrite conductor state. |
| `MainWindowView.xaml:80` | `<Setter Property="CloseCommand" Value="{Binding Model.CloseCommand}"/>` on `LayoutItem` | **Bypasses AvalonDock's close lifecycle:** Replacing `LayoutItem.CloseCommand` prevents `DockingManager.ExecuteCloseCommand(LayoutDocument)` from running, so **`DocumentClosing` and `DocumentClosed` events never fire** on UI closes. Conversely, any programmatic `LayoutDocument.Close()` bypasses `Model.CloseCommand` / `CanCloseAsync` and leaves `Conductor.Items` out of sync with `DockingManager.Layout`. | Keep AvalonDock's default `CloseCommand` and handle `DockingManager.DocumentClosing`: set `e.Cancel = true` and route asynchronously through `IConductor.CloseItemAsync`, letting collection removal detach the `LayoutDocument` cleanly without re-entrancy. |
| `MainWindowView.xaml:77–82` | `LayoutItemContainerStyle` sets `Title` and `CloseCommand` only (no `ContentId`) | Every generated `LayoutDocument` has `ContentId == null` (`""`). Without `ContentId`, `XmlLayoutSerializer` (`Dirkster.AvalonDock.Serializer.Xml`) cannot match or persist documents across layout save/restore. | Implement `IDockable` (`Id`) on ViewModels and bind `<Setter Property="ContentId" Value="{Binding Model.Id}"/>` in `LayoutItemContainerStyle`. |
| `MainWindowView.xaml:57–71` | `DocumentHeaderTemplate` binds `{Binding Content.IsDirty}` and `{Binding Content.DisplayName, FallbackValue=#ERROR#}` | Tightly couples tab headers to `LayoutDocument.Content` (`TestViewModel`). During document close (`DockingManager.cs`), AvalonDock sets `document.Content = null` before visual teardown, transiently tripping `FallbackValue=#ERROR#`. | Bind `{Binding Title}` on the `LayoutContent` model (already populated via `LayoutItem.Title`), or guard custom header templates against `Content == null`. |
| `MainWindowView.xaml:53, 78` | Unprefixed `<DockingManager>` and `<LayoutItem>` tags in the default WPF XML namespace | Works only because `AvalonDock`'s `AssemblyInfo.cs` maps `http://schemas.microsoft.com/winfx/2006/xaml/presentation` alongside `https://github.com/Dirkster99/AvalonDock`, making AvalonDock types indistinguishable from built-in WPF controls in XAML. | Declare an explicit XAML namespace prefix: `xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"`. |
| `MainWindowView.xaml:53` | Unset `DockingManager.Theme`, `AllowFloatingWindows`, and `AllowDetachedWindows` | Falls back to the legacy unthemed `Themes/generic.xaml` dictionary (which also exposed the `LayoutDocumentControl` vs themed `ContentTemplate` discrepancy) and leaves v5 multi-window policies unconfigured. | Reference a v5 theme package (e.g., `Dirkster.AvalonDock.Themes.Arc` with `ArcDarkTheme` / `ArcLightTheme`) and configure window policies explicitly. |

---

## 7. Summary Matrix of Baseline Quirks & Bugs

For quick reference when comparing against modernized implementations, these are the exact observable behaviors of `CaliburnDockTestApp` out of the box:

1. **Left `AvalonDock` document body is always blank** (`BindingExpression path error: 'Content' property not found on 'object' ''TestViewModel''`), while the right `TabControl` renders the `IsDirty` checkbox and `Custom Text` textbox.
2. **Startup populates 4 tabs (`Test A`, `Test B`, `Test C`, `Test D`) asynchronously over ~400 ms** from a background `Task.Run` thread, leaving `Test D` selected.
3. **Initial dirty flags alternate by creation counter (`_createCount % 2 > 0`):** `Test A` (dirty), `Test B` (clean), `Test C` (dirty), `Test D` (clean). Reopening a closed tab increments `_createCount` and flips whether it starts dirty or clean.
4. **Editing `Custom Text` in the right `TabControl` immediately lights up the red dirty dot on both the `AvalonDock` tab and the `TabControl` tab.**
5. **Closing a dirty tab (from either `AvalonDock`, a floating `AvalonDock` document window, or `TabControl`) prompts with a `Yes`/`No` `MessageBox`**; clicking `No` cancels the close in both controls.
6. **Closing `Test D` makes `Test D` permanently unreachable** for the rest of the session because `_showDCommand` has no public `ShowDCommand` property and no `MenuItem` under `_Window`.
7. **Every closed tab remains leaked in memory** for the lifetime of `MainWindowViewModel` due to `item.ConductWith(this)` subscribing `MainWindowViewModel.Activated` / `Deactivated` to every created `TestViewModel`.
