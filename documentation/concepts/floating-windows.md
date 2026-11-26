---
title: Floating Windows
layout: default
parent: Concepts
nav_order: 4
description: "How tear-off floating windows, standalone detached OS windows, overlay drop targets, and multi-monitor persistence work in AvalonDock v5."
---

# Floating & Detached Windows
{: .no_toc }

AvalonDock v5 offers two distinct multi-window modes: **Tear-Off Floating Windows** (`LayoutFloatingWindowControl`) that participate in interactive drag-and-drop docking, and **Standalone Detached Windows** (`DetachedAnchorableWindow`, the IDE **"Window"** view mode) that behave as independent operating-system windows with their own taskbar entry and z-order.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Floating Windows vs Standalone Detached Windows

| Characteristic | Tear-Off Floating Window (`LayoutFloatingWindowControl`) | Standalone Window (`DetachedAnchorableWindow`) |
|:---------------|:---------------------------------------------------------|:-----------------------------------------------|
| **Supported content** | Both `LayoutDocument` and `LayoutAnchorable` | `LayoutAnchorable` tool windows |
| **Window chrome** | Custom themed borderless chrome (`WindowChrome`) | Standard OS window chrome (`SingleBorderWindow`) |
| **Owner & z-order** | Owned by main window by default (`OwnedByDockingManagerWindow = true`); stays above main window | Unowned (`Owner = null`); can move behind the main window freely |
| **Taskbar & minimize** | `ShowInTaskbar = false`; minimizes with owner by default | `ShowInTaskbar = true`; minimizes and restores independently |
| **Multi-tab & splits** | Hosts full `LayoutDocumentPaneGroup` or `LayoutAnchorablePaneGroup` trees | Hosts a single `LayoutAnchorable`'s content presenter |
| **Drag-docking overlays** | Participates in drag-and-drop docking as both drag source and drop host | Moved by the OS window manager; closing returns the panel to the layout |
| **Global switch** | `DockingManager.AllowFloatingWindows` (default `true`) | `DockingManager.AllowDetachedWindows` (default `true`) |

---

## Tear-Off Floating Windows

Users can float any `LayoutDocument` or `LayoutAnchorable` (when `AllowFloatingWindows` and `CanFloat` are `true`) by:
1. **Dragging** a tab (`LayoutDocumentTabItem` / `LayoutAnchorableTabItem`) or pane header (`AnchorablePaneTitle`) out of its pane.
2. Selecting **Float** from the tab/header context menu (`LayoutItem.FloatCommand`).
3. Calling `content.Float()` or `dockManager.CreateFloatingWindow(content, isContentImmutable: false)` in C#.

```csharp
using System.Linq;
using AvalonDock.Layout;

var properties = dockManager.Layout.Descendents()
    .OfType<LayoutAnchorable>()
    .First(a => a.ContentId == "properties");

properties.FloatingWidth = 360;
properties.FloatingHeight = 480;
properties.Float();
```

AvalonDock creates one of two concrete window controls paired with its layout model:

| Layout Model (`AvalonDock.Layout`) | WPF Window Control (`AvalonDock.Controls`) | Hosts |
|:-----------------------------------|:-------------------------------------------|:------|
| `LayoutAnchorableFloatingWindow` | `LayoutAnchorableFloatingWindowControl` | `LayoutAnchorablePaneGroup` (one or more split tool panes) |
| `LayoutDocumentFloatingWindow` | `LayoutDocumentFloatingWindowControl` | `LayoutDocumentPaneGroup` (one or more split document wells) |

---

## Controlling Floating & Detached Policies

### Disable Floating Per Item (`CanFloat`)

Set `CanFloat="False"` on an individual `LayoutDocument` or `LayoutAnchorable` (or `IDockable.CanFloat = false` in MVVM) to keep that item docked while allowing other panels to float:

```xml
<avalonDock:LayoutAnchorable Title="Pinned Explorer"
                             ContentId="explorer"
                             CanFloat="False" />
```

{: .note }
`LayoutAnchorableItem.DetachToWindowCommand` checks both `DockingManager.AllowDetachedWindows` and `LayoutAnchorable.CanFloat`, so setting `CanFloat="False"` prevents both tear-off floating and standalone detaching for that panel.

### Turn Tear-Off Floating Off Globally (`AllowFloatingWindows`)

{: .new }
New in AvalonDock v5: `DockingManager.AllowFloatingWindows` disables tear-off floating across the entire workspace with a single property.

```xml
<avalonDock:DockingManager AllowFloatingWindows="False" />
```

While `AllowFloatingWindows` is `false`:
- Dragging tabs or tool window headers outside their pane does not tear them off.
- `LayoutItem.FloatCommand` reports `CanExecute == false`, disabling **Float** menu items automatically.
- `LayoutContent.Float()` does nothing and `DockingManager.CreateFloatingWindow(...)` returns `null`.
- Setting `AllowFloatingWindows = false` at runtime immediately docks all open floating windows back into the main layout via `DockAllFloatingWindows()`.
- Loading a saved XML layout that contains floating windows automatically docks their content into the main window before creating visuals.

You can also call `dockManager.DockAllFloatingWindows()` directly (for example, from a **Window → Dock All Floating Windows** menu command) while keeping `AllowFloatingWindows = true`.

### Turn Standalone Windows Off Globally (`AllowDetachedWindows`)

{: .new }
New in AvalonDock v5: `DockingManager.AllowDetachedWindows` controls standalone OS tool windows independently of `AllowFloatingWindows`.

```xml
<avalonDock:DockingManager AllowDetachedWindows="False" />
```

While `AllowDetachedWindows` is `false`:
- `DockingManager.DetachAnchorableToWindow(anchorable)` is a no-op and `LayoutAnchorableItem.DetachToWindowCommand` is disabled.
- Setting `AllowDetachedWindows = false` at runtime calls `ReattachAllDetachedAnchorables()`, closing any open standalone windows and returning their content to the layout.
- Saved layouts with `IsDetached="true"` restore those anchorables inside the main window instead of opening standalone windows.

### MVVM (`IRootDock`) and Dependency Injection Configuration

In ViewModel-driven (`DockLayout`) applications, configure both switches on `IRootDock` or through `AvalonDock.DependencyInjection`:

```csharp
// Direct MVVM service configuration
dockService.Layout.AllowFloatingWindows = false;
dockService.Layout.AllowDetachedWindows = true;

// Or during DI registration (ConfigureDocking or ConfigureToggleDock)
services.AddDockLayoutService(dock =>
{
    dock.AddToolbox<ExplorerViewModel>();
    dock.ConfigureDocking(options =>
    {
        options.AllowFloatingWindows = false;
        options.AllowDetachedWindows = true;
    });
});
```

{: .warning }
**`DockLayout` Precedence:** Binding `DockingManager.DockLayout` synchronizes `IRootDock.AllowFloatingWindows` and `IRootDock.AllowDetachedWindows` onto the `DockingManager`. If you set `AllowFloatingWindows="False"` in XAML *and* bind a `DockLayout` whose `IRootDock` leaves `AllowFloatingWindows` at its `true` default, the `DockLayout` value wins and re-enables floating. Configure the switches in one place—either on `IRootDock` / DI options when using `DockLayout`, or on `DockingManager` in XAML/code-behind mode.

---

## Customizing Floating Window Chrome, Ownership & Behavior

### Styling & Resizing Properties on `LayoutFloatingWindowControl`

Style `LayoutFloatingWindowControl` (or customize instances in `DockingManager.LayoutFloatingWindowControlCreated`) using these DependencyProperties:

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `ResizeBorderThickness` | `Thickness` | `default` (theme `WindowChrome`) | Overrides the interactive resize grip thickness on the floating window's `WindowChrome`. |
| `AllowMinimize` | `bool` | `false` | Allows minimizing floating windows instead of forcing non-maximized windows back to `WindowState.Normal`. |
| `OwnedByDockingManagerWindow` | `bool` | `true` | Controls whether the main window owns the floating window. Set to `false` to allow minimizing the main window while keeping floating windows visible on other monitors. |
| `IsMaximized` | `bool` | `false` | Tracks and persists maximized state across owner minimize/restore cycles and layout serialization. |

```xml
<Style TargetType="{x:Type avalonDockControls:LayoutFloatingWindowControl}">
    <Setter Property="ResizeBorderThickness" Value="8" />
    <Setter Property="AllowMinimize" Value="True" />
    <Setter Property="OwnedByDockingManagerWindow" Value="False" />
</Style>
```

### Additional `DockingManager` Floating Options

- **`AutoWindowSizeWhenOpened`** (`bool`, default `false`) — Automatically sizes newly floated windows from the hosted content's `MinWidth` / `MinHeight` plus window chrome margins (`TotalMargin`).
- **`AllowMovingFloatingWindowWithKeyboard`** (`bool`, default `false`) — Allows moving a focused floating window in `10px` increments with the `Left`, `Right`, `Up`, and `Down` arrow keys.
- **`ShowSystemMenu`** (`bool`, default `true`) — Displays the standard Windows system menu when right-clicking a floating window title bar if no custom `DocumentContextMenu` / `AnchorableContextMenu` handles the click.

### Engine & Lifecycle Guarantees in v5

- **Deferred Owner & Show Before Host Window Handle Creation:** Calling `Float()` or assigning a layout before the host `Window` has created its Win32 `HWND` defers ownership attachment to `Window.SourceInitialized` and postpones `Show()` until `DockingManager.Loaded`, preventing `InvalidOperationException` and orphaned windows.
- **Inheritable Property Mirroring:** Because a WPF `Window` is the root of its own tree and cannot inherit properties through the logical tree, `DockingManager` explicitly mirrors inheritable DependencyProperties (`FontFamily`, `FontSize`, `FlowDirection`, `TextOptions.TextFormattingMode`, `TextOptions.TextRenderingMode`, etc.) onto open floating windows without overriding local or style-set values on the floating window itself.
- **Consistent System Close (`Alt+F4` / System Menu) Routing:** Closing a `LayoutAnchorableFloatingWindowControl` via `Alt+F4` or `WM_CLOSE` executes `HideWindowCommand` (or `CloseWindowCommand` when `CanHide` is `false` and `CanClose` is `true`), matching the title-bar button behavior rather than bypassing `AnchorableHiding`. Closing a `LayoutDocumentFloatingWindowControl` routes through `CloseWindowCommand` so `DocumentClosing` handlers can cancel unsaved closes.

---

## Docking Targets & `OverlayWindow` Reuse

Dragging a floating window over `DockingManager` or another floating window displays vector XAML drop indicators crisp at any DPI (100%–400%):
- **Outer Manager Targets (4):** Dock to the outer Top, Bottom, Left, or Right edge of `DockingManager`.
- **Pane Targets (5):** Split Left, Top, Right, or Bottom, or drop **Into** the pane as a tab (dropping directly onto an existing tab header inserts at that tab index).
- **Document Pane Full Targets (9):** When dragging a floating `LayoutAnchorable` with `CanDockAsTabbedDocument = true` over a main-window `LayoutDocumentPane`, four inner buttons split/tab within the document well while four outer `*AsAnchorablePane` buttons dock an anchorable pane adjacent to the document well.

### Single `OverlayWindow` Per Host

Each drop host (`DockingManager` and each open `LayoutFloatingWindowControl`) allocates **at most one `OverlayWindow`** and hides/reuses it across drags instead of creating and closing a new window per drag. If Windows terminates a modal drag loop without a final message (for example, across monitors with different DPI scaling), every drag start and drag end clears all hosts' overlay windows—ensuring transparent overlay windows never accumulate over long sessions and are closed only when their owning host closes.

---

## Floating & Docking Events

`DockingManager` exposes pre-operation cancellable events and post-operation notifications for the complete float/dock lifecycle:

| Event | EventArgs | Cancellable | When It Fires |
|:------|:----------|:------------|:--------------|
| `ContentFloating` | `ContentFloatingEventArgs` | Yes (`Cancel = true`) | Before docked content (or an anchorable pane drag) is torn off into a floating window. |
| `ContentFloated` | `ContentFloatedEventArgs` | No | After the floating window has been shown for the floated content. |
| `ContentDocking` | `ContentDockingEventArgs` | Yes (`Cancel = true`) | Before floating content is docked back—fires for **both** interactive drag-and-drop onto an `OverlayWindow` target and programmatic/command `Dock()` / `DockAsDocument()`. |
| `ContentDocked` | `ContentDockedEventArgs` | No | After floating content settles into its docked pane (via drag-and-drop, `Dock()`, `DockAsDocument()`, or `DockAllFloatingWindows()`). |
| `LayoutFloatingWindowControlCreated` | `LayoutFloatingWindowControlCreatedEventArgs` | No | Immediately after a new `LayoutFloatingWindowControl` is created (`args.LayoutFloatingWindowControl`). |
| `LayoutFloatingWindowControlClosed` | `LayoutFloatingWindowControlClosedEventArgs` | No | When a `LayoutFloatingWindowControl` closes and leaves `DockingManager.FloatingWindows`. |

```csharp
dockManager.ContentFloating += (_, args) =>
{
    if (args.Content.ContentId == "lockedEditor")
    {
        args.Cancel = true; // Prevent tearing off this item
    }
};

dockManager.ContentDocking += (_, args) =>
{
    if (args.Content.ContentId == "externalScopeMonitor")
    {
        args.Cancel = true; // Keep this panel floating
    }
};

dockManager.LayoutFloatingWindowControlCreated += (_, args) =>
{
    args.LayoutFloatingWindowControl.AllowMinimize = true;
};
```

---

## Standalone Windows ("Window" View Mode)

{: .new }
New in AvalonDock v5: `DetachAnchorableToWindow` hosts a tool window inside an independent OS `DetachedAnchorableWindow`—ideal for parking terminals, profilers, or inspectors on a secondary monitor with their own taskbar button and independent minimize/z-order behavior.

### Programmatic & Command APIs

```csharp
// Move an anchorable into a standalone top-level OS window
dockManager.DetachAnchorableToWindow(anchorable);

// Query detached state
bool isInWindow = dockManager.IsDetached(anchorable);
var allDetached = dockManager.DetachedAnchorables;

// Close the standalone window and return the panel to its layout slot
dockManager.ReattachAnchorable(anchorable);

// Return every standalone tool window to the main window
dockManager.ReattachAllDetachedAnchorables();
```

In the UI, users toggle standalone mode via **View Mode → Window** in the tool window options menu (or by binding a button/menu item to `LayoutAnchorableItem.DetachToWindowCommand`, which detaches when docked and reattaches when already detached). Closing the `DetachedAnchorableWindow` with the OS **×** button returns the panel to the layout automatically.

### Layout Slot, Multi-Monitor Safety & Lifetime

1. **Single Presenter Ownership:** Because a WPF visual can have only one parent, `DetachAnchorableToWindow` disconnects `LayoutItem.View` (`ContentPresenter`) from the main visual/logical tree and places it inside `DetachedAnchorableWindow` beneath the panel's `AnchorablePaneTitle` (or `ToggleAnchorablePaneTitle`) header, merging the active `DockingManager.Theme` resources into the standalone window.
2. **Manager-Specific Slot Behavior:** Classic `DockingManager` parks the detached `LayoutAnchorable` in `LayoutRoot.Hidden` (remembering `PreviousContainer` and `PreviousContainerIndex`) so `ReattachAnchorable` restores its exact tab position. `ToggleDockingManager` collapses the panel onto its sidebar rail so its `ToggleDockButton` remains visible—clicking the sidebar button activates and brings the standalone window to the front.
3. **Serialization & Off-Screen Protection:** `LayoutAnchorable.IsDetached` (`LayoutAnchorableDto.IsDetached`) and window bounds (`FloatingLeft`, `FloatingTop`, `FloatingWidth`, `FloatingHeight`) round-trip through `XmlLayoutSerializer`. On restore, `DetachedAnchorableWindow` verifies that at least an `80 × 80` DIP region intersects `SystemParameters.VirtualScreen*`; if a secondary monitor was disconnected, it falls back to `WindowStartupLocation.CenterScreen` so the window is never stranded off-screen.
4. **Clean Shutdown:** Even though `DetachedAnchorableWindow` has `Owner = null`, `DockingManager` hooks the host window's `Closed` event (and `OnLayoutChanged`) to close all standalone windows automatically so ownerless windows never keep the process alive under `ShutdownMode.OnLastWindowClose`.

---

## Related Documentation

- [Controls API Reference]({{ site.baseurl }}{% link api-reference/controls.md %}) — Member tables for `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, `OverlayWindow`, and `LayoutAnchorableItem`.
- [DockingManager API Reference]({{ site.baseurl }}{% link api-reference/docking-manager.md %}) — Global windowing switches and detached/floating management methods.
- [Events API Reference]({{ site.baseurl }}{% link api-reference/events.md %}) — Full `EventArgs` reference for all floating and docking events.
- [Layout Serialization Guide]({{ site.baseurl }}{% link guides/serialization.md %}) — How floating windows and `IsDetached` panels are persisted.
