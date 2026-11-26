---
title: Layout Classes
layout: default
parent: API Reference
nav_order: 3
description: "Complete API reference for AvalonDock.Layout classes, enums, extension methods, ILayoutEngine, and layout interfaces."
---

# Layout Classes
{: .no_toc }

All layout tree classes, enums, traversal extension methods, and structural interfaces reside in `AvalonDock.Layout` (with `ILayoutEngine` and `DefaultLayoutEngine` in `AvalonDock`).
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Exact Class Inheritance Hierarchy

```text
System.Windows.DependencyObject
└── LayoutElement (abstract : ILayoutElement, ISerializableLayoutElement)
    ├── LayoutRoot (: ILayoutContainer, ILayoutRoot, ISerializableLayoutRoot)
    ├── LayoutContent (abstract : IComparable<LayoutContent>, ILayoutPreviousContainer, ISerializableLayoutContent, ISerializablePreviousContainer)
    │   ├── LayoutDocument (: ISerializableLayoutDocument)
    │   └── LayoutAnchorable (: ISerializableLayoutAnchorable)
    ├── LayoutGroupBase (abstract : ISerializableLayoutContainer)
    │   └── LayoutGroup<T> (abstract : ILayoutGroup)
    │       ├── LayoutAnchorSide (: LayoutGroup<LayoutAnchorGroup>)
    │       ├── LayoutAnchorGroup (: LayoutGroup<LayoutAnchorable>, ILayoutPreviousContainer, ILayoutPaneSerializable, ...)
    │       └── LayoutPositionableGroup<T> (abstract : LayoutGroup<T>)
    │           ├── LayoutPanel (: LayoutPositionableGroup<ILayoutPanelElement>, ILayoutPanelElement, ILayoutOrientableGroup, ILayoutElementWithVisibility)
    │           ├── LayoutDocumentPaneGroup (: LayoutPositionableGroup<ILayoutDocumentPane>, ILayoutDocumentPane, ILayoutOrientableGroup)
    │           ├── LayoutDocumentPane (: LayoutPositionableGroup<LayoutContent>, ILayoutDocumentPane, ILayoutContentSelector, ILayoutPaneSerializable, ...)
    │           ├── LayoutAnchorablePaneGroup (: LayoutPositionableGroup<ILayoutAnchorablePane>, ILayoutAnchorablePane, ILayoutOrientableGroup)
    │           └── LayoutAnchorablePane (: LayoutPositionableGroup<LayoutAnchorable>, ILayoutAnchorablePane, ILayoutContentSelector, ILayoutPaneSerializable, ...)
    └── LayoutFloatingWindow (abstract : ILayoutContainer)
        ├── LayoutDocumentFloatingWindow (: ILayoutElementWithVisibility)
        └── LayoutAnchorableFloatingWindow (: ILayoutElementWithVisibility)
```

{: .note }
`LayoutRoot` and `LayoutFloatingWindow` inherit directly from `LayoutElement` (implementing `ILayoutContainer`), **not** from `LayoutGroup<T>`.

---

## `LayoutElement` (Abstract Base)

Base class for every node in the layout tree.

| Member | Type | Description |
|:-------|:-----|:------------|
| `Parent` | `ILayoutContainer` | Parent container (`null` on `LayoutRoot` or detached elements). Changing `Parent` fires `LayoutRoot.Updated`. |
| `Root` | `ILayoutRoot` | Walks `Parent` pointers up to the owning `LayoutRoot` (or `null` if unparented). |
| `PropertyChanging` / `PropertyChanged` | `event` | Standard `INotifyPropertyChanging` and `INotifyPropertyChanged` notifications. |
| `FixCachedRootOnDeserialize()` | `void` | Refreshes cached root reference after deserialization. |

---

## `LayoutRoot`

Root container of the layout tree (`[ContentProperty(nameof(RootPanel))]`).

| Property | Type | Description |
|:---------|:-----|:------------|
| `Manager` | `DockingManager` | Owning `DockingManager` (read-only publicly; assigned when set on `DockingManager.Layout`). |
| `RootPanel` | `LayoutPanel` | Primary docked panel tree. Assigning `null` initializes a new `LayoutPanel(new LayoutDocumentPane())`. |
| `TopSide` / `BottomSide` / `LeftSide` / `RightSide` | `LayoutAnchorSide` | Auto-hide edge containers initialized automatically by the constructor. |
| `FloatingWindows` | `ObservableCollection<LayoutFloatingWindow>` | Active `LayoutDocumentFloatingWindow` and `LayoutAnchorableFloatingWindow` models. |
| `Hidden` | `ObservableCollection<LayoutAnchorable>` | Hidden tool windows (`IsHidden == true`). |
| `ActiveContent` | `LayoutContent` | Currently focused `LayoutDocument` or `LayoutAnchorable`. |
| `LastFocusedDocument` | `LayoutContent` | Most recently active document-well item (read-only). |
| `Children` / `ChildrenCount` | `IEnumerable<ILayoutElement>` / `int` | Enumerates `RootPanel`, `FloatingWindows`, all 4 `LayoutAnchorSide`s, and `Hidden`. |

| Method / Event | Signature | Description |
|:---------------|:----------|:------------|
| `CollectGarbage()` | `void` | Removes empty panes, groups, panels, floating windows, and stale `PreviousContainer` references while preserving at least one main-window `LayoutDocumentPane`. |
| `RemoveChild(ILayoutElement)` / `ReplaceChild(old, new)` | `void` | `ILayoutContainer` child management. |
| `Updated` | `event EventHandler` | Raised whenever structural parent links change within this `LayoutRoot`. |
| `ElementAdded` / `ElementRemoved` | `event EventHandler<LayoutElementEventArgs>` | Raised when an element enters or leaves this `LayoutRoot`. |

---

## Group & Positionable Base Classes

### `LayoutGroupBase` & `LayoutGroup<T>`

| Member | Type | Description |
|:-------|:-----|:------------|
| `Children` | `ObservableCollection<T>` | Typed child collection; adding an element automatically detaches it from its old parent and sets `element.Parent = this`. |
| `ChildrenCount` | `int` | Number of direct children. |
| `IsVisible` | `bool` | Computed visibility based on child visibility (`ComputeVisibility()`). |
| `MoveChild(oldIndex, newIndex)` | `void` | Reorders a child within `Children`. |
| `IndexOfChild` / `InsertChildAt` / `RemoveChildAt` / `RemoveChild` / `ReplaceChild` / `ReplaceChildAt` | methods | `ILayoutGroup` / `ILayoutContainer` manipulation methods. |
| `ChildrenCollectionChanged` | `event EventHandler` | Raised when `Children` changes directly. |
| `ChildrenTreeChanged` | `event EventHandler<ChildrenTreeChangedEventArgs>` | Bubbles `DirectChildrenChanged` and `TreeChanged` notifications up ancestor groups. |

### `LayoutPositionableGroup<T>`

Inherited by `LayoutPanel`, `LayoutDocumentPane`, `LayoutDocumentPaneGroup`, `LayoutAnchorablePane`, and `LayoutAnchorablePaneGroup`:

| Property / Method | Type | Default | Description |
|:------------------|:-----|:--------|:------------|
| `DockWidth` / `DockHeight` | `GridLength` | `1*` | Docked width and height (`Pixel`, `Star`, or `Auto`). |
| `DockMinWidth` / `DockMinHeight` | `double` | `25.0` | Minimum docked pixel dimensions. |
| `FixedDockWidth` / `FixedDockHeight` | `double` | — | Effective pixel size clamped to `DockMinWidth` / `DockMinHeight`. |
| `ResizableAbsoluteDockWidth` / `ResizableAbsoluteDockHeight` | `double` | `0.0` | Runtime pixel dimensions adjusted during splitter drags. |
| `CalculatedDockMinWidth()` / `CalculatedDockMinHeight()` | `double` | — | Effective minimum width/height accounting for visible children and splitter thickness. |
| `FloatingLeft` / `FloatingTop` / `FloatingWidth` / `FloatingHeight` | `double` | `0.0` | Saved/active floating window geometry. |
| `IsMaximized` | `bool` | `false` | Whether the floating window for this group is maximized. |
| `AllowDuplicateContent` | `bool` | `true` | Whether duplicate content items are permitted. |
| `CanRepositionItems` | `bool` | `true` | Whether child tabs can be reordered by dragging. |
| `FloatingPropertiesUpdated` | `event EventHandler` | — | Raised when monitor-clamping updates floating bounds. |

---

## `LayoutPanel`

Primary structural split container (`[ContentProperty(nameof(Children))]`).

| Member | Type | Default | Description |
|:-------|:-----|:--------|:------------|
| `LayoutPanel()` / `LayoutPanel(ILayoutPanelElement firstChild)` | constructors | — | Creates an empty panel or one seeded with `firstChild`. |
| `Orientation` | `Orientation` | `Horizontal` | `Orientation.Horizontal` (left-to-right) or `Orientation.Vertical` (top-to-bottom). |
| `CanDock` | `bool` (DP) | `true` | Whether interactive docking into this panel is allowed. |
| `Children` | `ObservableCollection<ILayoutPanelElement>` | empty | Child panels, pane groups, and panes. |

---

## `LayoutContent`, `LayoutDocument` & `LayoutAnchorable`

### `LayoutContent` (Abstract Base)

`[ContentProperty(nameof(Content))]` — shared by `LayoutDocument` and `LayoutAnchorable`.

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `Title` | `string` (DP) | `null` | Display title on tabs and headers. |
| `ContentId` | `string` (DP) | `null` | Serialization identifier (falls back to `FrameworkElement.Name` when `Content` is a named `FrameworkElement`). |
| `Content` | `object` | `null` | Hosted `UIElement` or MVVM ViewModel. |
| `IconSource` | `ImageSource` | `null` | Optional tab/header icon. |
| `ToolTip` | `object` | `null` | Tab tooltip. |
| `IsActive` | `bool` | `false` | Whether this item is the active content of `LayoutRoot`. |
| `IsSelected` | `bool` | `false` | Whether this item is selected in its parent pane. |
| `IsLastFocusedDocument` | `bool` | `false` | Read-only; `true` for `LayoutRoot.LastFocusedDocument`. |
| `IsFloating` | `bool` | — | Read-only; `true` when parented inside a `LayoutFloatingWindow`. |
| `IsEnabled` | `bool` | `true` | Whether the tab can be selected/activated. |
| `CanClose` | `bool` | `true` (`LayoutDocument`), `false` (`LayoutAnchorable`) | Whether closing is permitted. |
| `CanFloat` | `bool` | `true` | Whether tearing off into a floating window is permitted. |
| `CanShowOnHover` | `bool` | `true` | Whether hovering an auto-hide tab expands the flyout. |
| `FloatingLeft` / `FloatingTop` / `FloatingWidth` / `FloatingHeight` | `double` | `0.0` | Floating (and detached) window coordinates and size. |
| `IsMaximized` | `bool` | `false` | Maximized state when floating or detached. |
| `LastActivationTimeStamp` | `DateTime?` | `null` | Timestamp updated whenever `IsActive` becomes `true` (orders `Ctrl+Tab` navigation). |
| `PreviousContainerIndex` | `int` | `-1` | Tab index inside `PreviousContainer`. |
| `TabItem` | `LayoutDocumentTabItem` | `null` | Associated document tab visual (read-only publicly). |

| Method | Signature | Description |
|:-------|:----------|:------------|
| `Float()` | `void` | Tears the item off into a `LayoutFloatingWindowControl` (subject to `AllowFloatingWindows` and `CanFloat`). |
| `Dock()` | `void` | Returns a floating item to its `PreviousContainer` (or docks via `InternalDock()`), raising `DockingManager.ContentDocked`. |
| `DockAsDocument()` | `void` | Docks the item as a tab inside a main-window `LayoutDocumentPane`, raising `ContentDocked` if previously floating. |
| `Close()` | `abstract void` | Closes the item (invokes `DockingManager` close pipeline when attached to a manager). |
| `CompareTo(LayoutContent other)` | `int` | Compares `Content` if `IComparable`, otherwise compares `Title`. |

### `LayoutDocument`

| Member | Type | Default | Description |
|:-------|:-----|:--------|:------------|
| `Description` | `string` | `null` | Extended description (persisted in DTOs and shown in `NavigatorWindow`). |
| `CanMove` | `bool` | `true` | Whether the document tab can be dragged/reordered. |
| `CanHide` | `bool` | `false` | Read-only `false`. |
| `IsVisible` | `bool` | `true` | Read-only `true` while open. |
| `Close()` | `void` | — | Raises `DocumentClosing` / `Closing`, removes from pane, and raises `DocumentClosed` / `Closed`. |

### `LayoutAnchorable`

| Member | Type | Default | Description |
|:-------|:-----|:--------|:------------|
| `CanHide` | `bool` | `true` | Whether hiding into `LayoutRoot.Hidden` is allowed. |
| `CanAutoHide` | `bool` | `true` | Whether auto-hiding onto a `LayoutAnchorSide` is allowed. |
| `CanDockAsTabbedDocument` | `bool` | `true` | Whether docking into a `LayoutDocumentPane` is allowed. |
| `CanMove` | `bool` | `true` | Whether dragging/repositioning is allowed. |
| `AutoHideWidth` / `AutoHideHeight` | `double` | `0.0` | Flyout size when auto-hidden. |
| `AutoHideMinWidth` / `AutoHideMinHeight` | `double` | `100.0` | Minimum flyout dimensions (`>= 0`). |
| `IsVisible` | `bool` | — | `true` when parented outside `LayoutRoot.Hidden`; setter calls `Show()` or `Hide()`. |
| `IsHidden` | `bool` | — | Read-only; `true` when parked in `LayoutRoot.Hidden`. |
| `IsAutoHidden` | `bool` | — | Read-only; `true` when inside a `LayoutAnchorGroup`. |
| `IsDetached` | `bool` | `false` | `true` when hosted in a standalone `DetachedAnchorableWindow` (persisted across serialization). |
| `AddToLayout(DockingManager manager, AnchorableShowStrategy strategy)` | `void` | — | Inserts a **new unparented** anchorable (`!IsVisible && !IsHidden`) into `manager.Layout`. Throws `InvalidOperationException` if already visible or hidden. |
| `Show()` | `void` | — | Restores a **hidden** anchorable (`IsHidden == true`) to `PreviousContainer` or an existing/new docked `LayoutAnchorablePane`. |
| `Hide()` | `void` | — | Hides via `DockingManager` (`AnchorableHiding` / `AnchorableHidden`) or `HideAnchorable(true)`. |
| `HideAnchorable(bool cancelable)` | `bool` | — | Moves into `LayoutRoot.Hidden`, optionally raising `LayoutAnchorable.Hiding`. |
| `RemoveFromLayout()` | `void` | — | Detaches from both visible containers and `LayoutRoot.Hidden` without raising close events or disturbing auto-hidden siblings. |
| `ToggleAutoHide()` | `void` | — | Toggles auto-hide for **all tabs** in the parent `LayoutAnchorablePane` or `LayoutAnchorGroup`. |
| `ToggleSingleAutoHide()` | `void` | — | Toggles auto-hide for **only this anchorable**, leaving sibling tabs in place. |

---

## Pane, Side & Floating Window Classes

### `LayoutDocumentPane` & `LayoutAnchorablePane`

| Class | Key Members |
|:------|:------------|
| `LayoutDocumentPane` | `LayoutDocumentPane()`, `LayoutDocumentPane(LayoutContent firstChild)`, `Children` (`ObservableCollection<LayoutContent>`), `ChildrenSorted` (`IEnumerable<LayoutContent>`), `ShowHeader` (`bool`, default `true`), `SelectedContentIndex` (`int`), `SelectedContent` (`LayoutContent`), `IsHostedInFloatingWindow` (`bool`), `IsDirectlyHostedInFloatingWindow` (`bool`), `IndexOf(LayoutContent)`. |
| `LayoutAnchorablePane` | `LayoutAnchorablePane()`, `LayoutAnchorablePane(LayoutAnchorable anchorable)`, `Children` (`ObservableCollection<LayoutAnchorable>`), `Name` (`string`), `CanHide` (`bool`), `CanClose` (`bool`), `SelectedContentIndex` (`int`), `SelectedContent` (`LayoutContent`), `IsHostedInFloatingWindow` (`bool`), `IsDirectlyHostedInFloatingWindow` (`bool`), `IndexOf(LayoutContent)`. |

### `LayoutDocumentPaneGroup` & `LayoutAnchorablePaneGroup`

- **`LayoutDocumentPaneGroup`** — Constructors `()` and `(LayoutDocumentPane documentPane)`; `Orientation` (`Orientation`), `Children` (`ObservableCollection<ILayoutDocumentPane>`).
- **`LayoutAnchorablePaneGroup`** — Constructors `()` and `(LayoutAnchorablePane firstChild)`; `Orientation` (`Orientation`), `Children` (`ObservableCollection<ILayoutAnchorablePane>`). Propagates absolute `DockWidth` / `DockHeight` to a single child automatically.

### `LayoutAnchorSide` & `LayoutAnchorGroup`

- **`LayoutAnchorSide`** — `Side` (`AnchorSide`: `Left`, `Top`, `Right`, `Bottom`), `Children` (`ObservableCollection<LayoutAnchorGroup>`).
- **`LayoutAnchorGroup`** — `Children` (`ObservableCollection<LayoutAnchorable>`); implements `ILayoutPreviousContainer` and `ILayoutPaneSerializable` so the group remembers the `LayoutAnchorablePane` it was unpinned from across save/restore.

### `LayoutFloatingWindow`, `LayoutDocumentFloatingWindow` & `LayoutAnchorableFloatingWindow`

- **`LayoutFloatingWindow`** *(abstract)* — `Children`, `ChildrenCount`, `IsValid`, `RemoveChild`, `ReplaceChild`.
- **`LayoutDocumentFloatingWindow`** — `RootPanel` (`LayoutDocumentPaneGroup`), `IsSinglePane` (`bool`), `SinglePane` (`LayoutDocumentPane`), `IsVisible` (`bool`), `IsVisibleChanged` event.
- **`LayoutAnchorableFloatingWindow`** — `RootPanel` (`LayoutAnchorablePaneGroup`), `IsSinglePane` (`bool`), `SinglePane` (`ILayoutAnchorablePane`), `IsVisible` (`bool`), `IsVisibleChanged` event.

---

## Enums & Traversal Extensions (`AvalonDock.Layout`)

### Enums

| Enum | Values | Description |
|:-----|:-------|:------------|
| `AnchorSide` | `Left`, `Top`, `Right`, `Bottom` | Identifies which edge of `LayoutRoot` an anchor side or pane belongs to. |
| `AnchorableShowStrategy` (`[Flags]`) | `Most = 0x01`, `Left = 0x02`, `Right = 0x04`, `Top = 0x10`, `Bottom = 0x20` | Placement flags passed to `LayoutAnchorable.AddToLayout(dockManager, strategy)`. Combine `Most` with a side flag to force a new outer pane. |
| `ChildrenTreeChange` | `DirectChildrenChanged`, `TreeChanged` | Indicates whether `LayoutGroupBase.ChildrenTreeChanged` originated on this group or a descendant group. |

### `AvalonDock.Layout.Extensions`

| Extension Method | Description |
|:-----------------|:------------|
| `element.Descendents()` | Depth-first `IEnumerable<ILayoutElement>` over all nested children of `element` (if `element` is an `ILayoutContainer`). |
| `element.FindParent<T>()` | Walks `Parent` ancestors and returns the first ancestor of type `T` (or `default`). |
| `element.GetRoot()` | Returns `element` if it is an `ILayoutRoot`, otherwise walks up to the owning `ILayoutRoot`. |
| `container.ContainsChildOfType<T>()` / `ContainsChildOfType<T, S>()` | Returns `true` if any descendant matches `T` (or `S`). |
| `container.IsOfType<T, S>()` | Returns `true` if `container` itself is `T` or `S`. |
| `element.GetSide()` | Infers the `AnchorSide` (`Left`, `Right`, `Top`, `Bottom`) of a docked pane from its enclosing `LayoutPanel`. |

---

## `ILayoutEngine` & Public Layout Interfaces

### `AvalonDock.ILayoutEngine` (Implemented by `DefaultLayoutEngine` and `ToggleLayoutEngine`)

| Method | Signature | Description |
|:-------|:----------|:------------|
| `InsertPane` | `void InsertPane(LayoutRoot root, LayoutAnchorablePane pane, AnchorSide side)` | Inserts `pane` on `side` of `root`. |
| `FindOrCreateContentPanel` | `LayoutPanel FindOrCreateContentPanel(LayoutRoot root, Orientation orientation)` | Locates or creates a `LayoutPanel` with the requested `Orientation`. |
| `FixSplitOrientation` | `void FixSplitOrientation(LayoutAnchorable anchorable, AnchorSide side)` | Groups contiguous side panes with `Vertical` orientation (Left/Right) or `Horizontal` orientation (Top/Bottom). |
| `EnsureBottomFullWidth` | `void EnsureBottomFullWidth(LayoutRoot root)` | Lifts bottom panes to the outermost vertical panel so they span full window width. |
| `EnsureSidesFullHeight` | `void EnsureSidesFullHeight(LayoutRoot root)` | Lifts left/right panes to the outermost horizontal panel so they span full window height. |
| `GetDesiredOrientation` | `Orientation GetDesiredOrientation(AnchorSide side)` | Returns `Horizontal` for `Top`/`Bottom` and `Vertical` for `Left`/`Right`. |

### Public Interfaces in `AvalonDock.Layout`

| Interface | Description |
|:----------|:------------|
| `ILayoutElement` | Base interface (`Parent`, `Root`, `INotifyPropertyChanged`, `INotifyPropertyChanging`). |
| `ILayoutContainer` | Container element (`Children`, `ChildrenCount`, `RemoveChild`, `ReplaceChild`). |
| `ILayoutGroup` | Indexed container (`ChildrenCollectionChanged`, `IndexOfChild`, `InsertChildAt`, `RemoveChildAt`, `ReplaceChildAt`). |
| `ILayoutOrientableGroup` | `ILayoutGroup` with `Orientation` (`Horizontal` / `Vertical`). |
| `ILayoutPane` | `ILayoutContainer` + `ILayoutElementWithVisibility` (`MoveChild`, `RemoveChildAt`). |
| `ILayoutPanelElement` | Element allowed inside `LayoutPanel.Children` (`IsVisible`). |
| `ILayoutDocumentPane` | Marker combining `ILayoutPanelElement` and `ILayoutPane` for document panes/groups. |
| `ILayoutAnchorablePane` | Marker combining `ILayoutPanelElement` and `ILayoutPane` for anchorable panes/groups. |
| `ILayoutContentSelector` | Tab selection contract (`SelectedContentIndex`, `SelectedContent`, `IndexOf`). |
| `ILayoutContentElement` | Exposes a hosted WPF `FrameworkElement Content { get; }`. |
| `ILayoutControl` | Implemented by visual `AvalonDock.Controls` classes (`ILayoutElement Model { get; }`). |
| `ILayoutRoot` | Root contract (`Manager`, `RootPanel`, 4 `LayoutAnchorSide`s, `ActiveContent`, `FloatingWindows`, `Hidden`, `CollectGarbage()`). |
| `ILayoutUpdateStrategy` | Placement hooks (`BeforeInsertAnchorable`, `AfterInsertAnchorable`, `BeforeInsertDocument`, `AfterInsertDocument`). |
| `ILayoutPreviousContainer` | Tracks `PreviousContainer` and `PreviousContainerId` for re-docking and `Show()`. |
| `ILayoutPaneSerializable` | Assigns a persistent `string Id { get; set; }` to panes referenced by `PreviousContainerId`. |
| `ILayoutElementWithVisibility` | Exposes `void ComputeVisibility()`. |
| `IAdjustableSizeLayout` | Exposes `void AdjustFixedChildrenPanelSizes(Size? parentSize = null)`. |
