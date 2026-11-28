---
title: API Reference
layout: default
nav_order: 7
has_children: true
description: "Source-verified API reference for AvalonDock v5 controls, layout model, events, MVVM, DI, and serialization."
permalink: /api-reference/
---

# API Reference
{: .no_toc }

Source-verified reference documentation for AvalonDock v5 controls, layout model classes, events, and UI-agnostic `AvalonDock.Core` / `AvalonDock.Mvvm` / `AvalonDock.DependencyInjection` contracts.
{: .fs-6 .fw-300 }

---

## Reference Sections

| Section | Summary |
|:--------|:--------|
| [DockingManager]({{ site.baseurl }}{% link api-reference/docking-manager.md %}) | Complete DependencyProperty, CLR property, method, and event reference for `AvalonDock.DockingManager`. |
| [ToggleDockingManager]({{ site.baseurl }}{% link api-reference/toggle-docking-manager.md %}) | Rider/VS Code-style sidebar docking manager, `DockLayoutPriority`, `ToggleLayoutEngine`, `ToggleDock` attached properties, and sidebar controls. |
| [Layout Classes]({{ site.baseurl }}{% link api-reference/layout-classes.md %}) | Full `AvalonDock.Layout` class hierarchy, `LayoutRoot`, `LayoutContent`, `LayoutDocument`, `LayoutAnchorable`, panes, `ILayoutEngine`, and layout interfaces. |
| [Controls]({{ site.baseurl }}{% link api-reference/controls.md %}) | Visual WPF controls (`LayoutItem`, `LayoutDocumentItem`, `LayoutAnchorableItem`, floating windows, `DetachedAnchorableWindow`, `NavigatorWindow`, `OverlayWindow`). |
| [Events]({{ site.baseurl }}{% link api-reference/events.md %}) | Lifecycle, floating, docking, and layout tree events on `DockingManager`, `IDockingManager`, `LayoutRoot`, and `LayoutContent`. |
| [Core, MVVM & DI]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}) | UI-agnostic interfaces (`IDockable`, `IToolbox`, `IDockLayoutService`, `ILayoutSerializer`), `AvalonDock.Mvvm`, `AvalonDock.Mvvm.CommunityToolkit`, and DI registration APIs. |

---

## Namespaces Across Shipped Packages

| Namespace | Package(s) | Purpose |
|:----------|:-----------|:--------|
| `AvalonDock` | `Dirkster.AvalonDock` | `DockingManager`, `ToggleDockingManager`, `ILayoutEngine`, `DefaultLayoutEngine`, `ToggleLayoutEngine`, `ThemeManager`, and root `*EventArgs` classes. |
| `AvalonDock.Layout` | `Dirkster.AvalonDock` | Layout tree models (`LayoutRoot`, `LayoutPanel`, `LayoutDocument`, `LayoutAnchorable`, panes, floating window models), `AnchorSide`, `AnchorableShowStrategy`, and `ILayout*` interfaces. |
| `AvalonDock.Controls` | `Dirkster.AvalonDock` | WPF visual controls, `LayoutItem` wrappers, `ToggleDock`, `ToggleDockButtonBar`, `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, and `NavigatorWindow`. |
| `AvalonDock.Themes` | `Dirkster.AvalonDock` | Abstract theme contracts: `Theme` (`GetResourceUri()`) and `DictionaryTheme` (`ThemeResourceDictionary`). |
| `AvalonDock.Themes.Type1Legacy` | `Dirkster.AvalonDock`, `Dirkster.AvalonDock.Themes.Type1Legacy` | `GenericTheme` (in `AvalonDock`) plus URI-based classic themes (`VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `AeroColors`). |
| `AvalonDock.Themes.Type2Hybrid` | `Dirkster.AvalonDock.Themes.Type2Hybrid` | `DictionaryTheme`-based Visual Studio 2013 theme classes (`Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme`). |
| `AvalonDock.Themes.Type3New` | `Dirkster.AvalonDock.Themes.Type3New` | `VsTheme` and Visual Studio 2015, 2022, and 2026 theme classes (`VS2015{Dark,Light,Blue}Theme`, `VS2022{Dark,Light,Blue}Theme`, `VS2026{Dark,Light,Blue}Theme`) plus `.vstheme` / JSON parsers. |
| `AvalonDock.Commands` | `Dirkster.AvalonDock` | Internal/template command helpers (`RelayCommand`). |
| `AvalonDock.Converters` | `Dirkster.AvalonDock` | Value converters used inside control templates (`BoolToVisibilityConverter`, `NullToDoNothingConverter`, `AnchorSideToOrientationConverter`, etc.). |
| `AvalonDock.Serialization` | `Dirkster.AvalonDock` | `LayoutDtoMapper` (`ILayoutDtoMapper` implementation mapping WPF `LayoutRoot` ↔ `LayoutRootDto`). |
| `AvalonDock.Core` | `Dirkster.AvalonDock.Core` | UI-agnostic contracts (`IDockingManager`, `IDockable`, `IToolbox`, `IDockLayoutService`, `IRootDock`, `DockZone`, `DockAlignment`, `ILayoutSerializer`, `LayoutSerializerBase`). |
| `AvalonDock.Core.Events` | `Dirkster.AvalonDock.Core` | UI-agnostic event argument types (`DocumentCancelEventArgs`, `AnchorableCancelEventArgs`, `ContentCancelEventArgs`, etc.). |
| `AvalonDock.Core.Serialization` | `Dirkster.AvalonDock.Core` | `ILayoutDtoMapper` and `ISerializable*` layout interfaces. |
| `AvalonDock.Core.Serialization.Dto` | `Dirkster.AvalonDock.Core` | All 17 plain-C# layout serialization DTOs (`LayoutRootDto`, `LayoutPanelDto`, `LayoutDocumentDto`, `LayoutAnchorableDto`, etc.). |
| `AvalonDock.Mvvm` | `Dirkster.AvalonDock.Mvvm` | Zero-dependency MVVM base classes and services (`DockableBase`, `ToolboxBase`, `DockLayoutService`, `SideToggleManager`, `RootDock`, `DocumentDock`, `ToolDock`). |
| `AvalonDock.Mvvm.CommunityToolkit` | `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `CommunityToolkit.Mvvm` source-generator-compatible base classes (`ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDocument`, `ObservableTool`). |
| `AvalonDock.DependencyInjection` | `Dirkster.AvalonDock.DependencyInjection` | `IServiceCollection` extensions (`AddAvalonDock`, `AddDockLayoutService`, `DockLayoutBuilder`, `DockingOptions`, `ToggleDockOptions`). |
| `AvalonDock.Serializer.Xml` | `Dirkster.AvalonDock.Serializer.Xml` | `XmlLayoutSerializer`. |
