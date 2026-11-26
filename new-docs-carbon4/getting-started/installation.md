---
title: Installation
layout: default
parent: Getting Started
nav_order: 1
description: "Install AvalonDock v5 NuGet packages and configure target frameworks in your WPF project."
---

# Installation

AvalonDock v5 is distributed as a modular family of **14 NuGet packages** under the `Dirkster.AvalonDock*` prefix. Install the main `Dirkster.AvalonDock` package for WPF docking controls, then add only the theme, serializer, MVVM, or Dependency Injection packages your application uses.

---

## 1. Install the Main WPF Package

### .NET CLI

```bash
dotnet add package Dirkster.AvalonDock
```

### Package Manager Console

```powershell
Install-Package Dirkster.AvalonDock
```

### PackageReference (`.csproj`)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="5.0.2" />
```

{: .note }
`Dirkster.AvalonDock` automatically brings in `Dirkster.AvalonDock.Core` as a transitive dependency, so you do not need to install `Core` separately in a WPF project.

---

## 2. Install a Theme Package

`Dirkster.AvalonDock` includes a fallback generic Windows theme (`GenericTheme`), and **7 dedicated theme packages** are available:

```bash
# Modern compact IDE theme — Dark & Light variants (recommended for modern apps)
dotnet add package Dirkster.AvalonDock.Themes.Arc

# Visual Studio .vstheme / JSON engine — VS2015, VS2022, and VS2026 (Dark, Light, Blue) + custom palettes
dotnet add package Dirkster.AvalonDock.Themes.VS

# Classic Visual Studio 2013 theme — Dark, Light, Blue (.vstheme.gz based; pulls in Themes.VS)
dotnet add package Dirkster.AvalonDock.Themes.VS2013

# Additional classic & flat themes
dotnet add package Dirkster.AvalonDock.Themes.VS2010
dotnet add package Dirkster.AvalonDock.Themes.Expression
dotnet add package Dirkster.AvalonDock.Themes.Metro
dotnet add package Dirkster.AvalonDock.Themes.Aero
```

---

## 3. Optional Architecture & Persistence Packages

All six extension packages below target **`netstandard2.0`** and have **no WPF dependency**, making them safe to reference directly from cross-platform or UI-agnostic ViewModel and domain projects:

| Area | Install Command | Transitive Dependencies & Purpose |
|:-----|:----------------|:----------------------------------|
| **Core Contracts** | `dotnet add package Dirkster.AvalonDock.Core` | Zero dependencies. UI-agnostic interfaces (`IDockable`, `IRootDock`, `IToolbox`, `IDockLayoutService`), enums, and serialization DTOs for decoupled class libraries. |
| **Serializer (XML)** | `dotnet add package Dirkster.AvalonDock.Serializer.Xml` | Depends on `Core`. Save/restore layouts via `XmlLayoutSerializer` (compatible with legacy v4 XML files). |
| **Serializer (JSON)** | `dotnet add package Dirkster.AvalonDock.Serializer.Json` | Depends on `Core` + `System.Text.Json`. Save/restore compact JSON layouts via `JsonLayoutSerializer`. |
| **MVVM** | `dotnet add package Dirkster.AvalonDock.Mvvm` | Depends only on `Core` (no third-party packages). Provides `DockableBase`, `ToolboxBase`, `DockLayoutService`, and `SideToggleManager`. |
| **MVVM + CommunityToolkit** | `dotnet add package Dirkster.AvalonDock.Mvvm.CommunityToolkit` | Depends on `Core` + `CommunityToolkit.Mvvm`. Provides `ObservableObject`-backed `ObservableDockableBase`, `ObservableToolboxBase`, `ObservableDocument`, and `ObservableTool` for `[ObservableProperty]` and `[RelayCommand]` source generators. |
| **Dependency Injection** | `dotnet add package Dirkster.AvalonDock.DependencyInjection` | Depends on `Core`, `Mvvm`, and `Microsoft.Extensions.DependencyInjection.Abstractions`. Provides `AddDockLayoutService(dock => ...)` builder registration. |

---

## 4. Verify Installation

Add the AvalonDock XML namespace (`https://github.com/Dirkster99/AvalonDock`) to a WPF `Window` or `UserControl`:

```xml
<Window x:Class="MyDockingApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock">

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

| Target Framework | WPF Packages (`AvalonDock`, `Themes.*`) | UI-Agnostic Packages (`Core`, `Mvvm*`, `DI`, `Serializer.*`) | Version Availability |
|:-----------------|:---------------------------------------:|:------------------------------------------------------------:|:---------------------|
| `.NET 11` (`net11.0-windows`) | ✅ | ✅ (via `netstandard2.0`) | `v5.1.0+` |
| `.NET 10` (`net10.0-windows`) | ✅ | ✅ (via `netstandard2.0`) | `v5.0.0+` |
| `.NET 9` (`net9.0-windows`) | ✅ | ✅ (via `netstandard2.0`) | `v5.0.0+` |
| `.NET 8` (`net8.0-windows`) | ✅ | ✅ (via `netstandard2.0`) | `v5.0.2+` |
| `.NET Framework 4.8` (`net48`) | ✅ | ✅ (via `netstandard2.0`) | `v5.0.0+` |
| `.NET Standard 2.0` (`netstandard2.0`) | — (requires WPF) | ✅ | `v5.0.0+` |

{: .important }
WPF application projects on .NET 8 or later **must** include the `-windows` target framework suffix (for example, `<TargetFramework>net10.0-windows</TargetFramework>`) and `<UseWPF>true</UseWPF>`.
