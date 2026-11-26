---
title: Themes
layout: default
nav_order: 6
has_children: true
permalink: /themes/
description: "Gallery and technical reference for all five AvalonDock theme NuGet packages and built-in GenericTheme."
---

# Themes

AvalonDock provides **five dedicated theme NuGet packages** (delivering 7 ready-to-use theme classes) in addition to the default `GenericTheme` and `DictionaryTheme` base class built into `Dirkster.AvalonDock`.

Every theme package targets `net8.0-windows`, `net9.0-windows`, `net10.0-windows`, `net11.0-windows`, and `net48` with zero third-party NuGet dependencies, and styles both classic `DockingManager` layouts and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebars natively.

---

## Preview: Arc Dark & Arc Light

![AvalonDock Arc Dark Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

![AvalonDock Arc Light Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Welcome.png)

---

## Theme Package Comparison

| Theme Package | NuGet Package | Namespace | Built-in Variants | Base Class | Customization Mechanism |
|:--------------|:--------------|:----------|:------------------|:-----------|:------------------------|
| [**Arc**]({{ site.baseurl }}{% link themes/arc.md %}) | `Dirkster.AvalonDock.Themes.Arc` | `AvalonDock.Themes` | `ArcDarkTheme`, `ArcLightTheme` | `Theme` | XAML `DarkBrushs.xaml` / `LightBrushs.xaml` + `Arc.Themes.ResourceKeys` |
| [**VS2010**]({{ site.baseurl }}{% link themes/vs2010.md %}) | `Dirkster.AvalonDock.Themes.VS2010` | `AvalonDock.Themes` | `VS2010Theme` | `Theme` | `/AvalonDock.Themes.VS2010;component/Brushes.xaml` (`AvalonDock_ThemeVS2010_BaseColor*`) |
| [**Expression**]({{ site.baseurl }}{% link themes/expression.md %}) | `Dirkster.AvalonDock.Themes.Expression` | `AvalonDock.Themes` | `ExpressionDarkTheme`, `ExpressionLightTheme` | `Theme` | `DarkBrushes.xaml` / `LightBrushes.xaml` (`AvalonDock_Expression_BaseColor*`) |
| [**Metro**]({{ site.baseurl }}{% link themes/metro.md %}) | `Dirkster.AvalonDock.Themes.Metro` | `AvalonDock.Themes` | `MetroTheme` | `Theme` | `/AvalonDock.Themes.Metro;component/Brushes.xaml` (`AvalonDock_ThemeMetro_BaseColor1`..`7`) |
| [**Aero**]({{ site.baseurl }}{% link themes/aero.md %}) | `Dirkster.AvalonDock.Themes.Aero` | `AvalonDock.Themes` | `AeroTheme` | `Theme` | `/AvalonDock.Themes.Aero;component/Brushes.xaml` (`AeroColors.BaseColor*Key`) |
| **Generic** *(built-in)* | `Dirkster.AvalonDock` | `AvalonDock.Themes` | `GenericTheme` | `Theme` | `/AvalonDock;component/Themes/generic.xaml` (`SystemColors`) |

---

## Feature & Tab Icon Matrix

Different IDE themes handle tab icons differently. Use this matrix when choosing a theme for applications that rely on `LayoutContent.IconSource` in document or tool window tab headers:

| Theme | Dark Variant | Light Variant | Blue Variant | Document Tab Icons (`IconSource`) | `Ctrl+Tab` Navigator Icons | `ToggleDockingManager` Native Styling |
|:------|:------------:|:-------------:|:------------:|:---------------------------------:|:--------------------------:|:-------------------------------------:|
| **Arc** | Yes | Yes | — | Title-only (Rider/Fleet style) | Yes | Yes |
| **VS2010** | — | — | Yes | Yes | Yes | Yes |
| **Expression** | Yes | Yes | — | Yes | Yes | Yes |
| **Metro** | — | Yes | — | Yes | Yes | Yes |
| **Aero** | — | Yes | — | Yes | Yes | Yes |
| **GenericTheme** | System | System | — | Yes | Yes | Yes |

{: .note }
Even in `Arc` (which uses clean title-only document tabs), setting `IconSource` on `LayoutDocument` and `LayoutAnchorable` still populates icons in the `Ctrl+Tab` `NavigatorWindow`, dropdown overflow menus, and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) edge sidebar buttons. To add tab icons to `Arc`, override `DocumentHeaderTemplate` or `AnchorableHeaderTemplate` on `DockingManager`.

---

## Quick Usage

### Install

```bash
# Modern Rider/Fleet-inspired Dark & Light themes:
dotnet add package Dirkster.AvalonDock.Themes.Arc
```

### Apply in C#

```csharp
using AvalonDock.Themes;

dockManager.Theme = new ArcDarkTheme();
// or:
dockManager.Theme = new ArcLightTheme();
```

---

## Related Guides

- [Theming Guide]({{ site.baseurl }}{% link guides/theming.md %}) — XAML setup, MVVM data binding, and `IThemeManager` with Dependency Injection.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Build branded themes using `DictionaryTheme` or `ResourceKeys`.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — End-to-end multi-theme application with saved user preferences.
