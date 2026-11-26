---
title: Tutorials
layout: default
nav_order: 5
has_children: true
description: "Hands-on step-by-step tutorials for building real-world WPF applications with AvalonDock v5."
permalink: /tutorials/
---

# Tutorials

Hands-on, step-by-step tutorials that walk you through building complete WPF applications with AvalonDock v5. Every tutorial contains self-contained, copy-pasteable code verified against the v5 APIs.

{: .note }
These tutorials assume you have completed the [Quick Start]({{ site.baseurl }}{% link getting-started/quick-start.md %}) guide and are comfortable with basic WPF XAML and C#.

---

## Choose a Tutorial

| Tutorial | What You'll Build | Key Topics |
|:---------|:------------------|:-----------|
| [MVVM IDE Application]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}) | A VS Code / Rider-style multi-panel IDE | `ObservableToolboxBase`, `ObservableDocument`, `IDockLayoutService`, `SideToggleManager`, `ToggleDockingManager`, `DockZone`, `Shortcut`, DI |
| [Dependency Injection App]({{ site.baseurl }}{% link tutorials/dependency-injection-app.md %}) | Scalable DI composition root & headless NUnit tests | `AddDockLayoutService` builder, `ConfigureDocking`, `ConfigureToggleDock`, factory toolboxes, `IDisposable` cleanup, NUnit testing |
| [Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) | A runtime theme-switchable app with custom branding | All 7 theme packages (`Arc`, `VS` 2015/2022/2026, `VS2013`, etc.), `ResourceKeys` brush overrides, custom `Theme` classes, `.vstheme` / JSON palettes |
| [Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}) | An app that saves and restores its workspace | `XmlLayoutSerializer` & `JsonLayoutSerializer`, v5 `DockLayout` MVVM callbacks, classic MVVM callbacks, `UnresolvedContentHandling`, layout reset |

---

## Sample Applications in the Repository

Every tutorial corresponds directly to working sample projects under `source/`:

| Sample Project | Target Frameworks | Architecture & Highlights |
|:---------------|:------------------|:--------------------------|
| `AvalonDockCodeApp` | `net10.0-windows`, `net11.0-windows` | Flagship v5 reference IDE: `ToggleDockingManager`, `AvalonDock.Mvvm.CommunityToolkit`, DI, AvalonEdit syntax highlighting, live PowerShell terminal, XML layout save/load, and theme/priority switching. |
| `MVVMTestApp` | `net9.0-windows`, `net48` | Classic MVVM using `DocumentsSource` / `AnchorablesSource` and `XmlLayoutSerializer`. |
| `TestApp` | `net10.0-windows`, `net11.0-windows`, `net48` | Comprehensive classic `DockingManager` feature showcase. |
| `VS2013Test` | `net9.0-windows` | Classic Visual Studio-style multi-tool IDE demo. |
| `MLibTest` | `net9.0-windows`, `net48` | Unified application + docking theming with `Dirkster.MLib`. |
| `CaliburnDockTestApp` | `net9.0-windows`, `net48` | Integration with the Caliburn.Micro MVVM framework. |
| `WinFormsTestApp` | `net9.0-windows`, `net48` | Hosting AvalonDock inside a Windows Forms `ElementHost`. |

Run the v5 reference IDE directly from the repository root:

```powershell
dotnet run --project source/AvalonDockCodeApp --framework net10.0-windows
```
