# ExperimentalTestApp

A reference sample for **AvalonDock v5** (NTLDR2 fork, `v5.1.0`) hosted by **Caliburn.Micro 6.0.15-beta**.

It uses the **classic `DockingManager`** driven through the modern v5 MVVM surface: `DockLayout` bound to an `IDockLayoutService`, with ViewModels that implement `IDockable` / `IToolbox`. Caliburn.Micro provides the conductor, the screen lifecycle, actions, the event aggregator and `SimpleContainer`.

> [!NOTE]
> **Scope.** This is an *NTLDR2 Fork v5.1.0* sample. It does not use `ToggleDockingManager`, `AvalonDock.DependencyInjection` or `Microsoft.Extensions.*`. There is also no `System.Text.Json` anywhere in its dependency graph, including on `net48`.

| | |
|:--|:--|
| **Target framework** | `net48` |
| **Caliburn.Micro** | NuGet `Caliburn.Micro` **6.0.15-beta**. It is built from upstream commit `83e93b34`, which is part of the [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro) fork history; the fork adds documentation only. Dependencies: `Caliburn.Micro.Core` (no dependencies) and `Microsoft.Xaml.Behaviors.Wpf` (no dependencies). |
| **AvalonDock** | `AvalonDock`, `AvalonDock.Core`, `AvalonDock.Mvvm` (for `DockLayoutService`), `AvalonDock.Serializer.Xml`, and the five theme packages |
| **Container** | Caliburn.Micro `SimpleContainer` |
| **Default theme** | `VS2010` (`AvalonDock.Themes.VS2010`) |

## Running it

```powershell
dotnet build source/ExperimentalTestApp/ExperimentalTestApp.csproj -c Release
dotnet run --project source/ExperimentalTestApp/ExperimentalTestApp.csproj -c Release -f net48
```

The layout is saved to `%LOCALAPPDATA%\ExperimentalTestApp\layout.xml` when the application exits and is restored at the next start. Delete that file to start from the default layout.

## What each file demonstrates

| File | Demonstrates |
|:-----|:-------------|
| [App.xaml](App.xaml) | The bootstrapper lives in merged resources; there is **no `StartupUri`**. |
| [Bootstrapper.cs](Bootstrapper.cs) | `BootstrapperBase` + `SimpleContainer`. The logger is assigned before `Initialize()`. All three IoC overrides are implemented. `async void OnStartup` awaits `DisplayRootViewForAsync<ShellViewModel>()`. Coroutine errors are logged. `ShellView` is container-built as a singleton. Toolboxes are registered under both their concrete type and `IToolbox` (same instance). `DockLayoutService` is built from the `IToolbox` list, and the **window policy is set once, on the `IRootDock`**. `ThemeManager` is registered as `IThemeManager`. |
| [Infrastructure/TraceLog.cs](Infrastructure/TraceLog.cs), [LogSink.cs](Infrastructure/LogSink.cs) | A custom `ILog` that formats safely (Caliburn.Micro's `DebugLog` throws on braces). It feeds the *Caliburn Log* tool window. |
| [Infrastructure/DialogSettings.cs](Infrastructure/DialogSettings.cs) | Window settings for `IWindowManager.ShowDialogAsync`. They live here so that WPF enums stay out of the ViewModels. |
| [Boilerplate/DockableScreen.cs](Boilerplate/DockableScreen.cs) | `Screen` + `IDockable`. `Title` and `DisplayName` share one backing field. `IDockable.IsActive` is implemented **explicitly** and surfaces as `IsDockActive`, so it never clashes with `Screen.IsActive`. |
| [Boilerplate/ToolboxScreen.cs](Boilerplate/ToolboxScreen.cs) | `Screen` + `IToolbox` (zone, shortcut, open state). |
| [Boilerplate/DocumentScreen.cs](Boilerplate/DocumentScreen.cs) | Base class for documents. |
| [Boilerplate/DockLayoutConductor.cs](Boilerplate/DockLayoutConductor.cs) | `Conductor<T>.Collection.OneActive` synchronized with `IDockLayoutService`: `Items` ↔ `IDocumentDock.VisibleDockables`, `ActiveItem` ↔ `IRootDock.ActiveDockable`, plus the AvalonDock focus flag (`IDockable.IsActive` / `OnSelected`). |
| [Docking/DockingResources.xaml](Docking/DockingResources.xaml) | An **implicit `DataTemplate` for `DockableScreen`** = `<ContentControl cal:View.Model="{Binding}"/>` (instead of `LayoutItemTemplate`; see rule 7); the document header with the "modified" dot; container styles binding `Title ← Model.Title` and `ContentId ← Model.Id`. |
| [Docking/PanesStyleSelector.cs](Docking/PanesStyleSelector.cs) | Chooses the container style per ViewModel kind. |
| [Docking/ZonePlacementStrategy.cs](Docking/ZonePlacementStrategy.cs) | An `ILayoutUpdateStrategy` that places each `IToolbox` into the pane named after its `Zone`. |
| [Docking/DocumentCloseBehavior.cs](Docking/DocumentCloseBehavior.cs) | Cancels AvalonDock's `DocumentClosing` and routes the close to the Caliburn.Micro conductor. Closes are queued. |
| [Docking/ToolboxVisibilityBehavior.cs](Docking/ToolboxVisibilityBehavior.cs) | Keeps `IToolbox.IsOpen` and the anchorable's visibility in sync. |
| [Docking/ToolboxShortcutBehavior.cs](Docking/ToolboxShortcutBehavior.cs) | Honours `IToolbox.Shortcut` on the classic manager. |
| [Services/LayoutPersistenceService.cs](Services/LayoutPersistenceService.cs) | `XmlLayoutSerializer` save, load and reset, with a content resolver supplied by the ViewModel. |
| [Services/SampleWorkspace.cs](Services/SampleWorkspace.cs) | In-memory sample files, so there are no file dialogs and no disk I/O beyond the layout file. |
| [Messages/Messages.cs](Messages/Messages.cs) | Event aggregator payloads. |
| [ViewModels/ShellViewModel.cs](ViewModels/ShellViewModel.cs) | The document conductor. Menu actions with `Can*` guards. `IHandle<T>`. Tool window lifecycle tied to `IsOpen`. Layout save/load/reset, with an auto-save on exit that runs **before** the conductor closes the documents. |
| [ViewModels/Documents/TextDocumentViewModel.cs](ViewModels/Documents/TextDocumentViewModel.cs) | `IsModified` and an async `CanCloseAsync` that asks Save / Don't Save / Cancel through `IWindowManager.ShowDialogAsync`. |
| [ViewModels/Documents/WelcomeViewModel.cs](ViewModels/Documents/WelcomeViewModel.cs) | A document whose tab cannot be closed (`CanClose = false`). Its buttons are bound by convention and publish messages. |
| [ViewModels/Tools/*](ViewModels/Tools) | One `ToolboxScreen` per zone. Subscribe in `OnActivatedAsync`, unsubscribe in `OnDeactivateAsync`. `x:Name` conventions (`Files` → `SelectedFile`) with guard properties. |
| [ViewModels/Dialogs/*](ViewModels/Dialogs) | Modal dialogs closed with `TryCloseAsync(bool)`. |
| [Views/ShellView.xaml(.cs)](Views/ShellView.xaml) | Menu (`cal:Message.Attach`), `KeyTrigger` + `cal:ActionMessage` shortcuts, the `Themes` → `SelectedTheme` combo box convention, the `DockingManager` bound to `DockLayout`, and the six `Zone_*` panes. The view is built by the container and receives `ThemeManager` and `LayoutPersistenceService` in its constructor. |

## Synchronization rules (read these before changing the docking code)

1. **The conductor's `Items` is the single source of truth for open documents.** `DockLayoutConductor` mirrors `Items` into the `IDocumentDock`. Nothing else may add to or remove from `VisibleDockables`. When a document leaves the bridge's `DocumentsSource`, AvalonDock drops its tab **silently**, without raising `DocumentClosing` again.
2. **Never bind `DockingManager.ActiveContent` when `DockLayout` is bound.** `LayoutSyncBridge` owns it. `ActiveItem` → `IDockLayoutService.ActiveDockable` → `ActiveContent`; AvalonDock focus → `IRootDock.ActiveDockable` → `ActivateItemAsync` (documents already in `Items` only). Toolboxes and `null` never enter `Items`.
3. **Every close goes through Caliburn.Micro.** `DocumentCloseBehavior` cancels `DocumentClosing` and calls `IConductor.CloseItemAsync`, so `CanCloseAsync` can veto asynchronously. This applies to the tab ✕, a middle click on the tab, the document context menu ("Close", "Close All", "Close All But This"), Ctrl+W, *Close All Documents*, and application exit.
4. **AvalonDock never calls `IDockable.OnClose()` or `OnSelected()`, and never sets `IDockable.IsActive`.** `DockLayoutConductor` maintains the focus flag and calls `OnSelected()`. `OnClose()` only reports `CanClose`.
5. **Two "IsActive" flags.** `Screen.IsActive` is Caliburn.Micro's lifecycle state; `IsDockActive` (`IDockable.IsActive`) is AvalonDock focus. Never bind `LayoutItem.IsActive` to `Model.IsActive`.
6. **Bind `Title` and `ContentId` in the container styles.** AvalonDock never copies `IDockable.Title` or `Id` onto documents. Without the `ContentId` binding, layouts cannot be restored.
7. **Host the views with an implicit `DataTemplate` for the ViewModel base type, not with `DockingManager.LayoutItemTemplate`.**
   - The theme templates show `LayoutItem.View` (a `ContentPresenter` whose `Content` is the ViewModel) inside an *outer* `ContentPresenter` that also receives `LayoutItem.View.ContentTemplate`.
   - A `LayoutItemTemplate` is therefore applied with `DataContext` = that inner `ContentPresenter` in docked and floating panes, but with `DataContext` = the ViewModel in a `DetachedAnchorableWindow`. Neither `{Binding}` nor `{Binding Content}` works in every host.
   - With `LayoutItemTemplate` unset, the outer presenter just shows `LayoutItem.View`, which resolves `<DataTemplate DataType="{x:Type boilerplate:DockableScreen}">` with `DataContext` = the ViewModel everywhere.
   - Each ViewModel then has one view (one context), and that same view instance travels with the presenter into floating and detached windows.
8. **Configure the window policy on the `IRootDock`** (`AllowFloatingWindows`, `AllowDetachedWindows`). The bridge overwrites XAML values.
9. **Tool window visibility.** A two-way `Visibility` binding in the `LayoutAnchorableItem` style does **not** work: the item assigns `Visibility` as a local value after applying the style. `ToolboxVisibilityBehavior` syncs `IsOpen` in code instead. After a layout load, the restored layout decides `IsOpen`.

### How `IToolbox` is honoured on the classic `DockingManager`

| Member | Natively consumed? | Handled by |
|:-------|:-------------------|:-----------|
| `Zone` | Partly: `DockLayoutService` groups toolboxes by side | `ZonePlacementStrategy` (all 6 zones, through the named panes) |
| `IsOpen` | No (only `ToggleDockingManager` syncs it) | `ToolboxVisibilityBehavior`; the shell activates or deactivates the tool `Screen` |
| `IsOpenByDefault` | No | The shell, at start-up and on *Reset Layout* |
| `Shortcut` | No | `ToolboxShortcutBehavior`; also shown in the View menu |
| `ToolTipText` | No | Container style `ToolTip` binding |
| `Icon` | No | Not used: binding `LayoutItem.IconSource` needs a WPF `ImageSource` |

## Why is there a `Boilerplate` folder?

No Caliburn.Micro ↔ AvalonDock base class library ships today. When a library is missing, applications write the glue themselves; this folder is that glue. It is **sample-internal and not a supported API**. A dedicated, optional base class library (like `AvalonDock.Mvvm.CommunityToolkit` for CommunityToolkit.Mvvm) is future work; see [ROADMAP.md](../../ROADMAP.md), Phase 3. The class names deliberately differ from the names sketched there.

## Caliburn.Micro notes that matter inside a docking manager

- **`SimpleContainer.GetAllInstances` enumerates its lazy result twice.** A `PerRequest` registration that `ViewLocator` resolves through `IoC.GetAllInstances(viewType)` is therefore constructed twice. For a `Window` view, the orphan copy stays in `Application.Windows` and keeps the process alive after the main window closes. That is why `ShellView` is registered as a **singleton**.
- Caliburn.Micro 6 logs every convention and action-resolution step at Info level. `LogSink` drops those `ViewModelBinder`/`ActionMessage` diagnostics unless *Show binder/action diagnostics* is ticked in the Caliburn Log tool window.
- `Screen.DisplayName` defaults to the type's full name. `DockableScreen` sets a proper title through `DisplayName`, which also updates `Title`.
- An `ItemsControl`/`ListBox` named by convention needs an explicit `ItemTemplate` (or `DisplayMemberPath`). Otherwise Caliburn.Micro applies its default `cal:View.Model` item template and looks for a view per item.
- `TryCloseAsync(true/false)` is valid only for modal dialogs (`ShowDialogAsync`). Documents close through their conductor.
- Caliburn.Micro's `WindowManager` makes the *active* window the owner of a dialog, so a prompt raised from a floating window is owned by that floating window.
- **Never attach guarded `<cal:ActionMessage>` triggers directly to `<Window>` or a layout panel.** `ActionMessage.ApplyAvailabilityEffect` sets `source.IsEnabled = Can<Method>()`, which sets `Window.IsEnabled = false` whenever any guard (`CanSave`, `CanSaveAll`, `CanCloseActiveDocument`) is `false`, disabling every mouse click and keyboard shortcut in the window. Host window-wide `<i:KeyTrigger>`s on a zero-sized leaf `<FrameworkElement>` inside the visual tree instead (`KeyTrigger` with `ActiveOnFocus=False` still listens on the root `Window`).
- `KeyTrigger` shortcuts listen on the main window; they do not fire while a floating window has keyboard focus.
- `ActionMessage.EnforceGuardsDuringInvocation` is `false` by default: a key trigger on that leaf element invokes an action even when its `Can*` guard is `false`. That is why the action methods check their preconditions themselves.

## Documentation

- AvalonDock: [documentation/guides/caliburn-micro.md](../../documentation/guides/caliburn-micro.md), [documentation/guides/mvvm.md](../../documentation/guides/mvvm.md), [AGENTS.md](../../AGENTS.md)
- Caliburn.Micro: [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro) (`docs/` and `AGENTS.md`)
