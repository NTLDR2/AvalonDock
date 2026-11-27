---
title: Themes
layout: default
nav_order: 6
has_children: true
permalink: /themes/
description: "Gallery and technical reference for all seven AvalonDock theme NuGet packages and built-in GenericTheme."
---

# Themes

AvalonDock provides **seven dedicated theme NuGet packages** (delivering 19 ready-to-use theme classes plus `VsTheme` custom palette loading) in addition to the default `GenericTheme` and `DictionaryTheme` base class built into `Dirkster.AvalonDock`.

Every theme package targets `net48` exclusively with zero third-party NuGet dependencies, and styles both classic `DockingManager` layouts and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebars natively.

{: .note }
**Fork Scope (`NTLDR2 Fork v5.1.0` vs. Upstream `v5.0.0`):** `Dirkster.AvalonDock.Themes.VS` and `Dirkster.AvalonDock.Themes.VS2013` are refactored in **`NTLDR2 Fork v5.1.0`** to use a self-contained pure-C# JSON override reader (`VsJsonThemeParser`) and frozen palette brushes — preserving 100% of upstream's 12 VS theme variants and `.vstheme` / VS2026 `.json` loading APIs with **zero `System.Text.Json` dependencies** on `net48`.

---

## Preview: Arc Dark & Arc Light

![AvalonDock Arc Dark Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

![AvalonDock Arc Light Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Welcome.png)

---

## Theme Package Comparison

| Theme Package | NuGet Package (`net48`) | Namespace | Built-in Variants | Base Class | Customization Mechanism |
|:--------------|:------------------------|:----------|:------------------|:-----------|:------------------------|
| [**Arc**]({{ site.baseurl }}{% link themes/arc.md %}) | `Dirkster.AvalonDock.Themes.Arc` | `AvalonDock.Themes` | `ArcDarkTheme`, `ArcLightTheme` | `Theme` | XAML `DarkBrushs.xaml` / `LightBrushs.xaml` + `Arc.Themes.ResourceKeys` |
| [**VS**]({{ site.baseurl }}{% link themes/vs.md %}) | `Dirkster.AvalonDock.Themes.VS` | **`AvalonDock.Themes.VS`** | `VS2026{Dark,Light,Blue}Theme`, `VS2022{Dark,Light,Blue}Theme`, `VS2015{Dark,Light,Blue}Theme` | `VsTheme : DictionaryTheme` | `.vstheme` / `.vstheme.gz` / VS2026 `.json` (`VsTheme`, `{vs:VsTheme}`, `VS.Themes.ResourceKeys`) |
| [**VS2013**]({{ site.baseurl }}{% link themes/vs2013.md %}) | `Dirkster.AvalonDock.Themes.VS2013` | `AvalonDock.Themes` | `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` | `DictionaryTheme` | Runtime `ThemeResourceDictionary` overrides (`VS2013.Themes.ResourceKeys`) |
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
| **VS (2015 / 2022 / 2026)** | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS2013** | Yes | Yes | Yes | Yes | Yes | Yes |
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

# Or Visual Studio 2015 / 2022 / 2026 themes:
dotnet add package Dirkster.AvalonDock.Themes.VS
```

### Apply in C#

```csharp
using AvalonDock.Themes;
using AvalonDock.Themes.VS;

dockManager.Theme = new ArcDarkTheme();
// or:
dockManager.Theme = new VS2026DarkTheme();
```

---

## Related Guides

- [Theming Guide]({{ site.baseurl }}{% link guides/theming.md %}) — XAML setup, MVVM data binding, and `IThemeManager` with Dependency Injection.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Build branded themes using `DictionaryTheme`, `.vstheme` / JSON palettes, or `ResourceKeys`.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — End-to-end multi-theme application with saved user preferences.
