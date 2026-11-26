---
title: Expression Theme
layout: default
parent: Themes
nav_order: 3
description: "Expression Blend-inspired Dark and Light themes for creative, media, and design tools."
---

# Expression Theme

`Dirkster.AvalonDock.Themes.Expression` delivers the high-contrast charcoal **Dark** and neutral **Light** styling inspired by Microsoft Expression Blend. It is well suited for creative tools, 3D viewports, node editors, timelines, and inspection panels, and renders `IconSource` icons directly in both document and tool window tabs.

---

## Installation

```bash
dotnet add package Dirkster.AvalonDock.Themes.Expression
```

| Property | Value |
|:---------|:------|
| **NuGet Package** | `Dirkster.AvalonDock.Themes.Expression` |
| **Assembly** | `AvalonDock.Themes.Expression` |
| **CLR Namespace** | `AvalonDock.Themes` |
| **Theme Classes** | `ExpressionDarkTheme`, `ExpressionLightTheme` |
| **Document Tab Icons** | Supported (`IconSource` displayed directly in tab headers) |

---

## Usage

### XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:expr="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Expression">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <expr:ExpressionDarkTheme />
            <!-- Or: <expr:ExpressionLightTheme /> -->
        </avalonDock:DockingManager.Theme>

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

### Code-Behind

```csharp
using AvalonDock.Themes;

dockManager.Theme = new ExpressionDarkTheme();
// Switch to Expression Light:
dockManager.Theme = new ExpressionLightTheme();
```

---

## Pack URIs & Notes

| Pack URI | Description |
|:---------|:------------|
| `/AvalonDock.Themes.Expression;component/DarkTheme.xaml` | Dark variant returned by `ExpressionDarkTheme.GetResourceUri()` |
| `/AvalonDock.Themes.Expression;component/LightTheme.xaml` | Light variant returned by `ExpressionLightTheme.GetResourceUri()` |
| `/AvalonDock.Themes.Expression;component/DarkBrushes.xaml` | Charcoal brush palette (`AvalonDock_Expression_BaseColor1`.. keys) |
| `/AvalonDock.Themes.Expression;component/LightBrushes.xaml` | Light brush palette (`AvalonDock_Expression_BaseColor1`.. keys) |
| `/AvalonDock.Themes.Expression;component/Theme.xaml` | Shared control templates merged by `DarkTheme.xaml` and `LightTheme.xaml` |

{: .note }
AvalonDock v5 includes updated docking target overlay hints (`#494`, `#634`) and native [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) sidebar support (`#636`) in both `ExpressionDarkTheme` and `ExpressionLightTheme`.

---

## See Also

- [Arc Theme]({{ site.baseurl }}{% link themes/arc.md %}) — Modern compact dark and light themes.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Customize brushes or build your own `Theme` subclass.
