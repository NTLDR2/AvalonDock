---
title: Home
layout: home
nav_order: 1
description: "AvalonDock — A powerful WPF docking layout library for building Visual Studio, VS Code, and Rider-like interfaces."
permalink: /
---

# AvalonDock v5.1.0 (NTLDR2 Fork)
{: .fs-9 }

A powerful WPF Document and Tool Window layout container for building Visual Studio, VS Code, and JetBrains Rider-like docking interfaces — hardened strictly for `.NET Framework 4.8` (`net48`) and `.NET Standard 2.0` (`netstandard2.0`) with zero `System.Text.Json` dependencies.
{: .fs-6 .fw-300 }

[Get Started]({{ site.baseurl }}{% link getting-started/index.md %}){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View Fork on GitHub](https://github.com/NTLDR2/AvalonDock){: .btn .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Upstream (Dirkster99)](https://github.com/Dirkster99/AvalonDock){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What is AvalonDock?

AvalonDock is a mature WPF docking layout system for arranging tabbed documents and dockable tool windows. Version 5 modernizes the framework with a modular package architecture, first-class MVVM and `Microsoft.Extensions.DependencyInjection` support, a VS Code / Rider-style `ToggleDockingManager` sidebar control, DTO-based XML layout serialization with zero third-party serializer dependencies, standalone detached top-level tool windows, and Visual Studio 2013–2026 `.vstheme` / JSON themes.

{: .important }
**Scope & Version Attribution (`NTLDR2 Fork v5.1.0` vs. `Upstream v5.0.0`):** This site documents **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0`)**, an opinionated fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock). Upstream `v5.0.0` introduced the modular v5 architecture across 14 NuGet packages (followed by `net8.0-windows` in `v5.0.2` and `net11.0-windows` in `v5.1.0`). **This `NTLDR2 Fork v5.1.0` baseline** prioritizes `.NET Framework 4.8` + WPF exclusively, omits `Serializer.Json` and `Themes.Arc`, consolidates themes into 3 zero-`System.Text.Json` category assemblies (`Themes.{Type1Legacy,Type2Hybrid,Type3New}`) to ship **9 component libraries**, unifies XAML under **`https://github.com/NTLDR2/AvalonDock`**, and targets vendored/source consumption (including [NTLDR2/SomaFrameQD](https://github.com/NTLDR2/SomaFrameQD)).

![AvalonDock v5 ToggleDockingManager]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

AvalonDock is used by [many open source and commercial projects](https://github.com/search?p=4&q=%22dirkster.avalondock%22&type=Code), including [Stride](https://github.com/stride3d/stride) (game engine), [Optick](https://github.com/bombomby/optick) (profiler), [RoslynPad](https://github.com/roslynpad/roslynpad) (C# editor), [WpfDesigner](https://github.com/icsharpcode/WpfDesigner) (SharpDevelop WPF designer), [DaxStudio](https://github.com/DaxStudio/DaxStudio) (DAX query tool), [Macad3D](https://github.com/Macad3D/Macad3D) (3D CAD), [Edi](https://dirkster99.github.io/Edi/) (text editor IDE), and Microsoft's [Profile Explorer](https://github.com/microsoft/profile-explorer).

---

## Key Features

| Feature | Description |
|:--------|:------------|
| **Classic & Toggle Docking** | Choose between classic Visual Studio-style `DockingManager` (auto-hide flyouts) or VS Code / Rider-style `ToggleDockingManager` (6-zone sidebar buttons). |
| **Tabbed Documents** | Organize documents in tabbed groups, split horizontally or vertically, with optional tab icons and overflow menus. |
| **Floating & Standalone Windows** | Tear off panels into owned floating windows or detach tool windows into independent top-level OS windows with taskbar entries. |
| **DTO Layout Serialization** | Save and restore layouts via XML (`XmlLayoutSerializer`) with backward-compatible reading of v4 XML layouts and extensible `LayoutSerializerBase` DTO hooks. |
| **First-Class MVVM** | Bind view-model trees via `AvalonDock.Mvvm` (zero external dependencies) or `AvalonDock.Mvvm.CommunityToolkit` (`[ObservableProperty]` / `[RelayCommand]` support). |
| **Dependency Injection** | Register `IDockLayoutService`, toolboxes, and docking options cleanly via `Microsoft.Extensions.DependencyInjection`. |
| **3 Theme Category Packages + `GenericTheme`** | `Type3New` (VS 2015, 2022 & 2026 Dark/Light/Blue + `.vstheme`/JSON loader), `Type2Hybrid` (VS 2013 Dark/Light/Blue), and `Type1Legacy` (`VS2010`, `Expression` Dark & Light, `Metro`, `Aero`, plus core `GenericTheme`) — 18 built-in themes with live runtime switching. |
| **Focused `.NET Framework 4.8` & `netstandard2.0` Targets** | Targets `.NET Framework 4.8` (`net48`) across all WPF controls, themes, samples, and test suites (Windows 7 SP1 through Windows 11 with zero `System.Text.Json` dependencies), plus `netstandard2.0` core/MVVM/DI/serializer packages. |

---

## Quick Example

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock">

    <avalonDock:DockingManager>
        <avalonDock:DockingManager.Theme>
            <avalonDock:VS2026DarkTheme />
        </avalonDock:DockingManager.Theme>

        <avalonDock:LayoutRoot>
            <avalonDock:LayoutPanel Orientation="Horizontal">
                <avalonDock:LayoutAnchorablePane DockWidth="250">
                    <avalonDock:LayoutAnchorable Title="Solution Explorer"
                                                  ContentId="solutionExplorer">
                        <TextBlock Text="Explorer content here" />
                    </avalonDock:LayoutAnchorable>
                </avalonDock:LayoutAnchorablePane>

                <avalonDock:LayoutDocumentPane>
                    <avalonDock:LayoutDocument Title="Document1.txt"
                                               ContentId="doc1">
                        <TextBox Text="Document content here" AcceptsReturn="True" />
                    </avalonDock:LayoutDocument>
                </avalonDock:LayoutDocumentPane>
            </avalonDock:LayoutPanel>
        </avalonDock:LayoutRoot>
    </avalonDock:DockingManager>
</Window>
```

---

## Component Libraries & Package IDs (`NTLDR2 Fork v5.1.0`)

This fork builds **9 modular libraries** (all with embedded XML IntelliSense documentation and zero `System.Text.Json` references). Package IDs retain the `Dirkster.AvalonDock*` prefix until the planned `AvalonDox` rebrand; consume this fork via vendored `ProjectReference` or local `dotnet pack` *(installing from `nuget.org` pulls upstream `Dirkster99` packages)*.

### Core, MVVM & Dependency Injection

| Package | Target | Description |
|:--------|:-------|:------------|
| [Dirkster.AvalonDock](https://www.nuget.org/packages/Dirkster.AvalonDock) | `net48` | Main WPF docking controls (`DockingManager`, `ToggleDockingManager`, layout model, generic theme) |
| [Dirkster.AvalonDock.Core](https://www.nuget.org/packages/Dirkster.AvalonDock.Core) | `netstandard2.0` | UI-agnostic interfaces (`IDockable`, `IRootDock`, `IToolbox`, `IDockLayoutService`), enums, and DTO contracts |
| [Dirkster.AvalonDock.Mvvm](https://www.nuget.org/packages/Dirkster.AvalonDock.Mvvm) | `netstandard2.0` | MVVM base classes (`DockableBase`, `ToolboxBase`, `DockLayoutService`, `SideToggleManager`) with zero external dependencies |
| [Dirkster.AvalonDock.Mvvm.CommunityToolkit](https://www.nuget.org/packages/Dirkster.AvalonDock.Mvvm.CommunityToolkit) | `netstandard2.0` | `CommunityToolkit.Mvvm` `ObservableObject`-based classes (`ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDocument`, `ObservableTool`) |
| [Dirkster.AvalonDock.DependencyInjection](https://www.nuget.org/packages/Dirkster.AvalonDock.DependencyInjection) | `netstandard2.0` | `IServiceCollection` builder extensions (`AddDockLayoutService`, `DockLayoutBuilder`, `DockingOptions`, `ToggleDockOptions`) |

### Layout Serializer

| Package | Target | Description |
|:--------|:-------|:------------|
| [Dirkster.AvalonDock.Serializer.Xml](https://www.nuget.org/packages/Dirkster.AvalonDock.Serializer.Xml) | `netstandard2.0` | XML layout persistence (`XmlLayoutSerializer`) with backward-compatible v4 layout loading and zero external dependencies |

### Theme Category Packages

| Package | Variants | Description |
|:--------|:---------|:------------|
| `Dirkster.AvalonDock.Themes.Type3New` | VS2015, VS2022, VS2026 (Dark, Light, Blue) | `VsTheme : DictionaryTheme` Visual Studio 2015/2022/2026 themes plus `.vstheme`, `.vstheme.gz`, and zero-dependency VS2026 JSON loading (`AvalonDock.Themes.Type3New`) |
| `Dirkster.AvalonDock.Themes.Type2Hybrid` | Dark, Light, Blue | `DictionaryTheme` flat Visual Studio 2013 themes (`Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` in `AvalonDock.Themes.Type2Hybrid`) |
| `Dirkster.AvalonDock.Themes.Type1Legacy` | VS2010, Expression Dark/Light, Metro, Aero | Classic URI-based `Theme` subclasses (`VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme` in `AvalonDock.Themes.Type1Legacy`) |

---

## Platform Support

| Target Framework | Status | Introduced In / Scope |
|:-----------------|:-------|:----------------------|
| `.NET Framework 4.8` (`net48`) | ✅ Supported | **Exclusive desktop WPF target in `NTLDR2 Fork v5.1.0`** (supported since `Upstream v5.0.0`, Win7 SP1 64-bit – Win11) |
| `.NET Standard 2.0` (`netstandard2.0`) | ✅ Supported | UI-agnostic packages (`Core`, `Mvvm*`, `DependencyInjection`, `Serializer.Xml`) since `Upstream v5.0.0` |
| `.NET 8`–`.NET 11` (`net8.0-windows`..`net11.0-windows`) | Upstream only | `NTLDR2 Fork v5.1.0` targets `net48` strictly on WPF assemblies (`Upstream v5.0.2`–`v5.1.0` additionally ships `net8.0-windows`–`net11.0-windows` builds) |

{: .note }
`Dirkster.AvalonDock` and `Dirkster.AvalonDock.Themes.*` are WPF packages targeting `.NET Framework 4.8` (`net48`) on Windows. Your UI-agnostic ViewModel and service projects can reference `Dirkster.AvalonDock.Core`, `Dirkster.AvalonDock.Mvvm*`, `Dirkster.AvalonDock.DependencyInjection`, and `Dirkster.AvalonDock.Serializer.Xml` via `netstandard2.0` without a WPF dependency.

---

## Upgrading from v4?

If you are migrating from AvalonDock v4.x, read the [Migration Guide]({{ site.baseurl }}{% link migration/index.md %}) and [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) — both explicitly label which changes originated in **Upstream `v5.0.0`–`v5.1.0`** vs. **`NTLDR2 Fork v5.1.0`**.

---

## License

AvalonDock is licensed under the [Microsoft Public License (Ms-PL)](https://github.com/NTLDR2/AvalonDock/blob/master/LICENSE) (forked from [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock)) and is free for both commercial and open source use.
