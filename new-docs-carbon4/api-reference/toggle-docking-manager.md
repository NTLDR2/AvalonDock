---
title: ToggleDockingManager
layout: default
parent: API Reference
nav_order: 2
description: "API reference for ToggleDockingManager, DockLayoutPriority, ToggleLayoutEngine, and ToggleDock controls."
---

# ToggleDockingManager Reference

{: .new }
> New in v5

`AvalonDock.ToggleDockingManager` subclasses [`DockingManager`]({{ site.baseurl }}{% link api-reference/docking-manager.md %}) to provide a VS Code / JetBrains Rider-style 6-zone sidebar toggle docking experience.

---

## Inheritance & Defaults

```text
System.Windows.Controls.Control
  └── AvalonDock.DockingManager
        └── AvalonDock.ToggleDockingManager
```

When constructed, `ToggleDockingManager` configures:
- `SupportsAutoHideFlyout = false` (disables classic hover/slide auto-hide popups)
- `LayoutEngine` → `ToggleLayoutEngine`
- `LayoutUpdateStrategy` → `ToggleLayoutStrategy`
- `BorderThickness` default metadata → `0`

---

## `ToggleDockingManager` Properties

In addition to all properties inherited from [`DockingManager`]({{ site.baseurl }}{% link api-reference/docking-manager.md %}) (such as `DockLayout`, `Layout`, `Theme`, `ActiveContent`, `AllowFloatingWindows`, and `AllowDetachedWindows`), `ToggleDockingManager` defines six Dependency Properties:

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `LayoutPriority` | `DockLayoutPriority` | `DockLayoutPriority.BottomFullWidth` | Controls how side and bottom panels are nested when both are docked. |
| `ButtonSize` | `double` | `25.0` | Cross-axis size (and minimum length) of sidebar toggle buttons in pixels. |
| `DefaultDockWidth` | `double` | `250.0` | Default pixel width applied when docking a left or right zone panel. |
| `DefaultDockHeight` | `double` | `200.0` | Default pixel height applied when docking a bottom zone panel. |
| `ShowHeaderMinimizeButton` | `bool` | `true` | Whether docked tool pane headers display the minimize (`—`) button. |
| `ShowHeaderOptionsButton` | `bool` | `true` | Whether docked tool pane headers display the options (`⋯`) menu button. |
| `LayoutEngine` | `ILayoutEngine` | `ToggleLayoutEngine` | Overridden read-only property returning the manager's `ToggleLayoutEngine`. |

---

## `ToggleDockingManager` Methods

### Public Methods

| Method | Signature | Description |
|:-------|:----------|:------------|
| `ToggleAnchorable` | `void ToggleAnchorable(LayoutAnchorable anchorable, DockZone zone)` | Docks a collapsed anchorable into `zone` (collapsing any other anchorable currently docked in that same bar, applying `LayoutPriority`, and focusing it), or collapses a docked anchorable back onto its sidebar bar. If `anchorable` is detached in a standalone window, activates that window instead. |
| `MoveAnchorableToZone` | `void MoveAnchorableToZone(LayoutAnchorable anchorable, DockZone targetZone)` | Relocates `anchorable` (and its sidebar `ToggleDockButton`) to `targetZone` and opens it there. If `anchorable` is currently detached in a standalone window, updates the zone it will return to upon reattachment. |

*(Inherited standalone/floating window methods such as `DetachAnchorableToWindow`, `ReattachAnchorable`, `ReattachAllDetachedAnchorables`, `IsDetached`, `DetachedAnchorables`, and `DockAllFloatingWindows` are also available and integrate natively with sidebar zones.)*

### Protected Extensibility Hooks

| Method | Signature | Description |
|:-------|:----------|:------------|
| `DetachFromLayout` | `protected override object DetachFromLayout(LayoutAnchorable anchorable)` | Collapses the anchorable onto its sidebar stripe rather than hiding it when entering standalone window mode, remembering its `DockZone`. |
| `ReturnToLayout` | `protected override void ReturnToLayout(LayoutAnchorable anchorable, object restoreState)` | Restores a reattached standalone anchorable back into its remembered `DockZone`. |
| `CreateDetachedWindowHeader` | `protected override FrameworkElement CreateDetachedWindowHeader(LayoutAnchorable anchorable)` | Returns a `ToggleAnchorablePaneTitle` so standalone tool windows retain the three-dot (`⋯`) options menu. |
| `OnDetachedAnchorablesChanged` | `protected override void OnDetachedAnchorablesChanged(LayoutAnchorable anchorable)` | Synchronizes `IToolbox.IsOpen` and sidebar button visual states when standalone windows open or close. |

---

## `DockLayoutPriority` Enum

Defined in `namespace AvalonDock`:

| Value | Description |
|:------|:------------|
| `BottomFullWidth` | Bottom zone panels span the full width of the manager; left and right panels sit above them (JetBrains Rider layout). |
| `SidesFullHeight` | Left and right zone panels span the full height of the manager; bottom panels sit between them (VS Code layout). |
| `Default` | No automatic layout tree restructuring after docking. |

---

## `ToggleLayoutEngine` & `ToggleLayoutStrategy`

Defined in `namespace AvalonDock`:

### `ToggleLayoutEngine : ILayoutEngine`

Zone-aware layout tree engine operating purely on the `LayoutRoot` model:

| Member | Signature | Description |
|:-------|:----------|:------------|
| `IsBottomZone` | `static bool IsBottomZone(DockZone zone)` | `true` for `BottomLeft` or `BottomRight`. |
| `IsLeftZone` | `static bool IsLeftZone(DockZone zone)` | `true` for `LeftTop` or `LeftBottom`. |
| `IsRightZone` | `static bool IsRightZone(DockZone zone)` | `true` for `RightTop` or `RightBottom`. |
| `ZoneToAnchorSide` | `static AnchorSide ZoneToAnchorSide(DockZone zone)` | Maps a 6-way `DockZone` to `AnchorSide.Left`, `Right`, or `Bottom`. |
| `GetDesiredOrientationForZone` | `static Orientation GetDesiredOrientationForZone(DockZone zone)` | `Horizontal` for bottom zones; `Vertical` for left/right zones. |
| `InsertPaneForZone` | `void InsertPaneForZone(LayoutRoot root, LayoutAnchorablePane pane, DockZone zone)` | Inserts `pane` into `root` respecting upper/lower (`LeftTop` vs `LeftBottom`, `RightTop` vs `RightBottom`) and left/right (`BottomLeft` vs `BottomRight`) ordering. |
| `FixSplitOrientationForZone` | `void FixSplitOrientationForZone(LayoutAnchorable anchorable, DockZone zone)` | Groups contiguous anchorable panes vertically on left/right edges or horizontally on the bottom edge. |
| `EnsureBottomFullWidth` | `void EnsureBottomFullWidth(LayoutRoot root)` | Restructures `root` so bottom panes span full width. |
| `EnsureSidesFullHeight` | `void EnsureSidesFullHeight(LayoutRoot root)` | Restructures `root` so left/right panes span full height. |

### `ToggleLayoutStrategy : ILayoutUpdateStrategy`

Default `ILayoutUpdateStrategy` installed by `ToggleDockingManager`. When an `IToolbox` view model is inserted into `LayoutRoot`, it copies `Title`, `Id` → `ContentId`, `Icon` → `ToggleDock.Icon`, and `ToolTipText` → `ToggleDock.ToolTip`, and places the anchorable onto the `LayoutAnchorSide` matching `IToolbox.Zone`.

---

## Supporting Controls (`AvalonDock.Controls`)

### `ToggleDock` (Attached Properties)

Static helper class providing XAML attached properties for `LayoutAnchorable` elements:

| Attached Property | CLR Accessors | Type | Description |
|:------------------|:--------------|:-----|:------------|
| `ToggleDock.IconProperty` | `GetIcon` / `SetIcon` | `object` | Sidebar button icon (`ImageSource`, `DrawingImage`, or `UIElement`). |
| `ToggleDock.ToolTipProperty` | `GetToolTip` / `SetToolTip` | `object` | Custom tooltip base text (shortcut from `IToolbox.Shortcut` is appended automatically). |
| `ToggleDock.IconTemplateProperty` | `GetIconTemplate` / `SetIconTemplate` | `DataTemplate` | Optional `DataTemplate` for rendering `IconContent`. |

### `ToggleDockButtonBar : ItemsControl`

Container hosting `ToggleDockButton` items for a single `DockZone` (assigned `AutomationProperties.AutomationId="ToggleDockBar_<Zone>"` for UI automation tests):

| Member | Type / Signature | Description |
|:-------|:-----------------|:------------|
| `Zone` | `DockZone` | Zone represented by this bar (default `DockZone.LeftTop`). |
| `Orientation` | `Orientation` | Bar item layout orientation (default `Orientation.Vertical`). |
| `SetAnchorables` | `void SetAnchorables(IEnumerable<LayoutAnchorable> anchorables, DockZone zone)` | Clears `Items` and populates a `ToggleDockButton` for each anchorable. |

### `ToggleDockButton : ToggleButton`

Individual sidebar toggle button supporting click-to-toggle, right-click context menu, and drag-to-zone gestures:

| Member | Type | Description |
|:-------|:-----|:------------|
| `ForegroundBrushKey` *(static)* | `ComponentResourceKey` | Shared foreground brush key (`new ComponentResourceKey(typeof(ToggleDockButton), "ForegroundBrush")`). |
| `Anchorable` | `LayoutAnchorable` | Linked layout anchorable model. |
| `Zone` | `DockZone` | Current `DockZone` of the button. |
| `IconSource` | `ImageSource` | Bitmap/drawing image icon source. |
| `IconContent` | `object` | Arbitrary WPF icon content (`UIElement`, `ImageSource`, etc.). |
| `IconTemplate` | `DataTemplate` | Template applied to `IconContent`. |
| `IsAnchorableFocused` | `bool` | `true` when `Anchorable` is docked and is the manager's `ActiveContent`. |

### `ToggleAnchorablePaneTitle : AnchorablePaneTitle`

Header control used on toggle-docked tool panes and standalone detached tool windows:

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `ShowMinimizeButton` | `bool` | `true` | Shows or hides `PART_MinimizeButton`. |
| `ShowOptionsButton` | `bool` | `true` | Shows or hides `PART_OptionsButton` (`⋯` context menu trigger). |
