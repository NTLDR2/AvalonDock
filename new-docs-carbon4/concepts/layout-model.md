---
title: Layout Model
layout: default
parent: Concepts
nav_order: 2
description: "Understanding the LayoutRoot tree, sizing rules, tree traversal, ILayoutUpdateStrategy, and ILayoutEngine in AvalonDock v5."
---

# Layout Model
{: .no_toc }

AvalonDock represents every docking workspace as an observable **layout model tree** in the `AvalonDock.Layout` namespace, completely separate from the WPF controls (`AvalonDock.Controls`) that render it on screen.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## The Layout Tree

Every `DockingManager` (and `ToggleDockingManager`) owns a `LayoutRoot` instance in its `Layout` property (`[ContentProperty(nameof(Layout))]`). A typical IDE layout tree looks like this:

```text
LayoutRoot
├── RootPanel: LayoutPanel (Orientation = Horizontal)
│   ├── LayoutAnchorablePane (DockWidth = 250)
│   │   └── LayoutAnchorable (Title = "Solution Explorer", ContentId = "explorer")
│   ├── LayoutDocumentPaneGroup (Orientation = Horizontal)
│   │   ├── LayoutDocumentPane
│   │   │   ├── LayoutDocument (Title = "Program.cs", ContentId = "program")
│   │   │   └── LayoutDocument (Title = "MainWindow.xaml", ContentId = "mainWindow")
│   │   └── LayoutDocumentPane
│   │       └── LayoutDocument (Title = "README.md", ContentId = "readme")
│   └── LayoutAnchorablePaneGroup (Orientation = Vertical, DockWidth = 280)
│       ├── LayoutAnchorablePane
│       │   └── LayoutAnchorable (Title = "Properties", ContentId = "properties")
│       └── LayoutAnchorablePane
│           └── LayoutAnchorable (Title = "Outline", ContentId = "outline")
├── TopSide: LayoutAnchorSide
├── LeftSide: LayoutAnchorSide
├── RightSide: LayoutAnchorSide
├── BottomSide: LayoutAnchorSide
│   └── LayoutAnchorGroup
│       └── LayoutAnchorable (Title = "Output", ContentId = "output")   [IsAutoHidden = true]
├── FloatingWindows: ObservableCollection<LayoutFloatingWindow>
│   └── LayoutAnchorableFloatingWindow
│       └── RootPanel: LayoutAnchorablePaneGroup
│           └── LayoutAnchorablePane
│               └── LayoutAnchorable (Title = "Diagnostics")            [IsFloating = true]
└── Hidden: ObservableCollection<LayoutAnchorable>
    └── LayoutAnchorable (Title = "Terminal", ContentId = "terminal")   [IsHidden = true]
```

{: .note }
When you bind `DockingManager.DockLayout` to an `IRootDock` (`IDockLayoutService.Layout`), `LayoutSyncBridge` constructs and synchronizes this `LayoutRoot` tree automatically from your UI-agnostic ViewModels. See [Architecture]({{ site.baseurl }}{% link concepts/architecture.md %}) for details.

---

## Element Roles

### Root & Structural Containers

| Class | Children Type | Purpose |
|:------|:--------------|:--------|
| `LayoutRoot` | `RootPanel`, 4 `LayoutAnchorSide`s, `FloatingWindows`, `Hidden` | Root of the layout tree (`dockManager.Layout`). Tracks `ActiveContent`, `LastFocusedDocument`, and `Manager`. |
| `LayoutPanel` | `ILayoutPanelElement` | Primary split container arranging nested panels, pane groups, and panes horizontally or vertically. |
| `LayoutDocumentPaneGroup` | `ILayoutDocumentPane` | Splits multiple `LayoutDocumentPane` instances (or nested `LayoutDocumentPaneGroup`s) horizontally or vertically. |
| `LayoutDocumentPane` | `LayoutContent` | Tabbed document well holding `LayoutDocument` tabs (and `LayoutAnchorable` items when `CanDockAsTabbedDocument` is `true`). |
| `LayoutAnchorablePaneGroup` | `ILayoutAnchorablePane` | Splits multiple `LayoutAnchorablePane` instances (or nested `LayoutAnchorablePaneGroup`s) along a side. |
| `LayoutAnchorablePane` | `LayoutAnchorable` | Tabbed tool window container with header, pin/auto-hide button, and options menu. |

### Auto-Hide, Floating & Hidden Containers

| Container / Collection | Holds | Purpose |
|:-----------------------|:------|:--------|
| `LayoutAnchorSide` (`TopSide`, `BottomSide`, `LeftSide`, `RightSide`) | `LayoutAnchorGroup` | Border strip along one edge of the `DockingManager`. Exposes `Side` (`AnchorSide`). |
| `LayoutAnchorGroup` | `LayoutAnchorable` | Groups auto-hidden tool windows that were unpinned together; remembers `PreviousContainer` so re-pinning returns to the original pane. |
| `LayoutDocumentFloatingWindow` | `RootPanel` (`LayoutDocumentPaneGroup`) | Model for a tear-off floating window containing document panes. |
| `LayoutAnchorableFloatingWindow` | `RootPanel` (`LayoutAnchorablePaneGroup`) | Model for a tear-off floating window containing tool panes. |
| `LayoutRoot.Hidden` | `LayoutAnchorable` | Holds hidden tool windows (`IsHidden == true`) so `anchorable.Show()` can restore them later. |

---

## Orientation and Sizing

### Split Orientation

`LayoutPanel`, `LayoutDocumentPaneGroup`, and `LayoutAnchorablePaneGroup` implement `ILayoutOrientableGroup` and arrange their `Children` using `System.Windows.Controls.Orientation`:

- **`Orientation.Horizontal`** — Stacks children left-to-right with vertical `LayoutGridResizerControl` splitters between them.
- **`Orientation.Vertical`** — Stacks children top-to-bottom with horizontal `LayoutGridResizerControl` splitters between them.

### `DockWidth`, `DockHeight`, and Minimum Sizes

All positionable containers (`LayoutPanel`, `LayoutDocumentPane`, `LayoutDocumentPaneGroup`, `LayoutAnchorablePane`, and `LayoutAnchorablePaneGroup`) inherit from `LayoutPositionableGroup<T>` and use WPF `GridLength` values for `DockWidth` and `DockHeight` (defaulting to `1*`):

| Sizing Mode | XAML Syntax | C# `GridLength` Equivalent | Behavior |
|:------------|:------------|:---------------------------|:---------|
| **Fixed pixels** | `DockWidth="250"` | `new GridLength(250, GridUnitType.Pixel)` (or `new GridLength(250)`) | Fixed pixel width/height; user splitter drags update `ResizableAbsoluteDockWidth` / `ResizableAbsoluteDockHeight`. |
| **Proportional (star)** | `DockWidth="*"` or `DockWidth="2*"` | `new GridLength(1, GridUnitType.Star)` / `new GridLength(2, GridUnitType.Star)` | Shares remaining space proportionally across star-sized siblings. |
| **Auto** | `DockWidth="Auto"` | `GridLength.Auto` | Sizes to child content. |

`DockMinWidth` and `DockMinHeight` default to `25.0` pixels, and `CalculatedDockMinWidth()` / `CalculatedDockMinHeight()` aggregate minimum dimensions across visible children and grid splitters so panes cannot be crushed to zero size.

```xml
<avalonDock:LayoutRoot>
    <avalonDock:LayoutPanel Orientation="Vertical">
        <avalonDock:LayoutPanel Orientation="Horizontal" DockHeight="*">
            <avalonDock:LayoutAnchorablePane DockWidth="260" DockMinWidth="150">
                <avalonDock:LayoutAnchorable Title="Explorer" ContentId="explorer" />
            </avalonDock:LayoutAnchorablePane>
            <avalonDock:LayoutDocumentPane DockWidth="*" />
        </avalonDock:LayoutPanel>

        <avalonDock:LayoutAnchorablePane DockHeight="180" DockMinHeight="80">
            <avalonDock:LayoutAnchorable Title="Output" ContentId="output" />
        </avalonDock:LayoutAnchorablePane>
    </avalonDock:LayoutPanel>
</avalonDock:LayoutRoot>
```

---

## Navigating the Layout Tree in Code

Import `AvalonDock.Layout` to bring the `Descendents()`, `FindParent<T>()`, `GetRoot()`, `ContainsChildOfType<T>()`, and `GetSide()` extension methods into scope:

```csharp
using System.Linq;
using AvalonDock.Layout;

LayoutRoot layout = dockManager.Layout;

// Enumerate every element across RootPanel, Side panels, FloatingWindows, and Hidden
var documents = layout.Descendents()
    .OfType<LayoutDocument>()
    .ToList();

// Locate a specific anchorable (whether docked, auto-hidden, floating, or hidden)
var explorer = layout.Descendents()
    .OfType<LayoutAnchorable>()
    .FirstOrDefault(a => a.ContentId == "explorer");

// Check which side a docked pane sits on
var explorerPane = explorer?.FindParent<LayoutAnchorablePane>();
if (explorerPane != null && !explorerPane.IsHostedInFloatingWindow)
{
    AnchorSide side = explorerPane.GetSide(); // Left, Right, Top, or Bottom
}
```

{: .tip }
Because `LayoutRoot.Children` yields `RootPanel`, `FloatingWindows`, `TopSide`, `RightSide`, `BottomSide`, `LeftSide`, and `Hidden`, calling `layout.Descendents()` finds content items regardless of whether they are docked, auto-hidden, floating, or hidden.

---

## Building a Layout Programmatically

You can construct a `LayoutRoot` tree in C# and assign it directly to `dockManager.Layout`:

```csharp
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Layout;

var explorerPane = new LayoutAnchorablePane
{
    Name = "LeftToolsPane",
    DockWidth = new GridLength(250, GridUnitType.Pixel),
    DockMinWidth = 140
};
explorerPane.Children.Add(new LayoutAnchorable
{
    Title = "Explorer",
    ContentId = "explorer",
    Content = new TextBlock { Text = "Workspace files" }
});

var documentPane = new LayoutDocumentPane();
documentPane.Children.Add(new LayoutDocument
{
    Title = "Welcome.md",
    ContentId = "welcome",
    Content = new TextBox { Text = "# Welcome to AvalonDock v5" }
});

var rootPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
rootPanel.Children.Add(explorerPane);
rootPanel.Children.Add(new LayoutDocumentPaneGroup(documentPane));

var layout = new LayoutRoot { RootPanel = rootPanel };

// Optionally add an auto-hidden tool window on the bottom border
var bottomGroup = new LayoutAnchorGroup();
bottomGroup.Children.Add(new LayoutAnchorable
{
    Title = "Output",
    ContentId = "output",
    AutoHideHeight = 180
});
layout.BottomSide.Children.Add(bottomGroup);

dockManager.Layout = layout;
```

### Garbage Collection (`LayoutRoot.CollectGarbage()`)

When panels are closed, floated, or hidden, empty panes and redundant nested groups can remain in the tree. `layout.CollectGarbage()` cleans up:
- Empty `LayoutAnchorablePane`, `LayoutAnchorablePaneGroup`, `LayoutPanel`, `LayoutFloatingWindow`, and `LayoutAnchorGroup` containers that are no longer referenced as a `PreviousContainer`.
- Empty floating `LayoutDocumentPane` instances—while always preserving at least one main-window `LayoutDocumentPane` so new documents have a drop target.
- Redundant single-child nested `LayoutAnchorablePaneGroup` and `LayoutDocumentPaneGroup` wrappers.

AvalonDock calls `CollectGarbage()` automatically after built-in close, float, dock, and deserialization operations; call `dockManager.Layout.CollectGarbage()` manually only if you mutate `Children` collections directly in custom layout code.

---

## `ILayoutUpdateStrategy` vs `ILayoutEngine`

AvalonDock v5 provides two distinct extension points for customizing how panels enter and rearrange the layout tree:

| Mechanism | Property on `DockingManager` | Purpose |
|:----------|:-----------------------------|:--------|
| **`ILayoutUpdateStrategy`** (`AvalonDock.Layout`) | `LayoutUpdateStrategy` (DependencyProperty) | Application-level hook invoked **before and after** inserting a `LayoutAnchorable` or `LayoutDocument` (for example, routing ViewModels from `AnchorablesSource` into named `LayoutAnchorablePane`s). When `DockLayout` is set and `LayoutUpdateStrategy` is `null`, `DockingManager` installs `DockAlignmentStrategy` automatically; `ToggleDockingManager` installs `ToggleLayoutStrategy`. |
| **`ILayoutEngine`** (`AvalonDock`) | `LayoutEngine` (`virtual` property) | Structural engine that inserts anchorable panes on a given `AnchorSide`, fixes split orientations (`Vertical` on Left/Right, `Horizontal` on Top/Bottom), and enforces corner priority (`EnsureBottomFullWidth` / `EnsureSidesFullHeight`). `DockingManager` uses `DefaultLayoutEngine`; `ToggleDockingManager` overrides `LayoutEngine` with `ToggleLayoutEngine`. |

### Custom `ILayoutUpdateStrategy` Example

When using `AnchorablesSource`, implement `ILayoutUpdateStrategy` to route tool ViewModels into named XAML panes:

```csharp
using System.Linq;
using AvalonDock.Layout;

public sealed class NamedPaneUpdateStrategy : ILayoutUpdateStrategy
{
    public bool BeforeInsertAnchorable(
        LayoutRoot layout,
        LayoutAnchorable anchorableToShow,
        ILayoutContainer destinationContainer)
    {
        if (anchorableToShow.Content is IMyTool tool)
        {
            var targetPane = layout.Descendents()
                .OfType<LayoutAnchorablePane>()
                .FirstOrDefault(p => p.Name == tool.TargetPaneName);

            if (targetPane != null)
            {
                targetPane.Children.Add(anchorableToShow);
                return true; // Handled — skip default placement
            }
        }

        return false; // Fall back to default placement
    }

    public void AfterInsertAnchorable(LayoutRoot layout, LayoutAnchorable anchorableShown) { }

    public bool BeforeInsertDocument(
        LayoutRoot layout,
        LayoutDocument documentToShow,
        ILayoutContainer destinationContainer) => false;

    public void AfterInsertDocument(LayoutRoot layout, LayoutDocument documentShown) { }
}
```

---

## Related Documentation

- [Documents & Anchorables]({{ site.baseurl }}{% link concepts/documents-and-anchorables.md %}) — Detailed comparison of `LayoutDocument` and `LayoutAnchorable`.
- [Layout Classes API Reference]({{ site.baseurl }}{% link api-reference/layout-classes.md %}) — Complete class hierarchy, properties, methods, and interfaces in `AvalonDock.Layout`.
- [Layout Serialization Guide]({{ site.baseurl }}{% link guides/serialization.md %}) — How `LayoutRoot` maps to and from `LayoutRootDto` for XML and JSON persistence.
