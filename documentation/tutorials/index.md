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
| [Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) | A runtime theme-switchable app with custom branding | All 5 theme packages (`Arc`, `VS2010`, `Expression`, `Metro`, `Aero`), `ResourceKeys` brush overrides, custom `Theme` & `DictionaryTheme` classes |
| [Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}) | An app that saves and restores its workspace | `XmlLayoutSerializer`, v5 `DockLayout` MVVM callbacks, classic MVVM callbacks, `UnresolvedContentHandling`, layout reset |

---

## Sample Applications in the Repository

Every tutorial corresponds directly to working sample projects under `source/`:

| Sample Project | Target Frameworks | Architecture & Highlights |
|:---------------|:------------------|:--------------------------|
| `AvalonDockCodeApp` | `net48` | Flagship v5 reference IDE: `ToggleDockingManager`, `AvalonDock.Mvvm.CommunityToolkit`, DI, AvalonEdit syntax highlighting, live PowerShell terminal, XML layout save/load, and theme/priority switching. |
| `MVVMTestApp` | `net48` | Classic MVVM using `DocumentsSource` / `AnchorablesSource` and `XmlLayoutSerializer`. |
| `TestApp` | `net48` | Comprehensive classic `DockingManager` feature showcase. |
| `MLibTest` | `net48` | Unified application + docking theming with `Dirkster.MLib`. |
| `ExperimentalTestApp` | `net48` | *(NTLDR2 Fork v5.1.0)* Caliburn.Micro 6.0.15-beta reference: classic `DockingManager` driven by `DockLayout` + `IDockLayoutService`, `SimpleContainer`, conductor-synchronized documents, async close guards, `IEventAggregator`, 6-zone tool placement, `XmlLayoutSerializer`, VS2010 default theme. See the [Caliburn.Micro Integration]({{ site.baseurl }}{% link guides/caliburn-micro.md %}) guide. |
| `WinFormsTestApp` | `net48` | Hosting AvalonDock inside a Windows Forms `ElementHost`. |

Run the v5 reference IDE directly from the repository root:

```powershell
dotnet run --project source/AvalonDockCodeApp --framework net48
```
