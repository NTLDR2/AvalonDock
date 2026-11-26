---
title: ToggleDockingManager
layout: default
parent: Guides
nav_order: 2
description: "Build VS Code and JetBrains Rider-style 6-zone sidebar toggle interfaces with ToggleDockingManager."
---

# ToggleDockingManager

{: .new }
> New in v5

`ToggleDockingManager` extends `DockingManager` with a modern **VS Code / JetBrains Rider-style** sidebar interface. Instead of classic slide-out auto-hide flyouts, tool windows collapse onto vertical **toggle button bars** along the left and right edges, complete with vector or bitmap icons, keyboard shortcuts, 6-zone drag-and-drop relocation, standalone window detachment, and Rider/VS Code layout restructuring modes.

![ToggleDockingManager with Toolbox Open in Arc Dark Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Toolbox.png)

{: .tip }
See `source/AvalonDockCodeApp` in the repository for the complete reference application, or build one step by step in [Tutorial: Build an MVVM IDE Application]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}). For complete class and member tables, see the [ToggleDockingManager API Reference]({{ site.baseurl }}{% link api-reference/toggle-docking-manager.md %}).

---

## Quick Start

### XAML

```xml
<avalonDock:ToggleDockingManager
    x:Name="dockManager"
    DockLayout="{Binding DockLayout}"
    LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">

    <avalonDock:ToggleDockingManager.Theme>
        <avalonDockThemes:ArcDarkTheme />
    </avalonDock:ToggleDockingManager.Theme>

    <avalonDock:LayoutRoot>
        <avalonDock:LayoutPanel Orientation="Horizontal">
            <avalonDock:LayoutDocumentPaneGroup>
                <avalonDock:LayoutDocumentPane />
            </avalonDock:LayoutDocumentPaneGroup>
        </avalonDock:LayoutPanel>
    </avalonDock:LayoutRoot>
</avalonDock:ToggleDockingManager>
```

### Code-Behind (or via DI `ToggleDockOptions`)

```csharp
dockManager.ButtonSize = 28;
dockManager.DefaultDockWidth = 280;
dockManager.DefaultDockHeight = 220;
dockManager.LayoutPriority = DockLayoutPriority.BottomFullWidth;
```

### Theme Support

`ToggleDockingManager` works out of the box with **every** AvalonDock theme package:
- `Dirkster.AvalonDock.Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`)
- `Dirkster.AvalonDock.Themes.VS` (`VS2015*`, `VS2022*`, `VS2026*`, and custom `.vstheme` / JSON themes — natively styled without requiring `Themes.Arc` resources since `v5.0.2`)
- `Dirkster.AvalonDock.Themes.VS2013`, `VS2010`, `Expression`, `Metro`, `Aero`, and built-in `GenericTheme`

| Arc Dark | Arc Light |
|:---------|:----------|
| ![ToggleDockingManager Arc Dark]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Toolbox.png) | ![ToggleDockingManager Arc Light]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Toolbox.png) |

---

## Six-Zone Layout & Sidebar Architecture

`ToggleDockingManager` organizes tool windows into **six `DockZone` regions** (two per left, right, and bottom workspace edge). Following the JetBrains Rider convention, all six `ToggleDockButtonBar` strips live along the **left and right vertical sidebars**, keeping the bottom edge of your window clean when bottom tools are collapsed:

```text
┌──────────────┬───────────────────────────────────────┬──────────────┐
│ [LeftTop]    │                                       │ [RightTop]   │
│   buttons    │                                       │   buttons    │
│ ──────────── │             Left / Right              │ ──────────── │
│ [LeftBottom] │            & Document Area            │ [RightBottom]│
│   buttons    │                                       │   buttons    │
│   [ ⋯ ]      │                                       │              │
│              ├───────────────────┬───────────────────┤              │
│              │  BottomLeft pane  │ BottomRight pane  │              │
│ [BottomLeft] │   (when open)     │   (when open)     │ [BottomRight]│
│   buttons    │                   │                   │   buttons    │
└──────────────┴───────────────────┴───────────────────┴──────────────┘
```

| `DockZone` | Sidebar Button Bar Position | Docked Panel Placement | UI Automation ID |
|:-----------|:----------------------------|:-----------------------|:-----------------|
| `LeftTop` | Left sidebar, above separator | Left side, upper split | `ToggleDockBar_LeftTop` |
| `LeftBottom` | Left sidebar, below separator | Left side, lower split | `ToggleDockBar_LeftBottom` |
| `RightTop` | Right sidebar, above separator | Right side, upper split | `ToggleDockBar_RightTop` |
| `RightBottom` | Right sidebar, below separator | Right side, lower split | `ToggleDockBar_RightBottom` |
| `BottomLeft` | Left sidebar, bottom corner | Bottom area, left split | `ToggleDockBar_BottomLeft` |
| `BottomRight` | Right sidebar, bottom corner | Bottom area, right split | `ToggleDockBar_BottomRight` |

- **Exclusive per zone, simultaneous across zones:** Each zone shows at most **one** docked panel at a time (clicking another button in `LeftTop` swaps it in), while `LeftTop` and `LeftBottom` can both be open simultaneously in a vertical split—and `BottomLeft` + `BottomRight` can both be open side by side in a horizontal bottom split!
- **Hidden Panels (`⋯`) button:** Located below `LeftBottom` on the left sidebar; clicking it opens a menu of any panels currently in `Layout.Hidden` so users can restore them to their zones interactively.

---

## Configuration Properties

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `LayoutPriority` | `DockLayoutPriority` | `BottomFullWidth` | Controls how side and bottom panels share corner space. |
| `ButtonSize` | `double` | `25.0` | Sidebar toggle button size in pixels. |
| `DefaultDockWidth` | `double` | `250.0` | Initial width applied to newly opened left/right panes. |
| `DefaultDockHeight` | `double` | `200.0` | Initial height applied to newly opened bottom panes. |
| `ShowHeaderMinimizeButton` | `bool` | `true` | Shows the minimize (`—`) button in docked tool pane headers. |
| `ShowHeaderOptionsButton` | `bool` | `true` | Shows the three-dot (`⋯`) options menu button in tool pane headers. |

---

## Layout Priority Modes

`LayoutPriority` controls how `ToggleLayoutEngine` restructures the panel tree when both side and bottom zones are open:

| Mode | IDE Style | Behavior |
|:-----|:----------|:---------|
| `DockLayoutPriority.BottomFullWidth` | JetBrains Rider | Bottom panels span the full width between the sidebars; left and right panels sit above them. |
| `DockLayoutPriority.SidesFullHeight` | VS Code | Left and right panels span the full height; bottom panels sit constrained between them. |
| `DockLayoutPriority.Default` | Classic | No automatic corner restructuring; panes stay where inserted. |

---

## MVVM Integration with `IToolbox`

Inherit your tool-window view models from `ToolboxBase` (`AvalonDock.Mvvm`) or `ObservableToolboxBase` (`AvalonDock.Mvvm.CommunityToolkit`) and register them via `AddDockLayoutService`:

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;

public class ExplorerToolbox : ToolboxBase
{
    public ExplorerToolbox()
    {
        Id = "Explorer";
        Title = "Explorer";
        Zone = DockZone.LeftTop;
        IsOpenByDefault = true;
        ToolTipText = "Solution Explorer";
        Shortcut = "Ctrl+Shift+E";   // Appends "(Ctrl+Shift+E)" to the tooltip automatically
        Icon = myExplorerIcon;       // ImageSource, DrawingImage, or UIElement (Path / Viewbox)
    }
}
```

```csharp
services.AddDockLayoutService(dock =>
{
    dock.ConfigureToggleDock(opts =>
    {
        opts.ButtonSize = 28;
        opts.DefaultDockWidth = 280;
        opts.DefaultDockHeight = 220;
    });

    dock.AddToolbox<ExplorerToolbox>();
    dock.AddToolbox<SearchToolbox>();
    dock.AddToolbox<OutputToolbox>();
});
```

### Startup & Runtime Visibility (`IsOpenByDefault` vs `IsOpen`)

- `IsOpenByDefault` seeds startup visibility for a fresh layout.
- `IsOpen` is the live two-way property reflecting whether the toolbox is currently showing. Because `ToggleDockingManager` checks **both** `IsOpen` and `IsOpenByDefault` when loading, setting `IsOpen = true` in a DI-constructed view model before the window exists still opens the panel on startup.
- Because a zone displays one panel at a time, setting `IsOpenByDefault = true` on two toolboxes in the same zone leaves the last one open and collapses the earlier sibling (`IsOpen == false`).
- A toolbox detached into a standalone top-level window (**View Mode → Window**) counts as open (`IsOpen == true`); toggling it while detached brings that standalone window to the foreground. Call `ReattachAnchorable` first if you want to collapse it onto the sidebar.

### Keyboard Shortcuts (`IToolbox.Shortcut`)

Setting `IToolbox.Shortcut` to any valid WPF `KeyGesture` string (such as `"Ctrl+Shift+E"`, `"Ctrl+Shift+F"`, or `"Ctrl+OemTilde"`) registers a `KeyBinding` on the host `Window` that toggles the toolbox open or closed.

`ToggleDockButton` also appends `" (<Shortcut>)"` to `ToolTipText` (or `Title`) automatically—so set `ToolTipText = "Solution Explorer"` rather than repeating the gesture string inside `ToolTipText`. Unparsable gesture strings are safely ignored without throwing.

---

## Customizing Sidebar Buttons in Pure XAML

When defining `LayoutAnchorable` elements directly in XAML without `IToolbox`, use the `ToggleDock` attached properties in `AvalonDock.Controls`:

```xml
xmlns:avalonDockControls="clr-namespace:AvalonDock.Controls;assembly=AvalonDock"

<avalonDock:LayoutAnchorable
    Title="Explorer"
    ContentId="explorer"
    avalonDockControls:ToggleDock.Icon="{StaticResource ExplorerIcon}"
    avalonDockControls:ToggleDock.ToolTip="File Explorer">
    <!-- Panel content -->
</avalonDock:LayoutAnchorable>
```

| Attached Property | Type | Description |
|:------------------|:-----|:------------|
| `ToggleDock.Icon` | `object` | Button icon (`ImageSource`, `DrawingImage`, or `UIElement`). Overrides `IToolbox.Icon` and `LayoutAnchorable.IconSource`. |
| `ToggleDock.ToolTip` | `object` | Custom tooltip base text (overrides `LayoutAnchorable.Title`). |
| `ToggleDock.IconTemplate` | `DataTemplate` | Optional `DataTemplate` used to render `IconContent`. |

### Icon vs Vertical Title Rendering

Each `ToggleDockButton` displays **either** an icon **or** its rotated title—never both:
- **With an icon** (`ToggleDock.Icon`, `IToolbox.Icon`, or `LayoutAnchorable.IconSource`): renders as a `ButtonSize × ButtonSize` square displaying the icon upright.
- **Without an icon:** renders the title rotated 90° vertically through `DockingManager.AnchorableHeaderTemplate`, growing along the bar axis so longer titles are never clipped.

To recolor the fallback button and `Hidden Panels` (`⋯`) glyph when using `GenericTheme`, override the `ToggleDockButton.ForegroundBrushKey` resource.

---

## Context Menus & View Modes

Right-clicking a sidebar `ToggleDockButton` (or clicking the `⋯` options button in a tool pane header) opens a built-in menu:

- **Hide** *(on sidebar button right-click)* — Hides the anchorable into `Layout.Hidden` and removes its sidebar button until restored from the **Hidden Panels (`⋯`)** button or `anchorable.Show()`.
- **Move To** — Moves the panel to any of the six zones (`Left Top`, `Left Bottom`, `Right Top`, `Right Bottom`, `Bottom Left`, `Bottom Right`).
- **View Mode:**
  - **Float** — Tears the panel into an owned floating window (disabled when `AllowFloatingWindows == false`).
  - **Window** — Detaches the panel into an independent top-level OS window with its own taskbar entry, while keeping its sidebar toggle button active to bring the window forward (disabled when `AllowDetachedWindows == false`).
  - **Docked** — Docks (or reattaches) the panel into its assigned zone.
  - **Hidden** — Hides the panel into `Layout.Hidden`.

---

## Drag-and-Drop Zone Overlay

Dragging either a sidebar `ToggleDockButton` or a docked tool pane header displays a semi-transparent **6-zone drop overlay** (`Left Top`, `Left Bottom`, `Right Top`, `Right Bottom`, `Bottom Left`, `Bottom Right`) plus sidebar insertion indicators. Press `Esc` while dragging to cancel.

| Dragging a Tool Window | Dragging a Document Tab |
|:-----------------------|:------------------------|
| ![6-Zone Tool Drag Overlay]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Drag.png) | ![Document Tab Drag Overlay]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Drag_Document.png) |

---

## Programmatic API

### From View Models (Recommended MVVM Way)

```csharp
// Show or collapse a single toolbox via IToolbox.IsOpen or IDockLayoutService extensions
explorerToolbox.IsOpen = true;
dockLayoutService.ShowAnchorable(explorerToolbox);
dockLayoutService.HideAnchorable(explorerToolbox);

// Toggle an entire workspace edge (remembers which panels on that side were open)
sideToggleManager.Toggle(ToolboxSide.Left);
sideToggleManager.Toggle(ToolboxSide.Bottom);
sideToggleManager.Toggle(ToolboxSide.Right);
```

### From Code-Behind (`ToggleDockingManager`)

```csharp
// Toggle an anchorable open/collapsed in a zone
dockManager.ToggleAnchorable(anchorable, DockZone.LeftTop);

// Relocate an anchorable to another zone and open it
dockManager.MoveAnchorableToZone(anchorable, DockZone.RightBottom);

// Detach to / reattach from a standalone top-level OS window (inherited from DockingManager)
dockManager.DetachAnchorableToWindow(anchorable);
dockManager.ReattachAnchorable(anchorable);
```

---

## Comparison: `DockingManager` vs `ToggleDockingManager`

| Aspect | `DockingManager` | `ToggleDockingManager` |
|:-------|:-----------------|:-----------------------|
| Collapsed tool UI | Classic auto-hide tabs with slide-out flyout (`SupportsAutoHideFlyout == true`) | Vertical icon/title toggle bars (`SupportsAutoHideFlyout == false`) |
| Placement granularity | 4 sides (`Left`, `Right`, `Top`, `Bottom`) | 6 zones (`LeftTop`/`Bottom`, `RightTop`/`Bottom`, `BottomLeft`/`Right`) |
| Layout engine | `DefaultLayoutEngine` | `ToggleLayoutEngine` (with `DockLayoutPriority`) |
| Default update strategy | `DockAlignmentStrategy` (when `DockLayout` bound) | `ToggleLayoutStrategy` |
| Tool drag overlay | Pane-level 5-arrow `OverlayWindow` | 6-zone visual overlay + classic document `OverlayWindow` |
