# AGENTS.md — `CaliburnDockTestApp` AI Coding Guide

Scoped architectural rules and anti-hallucination guardrails for **`source/CaliburnDockTestApp/`** (`net48` WPF, **AvalonDock 5.1.0** + **Caliburn.Micro 6.0.15-beta**, `SimpleContainer`, **zero `System.Text.Json`**).

> [!IMPORTANT]
> **Role of This Sample (Mode B — Document-Only Side-by-Side Harness):**  
> `CaliburnDockTestApp` intentionally tests **Mode B (`DockingManager.DocumentsSource` + `ActiveContent`)** side-by-side against a standard WPF **`TabControl` (`ItemsSource` + `SelectedItem`)** bound to the exact same `Conductor<Screen>.Collection.OneActive` (`MainWindowViewModel`).  
> Do **not** convert this sample to `DockLayout` / `IDockLayoutService`—full IDE **Mode A (`DockLayout` + 6-zone `IToolbox` tools + 18 themes + XML persistence)** is covered in `../ExperimentalTestApp/` and `../ThemeSuperTestApp/`.

---

## 1. Non-Negotiable Architecture & Anti-Hallucination Rules

| Area | DO NOT Emit (Anti-Pattern / Regression) | ALWAYS Use (Idiomatic v5 + Caliburn.Micro v6) |
|:-----|:----------------------------------------|:----------------------------------------------|
| **Docking Binding Mode** | Replacing `DocumentsSource` with `DockLayout` here, *or* adding `AnchorablesSource` tools while keeping unfiltered `ActiveContent` TwoWay bound to `ActiveItem` | Keep `DocumentsSource="{Binding Items}"` + `ActiveContent="{Binding ActiveItem, Mode=TwoWay}"` for document-only symmetry with `TabControl`. *(If tools are added in another app, filter `ActiveContent` or graduate to Mode A `DockLayout`.)* |
| **Dual-Host View Caching** | Sharing the default `ViewAware` context across both panes, or setting `DockingManager.LayoutItemTemplate` | Keep `LayoutItemTemplate` unset; use the implicit `<DataTemplate DataType="{x:Type vm:ViewModelBase}">` (`null` context) in `<avalonDock:DockingManager.Resources>` and `cal:View.Context="TabControl"` on `TabManager` (backed by `Bootstrapper`'s `ViewLocator.LocateTypeForModelType` fallback). |
| **XAML Namespaces** | `https://github.com/Dirkster99/AvalonDock`, unprefixed `<DockingManager>`/`<LayoutItem>`, or `clr-namespace:Caliburn.Micro` | `xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"` and `xmlns:cal="http://caliburnmicro.com"`. |
| **DI & ViewModel Purity** | `using System.Windows`, `System.Diagnostics.Trace`, `RelayCommand`/`ICommand`, `MessageBox.Show`, `Activator.CreateInstance`, or `new WindowManager()` in `ViewModels/` | Inject `IWindowManager` and zero-arg `Func<TestViewModel>` / `Func<AboutViewModel>` via `SimpleContainer` (`Singleton` shell/services, `PerRequest` child/dialog VMs); log via `LogManager.GetLog(...)` (`ILog`); construct parameterized `new ConfirmCloseViewModel(DisplayName)` at the prompt site. |
| **View Registration in `SimpleContainer`** | Registering WPF `Window` / `UserControl` views as `PerRequest` in `SimpleContainer` | Leave Views unregistered so `ViewLocator` instantiates each view once (`SimpleContainer.GetAllInstances` enumerates its lazy projection twice, which orphans extra `Window`s in `Application.Windows`). |
| **Conventions vs. `cal:Message.Attach`** | Manual `{Binding}` or verbose `[Event Click] = [Action ...]` where `x:Name` works, or naming a `Button` `x:Name="Close"` on a `Window` (`CS0108`) | Use pure `x:Name` conventions on top-level views (`IsDirty`, `CustomText`, `Message`, `Confirm` → `ConfirmAsync`, `Cancel` → `CancelAsync`, `AboutTitle`, `AboutText`, `Ok` → `OkAsync`; omit `Window.Title` so `WindowManager` binds `DisplayName`). Reserve `cal:Message.Attach` shorthand for literal args (`OpenDocumentAsync('Test A')`) and item `DataTemplate`s (`CloseDocumentAsync($dataContext)`). |
| **Async Document Closing** | Binding `LayoutItem.CloseCommand` to a ViewModel command, or calling `Screen.TryCloseAsync` on child `UserControl` document tabs | Leave `LayoutItem.CloseCommand` unset; let `DocumentCloseBehavior` cancel `DockingManager.DocumentClosing` (`e.Cancel = true`) and FIFO-queue `IConductor.CloseItemAsync` (`TabManager` calls `CloseDocumentAsync($dataContext)`), awaiting `ShowDialogAsync(ConfirmCloseViewModel)` in `CanCloseAsync`. |
| **Identity, Headers & `_Window` Menu** | Binding `DocumentHeaderTemplate` to `Content.DisplayName` (`FallbackValue=#ERROR#`), or setting `MenuItem.IsCheckable="True"` on `_Window` items | Map `Model.Id` (`"TestA"`–`"TestD"`) → `LayoutItem.ContentId` separately from `Model.DisplayName` (`"Test A"`–`"Test D"`) → `LayoutItem.Title`; bind header text to `{Binding Title}` (safe when `Content` clears on close); bind `_Window` `MenuItem.IsChecked` `OneWay` to `ActiveItem` via `ActiveDocumentConverter` with `IsCheckable="False"` (`<= 1` checkmark always, `0` when all tabs close). |
| **UI Chrome & Styling** | Custom fonts, `FontSize`, `FontWeight`, hex colors (`#RRGGBB`), or hard-coded dialog brushes | Strictly WPF / Windows OS system defaults (`0` `BaseValueSource.Local` font/color overrides on `AboutView` and `ConfirmCloseView`). |

---

## 2. Build, Formatting & STA Test Verification

- **Formatting (`../.editorconfig`):** Tabs (`indent_style = tab`) in `.cs`, UTF-8 without BOM, `LF` line endings, no final newline, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<TargetFramework>net48</TargetFramework>` (singular).
- **Critical STA Test Rule:** `CaliburnDockTestAppTests` (`../AutomationTest/AvalonDockTest/CaliburnDockTestAppTests.cs`) inherits `AutomationTestBase` and runs on `await TestHost.SwitchToAppThread()`. **Never** call `Application.Current.Shutdown()` in tests, and always pass **`-m:1`**.

```powershell
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Debug --warnaserror
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Release --warnaserror
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --framework net48 --filter "FullyQualifiedName~CaliburnDockTestAppTests" -m:1
```

---

## 3. Companion References

- **Sample Overview & Mode A vs. Mode B Guide:** [README.md](README.md)
- **Detailed Runtime Walkthrough & Change Log (Iterations 1–10):** [99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md](99_CaliburnDockTestApp-Updated-Technicals-and-Applied-Fixes.md)
- **Repository Guide & Caliburn.Micro Docs:** [../../AGENTS.md](../../AGENTS.md), [../../documentation/guides/caliburn-micro.md](../../documentation/guides/caliburn-micro.md), and [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro)