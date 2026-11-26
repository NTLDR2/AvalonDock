---
title: Home
layout: home
nav_order: 1
description: "AvalonDock — A powerful WPF docking layout library for building Visual Studio, VS Code, and Rider-like interfaces."
permalink: /
---

# AvalonDock v5.1.0 (NTLDR2 Fork)
{: .fs-9 }

A powerful WPF Document and Tool Window layout container for building Visual Studio, VS Code, and JetBrains Rider-like docking interfaces — hardened for `.NET Framework 4.8` and modern `.NET` with zero `System.Text.Json` dependencies.
{: .fs-6 .fw-300 }

[Get Started]({{ site.baseurl }}{% link getting-started/index.md %}){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View Fork on GitHub](https://github.com/NTLDR2/AvalonDock){: .btn .fs-5 .mb-4 .mb-md-0 .mr-2 }
[Upstream (Dirkster99)](https://github.com/Dirkster99/AvalonDock){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## What is AvalonDock?

AvalonDock is a mature WPF docking layout system for arranging tabbed documents and dockable tool windows. Version 5 modernizes the framework with a modular package architecture, first-class MVVM and `Microsoft.Extensions.DependencyInjection` support, a VS Code / Rider-style `ToggleDockingManager` sidebar control, DTO-based XML layout serialization with zero third-party serializer dependencies, standalone detached top-level tool windows, and modern Arc dark and light themes.

{: .important }
**Scope & Version Attribution (`NTLDR2 Fork v5.1.0` vs. `Upstream v5.0.0`):** This site documents **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0`)**, an opinionated fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock). Upstream `v5.0.0` introduced the modular v5 architecture across 14 NuGet packages (followed by `net8.0-windows` in `v5.0.2` and `net11.0-windows` in `v5.1.0`). **This `NTLDR2 Fork v5.1.0` baseline** prioritizes `.NET Framework 4.8` + WPF, surgically removes the 3 `System.Text.Json`-dependent upstream packages (`Serializer.Json`, `Themes.VS`, `Themes.VS2013`) to ship **11 zero-`System.Text.Json` libraries**, and targets vendored/source consumption (including [NTLDR2/SomaFrameQD](https://github.com/NTLDR2/SomaFrameQD)).

![AvalonDock v5 ToggleDockingManager with Arc Dark Theme]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

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
| **5 Theme Packages + `GenericTheme`** | Modern `Arc` (Dark & Light), `VS2010`, `Expression` (Dark & Light), `Metro`, and `Aero` themes, plus live runtime switching and `DictionaryTheme` customization. |
| **Broad .NET Support** | Targets `.NET 8`, `.NET 9`, `.NET 10`, `.NET 11` *(v5.1+)*, and `.NET Framework 4.8` (Windows 7 SP1 through Windows 11 with zero `System.Text.Json` dependencies), plus `netstandard2.0` core/MVVM/DI/serializer packages. |

---

## Quick Example

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:themes="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Arc">

    <avalonDock:DockingManager>
        <avalonDock:DockingManager.Theme>
            <themes:ArcDarkTheme />
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

This fork builds **11 modular libraries** (all with embedded XML IntelliSense documentation and zero `System.Text.Json` references). Package IDs retain the upstream `Dirkster.AvalonDock*` naming for project compatibility; consume this fork via vendored `ProjectReference` or local `dotnet pack` *(installing from `nuget.org` pulls upstream `Dirkster99` packages)*.

### Core, MVVM & Dependency Injection

| Package | Target | Description |
|:--------|:-------|:------------|
| [Dirkster.AvalonDock](https://www.nuget.org/packages/Dirkster.AvalonDock) | `net8.0-windows`–`net11.0-windows`, `net48` | Main WPF docking controls (`DockingManager`, `ToggleDockingManager`, layout model, generic theme) |
| [Dirkster.AvalonDock.Core](https://www.nuget.org/packages/Dirkster.AvalonDock.Core) | `netstandard2.0` | UI-agnostic interfaces (`IDockable`, `IRootDock`, `IToolbox`, `IDockLayoutService`), enums, and DTO contracts |
| [Dirkster.AvalonDock.Mvvm](https://www.nuget.org/packages/Dirkster.AvalonDock.Mvvm) | `netstandard2.0` | MVVM base classes (`DockableBase`, `ToolboxBase`, `DockLayoutService`, `SideToggleManager`) with zero external dependencies |
| [Dirkster.AvalonDock.Mvvm.CommunityToolkit](https://www.nuget.org/packages/Dirkster.AvalonDock.Mvvm.CommunityToolkit) | `netstandard2.0` | `CommunityToolkit.Mvvm` `ObservableObject`-based classes (`ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDocument`, `ObservableTool`) |
| [Dirkster.AvalonDock.DependencyInjection](https://www.nuget.org/packages/Dirkster.AvalonDock.DependencyInjection) | `netstandard2.0` | `IServiceCollection` builder extensions (`AddDockLayoutService`, `DockLayoutBuilder`, `DockingOptions`, `ToggleDockOptions`) |

### Layout Serializer

| Package | Target | Description |
|:--------|:-------|:------------|
| [Dirkster.AvalonDock.Serializer.Xml](https://www.nuget.org/packages/Dirkster.AvalonDock.Serializer.Xml) | `netstandard2.0` | XML layout persistence (`XmlLayoutSerializer`) with backward-compatible v4 layout loading and zero external dependencies |

### Theme Packages

| Package | Variants | Description |
|:--------|:---------|:------------|
| [Dirkster.AvalonDock.Themes.Arc](https://www.nuget.org/packages/Dirkster.AvalonDock.Themes.Arc) | Dark, Light | Modern compact IDE theme with rounded corners (`ArcDarkTheme`, `ArcLightTheme`) |
| [Dirkster.AvalonDock.Themes.VS2010](https://www.nuget.org/packages/Dirkster.AvalonDock.Themes.VS2010) | Single | Classic Visual Studio 2010 theme (`VS2010Theme`) |
| [Dirkster.AvalonDock.Themes.Expression](https://www.nuget.org/packages/Dirkster.AvalonDock.Themes.Expression) | Dark, Light | Expression Blend-inspired dark and light themes (`ExpressionDarkTheme`, `ExpressionLightTheme`) |
| [Dirkster.AvalonDock.Themes.Metro](https://www.nuget.org/packages/Dirkster.AvalonDock.Themes.Metro) | Single | Flat Metro / WinUI style theme (`MetroTheme`) |
| [Dirkster.AvalonDock.Themes.Aero](https://www.nuget.org/packages/Dirkster.AvalonDock.Themes.Aero) | Single | Classic Windows Aero theme (`AeroTheme`) |

---

## Platform Support

| Target Framework | Status | Introduced In / Scope |
|:-----------------|:-------|:----------------------|
| `.NET Framework 4.8` (`net48`) | ✅ Supported | **Primary prioritized target in `NTLDR2 Fork v5.1.0`** (supported since `Upstream v5.0.0`, Win7 SP1 64-bit – Win11) |
| `.NET 11` (`net11.0-windows`) | ✅ Supported | Added in `Upstream v5.1.0` & included in `NTLDR2 Fork v5.1.0` |
| `.NET 10` (`net10.0-windows`) | ✅ Supported | Supported since `Upstream v5.0.0` |
| `.NET 9` (`net9.0-windows`) | ✅ Supported | Supported since `Upstream v5.0.0` |
| `.NET 8` (`net8.0-windows`) | ✅ Supported | Added in `Upstream v5.0.2` (`#673`) |
| `.NET Standard 2.0` (`netstandard2.0`) | ✅ Supported | UI-agnostic packages (`Core`, `Mvvm*`, `DependencyInjection`, `Serializer.Xml`) since `Upstream v5.0.0` |

{: .note }
`Dirkster.AvalonDock` and `Dirkster.AvalonDock.Themes.*` are WPF packages and run on Windows (`-windows` TFM on .NET 8+). Your UI-agnostic ViewModel and service projects can reference `Dirkster.AvalonDock.Core`, `Dirkster.AvalonDock.Mvvm*`, `Dirkster.AvalonDock.DependencyInjection`, and `Dirkster.AvalonDock.Serializer.Xml` via `netstandard2.0` without a WPF dependency.

---

## Upgrading from v4?

If you are migrating from AvalonDock v4.x, read the [Migration Guide]({{ site.baseurl }}{% link migration/index.md %}) and [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) — both explicitly label which changes originated in **Upstream `v5.0.0`–`v5.1.0`** vs. **`NTLDR2 Fork v5.1.0`**.

---

## License

AvalonDock is licensed under the [Microsoft Public License (Ms-PL)](https://github.com/NTLDR2/AvalonDock/blob/master/LICENSE) (forked from [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock)) and is free for both commercial and open source use.
