# Where We Last Left Off — `CaliburnDockTestApp`

**Date:** 2026-11-26  
**Next Iteration Number:** **Iteration 8** *(Iterations 1–7 completed & verified)*  
**Location:** `source/CaliburnDockTestApp/`  
**Branch:** `master` (`origin` → `https://github.com/NTLDR2/AvalonDock.git`)  
**Target & Frameworks:** `.NET Framework 4.8` (`<TargetFramework>net48</TargetFramework>`, singular) · `Microsoft.NET.Sdk` (`.NET 8.0 SDK` only) · `AvalonDock 5.1.0` · `Caliburn.Micro 6.0.15-beta` · **Zero `System.Text.Json`**

### Companion Documentation in `source/CaliburnDockTestApp/`
1. **`00_CaliburnDockTestApp- Baseline-Technicals-and-Deprecation-Audit.md`** — **FROZEN BASELINE (DO NOT MODIFY):** Captures the exact pre-modernization 14-file baseline, runtime quirks, and deprecation audit (`39,824` bytes).
2. **`99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md`** — **LIVING TECHNICAL SPEC:** Tracks current architecture, runtime behavior, Applied Fixes Log (Iterations `1`–`7`), and remaining Caliburn.Micro / AvalonDock candidates.

---

## 1. Purpose & Core Architecture of `CaliburnDockTestApp`

`CaliburnDockTestApp` is a side-by-side WPF diagnostic and reference harness proving that **AvalonDock's `DockingManager`** (`DockManager`, left pane) and a standard **WPF `TabControl`** (`TabManager`, right pane, acting as a baseline "control group") synchronize accurately when bound simultaneously to the exact same `MainWindowViewModel` (`Conductor<Screen>.Collection.OneActive`):

- **Collection & Active-Item Sync:** `DocumentsSource` / `ItemsSource` bind to `Conductor.Items`; `ActiveContent` / `SelectedItem` bind two-way to `Conductor.ActiveItem`.
- **Dual-Host `ViewAware` Isolation:** Because Caliburn.Micro's `ViewAware` caches one `UIElement` per context key, `DockManager` resolves `TestView` on the default (`null`) context via an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` in `<DockingManager.Resources>`, while `TabManager` specifies `cal:View.Context="TabControl"` backed by a `ViewLocator.LocateTypeForModelType` default-view fallback in `Bootstrapper.Configure()`. Both panes render live, distinct `TestView` instances simultaneously without stealing visuals from one another—even across split tab groups and floating windows.
- **Self-Documenting Help Dialog:** `Help -> What's This App About...` opens a `CenterOwner` modal `AboutView` window (~234 words) summarizing this architecture using idiomatic Caliburn.Micro v5/v6 patterns (`IWindowManager.ShowDialogAsync`, `Screen.TryCloseAsync`, leaf `cal:Message.Attach`) and strictly WPF/OS system-default fonts, font sizes, and colors.

---

## 2. Completed Iterations (Iterations 1–7)

| Iteration | File(s) Touched | Summary of Applied & Verified Changes |
|:----------|:----------------|:--------------------------------------|
| **1** | `CaliburnDockTestApp.csproj` | Changed plural `<TargetFrameworks>net48</TargetFrameworks>` to explicitly singular `<TargetFramework>net48</TargetFramework>`, eliminating MSBuild's outer multi-target dispatch pass. |
| **2.1** | `CaliburnDockTestApp.csproj` | Updated SDK moniker from legacy `Microsoft.NET.Sdk.WindowsDesktop` to `Microsoft.NET.Sdk` (`<UseWPF>true</UseWPF>`). |
| **2.2** | `ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Exposed `public RelayCommand ShowDCommand` for `_showDCommand` and added `<MenuItem Header="Window D" Command="{Binding ShowDCommand}"/>` under `_Window` so all 4 seeded tabs (`Test A`–`Test D`) can be reopened. |
| **2.3** | `ViewModels/MainWindowViewModel.cs` | Removed redundant `item.Parent = this;` and memory-leaking `item.ConductWith(this);` from `ActivateOrCreate<T>`. |
| **3.1** | `CaliburnDockTestApp.csproj`<br>`ViewModels/ViewModelBase.cs`<br>`ViewModels/MainWindowViewModel.cs` | Removed custom `<DefineConstants>` (`CALIBURN_ASYNC`) overrides that clobbered SDK constants (`TRACE;DEBUG;NETFRAMEWORK;NET48;`), unwrapped `#if CALIBURN_ASYNC`, and deleted dead Caliburn.Micro 3.2 `#else` shims. |
| **3.2** | `ViewModels/MainWindowViewModel.cs` | Replaced off-UI-thread `Task.Run(() => StartInitialDocuments())` with `_ = Execute.OnUIThreadAsync(StartInitialDocuments);` so all `Items` queries, `_createCount` updates, and `ActivateItemAsync` calls run strictly on the STA UI thread. |
| **4.1** | `Views/MainWindowView.xaml` | Replaced broken `<DockingManager.LayoutItemTemplate>` (`cal:View.Model="{Binding Content}"`) with an implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` in `<DockingManager.Resources>`, fixing blank AvalonDock document bodies and eliminating WPF `DataBinding` errors. |
| **4.2** | `Views/MainWindowView.xaml`<br>`Bootstrapper.cs` | Added `cal:View.Context="TabControl"` to `TabManager`'s `ContentTemplate` and added a default-view fallback to `ViewLocator.LocateTypeForModelType` in `Bootstrapper.Configure()` (with inline stress-test notes for future AvalonDock v5 features), eliminating `ViewAware` view-theft. |
| **5** | `ViewModels/AboutViewModel.cs` *(new)*<br>`Views/AboutView.xaml` *(new)*<br>`Views/AboutView.xaml.cs` *(new)*<br>`ViewModels/MainWindowViewModel.cs`<br>`Views/MainWindowView.xaml` | Added `Help -> What's This App About...` wired via leaf `cal:Message.Attach="[Event Click] = [Action ShowAbout]"` to `IWindowManager.ShowDialogAsync(new AboutViewModel())` (preserving parameterless `MainWindowViewModel() : this(null)` alongside constructor-injectable `IWindowManager`). |
| **6.1–6.3** | `CaliburnDockTestApp.csproj`<br>`Bootstrapper.cs`<br>`ViewModels/MainWindowViewModel.cs` | Upgraded `Caliburn.Micro` from `4.0.136-rc` to `6.0.15-beta`, updated `OnStartup` to `async void` + `await DisplayRootViewForAsync<MainWindowViewModel>()`, and replaced `[Obsolete]` `OnInitializeAsync` with `OnInitializedAsync`. |
| **7** | `Views/AboutView.xaml` | Removed all custom hex colors, brushes, border overrides, font weights, and font sizes (`#F0F2F5`, `#F6F8FA`, `#D0D7DE`, `Transparent`, `FontSize="14"`, `FontWeight="SemiBold"`, `FontSize="12.5"`, fixed `Height="26"`), simplifying `AboutView.xaml` to strictly WPF/OS system defaults (`0` `Local` font/color overrides). |

---

## 3. Current Verified Build & Runtime Health Snapshot

```powershell
# Clean & build Debug and Release with warnings-as-errors (0 Warning(s), 0 Error(s))
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Debug --warnaserror
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Release --warnaserror

# Launch built desktop executable
.\source\CaliburnDockTestApp\bin\Debug\net48\CaliburnDockTestApp.exe
```

### Verified Output Assemblies (`bin\Debug\net48` & `bin\Release\net48`)
- `CaliburnDockTestApp.exe` (`5.1.0`)
- `AvalonDock.dll` (`5.1.0`) & `AvalonDock.Core.dll` (`5.1.0`)
- `Caliburn.Micro.Core.dll`, `Caliburn.Micro.Platform.dll`, & `Caliburn.Micro.Platform.Core.dll` (`6.0.15-beta`)
- `Microsoft.Xaml.Behaviors.dll` (`1.1.135`)
- `*Json*` assemblies: **`0`**

### Verified STA Runtime Behavior
- **Startup & Seeding:** `await DisplayRootViewForAsync<MainWindowViewModel>()` → `OnInitializedAsync` → staggered UI-thread seeding of `Test A` (dirty), `Test B` (clean), `Test C` (dirty), `Test D` (clean, active).
- **Dual-Host Sync:** `1` visible `TestView` in `DockManager` + `1` distinct visible `TestView` in `TabManager`; live bidirectional `CustomText` typing and `IsDirty` checkbox/red-dot sync across docked tabs, horizontal split tab groups, and torn-off floating windows (`FloatCommand` / `DockAsDocumentCommand`).
- **About Modal (`AboutView.xaml`):** `0` static XAML font/color/hex matches, `0` `BaseValueSource.Local` font/color overrides at runtime (inherits `SystemFonts.MessageFontFamily` and `SystemFonts.MessageFontSize`), selectable/copyable native read-only `TextBox`, and clean dismissal via `[Action Close]`.
- **WPF DataBinding Errors:** **`0`** warnings/errors in `PresentationTraceSources.DataBindingSource`.

---

## 4. Strict Guardrails for Future Sessions

1. **Frozen Baseline Doc:** Never edit `00_CaliburnDockTestApp- Baseline-Technicals-and-Deprecation-Audit.md` (`39,824` bytes).
2. **Living Technical Doc:** Update `99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md` after every applied iteration.
3. **Strict System Defaults UI Rule:** Do **not** introduce custom fonts, custom font sizes, custom font weights, or hard-coded colors/hex brushes in dialogs or views—use strictly WPF/OS system defaults (padding, margins, and `TextWrapping="Wrap"` are fine).
4. **Zero `System.Text.Json` (`AGENTS.md`):** Never add `System.Text.Json` or any package that pulls it onto `net48`.
5. **Delicate Incremental Refactoring:** Propose and apply changes one focused iteration at a time (starting at **Iteration 8**), verifying both `Debug` and `Release` (`--warnaserror`) after each step.

---

## 5. Candidate Next Steps (Iteration 8+)

Detailed in Sections 5 and 6 of `99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md`:

1. **Caliburn.Micro Action & Async Close-Confirmation Modernization (Section 5 of `99_`):**
   - Replace the global-namespace `RelayCommand.cs` fire-and-forget wrappers (`ShowACommand`–`ShowDCommand`, `CloseCommand`) with idiomatic `Task`-returning Caliburn.Micro actions (`cal:Message.Attach` on leaf elements).
   - Replace synchronous `MessageBox.Show(...)` inside `ViewModelBase.CanClosePrompt()` with an async `IWindowManager.ShowDialogAsync(...)` confirmation dialog styled strictly with WPF/OS system defaults so `CanCloseAsync` is non-blocking and unit-testable.
2. **AvalonDock v5 Modernization Candidates (Section 6 of `99_`):**
   - Declare an explicit `xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"` XAML prefix in `MainWindowView.xaml` and guard `DocumentHeaderTemplate` against transient `Content == null` during close teardown.
   - Route `DockingManager.DocumentClosing` (`e.Cancel = true` → `IConductor.CloseItemAsync`) instead of overriding `LayoutItem.CloseCommand`, restoring AvalonDock's `DocumentClosing` and `DocumentClosed` events.
   - Map `ContentId` (`IDockable.Id`) in `LayoutItemContainerStyle` for `Dirkster.AvalonDock.Serializer.Xml` layout persistence, optionally demonstrate v5 theme switching (`Dirkster.AvalonDock.Themes.Arc`), and—if `DetachedAnchorableWindow` tool panes are ever introduced—move the implicit `ViewModelBase` `DataTemplate` from `<DockingManager.Resources>` to `App.xaml`'s `<Application.Resources>` and re-run the dual-host stress test noted in `Bootstrapper.Configure()`.
3. **Solution Membership Decision:**
   - Decide whether `CaliburnDockTestApp.csproj` should remain a standalone sample project or be added to `source/AvalonDock.sln`.
