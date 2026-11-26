---
title: Metro Theme
layout: default
parent: Themes
nav_order: 6
description: "Clean, flat Metro light theme with crisp borders and sky-blue accents."
---

# Metro Theme

`Dirkster.AvalonDock.Themes.Metro` provides a clean, flat Modern UI / Windows 8 light theme with crisp single-pixel borders, generous whitespace, `#41B1E1` sky-blue accents, and full `IconSource` tab icon rendering.

---

## Installation

```bash
dotnet add package Dirkster.AvalonDock.Themes.Metro
```

| Property | Value |
|:---------|:------|
| **NuGet Package** | `Dirkster.AvalonDock.Themes.Metro` |
| **Assembly** | `AvalonDock.Themes.Metro` |
| **CLR Namespace** | `AvalonDock.Themes` |
| **Theme Class** | `MetroTheme` |
| **Document Tab Icons** | Supported (`IconSource` displayed directly in tab headers) |

---

## Usage

### XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:metro="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Metro">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <metro:MetroTheme />
        </avalonDock:DockingManager.Theme>

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

### Code-Behind

```csharp
using AvalonDock.Themes;

dockManager.Theme = new MetroTheme();
```

---

## Pack URIs & Compact 7-Brush Palette

`MetroTheme` uses one of the simplest brush dictionaries in AvalonDock—just seven `SolidColorBrush` keys in `/AvalonDock.Themes.Metro;component/Brushes.xaml`—making it very easy to re-accent in a custom XAML `ResourceDictionary`:

| Resource Key (`x:Key`) | Default Color | Role |
|:-----------------------|:--------------|:-----|
| `AvalonDock_ThemeMetro_BaseColor1` | `White` (`#FFFFFFFF`) | Primary surface background |
| `AvalonDock_ThemeMetro_BaseColor2` | `Black` (`#FF000000`) | Primary foreground text |
| `AvalonDock_ThemeMetro_BaseColor3` | `#41B1E1` | Primary accent (active headers, tabs, highlights) |
| `AvalonDock_ThemeMetro_BaseColor4` | `#5EA1C0` | Secondary accent / hover border |
| `AvalonDock_ThemeMetro_BaseColor5` | `#333333` | Inactive tab and caption text |
| `AvalonDock_ThemeMetro_BaseColor6` | `#8C8E94` | Splitter and subtle panel borders |
| `AvalonDock_ThemeMetro_BaseColor7` | `#EEEEEE` | Secondary / inactive header surface |

To override the `#41B1E1` accent, merge `/AvalonDock.Themes.Metro;component/Theme.xaml` and redefine `AvalonDock_ThemeMetro_BaseColor3` and `AvalonDock_ThemeMetro_BaseColor4` in a custom `Theme` or `DictionaryTheme`.

---

## See Also

- [Aero Theme]({{ site.baseurl }}{% link themes/aero.md %}) — Classic Windows Aero light theme.
- [Arc Theme]({{ site.baseurl }}{% link themes/arc.md %}) — Modern Arc Light and Arc Dark themes.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Create reusable custom themes.
