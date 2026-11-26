# AGENTS.md — AvalonDock v5 Guide for Humans & AI Coding Agents

This document is the authoritative technical orientation for **AvalonDock v5** (`v5.1.0` on `master`, NuGet prefix `Dirkster.AvalonDock*`). It is designed so both human developers and AI coding agents / LLMs can quickly grasp **what AvalonDock v5 is**, **how v5 differs from v4** (avoiding stale pre-trained v4 patterns), and **how AvalonDock v5 works from the inside out**.

> [!IMPORTANT]
> **Documentation Source of Truth:**
> - **READ:** `new-docs-carbon4/` — Fresh, complete, up-to-date AvalonDock v5 documentation (40 pages covering concepts, guides, tutorials, themes, migration, and API reference).
> - **IGNORE:** `docs/` and `AGENTS.md.old` — Legacy/outdated files kept temporarily during migration. Never copy namespaces, package names, or XAML pack URIs from `docs/`.

---

## Table of Contents

1. [What Is AvalonDock v5?](#1-what-is-avalondock-v5)
2. [v4 → v5 Differences & LLM Anti-Hallucination Guardrails](#2-v4--v5-differences--llm-anti-hallucination-guardrails)
3. [Package & Layer Architecture (14 NuGet Packages)](#3-package--layer-architecture-14-nuget-packages)
4. [How AvalonDock v5 Works from the Inside Out](#4-how-avalondock-v5-works-from-the-inside-out)
   - [4.1 The Three Parallel Trees: MVVM `IRootDock` ↔ Logical `LayoutRoot` ↔ Visual `Control`s](#41-the-three-parallel-trees-mvvm-irootdock--logical-layoutroot--visual-controls)
   - [4.2 `LayoutItem` Wrappers, Single-Presenter Rule & Logical-Tree Depth Ordering](#42-layoutitem-wrappers-single-presenter-rule--logical-tree-depth-ordering)
   - [4.3 Dynamic Grid & Splitter Mechanics (`LayoutGridControl<T>`) & Tab Virtualization (`TabControlEx`)](#43-dynamic-grid--splitter-mechanics-layoutgridcontrolt--tab-virtualization-tabcontrolex)
   - [4.4 MVVM Synchronization & Placement Pipeline (`LayoutSyncBridge`, `ILayoutUpdateStrategy`, `ILayoutEngine`)](#44-mvvm-synchronization--placement-pipeline-layoutsyncbridge-ilayoutupdatestrategy-ilayoutengine)
   - [4.5 `ToggleDockingManager` Internals (6-Zone Sidebars, Exclusive Bars, `IToolbox` Sync & Shortcuts)](#45-toggledockingmanager-internals-6-zone-sidebars-exclusive-bars-itoolbox-sync--shortcuts)
   - [4.6 Windowing Internals: Floating Windows vs Standalone `DetachedAnchorableWindow`](#46-windowing-internals-floating-windows-vs-standalone-detachedanchorablewindow)
   - [4.7 Interactive Drag-and-Drop, `OverlayWindow` Reuse & Focus / `HwndHost` Airspace](#47-interactive-drag-and-drop-overlaywindow-reuse--focus--hwndhost-airspace)
   - [4.8 DTO Serialization & Deserialization Fixup Pipeline](#48-dto-serialization--deserialization-fixup-pipeline)
   - [4.9 Theme Engine Internals (`Theme` vs `DictionaryTheme`, `.vstheme` & VS2026 JSON)](#49-theme-engine-internals-theme-vs-dictionarytheme-vstheme--vs2026-json)
5. [Build, Test & Verification Commands](#5-build-test--verification-commands)
6. [Testing Architecture & Critical STA / FlaUI Rules](#6-testing-architecture--critical-sta--flaui-rules)
7. [Code Style, Static Analysis & Commit Conventions](#7-code-style-static-analysis--commit-conventions)
8. [Repository & Documentation Lookup Map](#8-repository--documentation-lookup-map)
9. [Vibecoding Pitfalls Checklist](#9-vibecoding-pitfalls-checklist)

---

## 1. What Is AvalonDock v5?

AvalonDock is a WPF document and tool-window docking layout system (`https://github.com/Dirkster99/AvalonDock` XAML namespace, licensed under Ms-PL) used to build Visual Studio, VS Code, and JetBrains Rider-style desktop IDEs and tools.

**AvalonDock v5** modernizes the classic WPF framework around six pillars:

1. **Strict UI-Agnostic / WPF Separation:** `Dirkster.AvalonDock.Core`, `Mvvm`, `Mvvm.CommunityToolkit`, `DependencyInjection`, `Serializer.Xml`, and `Serializer.Json` all target **`netstandard2.0`** with **zero WPF dependencies**, so ViewModel, application service, and persistence projects remain platform-clean while WPF host projects target `net8.0-windows`, `net9.0-windows`, `net10.0-windows`, `net11.0-windows`, or `net48`.
2. **Two Root Docking Controls:**
   - **`DockingManager`** — Classic Visual Studio-style docking with 4 auto-hide slide-out flyout edges (`TopSide`, `BottomSide`, `LeftSide`, `RightSide`), tabbed documents, split panes, XAML-first layouts, `DocumentsSource`/`AnchorablesSource`, and `DockLayout` MVVM binding.
   - **`ToggleDockingManager`** *(new in v5, subclasses `DockingManager`)* — VS Code / JetBrains Rider-style docking with **6 click-to-toggle sidebar `DockZone`s** (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`), `DockLayoutPriority` (`BottomFullWidth` vs `SidesFullHeight`), `IToolbox.Shortcut` keybindings, and a 6-zone visual drag overlay.
3. **First-Class MVVM & Dependency Injection:** Zero-dependency base classes (`AvalonDock.Mvvm`), `CommunityToolkit.Mvvm` source-generator-ready base classes (`AvalonDock.Mvvm.CommunityToolkit`), `IDockLayoutService` + `SideToggleManager`, and `services.AddDockLayoutService(dock => ...)` builder registration (`AvalonDock.DependencyInjection`).
4. **DTO-Based XML & JSON Layout Persistence:** Layouts map cleanly between `LayoutRoot` and 17 plain DTOs (`AvalonDock.Core.Serialization.Dto`), supporting `XmlLayoutSerializer` (backward-compatible with v4 XML layout files), `JsonLayoutSerializer` (`System.Text.Json`), and custom `LayoutSerializerBase` implementations.
5. **Dual Multi-Window Modes & Global Policy Switches:** Owned borderless tear-off floating windows (`LayoutFloatingWindowControl`, controlled globally by `AllowFloatingWindows`) **plus** independent top-level OS tool windows with taskbar entries (`DetachedAnchorableWindow` / **"Window"** view mode, controlled globally by `AllowDetachedWindows`).
6. **Modernized Theme Engine (7 Theme Packages + `GenericTheme`):** Adds `Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`) and `Themes.VS` (`VS2015*`, `VS2022*`, `VS2026*`, plus custom `.vstheme`, `.vstheme.gz`, and VS2026 JSON color overrides). Every theme styles both `DockingManager` and `ToggleDockingManager` and switches live at runtime.

---

## 2. v4 → v5 Differences & LLM Anti-Hallucination Guardrails

AI models trained on older repositories frequently emit **AvalonDock v4** code that fails to compile or misbehaves in v5. Always apply the **v5** patterns below:

| Area | Legacy v4 Pattern (DO NOT USE) | Modern v5 Pattern (USE THIS) |
|:-----|:-------------------------------|:-----------------------------|
| **XML Serializer Package & Namespace** | Bundled in `Dirkster.AvalonDock` under `using AvalonDock.Layout.Serialization;` | Extracted to **`Dirkster.AvalonDock.Serializer.Xml`** under `using AvalonDock.Serializer.Xml;` + `using AvalonDock.Core;` |
| **JSON Serialization** | Not available | Install **`Dirkster.AvalonDock.Serializer.Json`** and use `AvalonDock.Serializer.Json.JsonLayoutSerializer` |
| **Custom Serialization Hooks** | `IXmlSerializable` (`ReadXml` / `WriteXml` on `LayoutRoot`, `LayoutContent`, etc.) | **`IXmlSerializable` is removed** from all layout classes. Subclass `AvalonDock.Core.LayoutSerializerBase` (`SerializeCore` / `DeserializeCore` over `LayoutRootDto`) |
| **Serialization Callback Model Type** | `args.Model` typed as WPF `LayoutContent` | `args.Model` typed as UI-agnostic `ISerializableLayoutContent` (`AvalonDock.Core.Serialization`) |
| **Unresolved Deserialized Anchorables** | Left indefinitely in `LayoutRoot.Hidden` | **Removed by default** (`UnresolvedContentHandling.Remove` calls `RemoveFromLayout()`). Opt into `UnresolvedContentHandling.Hide` globally or per-item on `args` for lazy-loaded plugins |
| **Serialized `ToolTip`** | Written as an XML attribute | **Not serialized** in v5 (ignored when loading v4 XML). Bind `ToolTip` in XAML styles, templates, or ViewModels |
| **VS2013 Custom XAML Dictionary Merges** | Merging `/AvalonDock.Themes.VS2013;component/DarkBrushs.xaml` or `DarkTheme.xaml` | **Removed (`IOException` at runtime)** — `Themes.VS2013` now builds dictionaries from embedded `.vstheme.gz` via `Themes.VS`. Base XAML custom themes on `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` (note spelling: `DarkBrushs.xaml` without `e`) or use `VsTheme` / `DictionaryTheme` |
| **Theme Namespaces & Casing** | Assuming all themes live in `AvalonDock.Themes` with uniform casing | `Themes.VS` classes live in **`AvalonDock.Themes.VS`** (`VS2026DarkTheme`, `VS2022DarkTheme`, `VS2015DarkTheme`, `VsTheme`). All other theme packages use **`AvalonDock.Themes`**. Note `Vs2013DarkTheme` (lowercase `s`) vs `VS2010Theme` / `VS2022DarkTheme` (uppercase `VS`) |
| **MVVM Identifier Property Name** | Ad-hoc `ContentId` property on custom VMs | `IDockable` / `DockableBase` / `ToolboxBase` / `ObservableDockableBase` name their identifier **`Id`**, while WPF `LayoutItem` names its DP **`ContentId`**. In `LayoutItemContainerStyle`, always bind `<Setter Property="ContentId" Value="{Binding Model.Id}" />` |
| **DI Registration** | Early preview `services.AddToolbox<T>()` / `AddToggleDockOptions(...)` | Consolidated builder: `services.AddDockLayoutService(dock => { dock.ConfigureToggleDock(...); dock.ConfigureDocking(...); dock.AddToolbox<T>(); })` |
| **Document Body Template Property** | Inventing `DockingManager.DocumentTemplate` (does not exist) | Use `DockingManager.LayoutItemTemplate`, `LayoutItemTemplateSelector`, or implicit `<DataTemplate DataType="{x:Type ...}">` resources |
| **Adding vs Showing Anchorables** | Calling `Show()` on a brand-new unparented `LayoutAnchorable` | `anchorable.AddToLayout(dockManager, strategy)` inserts a **new unparented** anchorable (`Parent == null`); `anchorable.Show()` restores a **hidden** anchorable (`IsHidden == true` in `Layout.Hidden`) |
| **Target Frameworks** | `net40`, `net452`, `netcoreapp3.1`, `net5.0-windows` | `net8.0-windows`, `net9.0-windows`, `net10.0-windows`, `net11.0-windows`, `net48` (WPF packages); `netstandard2.0` (Core/MVVM/DI/Serializers) |
| **Source Binding Suspension** | `SuspendDocumentsSourceBinding` / `SuspendAnchorablesSourceBinding` as fields | Now public `bool` **properties** (`{ get; set; }`) on `DockingManager` and `IDockingManager` |

---

## 3. Package & Layer Architecture (14 NuGet Packages)

```text
┌─────────────────────────────────────────────────────────────────────────┐
│                            Host Application                             │
├─────────────────────────────────────────────────────────────────────────┤
│     AvalonDock.Mvvm.CommunityToolkit (optional, netstandard2.0)         │
│     (ObservableDockableBase, ObservableToolboxBase, ObservableDocument) │
├────────────────────────────────────┬────────────────────────────────────┤
│   AvalonDock.Mvvm (netstandard2.0) │ AvalonDock.DependencyInjection     │
│   (DockableBase, ToolboxBase,      │ (AddDockLayoutService,             │
│    DockLayoutService, SideToggle,  │  DockLayoutBuilder, DockingOptions,│
│    RootDock/ToolDock/DocumentDock) │  ToggleDockOptions, netstandard2.0)│
├────────────────────────────────────┴────────────────────────────────────┤
│                         AvalonDock (WPF UI)                             │
│  ┌──────────────────────┐ ┌──────────────────┐ ┌─────────────────────┐  │
│  │ Managers & Engines   │ │ Themes (7 pkgs)  │ │ Serializers (2 pkgs)│  │
│  │ • DockingManager     │ │ • Arc            │ │ • Serializer.Xml    │  │
│  │ • ToggleDockingMgr   │ │ • VS (.vstheme / │ │ • Serializer.Json   │  │
│  │ • LayoutSyncBridge   │ │   VS2026 JSON)   │ │   (netstandard2.0)  │  │
│  │ • Default/Toggle     │ │ • VS2013, VS2010 │ │                     │  │
│  │   LayoutEngine       │ │ • Expression,    │ │                     │  │
│  │ • LayoutDtoMapper    │ │   Metro, Aero    │ │                     │  │
│  └──────────┬───────────┘ └────────┬─────────┘ └──────────┬──────────┘  │
│             └──────────────────────┼──────────────────────┘             │
│                                    ▼                                    │
│             WPF Layout Model (AvalonDock.Layout.*) & Controls           │
├─────────────────────────────────────────────────────────────────────────┤
│                    AvalonDock.Core (netstandard2.0)                     │
│  (IDockable, IRootDock, IToolbox, IDockLayoutService, IDockingManager,  │
│   IFactory, FactoryBase, ILayoutSerializer, LayoutSerializerBase, DTOs) │
└─────────────────────────────────────────────────────────────────────────┘
```

| Project (`source/Components/…`) | NuGet Package | Target Frameworks | Primary Namespace(s) |
|:--------------------------------|:--------------|:------------------|:---------------------|
| `AvalonDock.Core` | `Dirkster.AvalonDock.Core` | `netstandard2.0` | `AvalonDock.Core`, `AvalonDock.Core.Events`, `AvalonDock.Core.Serialization`, `AvalonDock.Core.Serialization.Dto`, `AvalonDock.Controls` *(pure geometry rules)* |
| `AvalonDock` | `Dirkster.AvalonDock` | `net8/9/10/11-windows;net48` | `AvalonDock`, `AvalonDock.Layout`, `AvalonDock.Controls`, `AvalonDock.Themes`, `AvalonDock.Converters`, `AvalonDock.Commands`, `AvalonDock.Serialization` |
| `AvalonDock.Mvvm` | `Dirkster.AvalonDock.Mvvm` | `netstandard2.0` | `AvalonDock.Mvvm` |
| `AvalonDock.Mvvm.CommunityToolkit` | `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `netstandard2.0` | `AvalonDock.Mvvm.CommunityToolkit` |
| `AvalonDock.DependencyInjection` | `Dirkster.AvalonDock.DependencyInjection` | `netstandard2.0` | `AvalonDock.DependencyInjection` |
| `AvalonDock.Serializer.Xml` | `Dirkster.AvalonDock.Serializer.Xml` | `netstandard2.0` | `AvalonDock.Serializer.Xml` |
| `AvalonDock.Serializer.Json` | `Dirkster.AvalonDock.Serializer.Json` | `netstandard2.0` | `AvalonDock.Serializer.Json` |
| `AvalonDock.Themes.Arc` | `Dirkster.AvalonDock.Themes.Arc` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes`, `AvalonDock.Themes.Arc.Themes` |
| `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | `net8/9/10/11-windows;net48` | **`AvalonDock.Themes.VS`**, `AvalonDock.Themes.VS.Themes` |
| `AvalonDock.Themes.VS2013` | `Dirkster.AvalonDock.Themes.VS2013` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes`, `AvalonDock.Themes.VS2013.Themes` *(depends on `Themes.VS`)* |
| `AvalonDock.Themes.VS2010` | `Dirkster.AvalonDock.Themes.VS2010` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Expression` | `Dirkster.AvalonDock.Themes.Expression` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Metro` | `Dirkster.AvalonDock.Themes.Metro` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Aero` | `Dirkster.AvalonDock.Themes.Aero` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |

---

## 4. How AvalonDock v5 Works from the Inside Out

Understanding these internal mechanisms prevents subtle bugs in layout mutation, focus tracking, MVVM bindings, window ownership, and serialization.

### 4.1 The Three Parallel Trees: MVVM `IRootDock` ↔ Logical `LayoutRoot` ↔ Visual `Control`s

AvalonDock is **model-first**. Visual WPF controls never own layout structure; instead, they observe and project the `LayoutRoot` model tree:

1. **Optional UI-Agnostic MVVM Tree (`AvalonDock.Core` / `AvalonDock.Mvvm`):**
   - `IRootDock` (`RootDock`) holds `IDocumentDock` (`DocumentDock`) and `IToolDock` (`ToolDock`, aligned `Left`, `Right`, `Top`, `Bottom`), whose `VisibleDockables` contain `IDockable` / `IToolbox` ViewModels.
   - Managed by `DockLayoutService` (`IDockLayoutService`) and bridged to WPF by `LayoutSyncBridge` when `DockingManager.DockLayout` is bound.
2. **WPF Layout Model Tree (`AvalonDock.Layout`):**
   - Every node inherits from `LayoutElement : DependencyObject` (`Parent`, `Root`, `INotifyPropertyChanged`, `INotifyPropertyChanging`).
   - **Important Inheritance Detail:** `LayoutRoot` and `LayoutFloatingWindow` inherit directly from `LayoutElement` (implementing `ILayoutContainer`), **not** from `LayoutGroup<T>`.
   - `LayoutRoot` (`DockingManager.Layout`) owns **7 child branches** enumerated by `LayoutRoot.Children` (so `layout.Descendents()` visits all of them!):
     - `RootPanel` (`LayoutPanel`) — docked split hierarchy (`LayoutPanel`, `LayoutDocumentPaneGroup`, `LayoutDocumentPane`, `LayoutAnchorablePaneGroup`, `LayoutAnchorablePane`, `LayoutDocument`, `LayoutAnchorable`)
     - `TopSide`, `BottomSide`, `LeftSide`, `RightSide` (`LayoutAnchorSide` → `LayoutAnchorGroup` → auto-hidden `LayoutAnchorable`s)
     - `FloatingWindows` (`ObservableCollection<LayoutFloatingWindow>` — `LayoutDocumentFloatingWindow` holding a `LayoutDocumentPaneGroup` `RootPanel`, or `LayoutAnchorableFloatingWindow` holding a `LayoutAnchorablePaneGroup` `RootPanel`)
     - `Hidden` (`ObservableCollection<LayoutAnchorable>` — hidden tool windows where `IsHidden == true`, i.e., `Parent is LayoutRoot`)
   - **Automatic Reparenting & Change Bubbling:** Adding an element to `LayoutGroup<T>.Children` automatically detaches it from its old `Parent`, assigns `element.Parent = this`, raises `ChildrenCollectionChanged`, bubbles `ChildrenTreeChanged` (`DirectChildrenChanged` on the immediate group, `TreeChanged` on ancestors), and fires `LayoutRoot.ElementAdded` / `Updated`.
   - **Garbage Collection (`LayoutRoot.CollectGarbage()`):** Prunes empty `LayoutAnchorablePane`, `LayoutAnchorablePaneGroup`, `LayoutPanel`, `LayoutFloatingWindow`, and `LayoutAnchorGroup` nodes **only when** no surviving element's `ILayoutPreviousContainer.PreviousContainer` points to them, collapses redundant single-child nested pane groups, and **always preserves at least one main-window `LayoutDocumentPane`** so new documents always have a drop target. Built-in close/hide/float/dock/deserialize paths call `CollectGarbage()` automatically; call it manually only after direct `Children` mutations.
3. **WPF Visual Control Tree (`AvalonDock.Controls`):**
   - `DockingManager.CreateUIElementForModel(ILayoutElement)` is the factory mapping model nodes to controls (`ILayoutControl`):
     - `LayoutPanel` → `LayoutPanelControl`
     - `LayoutDocumentPaneGroup` → `LayoutDocumentPaneGroupControl`
     - `LayoutAnchorablePaneGroup` → `LayoutAnchorablePaneGroupControl`
     - `LayoutDocumentPane` → `LayoutDocumentPaneControl`
     - `LayoutAnchorablePane` → `LayoutAnchorablePaneControl`
     - `LayoutAnchorSide` / `LayoutAnchorGroup` → `LayoutAnchorSideControl` / `LayoutAnchorGroupControl` → `LayoutAnchorControl`
     - `LayoutDocumentFloatingWindow` / `LayoutAnchorableFloatingWindow` → `LayoutDocumentFloatingWindowControl` / `LayoutAnchorableFloatingWindowControl`

### 4.2 `LayoutItem` Wrappers, Single-Presenter Rule & Logical-Tree Depth Ordering

Every `LayoutDocument` and `LayoutAnchorable` in `Layout` gets a companion wrapper in `DockingManager._layoutItems` (retrieved via `dockManager.GetLayoutItemFromModel(layoutContent)`):

- **`LayoutDocumentItem : LayoutItem`** (for `LayoutDocument`) and **`LayoutAnchorableItem : LayoutItem`** (for `LayoutAnchorable`).
- **Key Properties on `LayoutItem`:**
  - `DataContext` of `LayoutItem` is **`this`** (the `LayoutItem` itself!).
  - `LayoutElement` → the `LayoutDocument` or `LayoutAnchorable`.
  - `Model` → `LayoutElement.Content` (the user's ViewModel or `UIElement`). This is why `LayoutItemContainerStyle` setters always bind `{Binding Model.Title}`, `{Binding Model.Id}`, etc.!
  - `View` → a **lazily created single `ContentPresenter`** bound to `LayoutElement.Content`, `DockingManager.LayoutItemTemplate`, and `DockingManager.LayoutItemTemplateSelector`.
- **Style Precedence Trick (`SetCurrentValue`):** When `LayoutItem.Attach(LayoutContent)` seeds `CanClose`, `CanHide`, `Title`, etc. from the model, it uses `SetCurrentValue(...)` rather than setting a local DP value. Consequently, a `<Setter Property="CanClose" Value="{Binding Model.CanClose}" />` in `LayoutItemContainerStyle` / `LayoutItemContainerStyleSelector` cleanly overrides the model default instead of losing to WPF local-value precedence.
- **Why `DockingManager.LogicalChildren` Sorts by Descending Visual Tree Depth (`GetOrderedLogicalChildren`):**
  - `LayoutItem.View` and raw `UIElement` contents are added as logical children of `DockingManager` (`InternalAddLogicalChild`), while visually residing deep inside `LayoutDocumentPaneControl` or `LayoutAnchorablePaneControl` (which are *also* reachable from `DockingManager`).
  - WPF's `DescendentsWalker` visits each node **at most once**, walking logical children before visual children. If a visual ancestor were enumerated first, `TreeWalkHelper.OnInheritablePropertyChanged` would hit the inner `View` via the visual tree first, skip it (because its visual parent != its logical parent), and then ignore it on the second logical visit—breaking `DataContext`, `FontSize`, and `FontFamily` inheritance!
  - Sorting `_logicalChildren` by **descending visual tree depth** guarantees deepest nodes are visited via their logical link first. Never bypass `InternalAddLogicalChild` / `InternalRemoveLogicalChild`.

### 4.3 Dynamic Grid & Splitter Mechanics (`LayoutGridControl<T>`) & Tab Virtualization (`TabControlEx`)

- **`LayoutGridControl<T>` (`LayoutPanelControl`, `LayoutDocumentPaneGroupControl`, `LayoutAnchorablePaneGroupControl`):**
  - Subscribes to `_model.ChildrenTreeChanged` (acting only on `ChildrenTreeChange.DirectChildrenChanged`, coalesced via `_asyncRefreshCalled` at `DispatcherPriority.Normal`).
  - `UpdateChildren()` reuses existing `ILayoutControl` visuals whose `.Model` still matches, creates visuals for new models via `CreateUIElementForModel`, interleaves `LayoutGridResizerControl` (`Thumb`) splitters between visible children, and builds `RowDefinitions` or `ColumnDefinitions` from `DockWidth` / `DockHeight` (`1*` default) and `CalculatedDockMinWidth()` / `CalculatedDockMinHeight()` (`25.0` px default minimum).
  - Dragging a `LayoutGridResizerControl` displays a lightweight ghost border (`_resizerGhost`) during drag and updates both neighbors' `DockWidth` / `DockHeight` (`Star` weights scaled proportionally, or `Pixel` / `Auto` converted to `Pixel`) on `DragCompleted`.
- **`TabControlEx` (`LayoutDocumentPaneControl` & `LayoutAnchorablePaneControl`):**
  - Standard WPF `TabControl` destroys and recreates the visual tree of tab content on every tab switch. `TabControlEx` (`IsVirtualizingDocument = true`, `IsVirtualizingAnchorable = true` by default) keeps materialized `ContentPresenter`s inside `PART_ItemsHolder` and toggles `Visibility.Visible` / `Visibility.Collapsed` so editors, scroll positions, and stateful controls survive tab switching.
  - When `DockingManager.IgnoreTabControlKeyBindings = true`, `TabControlEx.OnKeyDown` skips `base.OnKeyDown(e)` so keys like `Home`, `End`, `PageUp`, `PageDown`, and `Ctrl+Tab` are not stolen from hosted text editors or grids.

### 4.4 MVVM Synchronization & Placement Pipeline (`LayoutSyncBridge`, `ILayoutUpdateStrategy`, `ILayoutEngine`)

AvalonDock v5 supports two MVVM binding modes on `DockingManager` (and `ToggleDockingManager`):
- **Mode A (Recommended v5):** Bind `DockLayout="{Binding DockLayout}"` (`IRootDock`, typically from `IDockLayoutService.Layout`).
- **Mode B (Classic v4/v5):** Bind `DocumentsSource` and `AnchorablesSource` directly.

**What happens internally when `DockLayout` is set (`OnDockLayoutChanged`):**
1. `LayoutSyncBridge.Attach()` creates internal `_documentModels` and `_anchorableModels` `ObservableCollection<object>` instances and walks the `IRootDock` tree:
   - For every `IDocumentDock`, copies `VisibleDockables` into `_documentModels` and subscribes to `CollectionChanged`.
   - For every `IToolDock`, maps `IToolDock.Alignment` → `AnchorSide` into `_contentToSide[child] = side` **before** adding `child` to `_anchorableModels`, and subscribes to `CollectionChanged`.
   - **Critical Ordering Invariant:** When a new tool is added to `IToolDock.VisibleDockables` at runtime, `OnAnchorableCollectionChanged` **must** write `_contentToSide[item] = addSide.Value` *before* `_anchorableModels.Add(item)`, because adding to `AnchorablesSource` synchronously triggers `LayoutUpdateStrategy.BeforeInsertAnchorable`, which immediately looks up `_contentToSide`!
2. `LayoutSyncBridge` assigns `_manager.DocumentsSource = _documentModels` and `_manager.AnchorablesSource = _anchorableModels`.
3. `SyncWindowPolicyToWpf()` pushes `_rootDock.AllowFloatingWindows` and `_rootDock.AllowDetachedWindows` **one-way** onto `DockingManager`. *(Note: if you bind `DockLayout`, configure window policy on `IRootDock` or DI `DockingOptions`, because `IRootDock`'s default `true` overwrites a XAML `AllowFloatingWindows="False"` attribute!)*
4. Two-way focus & close sync (guarded by `_isSyncing`): `ActiveContentChanged` ↔ `IRootDock.ActiveDockable`, and `DocumentClosed` removes the closed ViewModel from `IDocumentDock.VisibleDockables`.

**`ILayoutUpdateStrategy` vs `ILayoutEngine` — Know the Difference:**
- **`ILayoutUpdateStrategy`** (`DockingManager.LayoutUpdateStrategy` DP): **Item-routing hook** called by `AttachAnchorablesSource` / `AttachDocumentsSource` (`BeforeInsertAnchorable`, `AfterInsertAnchorable`, `BeforeInsertDocument`, `AfterInsertDocument`).
  - Classic `DockingManager` auto-installs **`DockAlignmentStrategy`** when `DockLayout` is set and `LayoutUpdateStrategy == null`: if a visible `LayoutAnchorablePane` already exists on the target `AnchorSide` in `RootPanel`, it adds the tool as a tab there; otherwise it places the tool into that side's auto-hide `LayoutAnchorGroup`.
  - `ToggleDockingManager` installs **`ToggleLayoutStrategy`** in its constructor: reads `IToolbox`, copies `Title`, `Id` → `ContentId`, `Icon` → `ToggleDock.Icon`, `ToolTipText` → `ToggleDock.ToolTip`, and places the `LayoutAnchorable` into a `LayoutAnchorGroup` on `LeftSide`, `RightSide`, or `BottomSide` based on `IToolbox.Zone`.
- **`ILayoutEngine`** (`DockingManager.LayoutEngine` virtual property): **Structural tree engine** operating purely on `LayoutRoot` (no visual-tree dependencies):
  - `DefaultLayoutEngine` (on `DockingManager`): `InsertPane`, `FindOrCreateContentPanel`, `FixSplitOrientation`, `EnsureBottomFullWidth`, `EnsureSidesFullHeight`.
  - `ToggleLayoutEngine` (on `ToggleDockingManager`): adds 6-zone awareness (`InsertPaneForZone`, `FixSplitOrientationForZone`) so `LeftTop` precedes `LeftBottom`, `RightTop` precedes `RightBottom`, and `BottomLeft` precedes `BottomRight`.

### 4.5 `ToggleDockingManager` Internals (6-Zone Sidebars, Exclusive Bars, `IToolbox` Sync & Shortcuts)

`ToggleDockingManager` (`source/Components/AvalonDock/ToggleDockingManager.cs`) subclasses `DockingManager` with `SupportsAutoHideFlyout = false` (preventing `AutoHideWindowManager` from opening hover/slide flyouts).

1. **6 Zones Mapped onto 2 Vertical Sidebars (`SetupToggleDockButtonBars`):**
   - Collapses classic `LeftSidePanel`, `RightSidePanel`, `TopSidePanel`, and `BottomSidePanel`.
   - Calls `AutoHideAllDockedAnchorables()` (`ToggleSingleAutoHide()`) to gather all non-floating anchorables onto `Layout.LeftSide`, `RightSide`, and `BottomSide`.
   - Creates 6 `ToggleDockButtonBar` controls with stable UI Automation IDs (`ToggleDockBar_LeftTop`, `ToggleDockBar_LeftBottom`, `ToggleDockBar_RightTop`, `ToggleDockBar_RightBottom`, `ToggleDockBar_BottomLeft`, `ToggleDockBar_BottomRight`).
   - Injects two `DockPanel` rails (`_injectedLeftDockPanel` at column `0` and `_injectedRightDockPanel` at column `2`, `RowSpan = 3`) directly into the `DockingManager` template root `Grid`:
     - **Left Rail:** `[LeftTop]` → separator → `[LeftBottom]` → `Hidden Panels (⋯)` button → `[BottomLeft]` (docked bottom).
     - **Right Rail:** `[RightTop]` → separator → `[RightBottom]` → `[BottomRight]` (docked bottom).
   - **Stale Icon Binding Guard (`ClearBarIconContent`):** Before removing bars on layout/theme rebuilds, `RemoveToggleDockButtonBars()` explicitly clears `btn.IconContent = null` and `btn.IconSource = null`. Because vector `Viewbox` icons use `RelativeSource.FindAncestor` bindings to `ToggleDockButton.Foreground`, clearing `IconContent` first forces WPF to re-evaluate foreground bindings when re-parented instead of leaving icons stuck in their previous focused/white brush!
   - **Thread-Safe Style Caching:** `_staticToggleStyle` (`ToggleAnchorablePaneControlStyle` loaded via absolute `pack://application:,,,` URI) is `[ThreadStatic]` because unsealed WPF `Style` objects have `Dispatcher` thread affinity—essential when multi-threaded STA tests instantiate managers on separate threads.
2. **Exclusive-Per-Zone, Simultaneous-Across-Zones Toggling (`ToggleAnchorable`):**
   - Opening an anchorable in `zone` calls `HideDockedInBar(GetBarForZone(zone))` — collapsing **only** any sibling currently open in that *same* zone bar (and writing `SetToolboxIsOpen` back to that collapsed sibling so its `IToolbox.IsOpen` never goes stale!).
   - Thus `LeftTop` and `LeftBottom` can be open simultaneously in a vertical split, and `BottomLeft` + `BottomRight` can be open side-by-side in a horizontal split (`EnsureBottomZoneOrder` guarantees `BottomLeft` panes always stay left of `BottomRight` panes).
   - After docking (`DockFromAutoHide` → `FixSplitOrientation`), `LayoutPriority` restructures corners via `ToggleLayoutEngine`:
     - `DockLayoutPriority.BottomFullWidth` *(default, JetBrains Rider style)*: lifts bottom panes to the outer vertical `LayoutPanel` so bottom spans full width.
     - `DockLayoutPriority.SidesFullHeight` *(VS Code style)*: lifts left/right panes to the outer horizontal `LayoutPanel` so sidebars span full height.
3. **Two-Way `IToolbox.IsOpen` & Deserialization State Reconciliation:**
   - `RegisterToolbox` subscribes to `IToolbox.PropertyChanged` for `nameof(IToolbox.IsOpen)` and syncs mutations in both directions using `_syncDepth` to prevent re-entrancy loops.
   - **Fresh Load vs Saved Layout Reload:** On initial load (`ApplyInitialToolboxState`), `ToggleDockingManager` opens toolboxes where `toolbox.IsOpen || toolbox.IsOpenByDefault` (so setting `IsOpen = true` in DI before the window loads works!). Conversely, in `OnLayoutChanged` (e.g., `XmlLayoutSerializer.Deserialize`), it captures `CollectDockedAnchorables(newLayout)` before rebuilding bars, re-opens **only** what was docked in the saved layout, and calls `SyncToolboxStateToLayout()`—deliberately **not** re-applying `IsOpenByDefault` over the user's saved layout!
4. **Host-Window Shortcuts (`RefreshShortcuts`):** Parses `IToolbox.Shortcut` via `KeyGestureConverter` (ignoring invalid strings without throwing) and attaches `KeyBinding`s (`ToggleToolboxCommand`) to `Window.GetWindow(this).InputBindings`. `ToggleDockButton` automatically appends `" (<Shortcut>)"` to the tooltip.

### 4.6 Windowing Internals: Floating Windows vs Standalone `DetachedAnchorableWindow`

AvalonDock v5 has **two** distinct multi-window mechanisms—do not confuse them:

| Mechanism | Control Class | Chrome & Ownership | Content Hosted | Drag-Docking | Global Switch |
|:----------|:--------------|:-------------------|:---------------|:-------------|:--------------|
| **Tear-Off Floating Window** | `LayoutDocumentFloatingWindowControl` / `LayoutAnchorableFloatingWindowControl` | Custom themed borderless `WindowChrome`; owned by main window by default (`OwnedByDockingManagerWindow = true`, `ShowInTaskbar = false`) | Full `Layout*PaneGroup` split/tab tree (via `FloatingWindowContentHost : HwndHost`) | Yes (`IOverlayWindowHost` + `DragService`) | `AllowFloatingWindows` |
| **Standalone Window ("Window" Mode)** | `DetachedAnchorableWindow` | Standard OS `SingleBorderWindow`; **unowned** (`Owner = null`, `ShowInTaskbar = true`), independent minimize & z-order | Single `LayoutAnchorable`'s `LayoutItem.View` (`ContentPresenter`) + header | No (OS window manager; closing reattaches) | `AllowDetachedWindows` |

**Key Windowing Invariants in v5:**
- **Pre-HWND Safe Floating:** Calling `Float()` before the host `Window` has created its Win32 `HWND` defers `Owner` assignment to `Window.SourceInitialized` (`UpdateOwnership`) and defers `Show()` via `ShowWhenHostWindowIsShown()`.
- **Inheritable Property Mirroring:** Because top-level WPF `Window`s cannot join `DockingManager`'s logical tree, `DockingManager.OnPropertyChanged` explicitly mirrors inheritable properties (`FontSize`, `FontFamily`, `FlowDirection`, `TextOptions.*`) onto open floating windows (`SyncInheritedProperty`) without clobbering local/style values on the floating window.
- **`Alt+F4` / System Close Consistency:** Closing a `LayoutAnchorableFloatingWindowControl` via `Alt+F4` or `WM_CLOSE` routes through `HideWindowCommand` (respecting `AnchorableHiding`), and closing a `LayoutDocumentFloatingWindowControl` routes through `CloseWindowCommand` (respecting `DocumentClosing` cancellation).
- **Single-Presenter Transfer in `DetachAnchorableToWindow`:** A WPF `Visual` can have only one parent. `DockingManager.DetachAnchorableToWindow` calls `DetachFromLayout(anchorable)`, severs `layoutItem.View` from both its old template parent (`DisconnectFromVisualParent`, clearing `ContentProperty` bindings first!) and `DockingManager`'s logical children (`InternalRemoveLogicalChild`), and hands `view` to `new DetachedAnchorableWindow(anchorable, view, header)`.
  - On classic `DockingManager`, `DetachFromLayout` hides the model into `LayoutRoot.Hidden` (preserving `PreviousContainer` and `PreviousContainerIndex`).
  - On `ToggleDockingManager`, `DetachFromLayout` overrides this to collapse the panel onto its sidebar stripe (`_detachedZones[anchorable] = zone`) so its `ToggleDockButton` stays visible, `IToolbox.IsOpen` remains `true`, and clicking the sidebar button brings the standalone window to the foreground (`ActivateDetachedWindow`)!
- **Off-Screen Monitor Protection & Shutdown Cleanup:** `DetachedAnchorableWindow` checks that at least an `80 × 80` DIP intersection overlaps `SystemParameters.VirtualScreen*` before restoring saved coordinates (falling back to `WindowStartupLocation.CenterScreen` if a secondary monitor was unplugged). `HookHostWindow()` closes all ownerless `DetachedAnchorableWindow`s when the main window closes so the process never hangs under `ShutdownMode.OnLastWindowClose`.
- **Temporary Manager Unload Survival:** When `DockingManager` unloads (e.g., inside an outer `TabControl`), floating windows are hidden into `_fwHiddenList` (`KeepContentVisibleOnClose = true`) and detached windows close with `keepDetachedFlag: true` so `DockingManager_Loaded` / `RestoreDetachedAnchorables` seamlessly restores them when re-loaded.

### 4.7 Interactive Drag-and-Drop, `OverlayWindow` Reuse & Focus / `HwndHost` Airspace

- **`DragService` & Single-`OverlayWindow`-Per-Host:**
  - During a floating-window drag (`WM_MOVING`), `DragService.UpdateMouseLocation` queries `DockingManager.GetOverlayWindowHostsByZOrder` (walking Win32 `GW_HWNDFIRST` → `GW_HWNDNEXT` handles so floating windows above the main window get hit-tested before `DockingManager`!).
  - Each `IOverlayWindowHost` (`DockingManager` and `LayoutDocumentFloatingWindowControl`) allocates **at most one `OverlayWindow`** and calls `HideOverlay()` rather than closing/recreating it per drag.
  - Because Windows can drop a modal move loop without firing `WM_EXITSIZEMOVE` when crossing monitors of different DPIs, `DragService` calls `_manager.HideAllOverlayWindows()` at **both** drag start and drag end/abort (`#587`), guaranteeing transparent overlay windows never leak.
  - Dropping onto a `DropTarget<T>` calls `DockingManager.RaiseContentDocking(content)` **before** mutating the layout tree (allowing atomic cancellation via `ContentDocking`), performs the tree move, calls `RaiseContentDocked(content)`, and runs `root.CollectGarbage()`.
- **`ToggleDockingManager` 6-Zone Drag Overlay (`ToggleDockDragOverlay`):**
  - Dragging a `ToggleDockButton` or tool pane header in `ToggleDockingManager` intercepts `StartDraggingFloatingWindowForPane` / `StartDraggingFloatingWindowForContent` and shows a 6-zone visual overlay (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`) instead of tearing off a classic floating pane, relocating via `MoveAnchorableToZone`. Document tab drags still use the standard `OverlayWindow`.
- **Focus & `HwndHost` Airspace (`FocusElementManager` & `AutoHideWindowManager`):**
  - `FocusElementManager` tracks the last focused WPF `IInputElement` (`FullWeakDictionary<ILayoutElement, IInputElement>`) **and** Win32 child `HWND` (`WindowHookHandler` hook for `WindowsFormsHost` / WebView2 / native hosts) per `LayoutContent`, restoring exact inner focus on tab activation (`SetFocusOnLastElement`).
  - Classic auto-hide flyouts use `LayoutAutoHideWindowControl : HwndHost` so slide-out tool panels render cleanly *above* Win32 airspace controls (`WindowsFormsHost`, DirectX/OpenGL viewports) and auto-collapse after `DockingManager.AutoHideDelay` (`500` ms default) once focus leaves.

### 4.8 DTO Serialization & Deserialization Fixup Pipeline

```text
Save:  LayoutRoot ──► LayoutDtoMapper.ToDto() ──► LayoutRootDto ──► SerializeCore(Stream, dto)
Load:  DeserializeCore(Stream) ──► LayoutRootDto ──► LayoutDtoMapper.FromDto() ──► FixupLayout() ──► Manager.Layout = layout
```

1. **17 Plain DTOs (`AvalonDock.Core.Serialization.Dto`):** Zero WPF or JSON library dependencies. `LayoutDtoMapper` (`source/Components/AvalonDock/Serialization/LayoutDtoMapper.cs`) maps `GridLength`, `Orientation`, `PreviousContainerId` (`ILayoutPaneSerializable.Id`), `Floating*` bounds, `IsMaximized`, and `LayoutAnchorable.IsDetached`.
2. **`XmlLayoutSerializer` (`AvalonDock.Serializer.Xml`):**
   - Writes namespace-free `<LayoutRoot>` XML using `System.Xml.Serialization.XmlSerializer` over `LayoutRootDto`.
   - **Legacy v4 XML Compatibility on Load:** Decodes streams into a `string` via `StreamReader(..., Encoding.UTF8, detectEncodingFromByteOrderMarks: true)` (so legacy files with mismatched `encoding="utf-16"` prologs on UTF-8 bytes do not throw) and runs `LegacyBooleanRegex` to lowercase `"True"`/`"False"` on the 12 v4 boolean attributes (`IsSelected`, `IsLastFocusedDocument`, `IsMaximized`, `CanClose`, `CanFloat`, `CanShowOnHover`, `CanHide`, `CanAutoHide`, `CanDockAsTabbedDocument`, `CanDock`, `CanMove`, `ShowHeader`) without touching user strings like `Title` or `ContentId`.
3. **`JsonLayoutSerializer` (`AvalonDock.Serializer.Json`):**
   - Uses `System.Text.Json` with a `DefaultJsonTypeInfoResolver` modifier (`DeclarePolymorphicDtos`) that attaches a `"_type"` discriminator **only** to the 3 abstract DTO base classes used polymorphically:
     - `LayoutPositionableGroupDto` (`"LayoutPanelDto"`, `"LayoutDocumentPaneDto"`, `"LayoutDocumentPaneGroupDto"`, `"LayoutAnchorablePaneDto"`, `"LayoutAnchorablePaneGroupDto"`)
     - `LayoutContentDto` (`"LayoutDocumentDto"`, `"LayoutAnchorableDto"`)
     - `LayoutFloatingWindowDto` (`"LayoutDocumentFloatingWindowDto"`, `"LayoutAnchorableFloatingWindowDto"`)
   - Custom `JsonSerializerOptions` passed to the constructor are cloned and respected, except `TypeInfoResolver` which is always replaced with AvalonDock's polymorphic resolver.
4. **`LayoutSerializerBase.Deserialize` Lifecycle (`source/Components/AvalonDock.Core/Serialization/LayoutSerializerBase.cs`):**
   - `StartDeserialization()` sets `SuspendDocumentsSourceBinding = true` and `SuspendAnchorablesSourceBinding = true` inside `try`/`finally`.
   - `CaptureCurrentState()` snapshots existing `ISerializableLayoutAnchorable[]` and `ISerializableLayoutDocument[]` so pre-existing content/titles/icons can be matched by `ContentId`.
   - `FixupLayout(layout)`:
     1. Calls `FixCachedRootOnDeserialize()` on every descendant.
     2. Relinks `PreviousContainerId` → `PreviousContainer` (`ISerializableLayoutPane.Id`) across **both** `LayoutContent` items and `LayoutAnchorGroup` auto-hide groups (`#696`).
     3. Invokes `LayoutSerializationCallback` for each anchorable and document.
     4. Cancelling an anchorable (`args.Cancel = true`) or leaving it unresolved under default `UnresolvedContentHandling.Remove` calls **`RemoveFromLayout()`** (never `Close()`, because closing an auto-hidden anchorable would restore its entire `LayoutAnchorGroup` including siblings!). Under `UnresolvedContentHandling.Hide`, unresolved anchorables call `HideAnchorable(false)` into `LayoutRoot.Hidden`. Unresolved documents always call `Close()`.
     5. Calls `layout.CollectGarbage()`.
   - Assigning `Manager.Layout = layout` triggers `OnLayoutChanged`, which enforces `AllowFloatingWindows` (`DockAllFloatingWindows()` before visuals are created if `false`) and schedules `RestoreDetachedAnchorables` at `DispatcherPriority.Loaded` (or clears `IsDetached` if `AllowDetachedWindows == false`).

### 4.9 Theme Engine Internals (`Theme` vs `DictionaryTheme`, `.vstheme` & VS2026 JSON)

1. **Two Base Classes in `AvalonDock.Themes`:**
   - **`Theme`** (`abstract`): Pack-URI-based themes (`ArcDarkTheme`, `ArcLightTheme`, `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `GenericTheme`) override `GetResourceUri()`.
   - **`DictionaryTheme : Theme`** (`abstract`): Runtime-constructed themes (`VsTheme`, `VS2015*Theme`, `VS2022*Theme`, `VS2026*Theme`, `Vs2013*Theme`) pass a built `ResourceDictionary` to `base(themeResourceDictionary)` and return `null` from `GetResourceUri()`.
   - Changing `DockingManager.Theme` swaps the merged dictionary on `DockingManager`, every open `LayoutFloatingWindowControl`, and every open `DetachedAnchorableWindow` (`UpdateThemeResources`), and triggers `ToggleDockingManager.ReapplyThemeStyles()` (which preserves currently open toolboxes across theme switches).
2. **`Dirkster.AvalonDock.Themes.VS` Engine (`namespace AvalonDock.Themes.VS`):**
   - `VsThemeParser` parses `.vstheme` XML and `.vstheme.gz` streams (auto-detecting GZIP magic bytes `0x1F 0x8B`) into an immutable-mergeable `VsThemeColorPalette`.
   - `VsJsonThemeParser` parses VS2026 JSON token override arrays (`AARRGGBB` hex strings) into a `VsThemeColorPalette` layered onto a base palette via `basePalette.Merge(overrides)`.
   - `VsThemeResourceBuilder` maps `VsThemeColorPalette` tokens onto frozen `SolidColorBrush` entries keyed by `AvalonDock.Themes.VS.Themes.ResourceKeys` + `ToggleDockButton.ForegroundBrushKey`, merging either flat VS2015 templates (`Themes/Generic.xaml`) or rounded VS2022/VS2026 templates (`Themes/VS2022/Generic.xaml`).
3. **Assembly-Scoped `ComponentResourceKey`s:** `Arc.Themes.ResourceKeys`, `VS.Themes.ResourceKeys`, and `VS2013.Themes.ResourceKeys` each scope keys to their own `typeof(ResourceKeys)`. Custom `DictionaryTheme` or XAML themes **must** reference the `ResourceKeys` class from the **exact** theme assembly whose `Generic.xaml` templates they merged.

---

## 5. Build, Test & Verification Commands

### Prerequisites
- **OS:** Windows (required — `UseWPF=true`)
- **.NET SDKs:** `.NET 8.0`, `.NET 9.0`, `.NET 10.0`, and `.NET 11.0` SDKs (plus `.NET Framework 4.8` targeting pack, included with Visual Studio / Windows SDK)
- **Solution:** `source/AvalonDock.sln`

### Restore & Build
`TreatWarningsAsErrors` is enabled globally in `source/Directory.Build.props`, and CI compiles with `--warnaserror`. Every compiler, analyzer, and XML doc warning fails the build.

```powershell
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

### Run Unit & Layout Regression Tests (Fast — Run First)
Always pass **`-m:1`** (single-threaded MSBuild execution). Test projects multi-target `net10.0-windows;net11.0-windows;net48` and run WPF STA dispatchers that cannot execute safely in parallel.

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1
```

To run a single NUnit test fixture or method quickly during iteration:

```powershell
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net10.0-windows --filter "FullyQualifiedName~LayoutSyncBridgeTests" -m:1
```

### Run FlaUI Desktop UI Automation Tests
FlaUI tests launch real desktop WPF windows (`TestApp.exe` and `AvalonDockCodeApp.exe`) and drive mouse/keyboard input via Windows UI Automation (`UIA3`). Run them separately per desktop framework:

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net10.0-windows -m:1
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net11.0-windows -m:1
```

---

## 6. Testing Architecture & Critical STA / FlaUI Rules

All automated tests live in `source/AutomationTest/AvalonDockTest/` using **NUnit 4** (`[TestFixture]`, `[Test]`, `Assert.That(..., Is...)`).

1. **Pure Model / Serializer / MVVM / DI Unit Tests (`Category!=FlaUI`):**
   - Tests for `AvalonDock.Core`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, `ToggleLayoutEngine`, `OverlayPreviewRules`, `VsThemeParser`, `VsJsonThemeParser`, `XmlLayoutSerializer`, and `JsonLayoutSerializer` can run directly or with `[Apartment(ApartmentState.STA)]` when instantiating `DependencyObject`-derived layout elements (`LayoutRoot`, `LayoutAnchorable`, `DockingManager`).
2. **Hosted WPF Window STA Tests (`AutomationTestBase` in `TestHelpers/AutomationTestBase.cs`):**
   - Uses `TestHost` + `ThreadExecutor.RunCodeAsSTA(...)` (or `SwitchContextToUiThreadAwaiter`) to run live WPF windows against a shared background STA `TestApp` `Application` instance.
   - **CRITICAL RULE — NEVER CALL `Application.Current.Shutdown()` IN TESTS:** `Application.Current` is a process-wide singleton and WPF's `Application.IsShuttingDown` is a permanent one-way latch. Calling `Shutdown()` in any fixture's teardown permanently breaks `pack://application:,,,` resource loading (such as `Themes/generic.xaml`) for **every** test fixture that runs afterward! Close individual `Window` instances in `[TearDown]` instead.
3. **FlaUI UI Automation Tests (`FlaUI/*.cs`, `[Category("FlaUI")]`):**
   - **`FlaUITestBase`** launches `TestApp.exe` (classic `DockingManager`). Build `source/AvalonDock.sln` before running `--no-build` FlaUI tests so `TestApp.exe` is up to date.
   - **`ToggleDockFlaUITestBase`** (`[Category("FlaUI")]`, `[Category("ToggleDock")]`) launches `AvalonDockCodeApp.exe` (`ToggleDockingManager`) and inspects sidebar zones via `AutomationId` `"ToggleDockBar_LeftTop"`, `"ToggleDockBar_LeftBottom"`, `"ToggleDockBar_RightTop"`, `"ToggleDockBar_RightBottom"`, `"ToggleDockBar_BottomLeft"`, and `"ToggleDockBar_BottomRight"`.

---

## 7. Code Style, Static Analysis & Commit Conventions

Configured via `source/Directory.Build.props` and `source/.editorconfig`:

- **Indentation & Formatting:** **Tabs** (`indent_style = tab`), UTF-8, LF line endings, Allman braces (`csharp_new_line_before_open_brace = all`), `using` directives outside `namespace`.
- **Language & Nullability:** `<LangVersion>13</LangVersion>` (or `latest` in `netstandard2.0` packages). `AvalonDock.Core`, `AvalonDock.Mvvm*`, and `AvalonDock.DependencyInjection` have `<Nullable>enable</Nullable>` (with `#nullable disable` in specific legacy-compatible files); classic `AvalonDock` files omit nullable annotations or use `#nullable disable`—match the surrounding file.
- **XML Documentation Required (`CS1591` / `SA1600`):** `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled on `AvalonDock` and component packages. Every `public` (and `protected`) type, member, parameter, and return value in non-test projects **must** have valid XML doc comments or the build will fail under `--warnaserror`.
- **Active Analyzers:**
  - `StyleCop.Analyzers` (enabled on all projects whose name does not contain `Test`)
  - `Microsoft.CodeAnalysis.NetAnalyzers`
  - `SonarAnalyzer.CSharp`
  - `Microsoft.CodeAnalysis.Metrics` (`source/CodeMetricsConfig.txt`)

### Commit Message Format
```text
[Fix|Feature] #<TicketID> - [<Topic>] <What was done>
```
Examples:
- `[Fix] #696 - [Serializer] Relink LayoutAnchorGroup.PreviousContainer on layout restore`
- `[Feature] #673 - [Build] Added net8.0-windows target to NuGet packages`
- `[Fix] #683 - [Themes] VS themes fully support ToggleDockingManager without Arc resources`

---

## 8. Repository & Documentation Lookup Map

| When Working On… | Key Source Files (`source/…`) | Authoritative v5 Docs (`new-docs-carbon4/…`) |
|:-----------------|:------------------------------|:-------------------------------------|
| **Root `DockingManager`, DPs, visual creation, logical children, events** | `Components/AvalonDock/DockingManager.cs`, `*EventArgs.cs` | `api-reference/docking-manager.md`, `api-reference/events.md`, `concepts/architecture.md` |
| **`ToggleDockingManager`, 6-zone sidebars, shortcuts, zone drag overlay** | `Components/AvalonDock/ToggleDockingManager.cs`, `ToggleLayoutEngine.cs`, `ToggleLayoutStrategy.cs`, `Controls/Toggle*.cs`, `AvalonDockCodeApp/` | `guides/toggle-docking-manager.md`, `api-reference/toggle-docking-manager.md` |
| **Layout model tree, `DockWidth`/`DockHeight`, `CollectGarbage()`, `AddToLayout` vs `Show`** | `Components/AvalonDock/Layout/*.cs`, `DefaultLayoutEngine.cs` | `concepts/layout-model.md`, `concepts/documents-and-anchorables.md`, `api-reference/layout-classes.md` |
| **WPF pane/tab controls, `LayoutItem`, splitters, `NavigatorWindow`, converters** | `Components/AvalonDock/Controls/*.cs`, `Converters/*.cs` | `api-reference/controls.md` |
| **Floating windows, standalone `DetachedAnchorableWindow`, `OverlayWindow`, `DragService`** | `Components/AvalonDock/Controls/LayoutFloatingWindowControl*.cs`, `DetachedAnchorableWindow.cs`, `DragService.cs`, `OverlayWindow.cs`, `*DropTarget.cs`, `Components/AvalonDock.Core/Controls/Overlay*.cs` | `concepts/floating-windows.md`, `api-reference/controls.md` |
| **UI-agnostic contracts, MVVM ViewModels, `IDockLayoutService`, `LayoutSyncBridge`** | `Components/AvalonDock.Core/Interfaces/*.cs`, `FactoryBase*.cs`, `Components/AvalonDock.Mvvm/*.cs`, `Components/AvalonDock.Mvvm.CommunityToolkit/*.cs`, `Components/AvalonDock/LayoutSyncBridge.cs`, `DockAlignmentStrategy.cs` | `guides/mvvm.md`, `api-reference/core-and-mvvm.md`, `tutorials/mvvm-ide.md` |
| **Dependency Injection (`AddDockLayoutService`, `DockingOptions`, `ToggleDockOptions`)** | `Components/AvalonDock.DependencyInjection/*.cs`, `Components/AvalonDock/ThemeManager.cs` | `guides/dependency-injection.md`, `tutorials/dependency-injection-app.md` |
| **XML / JSON Serialization & DTO mapping** | `Components/AvalonDock.Core/Serialization/*.cs`, `Dto/*.cs`, `Components/AvalonDock/Serialization/LayoutDtoMapper.cs`, `Components/AvalonDock.Serializer.Xml/XmlLayoutSerializer.cs`, `Components/AvalonDock.Serializer.Json/JsonLayoutSerializer.cs` | `guides/serialization.md`, `tutorials/layout-persistence.md` |
| **Themes (`Arc`, `VS` `.vstheme` / JSON, `VS2013`, custom themes)** | `Components/AvalonDock/Themes/*.cs`, `Components/AvalonDock.Themes.Arc/`, `Components/AvalonDock.Themes.VS/`, `Components/AvalonDock.Themes.VS2013/`, `Components/AvalonDock.Themes.*` | `guides/theming.md`, `guides/custom-themes.md`, `themes/index.md`, `themes/vs.md`, `themes/arc.md` |
| **Migrating v4 apps & breaking changes** | — | `migration/index.md`, `migration/breaking-changes.md` |
| **Unit tests & FlaUI UI automation tests** | `AutomationTest/AvalonDockTest/*.cs`, `TestHelpers/*.cs`, `FlaUI/*.cs` | `getting-started/building-from-source.md`, `contributing.md` |

---

## 9. Vibecoding Pitfalls Checklist

Before finishing any code change or generating sample code for AvalonDock v5, verify:

- [ ] **No `netstandard2.0` boundary leaks:** `AvalonDock.Core`, `Mvvm*`, `DependencyInjection`, and `Serializer.*` must never reference WPF types (`System.Windows.*`).
- [ ] **`Id` ↔ `ContentId` mapped:** When binding ViewModels implementing `IDockable` / `IToolbox`, `LayoutItemContainerStyle` binds `ContentId` to `{Binding Model.Id}`, and `LayoutSerializationCallback` compares `vm.Id == args.Model.ContentId`.
- [ ] **Window policy configured in one place:** When `DockLayout` is bound, set `AllowFloatingWindows` / `AllowDetachedWindows` on `IRootDock` or `DockingOptions` (not only in XAML, which `LayoutSyncBridge` overwrites from `IRootDock`).
- [ ] **Adding vs restoring anchorables:** Use `AddToLayout` only when `Parent == null` (`!IsVisible && !IsHidden`); use `Show()` when `IsHidden == true`; use `RemoveFromLayout()` to detach without firing close events or disturbing auto-hide siblings.
- [ ] **DTO round-trip parity:** Any new persisted layout property is added to the appropriate DTO in `AvalonDock.Core.Serialization.Dto` (with `ShouldSerialize*()` / `[DefaultValue]` XML guards as appropriate), mapped in **both** `ToDto` and `FromDto` in `LayoutDtoMapper.cs`, and tested with **both** `XmlLayoutSerializer` and `JsonLayoutSerializer`.
- [ ] **Tab icons in modern themes:** Remember that flat modern themes (`Arc`, `VS`, `VS2013`) render clean title-only document tabs by default; provide a custom `DocumentHeaderTemplate` if tab icons are required in those themes.
- [ ] **Zero warnings & `-m:1` tests:** `dotnet build source/AvalonDock.sln --configuration Release --warnaserror` passes with 0 warnings, and `dotnet test ... -m:1` passes without calling `Application.Current.Shutdown()`.
