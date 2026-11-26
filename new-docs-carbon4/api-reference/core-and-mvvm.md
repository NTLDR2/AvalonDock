---
title: Core, MVVM & DI
layout: default
parent: API Reference
nav_order: 6
description: "API reference for AvalonDock.Core, AvalonDock.Mvvm, AvalonDock.Mvvm.CommunityToolkit, and AvalonDock.DependencyInjection."
---

# Core, MVVM & Dependency Injection Reference

{: .new }
> New in v5

This page documents the UI-agnostic `netstandard2.0` packages introduced in AvalonDock v5:

- **`Dirkster.AvalonDock.Core`** — Core interfaces, enums, event args, drop-zone geometry rules, and `FactoryBase`
- **`Dirkster.AvalonDock.Mvvm`** — Zero-dependency view-model base classes, `DockLayoutService`, `SideToggleManager`, and `Factory`
- **`Dirkster.AvalonDock.Mvvm.CommunityToolkit`** — `CommunityToolkit.Mvvm` `ObservableObject`-based view-model base classes
- **`Dirkster.AvalonDock.DependencyInjection`** — `IServiceCollection` extensions, `DockLayoutBuilder`, `DockingOptions`, and `ToggleDockOptions`

*(For `AvalonDock.Core.Serialization` and DTO types, see [Layout Serialization]({{ site.baseurl }}{% link guides/serialization.md %}).)*

---

## 1. `AvalonDock.Core` Interfaces

### `IDockable`

Fundamental unit of dockable content or containers (`namespace AvalonDock.Core`).

| Member | Type | Description |
|:-------|:-----|:------------|
| `Id` | `string` | Unique identifier for lookup and serialization (mapped to `LayoutItem.ContentId` via style binding). |
| `Title` | `string` | Display title shown on tabs or headers. |
| `Context` | `object?` | Optional associated runtime data context. |
| `Owner` | `IDockable?` | Parent dock container that owns this dockable. |
| `Factory` | `IFactory?` | Factory managing this dockable. |
| `CanClose` | `bool` | Whether the dockable may be closed. |
| `CanPin` | `bool` | Whether the dockable may be pinned/auto-hidden. |
| `CanFloat` | `bool` | Whether the dockable may be floated. |
| `CanDrag` | `bool` | Whether the dockable may be dragged. |
| `CanDrop` | `bool` | Whether other dockables may be dropped onto this one. |
| `IsModified` | `bool` | Whether the content has unsaved changes. |
| `IsActive` | `bool` | Whether this dockable is currently active. |
| `DockState` | `DockState` | Current docking state (`Docked`, `AutoHidden`, `Float`, `Hidden`). |
| `OnClose()` | `bool` | Called before closing; return `false` to veto closing. |
| `OnSelected()` | `void` | Called when the dockable is selected. |

---

### `IDock`, `IRootDock`, `IDocumentDock` & `IToolDock`

Container interfaces extending `IDockable` (`namespace AvalonDock.Core`).

#### `IDock : IDockable`

| Property | Type | Description |
|:---------|:-----|:------------|
| `VisibleDockables` | `IList<IDockable>?` | Currently visible child dockables. |
| `ActiveDockable` | `IDockable?` | Currently active child dockable. |
| `DefaultDockable` | `IDockable?` | Fallback dockable activated when `ActiveDockable` closes or detaches. |
| `FocusedDockable` | `IDockable?` | Currently focused dockable. |
| `CanGoBack` | `bool` | Read-only. Whether backward navigation is available. |
| `CanGoForward` | `bool` | Read-only. Whether forward navigation is available. |

#### `IRootDock : IDock`

| Member | Type | Description |
|:-------|:-----|:------------|
| `FloatingDockables` | `IList<IDockable>?` | Dockables currently in floating windows. |
| `PinnedDockables` | `IList<IDockable>?` | Dockables currently pinned/auto-hidden to a side. |
| `DefaultLayout` | `IDockable?` | Default layout tree for reset scenarios. |
| `AllowFloatingWindows` | `bool` | Manager-wide policy allowing or disabling tear-off floating windows (`true` by default). |
| `AllowDetachedWindows` | `bool` | Manager-wide policy allowing or disabling standalone top-level tool windows (`true` by default). |
| `ShowWindows()` | `void` | Shows floating windows associated with the root dock. |
| `HideWindows()` | `void` | Hides floating windows associated with the root dock. |

#### `IDocumentDock : IDock`

| Property | Type | Description |
|:---------|:-----|:------------|
| `CanCreateDocument` | `bool` | Whether new documents may be created in this dock. |

#### `IToolDock : IDock`

| Property | Type | Description |
|:---------|:-----|:------------|
| `Alignment` | `DockAlignment` | Side where this tool dock is aligned (`Left`, `Right`, `Top`, `Bottom`). |
| `IsExpanded` | `bool` | Whether the tool dock is expanded. |
| `AutoHide` | `bool` | Whether the tool dock uses auto-hide behavior. |

---

### `IToolbox : IDockable`

Contract for tool-window view models participating in `ToggleDockingManager` and `IDockLayoutService` (`namespace AvalonDock.Core`).

| Property | Type | Description |
|:---------|:-----|:------------|
| `ToolTipText` | `string?` | Tooltip shown on the sidebar toggle button (defaults to `Title` if `null`). |
| `Zone` | `DockZone` | Target sidebar zone (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`). |
| `Shortcut` | `string?` | Optional WPF `KeyGesture` string (e.g. `"Ctrl+Shift+E"`). Registers a host-window `KeyBinding` and appends the shortcut to the button tooltip. |
| `IsOpenByDefault` | `bool` | Declarative startup visibility seed for fresh layouts. |
| `IsOpen` | `bool` | Live visibility state (`true` when docked or in a standalone window; `false` when collapsed to the sidebar). Setting this in code toggles the panel. |
| `Icon` | `object?` | Sidebar button icon (`ImageSource`, `DrawingImage`, `UIElement` such as `Path`/`Viewbox`, or `null` for vertical rotated title text). |

---

### `IDockLayoutService` & `DockLayoutServiceExtensions`

Central MVVM layout orchestration service (`namespace AvalonDock.Core`).

| Member | Source | Signature & Description |
|:-------|:-------|:------------------------|
| `Layout` | `IDockLayoutService` | `IRootDock Layout { get; }` — Bind to `DockingManager.DockLayout` or `ToggleDockingManager.DockLayout`. |
| `ActiveDockable` | `IDockLayoutService` | `IDockable? ActiveDockable { get; set; }` — Active document or tool. |
| `Documents` | `IDockLayoutService` | `IEnumerable<IDockable> Documents { get; }` — Open documents. |
| `Anchorables` | `IDockLayoutService` | `IEnumerable<IDockable> Anchorables { get; }` — Registered toolboxes/anchorables. |
| `OpenDocument` | `IDockLayoutService` | `void OpenDocument(IDockable document)` — Adds (if absent) and activates a document. |
| `CloseDocument` | `IDockLayoutService` | `void CloseDocument(IDockable document)` — Removes a document and activates the last remaining document. |
| `AnchorableStateChanged` | `IDockLayoutService` | `event EventHandler? AnchorableStateChanged` — Raised when any toolbox's `IsOpen` changes. |
| `FindDocument<T>` | Extension | `T? FindDocument<T>(Func<T, bool> predicate) where T : class, IDockable` |
| `OpenOrActivateDocument<T>` | Extension | `T OpenOrActivateDocument<T>(Func<T, bool> predicate, Func<T> factory) where T : class, IDockable` |
| `GetAnchorable<T>` | Extension | `T? GetAnchorable<T>() where T : class, IToolbox` |
| `ShowAnchorable` | Extension | `void ShowAnchorable(IDockable anchorable)` — Sets `IToolbox.IsOpen = true`. |
| `HideAnchorable` | Extension | `void HideAnchorable(IDockable anchorable)` — Sets `IToolbox.IsOpen = false`. |
| `IsAnchorableOpen` | Extension | `bool IsAnchorableOpen(IDockable anchorable)` |
| `IsSideOpen` | Extension | `bool IsSideOpen(ToolboxSide side)` |

---

### `IFactory` & `FactoryBase`

Abstract factory and base orchestration class for creating and manipulating MVVM dock trees (`namespace AvalonDock.Core`).

| Member | Signature | Description |
|:-------|:----------|:------------|
| `ContextLocator` | `IDictionary<string, Func<object?>>?` | Maps dockable `Id` strings to runtime `Context` factories during `InitLayout`. |
| `DockableLocator` | `IDictionary<string, Func<IDockable?>>?` | Maps dockable `Id` strings to `IDockable` factories. |
| `CreateRootDock()` | `IRootDock` | Creates a new root dock container. |
| `CreateDocumentDock()` | `IDocumentDock` | Creates a new document dock container. |
| `CreateToolDock()` | `IToolDock` | Creates a new tool dock container. |
| `CreateList<T>(params T[])` | `IList<T>` | Creates an observable list populated with `items`. |
| `CreateLayout()` | `IRootDock` | Creates the initial layout tree. |
| `InitLayout(IDockable)` | `void` | Recursively sets `Owner`, `Factory`, and `Context` across the tree. |
| `MoveDockable(IDock, IDock, IDockable)` | `void` | Moves `dockable` from `source` to `target` and activates it. |
| `PinDockable(IDockable)` | `void` | Toggles `dockable` between `PinnedDockables` (`AutoHidden`) and its `Owner` dock (`Docked`) when `CanPin` is `true`. |
| `FloatDockable(IDockable)` | `void` | Moves `dockable` into `RootDock.FloatingDockables` (`Float`) when `CanFloat` is `true`. |
| `CloseDockable(IDockable)` | `void` | Calls `dockable.OnClose()` and removes it (`DockState.Hidden`) if not vetoed. |

`FactoryBase` also provides virtual lifecycle hooks for subclasses: `OnDockableMoved`, `OnDockablePinned`, `OnDockableFloated`, `OnDockableClosed`, `OnDockableAdded`, and `OnActiveDockableChanged`.

---

### UI-Agnostic Host & Service Interfaces

| Interface | Key Members |
|:----------|:------------|
| `IDockingManager` | `DockLayout`, `ActiveContent`, `DocumentsSource`, `AnchorablesSource`, `AutoHideDelay`, `SupportsAutoHideFlyout`, `AllowMixedOrientation`, `AllowFloatingWindows`, `AllowDetachedWindows`, plus UI-agnostic `DocumentClosing`/`Closed`, `AnchorableClosing`/`Closed`/`Hiding`/`Hidden`, `ContentFloating`/`Floated`/`Docking`/`Docked`, `ActiveContentChanged`, `LayoutChanging`, and `LayoutChanged` events. |
| `ILayoutSerializer` | `LayoutSerializationCallback`, `Serialize(Stream)`, `Deserialize(Stream)`, `Serialize(string filepath)`, `Deserialize(string filepath)`, plus `LayoutSerializerExtensions.Serialize(TextWriter)` and `Deserialize(TextReader)`. |
| `IDockState` | `void Save(IRootDock rootDock)`, `void Restore(IRootDock rootDock)`. |
| `IAutoHideManager` | `int AutoHideDelayMs { get; set; }`, `void ShowAutoHide(string contentId)`, `void HideAutoHide()`, `bool IsAutoHideVisible { get; }`, `string CurrentAutoHideContentId { get; }`. |
| `IFloatingWindowService` | `bool Float(string contentId)`, `int FloatingWindowCount { get; }`, `void CloseAllFloatingWindows()`, `FloatingWindowCreated`, `FloatingWindowClosed`. |
| `IDragDropHandler` | `bool CanDrop(string contentId, string targetContentId, DockPosition position)`, `bool IsDragDropEnabled { get; set; }`. |
| `IThemeInfo` / `IThemeManager` | `IThemeInfo` (`Name`, `ResourceUri`); `IThemeManager` (`CurrentThemeName`, `AvailableThemes`, `bool ApplyTheme(string themeName)`, `ThemeChanged`). |

---

## 2. `AvalonDock.Core` Enums, Events & Geometry Helpers

### Enumerations

| Enum | Namespace | Values |
|:-----|:----------|:-------|
| `DockState` | `AvalonDock.Core` | `Docked`, `AutoHidden`, `Float`, `Hidden` |
| `DockAlignment` | `AvalonDock.Core` | `Left`, `Right`, `Top`, `Bottom` |
| `DockZone` | `AvalonDock.Core` | `LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight` *(convert to `ToolboxSide` via `zone.ToSide()`)* |
| `ToolboxSide` | `AvalonDock.Core` | `Left`, `Right`, `Bottom` |
| `DockPosition` | `AvalonDock.Core` | `None`, `Left`, `Right`, `Top`, `Bottom`, `Center`, `Float` |
| `UnresolvedContentHandling` | `AvalonDock.Core` | `Remove`, `Hide` |
| `DropTargetType` | `AvalonDock.Controls` *(in `AvalonDock.Core`)* | `DockingManagerDockLeft`/`Top`/`Right`/`Bottom`, `DocumentPaneDockLeft`/`Top`/`Right`/`Bottom`/`Inside`, `DocumentPaneGroupDockInside`, `AnchorablePaneDockLeft`/`Top`/`Right`/`Bottom`/`Inside`, `DocumentPaneDockAsAnchorableLeft`/`Top`/`Right`/`Bottom` |

### UI-Agnostic Event Args (`AvalonDock.Core.Events`)

| Class | Base Type | Key Properties |
|:------|:----------|:---------------|
| `DocumentEventArgs` | `EventArgs` | `ISerializableLayoutDocument Document` |
| `DocumentCancelEventArgs` | `CancelEventArgs` | `ISerializableLayoutDocument Document`, `bool Cancel` |
| `AnchorableEventArgs` | `EventArgs` | `ISerializableLayoutAnchorable Anchorable` |
| `AnchorableCancelEventArgs` | `CancelEventArgs` | `ISerializableLayoutAnchorable Anchorable`, `bool Cancel`, `bool CloseInsteadOfHide` |
| `ContentEventArgs` | `EventArgs` | `ISerializableLayoutContent Content` |
| `ContentCancelEventArgs` | `CancelEventArgs` | `ISerializableLayoutContent Content`, `bool Cancel` |

### Shared Overlay Geometry Helpers (`AvalonDock.Controls` in `AvalonDock.Core`)

- **`OverlayPreviewRules`:** `TryComputePanePreviewRect(...)` and `TryComputeManagerPreviewRect(...)` — pure logical-pixel drop preview rectangle calculations shared without WPF dependencies.
- **`OverlayTabTargetRules`:** `ShouldUseAsTrailingTabCandidate(...)` and `TryComputeTrailingTabDropArea(...)` — tab strip trailing drop-zone calculations.

---

## 3. `AvalonDock.Mvvm` Classes

Zero-dependency `INotifyPropertyChanged` implementations (`namespace AvalonDock.Mvvm`, decorated with `[DataContract]` / `[DataMember]` attributes for ViewModel state persistence):

| Type | Inherits / Implements | Purpose |
|:-----|:----------------------|:--------|
| `DockableBase` *(abstract)* | `INotifyPropertyChanged`, `IDockable` | Base view model with `SetProperty<T>(ref T, T)` and `OnPropertyChanged`. Defaults `CanClose`, `CanPin`, `CanFloat`, `CanDrag`, `CanDrop` to `true`, and `DockState` to `DockState.Docked`. |
| `ToolboxBase` *(abstract)* | `DockableBase`, `IToolbox` | Base class for sidebar toolboxes (`Zone` defaults to `DockZone.LeftTop`). |
| `DockBase` *(abstract)* | `DockableBase`, `IDock` | Base container for child dockables (`VisibleDockables`, `ActiveDockable`, `DefaultDockable`, `FocusedDockable`). |
| `RootDock` | `DockBase`, `IRootDock` | Root layout VM (`FloatingDockables`, `PinnedDockables`, `DefaultLayout`, `AllowFloatingWindows`, `AllowDetachedWindows`). |
| `DocumentDock` | `DockBase`, `IDocumentDock` | Document tab container VM (`CanCreateDocument = true`). |
| `ToolDock` | `DockBase`, `IToolDock` | Tool pane container VM (`Alignment`, `IsExpanded`, `AutoHide`). |
| `Document` | `DockableBase` | Concrete leaf view model for documents. |
| `Tool` | `DockableBase` | Concrete leaf view model for tools/anchorables. |
| `DockLayoutService` | `IDockLayoutService` | Default layout service that groups `IEnumerable<IToolbox>` by `Zone` alignment into `ToolDock` containers alongside a `DocumentDock` under a `RootDock`. |
| `SideToggleManager` | — | `Toggle(ToolboxSide)` — hides all open toolboxes on `Left`, `Right`, or `Bottom` while remembering which ones were open so the next call restores them. |
| `Factory` | `FactoryBase` | Concrete `IFactory` creating `RootDock`, `DocumentDock`, `ToolDock`, and `ObservableCollection<T>`. |

---

## 4. `AvalonDock.Mvvm.CommunityToolkit` Classes

`CommunityToolkit.Mvvm` equivalents (`namespace AvalonDock.Mvvm.CommunityToolkit`) inheriting from `ObservableObject` so derived classes can use `[ObservableProperty]` and `[RelayCommand]` source generators:

| Type | Inherits / Implements | Purpose |
|:-----|:----------------------|:--------|
| `ObservableDockableBase` *(abstract)* | `ObservableObject`, `IDockable` | Source-generator-compatible base for any dockable view model. |
| `ObservableToolboxBase` *(abstract)* | `ObservableDockableBase`, `IToolbox` | Source-generator-compatible base for sidebar toolboxes. |
| `ObservableDockBase` *(abstract)* | `ObservableDockableBase`, `IDock` | Source-generator-compatible base for dock containers. |
| `ObservableDocument` | `ObservableDockableBase` | Concrete leaf document view model. |
| `ObservableTool` | `ObservableDockableBase` | Concrete leaf tool/anchorable view model. |

---

## 5. `AvalonDock.DependencyInjection` Classes

DI registration types (`namespace AvalonDock.DependencyInjection`):

| Type | Key Members |
|:-----|:------------|
| `ServiceCollectionExtensions` | `AddDockLayoutService(Action<DockLayoutBuilder>)`, `AddDockLayoutService()`, `AddDockLayoutService<TService>()`, `AddAvalonDock<TFactory>(ServiceLifetime)`, `AddAvalonDockSerializer<TSerializer>()`, `AddAvalonDockState<TState>()`, `AddAvalonDockThemeManager<TThemeManager>()`, `AddDockingManager(factory)`, `AddAutoHideManager<TAutoHide>()`, `AddFloatingWindowService<TFloating>()`, `AddDragDropHandler<TDragDrop>()`. |
| `DockLayoutBuilder` | `ConfigureDocking(Action<DockingOptions>?)`, `ConfigureToggleDock(Action<ToggleDockOptions>?)`, `AddToolbox<T>()`, `AddToolbox<T>(Func<IServiceProvider, T>)`. |
| `DockingOptions` | `AllowFloatingWindows` (`true`), `AllowDetachedWindows` (`true`). |
| `ToggleDockOptions : DockingOptions` | `ButtonSize` (`25`), `DefaultDockWidth` (`250`), `DefaultDockHeight` (`200`), `LayoutPriority` (`"BottomFullWidth"`), `ShowHeaderMinimizeButton` (`true`), `ShowHeaderOptionsButton` (`true`). |
