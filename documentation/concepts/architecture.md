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
│  │ Controls & Managers│ │ Themes (3 pkgs)│ │ Serializer (1 pkg)│  │
│  │ (DockingManager,   │ │ (Type1Legacy,  │ │ (Serializer.Xml,  │  │
│  │  ToggleDockingMgr, │ │  Type2Hybrid,  │ │  netstandard2.0)  │  │
│  │  LayoutSyncBridge) │ │  Type3New)     │ │                   │  │
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

## Package Architecture (`NTLDR2 Fork v5.1.0`)

**[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0` fork)** ships as **9 focused libraries** with strict dependency boundaries:

{: .note }
**Fork vs. Upstream `v5.0.0` Scope:** Upstream [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) `v5.0.0` introduced this layered architecture across 14 packages. `NTLDR2 Fork v5.1.0` omits `Serializer.Json` and `Themes.Arc`, consolidates the remaining themes into 3 category packages (`Themes.{Type1Legacy,Type2Hybrid,Type3New}`) refactored to zero `System.Text.Json` dependencies, and maps XAML to `https://github.com/NTLDR2/AvalonDock` so all 9 libraries deploy cleanly on `.NET Framework 4.8` without binding-redirect conflicts.

### Core & WPF Host Packages

| Package | Target Frameworks | Purpose |
|:--------|:------------------|:--------|
| `Dirkster.AvalonDock.Core` | `netstandard2.0` | UI-agnostic interfaces (`IDockable`, `IDock`, `IRootDock`, `IToolbox`, `IDockLayoutService`, `IDockingManager`, `IFactory`, `ILayoutSerializer`), enums, `FactoryBase`, and serialization DTOs. Zero external or WPF dependencies. |
| `Dirkster.AvalonDock` | `net48` | Main WPF docking library containing `DockingManager`, `ToggleDockingManager`, `LayoutSyncBridge`, all layout model classes, WPF controls, `DictionaryTheme`, and `GenericTheme`. Depends only on `AvalonDock.Core`. |

### MVVM, DI & Serializer Extension Packages

All four extension packages target **`netstandard2.0`**, allowing your ViewModel, application service, and persistence layers to remain completely decoupled from WPF:

| Package | Target Frameworks | Purpose |
|:--------|:------------------|:--------|
| `Dirkster.AvalonDock.Mvvm` | `netstandard2.0` | Zero-dependency MVVM base classes (`DockableBase`, `ToolboxBase`, `DockBase`, `RootDock`, `DocumentDock`, `ToolDock`, `Document`, `Tool`), `DockLayoutService`, `SideToggleManager`, and `Factory`. |
| `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `netstandard2.0` | `CommunityToolkit.Mvvm` `ObservableObject`-backed equivalents (`ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDockBase`, `ObservableDocument`, `ObservableTool`) supporting `[ObservableProperty]` and `[RelayCommand]` source generators. |
| `Dirkster.AvalonDock.DependencyInjection` | `netstandard2.0` | `Microsoft.Extensions.DependencyInjection` extensions (`AddDockLayoutService`, `DockLayoutBuilder`, `DockingOptions`, `ToggleDockOptions`, `AddAvalonDock<TFactory>`). |
| `Dirkster.AvalonDock.Serializer.Xml` | `netstandard2.0` | Zero-dependency XML layout persistence via `XmlLayoutSerializer` (reads both v5 DTO XML and legacy v4 XML layouts). |

### Theme Category Packages

Each of the **3 theme category packages** targets `net48` exclusively with zero third-party dependencies, matching its CLR namespace and direct base class:

| Package | CLR Namespace & Theme Classes |
|:--------|:------------------------------|
| `Dirkster.AvalonDock.Themes.Type3New` | `AvalonDock.Themes.Type3New` — `VS2026{Dark,Light,Blue}Theme`, `VS2022{Dark,Light,Blue}Theme`, `VS2015{Dark,Light,Blue}Theme`, plus `VsTheme` (`.vstheme` / `.vstheme.gz` / zero-dependency VS2026 JSON) |
| `Dirkster.AvalonDock.Themes.Type2Hybrid` | `AvalonDock.Themes.Type2Hybrid` — `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` (`DictionaryTheme`) |
| `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` — `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme` (`Theme` URI; paired with core `GenericTheme`) |

---

## Key Design Principles

### 1. Model-First Layout Tree

AvalonDock uses a **model-first** design: the docking workspace is represented as an observable tree of layout objects (`LayoutRoot`, `LayoutPanel`, `LayoutDocumentPane`, `LayoutAnchorablePane`, `LayoutDocument`, `LayoutAnchorable`). WPF visual controls (`LayoutPanelControl`, `LayoutDocumentPaneControl`, etc.) are generated from—and kept synchronized with—this model. This enables:

- Programmatic layout inspection and restructuring
- DTO-based serialization decoupled from live WPF visual trees
- Clean separation between content (views/view models) and layout chrome

### 2. Two-Way MVVM Synchronization (`LayoutSyncBridge`)

When you bind `DockingManager.DockLayout` (or `ToggleDockingManager.DockLayout`) to an `IRootDock` tree built by `IDockLayoutService` (or a custom `IFactory`):

- `LayoutSyncBridge` walks the `IRootDock` hierarchy (`IDocumentDock`, `IToolDock`), records each tool's target side from `IToolDock.Alignment` *before* inserting it, and synchronizes `VisibleDockables`, `ActiveDockable`, `AllowFloatingWindows`, and `AllowDetachedWindows` with the WPF manager.
- On the classic `DockingManager`, `DockAlignmentStrategy` places tool windows into an existing visible docked pane on their assigned side (`Left`, `Right`, `Top`, `Bottom`), or into that side's auto-hide `LayoutAnchorGroup` when no docked pane exists on that side.
- On `ToggleDockingManager`, registered `IToolbox` instances are automatically wired to the six sidebar `DockZone` button bars (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`).

### 3. Pluggable Layout Engine & DTO Serialization

- **`ILayoutEngine`:** Layout tree restructuring is abstracted behind `ILayoutEngine` (`DefaultLayoutEngine` for classic Visual Studio docking; `ToggleLayoutEngine` for VS Code / Rider-style `DockLayoutPriority` restructuring).
- **DTO Serialization (`LayoutSerializerBase`):** Layout persistence maps `ISerializableLayoutRoot` ↔ plain DTOs (`LayoutRootDto`, `LayoutPanelDto`, `LayoutDocumentDto`, `LayoutAnchorableDto`, etc.) via `ILayoutDtoMapper`, allowing `XmlLayoutSerializer` and any custom `LayoutSerializerBase` subclass to share identical content-reconnection and fixup logic.

### 4. Runtime-Switchable Theme Isolation

Themes supply XAML `ResourceDictionary` styles and brushes (or dynamically constructed `DictionaryTheme` dictionaries). Setting `DockingManager.Theme` at design time or runtime swaps all docking chrome, tab strips, floating windows, detached windows, and `ToggleDockingManager` sidebars immediately without restarting the application.

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
        <avalonDock:VS2026DarkTheme />
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
dockManager.Theme = new VS2026LightTheme();

// Query layout elements using AvalonDock.Layout.Extensions
var explorer = layout.Descendents()
    .OfType<LayoutAnchorable>()
    .FirstOrDefault(a => a.ContentId == "explorer");
```

See [Core, MVVM & DI Reference]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}) for complete interface and class specifications across the `netstandard2.0` layers.
