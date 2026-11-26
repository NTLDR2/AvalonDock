---
title: Architecture
layout: default
parent: Concepts
nav_order: 1
description: "High-level layered and package architecture of AvalonDock v5."
---

# Architecture

AvalonDock v5 is built on a modular, layered architecture that cleanly separates UI-agnostic contracts and view models (`netstandard2.0`) from WPF visual controls, themes, and persistence formats.

---

## Component Layers

```text
┌───────────────────────────────────────────────────────────────────┐
│                         Your Application                          │
├───────────────────────────────────────────────────────────────────┤
│      AvalonDock.Mvvm.CommunityToolkit  (optional, netstandard2.0) │
│      (ObservableObject-based dockables & toolboxes)               │
├───────────────────────────────────┬───────────────────────────────┤
│  AvalonDock.Mvvm (netstandard2.0) │ AvalonDock.DependencyInjection│
│  (DockableBase, ToolboxBase,      │ (IServiceCollection &         │
│   DockLayoutService, Factory)     │  DockLayoutBuilder, netstd2.0)│
├───────────────────────────────────┴───────────────────────────────┤
│                     AvalonDock  (WPF UI)                          │
│  ┌────────────────────┐ ┌────────────────┐ ┌───────────────────┐  │
│  │ Controls & Managers│ │ Themes (7 pkgs)│ │ Serializers (2)   │  │
│  │ (DockingManager,   │ │ (Arc, VS,      │ │ (Serializer.Xml,  │  │
│  │  ToggleDockingMgr, │ │  VS2013/2010,  │ │  Serializer.Json, │  │
│  │  LayoutSyncBridge) │ │  Expr/Metro/   │ │  netstandard2.0)  │  │
│  │                    │ │  Aero)         │ │                   │  │
│  └─────────┬──────────┘ └───────┬────────┘ └─────────┬─────────┘  │
│            │                    │                    │            │
│  ┌─────────┴────────────────────┴────────────────────┴─────────┐  │
│  │                       WPF Layout Model                      │  │
│  │      (LayoutRoot, LayoutPanel, LayoutDocument, etc.)        │  │
│  └──────────────────────────────┬──────────────────────────────┘  │
├─────────────────────────────────┼─────────────────────────────────┤
│                 AvalonDock.Core (netstandard2.0)                  │
│   (Interfaces, Enums, FactoryBase, LayoutSerializerBase & DTOs)   │
└───────────────────────────────────────────────────────────────────┘
```

---

## Package Architecture

AvalonDock v5 ships as **14 focused NuGet packages** with strict dependency boundaries:

### Core & WPF Host Packages

| Package | Target Frameworks | Purpose |
|:--------|:------------------|:--------|
| `Dirkster.AvalonDock.Core` | `netstandard2.0` | UI-agnostic interfaces (`IDockable`, `IDock`, `IRootDock`, `IToolbox`, `IDockLayoutService`, `IDockingManager`, `IFactory`, `ILayoutSerializer`), enums, `FactoryBase`, and serialization DTOs. Zero external or WPF dependencies. |
| `Dirkster.AvalonDock` | `net8.0-windows`–`net11.0-windows`, `net48` | Main WPF docking library containing `DockingManager`, `ToggleDockingManager`, `LayoutSyncBridge`, all layout model classes, WPF controls, and `GenericTheme`. Depends on `AvalonDock.Core`. |

### MVVM, DI & Serializer Extension Packages

All five extension packages target **`netstandard2.0`**, allowing your ViewModel, application service, and persistence layers to remain completely decoupled from WPF:

| Package | Target Frameworks | Purpose |
|:--------|:------------------|:--------|
| `Dirkster.AvalonDock.Mvvm` | `netstandard2.0` | Zero-dependency MVVM base classes (`DockableBase`, `ToolboxBase`, `DockBase`, `RootDock`, `DocumentDock`, `ToolDock`, `Document`, `Tool`), `DockLayoutService`, `SideToggleManager`, and `Factory`. |
| `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `netstandard2.0` | `CommunityToolkit.Mvvm` `ObservableObject`-backed equivalents (`ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDockBase`, `ObservableDocument`, `ObservableTool`) supporting `[ObservableProperty]` and `[RelayCommand]` source generators. |
| `Dirkster.AvalonDock.DependencyInjection` | `netstandard2.0` | `Microsoft.Extensions.DependencyInjection` extensions (`AddDockLayoutService`, `DockLayoutBuilder`, `DockingOptions`, `ToggleDockOptions`, `AddAvalonDock<TFactory>`). |
| `Dirkster.AvalonDock.Serializer.Xml` | `netstandard2.0` | XML layout persistence via `XmlLayoutSerializer` (reads both v5 DTO XML and legacy v4 XML layouts). |
| `Dirkster.AvalonDock.Serializer.Json` | `netstandard2.0` | `System.Text.Json` layout persistence via `JsonLayoutSerializer`. |

### Theme Packages

Each of the **7 theme packages** targets `net8.0-windows`–`net11.0-windows` and `net48`, so you only ship the visual themes your application uses:

| Package | Theme Classes & Capabilities |
|:--------|:-----------------------------|
| `Dirkster.AvalonDock.Themes.Arc` | `ArcDarkTheme`, `ArcLightTheme` — modern compact IDE aesthetic with rounded corners |
| `Dirkster.AvalonDock.Themes.VS` | `VS2015*`, `VS2022*`, and `VS2026*` (`Dark`, `Light`, `Blue`), plus `VsTheme` for loading custom `.vstheme`, `.vstheme.gz`, and VS2026 JSON override files |
| `Dirkster.AvalonDock.Themes.VS2013` | `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` (backed by embedded `.vstheme.gz` palettes via `Themes.VS`) |
| `Dirkster.AvalonDock.Themes.VS2010` | `VS2010Theme` |
| `Dirkster.AvalonDock.Themes.Expression` | `ExpressionDarkTheme`, `ExpressionLightTheme` |
| `Dirkster.AvalonDock.Themes.Metro` | `MetroTheme` |
| `Dirkster.AvalonDock.Themes.Aero` | `AeroTheme` |

---

## Key Design Principles

### 1. Model-First Layout Tree

AvalonDock uses a **model-first** design: the docking workspace is represented as an observable tree of layout objects (`LayoutRoot`, `LayoutPanel`, `LayoutDocumentPane`, `LayoutAnchorablePane`, `LayoutDocument`, `LayoutAnchorable`). WPF visual controls (`LayoutPanelControl`, `LayoutDocumentPaneControl`, etc.) are generated from—and kept synchronized with—this model. This enables:

- Programmatic layout inspection and restructuring
- DTO-based XML and JSON serialization decoupled from live WPF visual trees
- Clean separation between content (views/view models) and layout chrome

### 2. Two-Way MVVM Synchronization (`LayoutSyncBridge`)

When you bind `DockingManager.DockLayout` (or `ToggleDockingManager.DockLayout`) to an `IRootDock` tree built by `IDockLayoutService` (or a custom `IFactory`):

- `LayoutSyncBridge` walks the `IRootDock` hierarchy (`IDocumentDock`, `IToolDock`), records each tool's target side from `IToolDock.Alignment` *before* inserting it, and synchronizes `VisibleDockables`, `ActiveDockable`, `AllowFloatingWindows`, and `AllowDetachedWindows` with the WPF manager.
- On the classic `DockingManager`, `DockAlignmentStrategy` places tool windows into an existing visible docked pane on their assigned side (`Left`, `Right`, `Top`, `Bottom`), or into that side's auto-hide `LayoutAnchorGroup` when no docked pane exists on that side.
- On `ToggleDockingManager`, registered `IToolbox` instances are automatically wired to the six sidebar `DockZone` button bars (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`).

### 3. Pluggable Layout Engine & DTO Serialization

- **`ILayoutEngine`:** Layout tree restructuring is abstracted behind `ILayoutEngine` (`DefaultLayoutEngine` for classic Visual Studio docking; `ToggleLayoutEngine` for VS Code / Rider-style `DockLayoutPriority` restructuring).
- **DTO Serialization (`LayoutSerializerBase`):** Layout persistence maps `ISerializableLayoutRoot` ↔ plain DTOs (`LayoutRootDto`, `LayoutPanelDto`, `LayoutDocumentDto`, `LayoutAnchorableDto`, etc.) via `ILayoutDtoMapper`, allowing any format (`XmlLayoutSerializer`, `JsonLayoutSerializer`, or custom serializers) to share identical content-reconnection and fixup logic.

### 4. Runtime-Switchable Theme Isolation

Themes supply XAML `ResourceDictionary` styles and brushes (or dynamically generated `.vstheme` / JSON dictionaries in `Themes.VS` and `Themes.VS2013`). Setting `DockingManager.Theme` at design time or runtime swaps all docking chrome, tab strips, floating windows, and `ToggleDockingManager` sidebars immediately without restarting the application.

### 5. Event-Driven Lifecycle Control

`DockingManager` (and `IDockingManager` in `AvalonDock.Core`) exposes lifecycle events—many of them cancellable—for every user or programmatic operation:

- **Documents:** `DocumentClosing` *(cancellable)*, `DocumentClosed`
- **Anchorables:** `AnchorableClosing` *(cancellable)*, `AnchorableClosed`, `AnchorableHiding` *(cancellable)*, `AnchorableHidden`
- **Floating & Docking:** `ContentFloating` *(cancellable)*, `ContentFloated`, `ContentDocking` *(cancellable)*, `ContentDocked`, `LayoutFloatingWindowControlCreated`, `LayoutFloatingWindowControlClosed`
- **Workspace State:** `ActiveContentChanged`, `LayoutChanging`, `LayoutChanged`

---

## Choosing an Entry-Point Control

AvalonDock provides two root controls in `Dirkster.AvalonDock`:

| Control | Style | Best For |
|:--------|:------|:---------|
| [`DockingManager`]({{ site.baseurl }}{% link api-reference/docking-manager.md %}) | Classic Visual Studio | Traditional auto-hide slide-out flyouts, 4 anchor sides, XAML-defined layouts, or `DocumentsSource`/`AnchorablesSource`/`DockLayout` bindings. |
| [`ToggleDockingManager`]({{ site.baseurl }}{% link api-reference/toggle-docking-manager.md %}) | VS Code / JetBrains Rider | Compact 6-zone sidebar toggle bars, keyboard shortcut badges, `DockLayoutPriority` (`BottomFullWidth` / `SidesFullHeight`), and DI-driven `IToolbox` discovery. |

```xml
<!-- Classic DockingManager -->
<avalonDock:DockingManager x:Name="dockManager">
    <avalonDock:DockingManager.Theme>
        <themes:ArcDarkTheme />
    </avalonDock:DockingManager.Theme>

    <avalonDock:LayoutRoot>
        <!-- Layout tree here -->
    </avalonDock:LayoutRoot>
</avalonDock:DockingManager>
```

```csharp
// Access the layout tree in code
LayoutRoot layout = dockManager.Layout;

// Switch themes dynamically at runtime
dockManager.Theme = new VS2026DarkTheme();

// Query layout elements using AvalonDock.Layout.Extensions
var explorer = layout.Descendents()
    .OfType<LayoutAnchorable>()
    .FirstOrDefault(a => a.ContentId == "explorer");
```

See [Core, MVVM & DI Reference]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}) for complete interface and class specifications across the `netstandard2.0` layers.
