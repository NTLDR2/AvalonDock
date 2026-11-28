---
title: Aero Theme
layout: default
parent: Themes
nav_order: 6
description: "Classic Windows 7 Aero theme with soft gradients and AeroColors resource keys."
---

# Aero Theme (`Type1Legacy`)

`AeroTheme` (shipped in `Dirkster.AvalonDock.Themes.Type1Legacy`) provides classic Windows 7 Aero styling with soft blue-tinted `LinearGradientBrush` headers, curved spline tab borders (`AeroSplineBorder`), and full `IconSource` support on both document and tool window tabs. It fits naturally into traditional enterprise and desktop line-of-business WPF applications using standard Windows controls.

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
| **Theme Class** | `AeroTheme` |
| **Resource Keys Class** | `AvalonDock.Themes.Type1Legacy.AeroColors` |
| **Spline Control** | `AvalonDock.Themes.Type1Legacy.Controls.AeroSplineBorder` |
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
            <avalonDock:AeroTheme />
        </avalonDock:DockingManager.Theme>

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

### Code-Behind

```csharp
using AvalonDock.Themes.Type1Legacy;

dockManager.Theme = new AeroTheme();
```

---

## Pack URIs & `AeroColors` Resource Keys

| Pack URI | Description |
|:---------|:------------|
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Theme.xaml` | Complete Aero theme dictionary returned by `AeroTheme.GetResourceUri()` |
| `/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Brushes.xaml` | Gradient and solid brushes keyed by `AvalonDock.Themes.Type1Legacy.AeroColors` |

`AvalonDock.Themes.Type1Legacy.AeroColors` exposes `ComponentResourceKey` fields (`BaseColor1Key` through `BaseColor17Key` and `BaseColor24Key` through `BaseColor33Key`) so you can override individual brushes in XAML or a `DictionaryTheme` subclass:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:aero="clr-namespace:AvalonDock.Themes.Type1Legacy;assembly=AvalonDock.Themes.Type1Legacy">

    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Theme.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <!-- Override Aero BaseColor1 workspace gradient -->
    <SolidColorBrush x:Key="{x:Static aero:AeroColors.BaseColor1Key}" Color="#FFF5F7FA" />
</ResourceDictionary>
```

---

## See Also

- [Metro Theme]({{ site.baseurl }}{% link themes/metro.md %}) — Clean, flat Windows Modern UI light theme (`Type1Legacy`).
- [VS2010 Theme]({{ site.baseurl }}{% link themes/vs2010.md %}) — Classic Visual Studio 2010 theme (`Type1Legacy`).
- [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) — Compare all three theme category packages and `GenericTheme`.