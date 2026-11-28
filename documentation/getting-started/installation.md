---
title: Installation
layout: default
parent: Getting Started
nav_order: 1
description: "Install AvalonDock v5 NuGet packages and configure target frameworks in your WPF project."
---

# Installation

**[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0` fork)** is organized into a modular family of **9 component libraries** under the `Dirkster.AvalonDock*` package prefix, targeting **`.NET Framework 4.8` (`net48`)** for WPF assemblies and **`.NET Standard 2.0` (`netstandard2.0`)** for UI-agnostic libraries, with **zero `System.Text.Json` dependencies**.

{: .important }
**Fork vs. Upstream Distribution & Planned `AvalonDox` Rebrand:**
- **Consuming `NTLDR2 Fork v5.1.0` (Recommended for Zero-`System.Text.Json` / Vendored Hosts):** Reference the projects under `source/Components/*` directly via `ProjectReference` (submodule/vendored source) or pack local `.nupkg` artifacts (`dotnet pack source/AvalonDock.sln -c Release -o ./artifacts/nupkgs`) and install from your local NuGet feed.
- **Upstream `nuget.org` Feed ([Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock)):** Running `dotnet add package Dirkster.AvalonDock*` against `nuget.org` installs upstream packages (where `Upstream v5.0.0` published 14 packages, including `Serializer.Json`, `Themes.Arc`, and `System.Text.Json`-dependent builds of `Themes.VS` / `Themes.VS2013` on `net48`).
- **Planned Rebrand (`AvalonDox`):** Assembly and `<PackageId>` names temporarily remain `Dirkster.AvalonDock*` during initial vendoring stabilization; a full rebrand to **`AvalonDox`** is scheduled on the fork `ROADMAP.md` before any standalone NuGet publication to eliminate package and CLR assembly name collisions.

---

## 1. Reference the Main WPF Library

### Vendored `ProjectReference` (`NTLDR2 Fork v5.1.0`)

```xml
<ProjectReference Include="..\vendor\AvalonDock\source\Components\AvalonDock\AvalonDock.csproj" />
```

### .NET CLI (Local NuGet Feed or Upstream `nuget.org`)

```bash
dotnet add package Dirkster.AvalonDock
```

### Package Manager Console

```powershell
Install-Package Dirkster.AvalonDock
```

### PackageReference (`.csproj`)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="5.1.0" />
```

{: .note }
`Dirkster.AvalonDock` automatically brings in `Dirkster.AvalonDock.Core` as a transitive dependency, so you do not need to install `Core` separately in a WPF project.

---

## 2. Install a Theme Category Package

`Dirkster.AvalonDock` includes a fallback generic Windows theme (`GenericTheme`), and **3 consolidated theme category packages** are available in this fork (all targeting `net48` exclusively with zero external NuGet dependencies):

```bash
# Category 3 (VsTheme : DictionaryTheme): Visual Studio 2015, 2022 & 2026 themes (+ .vstheme / JSON loader)
dotnet add package Dirkster.AvalonDock.Themes.Type3New

# Category 2 (DictionaryTheme): Visual Studio 2013 Dark, Light & Blue themes
dotnet add package Dirkster.AvalonDock.Themes.Type2Hybrid

# Category 1 (Theme URI): Aero, Expression Dark/Light, Metro & VS2010 themes
dotnet add package Dirkster.AvalonDock.Themes.Type1Legacy
```

---

## 3. Optional Architecture & Persistence Packages

All five extension packages below target **`netstandard2.0`** and have **no WPF dependency**, making them safe to reference directly from cross-platform or UI-agnostic ViewModel and domain projects:

| Area | Install Command | Transitive Dependencies & Purpose |
|:-----|:----------------|:----------------------------------|
| **Core Contracts** | `dotnet add package Dirkster.AvalonDock.Core` | Zero dependencies. UI-agnostic interfaces (`IDockable`, `IRootDock`, `IToolbox`, `IDockLayoutService`), enums, and serialization DTOs for decoupled class libraries. |
| **Serializer (XML)** | `dotnet add package Dirkster.AvalonDock.Serializer.Xml` | Depends only on `Core` (zero external dependencies). Save/restore layouts via `XmlLayoutSerializer` (compatible with legacy v4 XML files). |
| **MVVM** | `dotnet add package Dirkster.AvalonDock.Mvvm` | Depends only on `Core` (no third-party packages). Provides `DockableBase`, `ToolboxBase`, `DockLayoutService`, and `SideToggleManager`. |
| **MVVM + CommunityToolkit** | `dotnet add package Dirkster.AvalonDock.Mvvm.CommunityToolkit` | Depends on `Core` + `CommunityToolkit.Mvvm`. Provides `ObservableObject`-backed `ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDocument`, and `ObservableTool` for `[ObservableProperty]` and `[RelayCommand]` source generators. |
| **Dependency Injection** | `dotnet add package Dirkster.AvalonDock.DependencyInjection` | Depends on `Core`, `Mvvm`, and `Microsoft.Extensions.DependencyInjection.Abstractions`. Provides `AddDockLayoutService(dock => ...)` builder registration. |

---

## 4. Verify Installation

Add the AvalonDock XML namespace (`https://github.com/NTLDR2/AvalonDock`) to a WPF `Window` or `UserControl`:

```xml
<Window x:Class="MyDockingApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock">

    <avalonDock:DockingManager>
        <avalonDock:LayoutRoot>
            <avalonDock:LayoutPanel>
                <avalonDock:LayoutDocumentPane />
            </avalonDock:LayoutPanel>
        </avalonDock:LayoutRoot>
    </avalonDock:DockingManager>
</Window>
```

Build the project (`dotnet build`). Once it compiles cleanly, continue to the [Quick Start]({{ site.baseurl }}{% link getting-started/quick-start.md %}) guide.

---

## Target Framework Compatibility

| Target Framework | WPF Libraries (`AvalonDock`, `Themes.*`) | UI-Agnostic Libraries (`Core`, `Mvvm*`, `DI`, `Serializer.Xml`) | Introduced In / Scope |
|:-----------------|:----------------------------------------:|:---------------------------------------------------------------:|:----------------------|
| `.NET Framework 4.8` (`net48`) | ✅ | ✅ (via `netstandard2.0`) | **Exclusive desktop WPF target in `NTLDR2 Fork v5.1.0`** (`Upstream v5.0.0+`) |
| `.NET Standard 2.0` (`netstandard2.0`) | — (requires WPF) | ✅ | `Upstream v5.0.0+` & `NTLDR2 Fork v5.1.0` |
| `.NET 8`–`.NET 11` (`net8.0-windows`..`net11.0-windows`) | Upstream only | ✅ (via `netstandard2.0`) | `NTLDR2 Fork v5.1.0` targets `net48` strictly on WPF assemblies (`Upstream v5.0.2`–`v5.1.0`, `#673`, ships `net8.0-windows`–`net11.0-windows` builds) |

{: .important }
WPF application projects consuming `NTLDR2 Fork v5.1.0` directly target `<TargetFramework>net48</TargetFramework>` with `<UseWPF>true</UseWPF>`.
