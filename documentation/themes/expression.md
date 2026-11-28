---
title: Expression Theme
layout: default
parent: Themes
nav_order: 4
description: "Expression Blend-inspired Dark and Light themes for creative, media, and design tools."
---

# Expression Theme (`Type1Legacy`)

`ExpressionDarkTheme` and `ExpressionLightTheme` (shipped in `Dirkster.AvalonDock.Themes.Type1Legacy`) deliver the high-contrast charcoal **Dark** and neutral **Light** styling inspired by Microsoft Expression Blend. They are well suited for creative tools, 3D viewports, node editors, timelines, and inspection panels, and render `IconSource` icons directly in both document and tool window tabs.

---

## Installation

```bash
dotnet add package Dirkster.AvalonDock.Themes.Type1Legacy
```

| Property | Value |
|:---------|:------|
| **NuGet Package** | `Dirkster.AvalonDock.Themes.Type1Legacy` |
| **Assembly** | `AvalonDock.Themes.Type1Legacy` |
| **CLR Namespace** | `AvalonDock.Themes.Type1Legacy` |
| **Theme Classes** | `ExpressionDarkTheme`, `ExpressionLightTheme` |
| **Spline Control** | `AvalonDock.Themes.Type1Legacy.Controls.ExpressionSplineBorder` |
| **Document Tab Icons** | Supported (`IconSource` displayed directly in tab headers) |

---

## Usage

### XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <avalonDock:ExpressionDarkTheme />
            <!-- Or: <avalonDock:ExpressionLightTheme /> -->
        </avalonDock:DockingManager.Theme>

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

### Code-Behind

```csharp
using AvalonDock.Themes.Type1Legacy;

dockManager.Theme = new ExpressionDarkTheme();
// Switch to Expression Light:
dockManager.Theme = new ExpressionLightTheme();
```

---

## Pack URIs & Notes

| Pack URI | Description |
|:---------|:------------|
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/DarkTheme.xaml` | Dark variant returned by `ExpressionDarkTheme.GetResourceUri()` |
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/LightTheme.xaml` | Light variant returned by `ExpressionLightTheme.GetResourceUri()` |
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/DarkBrushes.xaml` | Charcoal brush palette (`AvalonDock_Expression_BaseColor1`.. keys) |
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/LightBrushes.xaml` | Light brush palette (`AvalonDock_Expression_BaseColor1`.. keys) |
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/Theme.xaml` | Shared control templates merged by `DarkTheme.xaml` and `LightTheme.xaml` |

{: .note }
AvalonDock v5 includes updated docking target overlay hints (`#494`, `#634`) and native [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebar support (`#636`) in both `ExpressionDarkTheme` and `ExpressionLightTheme`.

---

## See Also

- [VS Theme]({{ site.baseurl }}{% link themes/vs.md %}) — Visual Studio 2015, 2022, and 2026 themes (`Type3New`).
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Customize brushes or build your own `Theme` subclass.