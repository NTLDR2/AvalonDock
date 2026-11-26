---
title: Arc Theme
layout: default
parent: Themes
nav_order: 1
description: "Modern JetBrains Rider and Fleet-inspired Dark and Light themes for AvalonDock."
---

# Arc Theme

`Dirkster.AvalonDock.Themes.Arc` is AvalonDock's modern flagship theme inspired by JetBrains Rider and Fleet. It features compact tabs with rounded geometry, vector icon glyphs, semi-transparent drag-and-drop docking indicators, and first-class styling for both `DockingManager` and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}).

---

## Installation

```bash
dotnet add package Dirkster.AvalonDock.Themes.Arc
```

| Property | Value |
|:---------|:------|
| **NuGet Package** | `Dirkster.AvalonDock.Themes.Arc` |
| **Assembly** | `AvalonDock.Themes.Arc` |
| **Theme Classes** | `AvalonDock.Themes.ArcDarkTheme`, `AvalonDock.Themes.ArcLightTheme` |
| **Resource Keys Class** | `AvalonDock.Themes.Arc.Themes.ResourceKeys` |
| **Default Accent** | `#FF3574F0` (IntelliJ / Rider Blue) |

---

## Screenshots

### Workspace (Dark & Light)

![Arc Dark Workspace]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

![Arc Light Workspace]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Welcome.png)

### Semi-Transparent Drag & Drop Indicators

![Arc Dark Drag Document Overlay]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Drag_Document.png)

![Arc Light Drag Document Overlay]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Drag_Document.png)

### Toggle Sidebar Flyouts (`ToggleDockingManager`)

![Arc Dark Toolbox Flyout]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Toolbox.png)

![Arc Light Toolbox Flyout]({{ site.baseurl }}/images/AvalonDockCodeApp_Light_Toolbox.png)

---

## Usage

### XAML

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:arc="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Arc">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <arc:ArcDarkTheme />
            <!-- Or: <arc:ArcLightTheme /> -->
        </avalonDock:DockingManager.Theme>

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

### Code-Behind

```csharp
using AvalonDock.Themes;

dockManager.Theme = new ArcDarkTheme();
// Switch to light mode:
dockManager.Theme = new ArcLightTheme();
```

---

## Compiled XAML Pack URIs

Unlike `VS` and `VS2013` (which generate brushes at runtime from `.vstheme` palettes), `AvalonDock.Themes.Arc` ships compiled XAML `ResourceDictionary` files, making it ideal as a base for XAML-merged [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}):

| Pack URI | Contents |
|:---------|:---------|
| `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` | Merges `DarkBrushs.xaml` + `Themes/Generic.xaml` |
| `/AvalonDock.Themes.Arc;component/LightTheme.xaml` | Merges `LightBrushs.xaml` + `Themes/Generic.xaml` |
| `/AvalonDock.Themes.Arc;component/DarkBrushs.xaml` | Dark color and `SolidColorBrush` palette *(note spelling: `Brushs`)* |
| `/AvalonDock.Themes.Arc;component/LightBrushs.xaml` | Light color and `SolidColorBrush` palette *(note spelling: `Brushs`)* |
| `/AvalonDock.Themes.Arc;component/Themes/Generic.xaml` | Control templates for all AvalonDock controls |
| `/AvalonDock.Themes.Arc;component/OverlayButtons.xaml` | Docking target overlay button templates |

---

## Customizing Accent & Surface Colors

Every color and brush in Arc is keyed by `AvalonDock.Themes.Arc.Themes.ResourceKeys`. You can create a custom XAML dictionary that merges `DarkTheme.xaml` (or `LightTheme.xaml`) and overrides just the accent or background keys:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:options="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"
                    xmlns:arcKeys="clr-namespace:AvalonDock.Themes.Arc.Themes;assembly=AvalonDock.Themes.Arc">

    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/AvalonDock.Themes.Arc;component/DarkTheme.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <!-- Replace default #FF3574F0 blue accent with violet -->
    <Color x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentColorKey}">#FF8B5CF6</Color>
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentBrushKey}"
                     options:Freeze="True" Color="#FF8B5CF6" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DocumentWellTabSelectedActiveBackground}"
                     options:Freeze="True" Color="#FF8B5CF6" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ToolWindowCaptionActiveBackground}"
                     options:Freeze="True" Color="#FF8B5CF6" />
</ResourceDictionary>
```

See [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) for the full `ResourceKeys` table and dynamic C# `DictionaryTheme` examples.
