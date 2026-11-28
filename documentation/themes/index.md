---
title: Themes
layout: default
nav_order: 6
has_children: true
permalink: /themes/
description: "Gallery and technical reference for all three AvalonDock theme category NuGet packages and built-in GenericTheme."
---

# Themes

AvalonDock provides **three consolidated theme category NuGet packages** (delivering 17 ready-to-use theme classes plus `VsTheme` custom palette loading) in addition to the default `GenericTheme` and abstract `Theme` / `DictionaryTheme` contracts built into `Dirkster.AvalonDock`.

Every theme package targets `net48` exclusively with zero third-party NuGet dependencies, maps onto **`https://github.com/NTLDR2/AvalonDock`** via `[XmlnsDefinition]`, and styles both classic `DockingManager` layouts and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebars natively.

{: .note }
**Fork Scope (`NTLDR2 Fork v5.1.0` vs. Upstream `v5.0.0`):** `NTLDR2 Fork v5.1.0` removes `Themes.Arc`, splits concrete themes into 3 explicit CLR namespaces matching their direct base classes (`Type1Legacy`, `Type2Hybrid`, `Type3New`), consolidates them into 3 matching assemblies, and refactors `VsJsonThemeParser` and `VsThemePaletteFactory` to pure zero-dependency C# with 100% frozen brushes on `net48`.

---

## Theme Category Comparison

| Theme Family | NuGet Package (`net48`) | CLR Namespace | Built-in Variants | Direct Base Class | Customization Mechanism |
|:-------------|:------------------------|:--------------|:------------------|:------------------|:------------------------|
| [**VS (2015–2026)**]({{ site.baseurl }}{% link themes/vs.md %}) | `Dirkster.AvalonDock.Themes.Type3New` | `AvalonDock.Themes.Type3New` | `VS2026{Dark,Light,Blue}Theme`, `VS2022{Dark,Light,Blue}Theme`, `VS2015{Dark,Light,Blue}Theme` | `VsTheme : DictionaryTheme` | `.vstheme` / `.vstheme.gz` / VS2026 `.json` (`VsTheme`, `{avalonDock:VsTheme}`, `Type3New.Themes.ResourceKeys`) |
| [**VS2013**]({{ site.baseurl }}{% link themes/vs2013.md %}) | `Dirkster.AvalonDock.Themes.Type2Hybrid` | `AvalonDock.Themes.Type2Hybrid` | `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` | `DictionaryTheme` | Runtime `ThemeResourceDictionary` overrides (`Type2Hybrid.Themes.ResourceKeys`) |
| [**VS2010**]({{ site.baseurl }}{% link themes/vs2010.md %}) | `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` | `VS2010Theme` | `Theme` | `/AvalonDock.Themes.Type1Legacy;component/Themes/VS2010/Brushes.xaml` (`AvalonDock_ThemeVS2010_BaseColor*`) |
| [**Expression**]({{ site.baseurl }}{% link themes/expression.md %}) | `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` | `ExpressionDarkTheme`, `ExpressionLightTheme` | `Theme` | `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/{Dark,Light}Brushes.xaml` |
| [**Metro**]({{ site.baseurl }}{% link themes/metro.md %}) | `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` | `MetroTheme` | `Theme` | `/AvalonDock.Themes.Type1Legacy;component/Themes/Metro/Brushes.xaml` (`AvalonDock_ThemeMetro_BaseColor1`..`7`) |
| [**Aero**]({{ site.baseurl }}{% link themes/aero.md %}) | `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` | `AeroTheme` | `Theme` | `/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Brushes.xaml` (`AeroColors.BaseColor*Key`) |
| **Generic** *(built-in)* | `Dirkster.AvalonDock` | `AvalonDock.Themes.Type1Legacy` | `GenericTheme` | `Theme` | `/AvalonDock;component/Themes/generic.xaml` (`SystemColors`) |

---

## Feature & Tab Icon Matrix

All 18 built-in themes render `LayoutContent.IconSource` in document tabs, tool headers, the `Ctrl+Tab` `NavigatorWindow`, and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebar buttons:

| Theme | Dark Variant | Light Variant | Blue Variant | Document Tab Icons (`IconSource`) | `Ctrl+Tab` Navigator Icons | `ToggleDockingManager` Native Styling |
|:------|:------------:|:-------------:|:------------:|:---------------------------------:|:--------------------------:|:-------------------------------------:|
| **VS (2015 / 2022 / 2026)** | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS2013** | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS2010** | — | — | Yes | Yes | Yes | Yes |
| **Expression** | Yes | Yes | — | Yes | Yes | Yes |
| **Metro** | — | Yes | — | Yes | Yes | Yes |
| **Aero** | — | Yes | — | Yes | Yes | Yes |
| **GenericTheme** | System | System | — | Yes | Yes | Yes |

---

## Quick Usage

### Install

```bash
# Visual Studio 2015 / 2022 / 2026 themes:
dotnet add package Dirkster.AvalonDock.Themes.Type3New
```

### Apply in C#

```csharp
using AvalonDock.Themes.Type1Legacy;
using AvalonDock.Themes.Type3New;

dockManager.Theme = new VS2026DarkTheme();
// or:
dockManager.Theme = new ExpressionDarkTheme();
```

---

## Related Guides

- [Theming Guide]({{ site.baseurl }}{% link guides/theming.md %}) — XAML setup, MVVM data binding, and `IThemeManager` with Dependency Injection.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Build branded themes using `DictionaryTheme`, `.vstheme` / JSON palettes, or `ResourceKeys`.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — End-to-end multi-theme application with saved user preferences.
