---
title: "Tutorial: Styling & Theming"
layout: default
parent: Tutorials
nav_order: 3
description: "Build a WPF docking application with live switching across all seven theme packages, persisted user theme preferences, and a custom corporate theme."
---

# Tutorial: Styling & Theming

In this tutorial you will build a WPF application that switches live across **all seven** AvalonDock theme packages (plus `GenericTheme`), synchronizes the host window chrome with light and dark themes, persists the user's selected theme across restarts, and adds both a XAML-based corporate theme and a JSON-based custom palette theme.

---

## Prerequisites

Create a new WPF application and install `Dirkster.AvalonDock`, `CommunityToolkit.Mvvm`, and the theme packages you want to offer:

```bash
dotnet new wpf -n AvalonDockThemeDemo --framework net10.0-windows
cd AvalonDockThemeDemo

dotnet add package Dirkster.AvalonDock
dotnet add package Dirkster.AvalonDock.Themes.Arc
dotnet add package Dirkster.AvalonDock.Themes.VS
dotnet add package Dirkster.AvalonDock.Themes.VS2013
dotnet add package Dirkster.AvalonDock.Themes.VS2010
dotnet add package Dirkster.AvalonDock.Themes.Expression
dotnet add package Dirkster.AvalonDock.Themes.Metro
dotnet add package Dirkster.AvalonDock.Themes.Aero
dotnet add package CommunityToolkit.Mvvm
```

---

## Step 1: Review All Built-in Theme Classes

Across the seven theme packages and `Dirkster.AvalonDock`, 20 ready-to-use `Theme` classes are available:

| Theme Class | Namespace | NuGet Package | Palette |
|:------------|:----------|:--------------|:--------|
| `ArcDarkTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Arc` | Dark |
| `ArcLightTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Arc` | Light |
| `VS2026DarkTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Dark |
| `VS2026LightTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Light |
| `VS2026BlueTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Blue / Light |
| `VS2022DarkTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Dark |
| `VS2022LightTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Light |
| `VS2022BlueTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Blue / Light |
| `VS2015DarkTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Dark |
| `VS2015LightTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Light |
| `VS2015BlueTheme` | `AvalonDock.Themes.VS` | `Dirkster.AvalonDock.Themes.VS` | Blue / Light |
| `Vs2013DarkTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.VS2013` | Dark |
| `Vs2013LightTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.VS2013` | Light |
| `Vs2013BlueTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.VS2013` | Blue / Light |
| `VS2010Theme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.VS2010` | Blue-Gray |
| `ExpressionDarkTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Expression` | Dark |
| `ExpressionLightTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Expression` | Light |
| `MetroTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Metro` | Light |
| `AeroTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock.Themes.Aero` | Light |
| `GenericTheme` | `AvalonDock.Themes` | `Dirkster.AvalonDock` *(built-in)* | System |

---

## Step 2: Create a Custom Corporate XAML Theme

Before building the theme selector, let's add a custom corporate theme (`CorporateTheme`) built on top of `Dirkster.AvalonDock.Themes.Arc` using verified `AvalonDock.Themes.Arc.Themes.ResourceKeys` overrides.

{: .warning }
Do **not** merge the v4 `DarkBrushs.xaml` or `DarkTheme.xaml` files from `AvalonDock.Themes.VS2013` as shown in older v4 samples—those loose XAML files were replaced by embedded `.vstheme.gz` resources in v5 (`#572`). Use `AvalonDock.Themes.Arc` for XAML dictionary inheritance, or `VsTheme.FromJson` (shown in Step 3) for Visual Studio palette overrides.

### `Themes/BrandOverrides.xaml`

Create a `Themes` folder and add `Themes/BrandOverrides.xaml` containing just your brand's colors and brushes:

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:options="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"
                    xmlns:avalonDockControls="clr-namespace:AvalonDock.Controls;assembly=AvalonDock"
                    xmlns:arcKeys="clr-namespace:AvalonDock.Themes.Arc.Themes;assembly=AvalonDock.Themes.Arc">

    <!-- Corporate Teal Accent -->
    <Color x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentColorKey}">#FF009688</Color>
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentBrushKey}"
                     options:Freeze="True" Color="#FF009688" />

    <!-- Deep Slate Surfaces -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.Background}"
                     options:Freeze="True" Color="#FF141B22" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.TabBackground}"
                     options:Freeze="True" Color="#FF1B242E" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PanelBorderBrush}"
                     options:Freeze="True" Color="#FF293645" />

    <!-- Active Document Tab & Tool Window Caption -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DocumentWellTabSelectedActiveBackground}"
                     options:Freeze="True" Color="#FF009688" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DocumentWellTabSelectedActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ToolWindowCaptionActiveBackground}"
                     options:Freeze="True" Color="#FF009688" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ToolWindowCaptionActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />

    <!-- Hover & Drop Indicators -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.AutoHideTabHoveredBorder}"
                     options:Freeze="True" Color="#FF26A69A" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.AutoHideTabHoveredText}"
                     options:Freeze="True" Color="#FF26A69A" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DockingButtonForegroundBrushKey}"
                     options:Freeze="True" Color="#FF009688" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PreviewBoxBorderBrushKey}"
                     options:Freeze="True" Color="#FF009688" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PreviewBoxBackgroundBrushKey}"
                     options:Freeze="True" Color="#44009688" />

    <!-- ToggleDockingManager Sidebar Button Foreground -->
    <SolidColorBrush x:Key="{x:Static avalonDockControls:ToggleDockButton.ForegroundBrushKey}"
                     options:Freeze="True" Color="#FFB0BEC5" />
</ResourceDictionary>
```

### `Themes/CorporateTheme.xaml`

Create `Themes/CorporateTheme.xaml` merging Arc Dark first and `BrandOverrides.xaml` last (in `MergedDictionaries`, later dictionaries override keys from earlier ones):

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/AvalonDock.Themes.Arc;component/DarkBrushs.xaml" />
        <ResourceDictionary Source="/AvalonDock.Themes.Arc;component/Themes/Generic.xaml" />
        <ResourceDictionary Source="/AvalonDockThemeDemo;component/Themes/BrandOverrides.xaml" />
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>
```

### `Themes/CorporateTheme.cs`

```csharp
using System;
using AvalonDock.Themes;

namespace AvalonDockThemeDemo.Themes;

public sealed class CorporateTheme : Theme
{
    public override string Name => "Corporate Teal";

    public override Uri GetResourceUri()
    {
        return new Uri(
            "/AvalonDockThemeDemo;component/Themes/CorporateTheme.xaml",
            UriKind.Relative);
    }
}
```

---

## Step 3: Add a Custom JSON Palette Option (`VsTheme`)

You can also create a custom Visual Studio 2026-style theme directly from JSON without any XAML file using `VsJsonThemeParser` and `VsTheme`:

```csharp
using System;
using AvalonDock.Themes.VS;

namespace AvalonDockThemeDemo.Themes;

public static class CustomVsPaletteFactory
{
    private const string NordJson = """
        [
          { "Name": "EnvironmentBackground",    "Background": "FF2E3440" },
          { "Name": "EnvironmentHeader",        "Background": "FF3B4252" },
          { "Name": "EnvironmentTab",           "Background": "FF3B4252" },
          { "Name": "EnvironmentTabInactive",   "Background": "FF2E3440" },
          { "Name": "FileTabSelectedBorder",    "Background": "FF88C0D0" },
          { "Name": "FileTabSelectedText",      "Background": "FF2E3440" },
          { "Name": "ToolWindowTabSelectedTab", "Background": "FF88C0D0" },
          { "Name": "ToolWindowBorder",         "Background": "FF434C5E" },
          { "Name": "TitleBarActiveBorder",     "Background": "FF5E81AC" },
          { "Name": "TitleBarActiveText",       "Background": "FFECEFF4" }
        ]
        """;

    public static VsTheme CreateNordTheme()
    {
        var palette = VsJsonThemeParser.Parse(NordJson);
        return new VsTheme(palette);
    }
}
```

---

## Step 4: Build the Theme Catalog and Persistence Model

Create `Models/ThemeOption.cs` to describe each selectable theme along with whether the surrounding application chrome should use dark or light colors:

```csharp
using System;
using System.Collections.Generic;
using AvalonDock.Themes;
using AvalonDock.Themes.VS;
using AvalonDockThemeDemo.Themes;

namespace AvalonDockThemeDemo.Models;

public sealed record ThemeOption(
    string Id,
    string DisplayName,
    bool IsDark,
    Func<Theme> Factory)
{
    public static IReadOnlyList<ThemeOption> All { get; } = new ThemeOption[]
    {
        new("arc-dark",       "Arc Dark",               IsDark: true,  () => new ArcDarkTheme()),
        new("arc-light",      "Arc Light",              IsDark: false, () => new ArcLightTheme()),
        new("vs2026-dark",    "Visual Studio 2026 Dark",  IsDark: true,  () => new VS2026DarkTheme()),
        new("vs2026-light",   "Visual Studio 2026 Light", IsDark: false, () => new VS2026LightTheme()),
        new("vs2026-blue",    "Visual Studio 2026 Blue",  IsDark: false, () => new VS2026BlueTheme()),
        new("vs2022-dark",    "Visual Studio 2022 Dark",  IsDark: true,  () => new VS2022DarkTheme()),
        new("vs2022-light",   "Visual Studio 2022 Light", IsDark: false, () => new VS2022LightTheme()),
        new("vs2022-blue",    "Visual Studio 2022 Blue",  IsDark: false, () => new VS2022BlueTheme()),
        new("vs2015-dark",    "Visual Studio 2015 Dark",  IsDark: true,  () => new VS2015DarkTheme()),
        new("vs2015-light",   "Visual Studio 2015 Light", IsDark: false, () => new VS2015LightTheme()),
        new("vs2015-blue",    "Visual Studio 2015 Blue",  IsDark: false, () => new VS2015BlueTheme()),
        new("vs2013-dark",    "Visual Studio 2013 Dark",  IsDark: true,  () => new Vs2013DarkTheme()),
        new("vs2013-light",   "Visual Studio 2013 Light", IsDark: false, () => new Vs2013LightTheme()),
        new("vs2013-blue",    "Visual Studio 2013 Blue",  IsDark: false, () => new Vs2013BlueTheme()),
        new("vs2010",         "Visual Studio 2010",       IsDark: false, () => new VS2010Theme()),
        new("expr-dark",      "Expression Dark",        IsDark: true,  () => new ExpressionDarkTheme()),
        new("expr-light",     "Expression Light",       IsDark: false, () => new ExpressionLightTheme()),
        new("metro",          "Metro",                  IsDark: false, () => new MetroTheme()),
        new("aero",           "Aero",                   IsDark: false, () => new AeroTheme()),
        new("generic",        "Generic (System)",       IsDark: false, () => new GenericTheme()),
        new("corporate-teal", "Corporate Teal (XAML)",  IsDark: true,  () => new CorporateTheme()),
        new("nord-json",      "Nord Frost (JSON)",      IsDark: true,  CustomVsPaletteFactory.CreateNordTheme)
    };

    public override string ToString() => DisplayName;
}
```

Create `Services/UserSettingsService.cs` to persist the user's choice in `%LOCALAPPDATA%`:

```csharp
using System;
using System.IO;
using System.Text.Json;

namespace AvalonDockThemeDemo.Services;

public sealed class UserSettings
{
    public string ThemeId { get; set; } = "arc-dark";
}

public static class UserSettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AvalonDockThemeDemo",
        "settings.json");

    public static UserSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath))
                       ?? new UserSettings();
            }
        }
        catch
        {
            // Fall back to default settings if settings.json is unreadable
        }

        return new UserSettings();
    }

    public static void Save(UserSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(
            SettingsPath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
```

---

## Step 5: Build the ViewModel and Synchronize Host Chrome

Create `ViewModels/MainViewModel.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Themes;
using AvalonDockThemeDemo.Models;
using AvalonDockThemeDemo.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AvalonDockThemeDemo.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public IReadOnlyList<ThemeOption> Themes => ThemeOption.All;

    [ObservableProperty]
    private ThemeOption _selectedThemeOption;

    [ObservableProperty]
    private Theme _currentTheme;

    public MainViewModel()
    {
        var saved = UserSettingsService.Load();
        var initial = Themes.FirstOrDefault(t =>
            string.Equals(t.Id, saved.ThemeId, StringComparison.OrdinalIgnoreCase))
            ?? Themes[0];

        _selectedThemeOption = initial;
        _currentTheme = initial.Factory();
        ApplyAppChromeResources(initial.IsDark);
    }

    partial void OnSelectedThemeOptionChanged(ThemeOption value)
    {
        if (value is null)
        {
            return;
        }

        CurrentTheme = value.Factory();
        ApplyAppChromeResources(value.IsDark);
        UserSettingsService.Save(new UserSettings { ThemeId = value.Id });
    }

    private static void ApplyAppChromeResources(bool isDark)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return;
        }

        resources["AppToolbarBackground"] = new SolidColorBrush(
            isDark ? Color.FromRgb(0x25, 0x27, 0x29) : Color.FromRgb(0xF3, 0xF3, 0xF3));
        resources["AppEditorBackground"] = new SolidColorBrush(
            isDark ? Color.FromRgb(0x1E, 0x1E, 0x1E) : Colors.White);
        resources["AppForeground"] = new SolidColorBrush(
            isDark ? Color.FromRgb(0xD4, 0xD4, 0xD4) : Color.FromRgb(0x1E, 0x1E, 0x1E));
    }
}
```

---

## Step 6: Wire Up `App.xaml` and `MainWindow.xaml`

Add default application brushes in `App.xaml` so `{DynamicResource}` bindings update live whenever `SelectedThemeOption` changes:

```xml
<Application x:Class="AvalonDockThemeDemo.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <SolidColorBrush x:Key="AppToolbarBackground" Color="#252729" />
        <SolidColorBrush x:Key="AppEditorBackground" Color="#1E1E1E" />
        <SolidColorBrush x:Key="AppForeground" Color="#D4D4D4" />
    </Application.Resources>
</Application>
```

Replace `MainWindow.xaml` with a complete docking layout bound to `MainViewModel`:

```xml
<Window x:Class="AvalonDockThemeDemo.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:vm="clr-namespace:AvalonDockThemeDemo.ViewModels"
        Title="AvalonDock Theme Studio"
        Width="1050" Height="680"
        Background="{Binding Background, ElementName=dockManager}"
        UseLayoutRounding="True">

    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>

    <DockPanel>
        <!-- Top Theme Selector Bar -->
        <Border DockPanel.Dock="Top"
                Background="{DynamicResource AppToolbarBackground}"
                Padding="10,6">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="Active Theme:"
                           Foreground="{DynamicResource AppForeground}"
                           VerticalAlignment="Center"
                           Margin="0,0,8,0" />
                <ComboBox ItemsSource="{Binding Themes}"
                          SelectedItem="{Binding SelectedThemeOption}"
                          DisplayMemberPath="DisplayName"
                          Width="240" />
            </StackPanel>
        </Border>

        <!-- Docking Workspace -->
        <avalonDock:DockingManager x:Name="dockManager"
                                   Theme="{Binding CurrentTheme}">
            <avalonDock:LayoutRoot>
                <avalonDock:LayoutPanel Orientation="Horizontal">

                    <!-- Left Tool Window -->
                    <avalonDock:LayoutAnchorablePane DockWidth="230">
                        <avalonDock:LayoutAnchorable Title="Solution Explorer"
                                                     ContentId="explorer">
                            <TreeView Background="{DynamicResource AppEditorBackground}"
                                      Foreground="{DynamicResource AppForeground}"
                                      BorderThickness="0">
                                <TreeViewItem Header="AvalonDockThemeDemo" IsExpanded="True"
                                              Foreground="{DynamicResource AppForeground}">
                                    <TreeViewItem Header="Themes/CorporateTheme.xaml"
                                                  Foreground="{DynamicResource AppForeground}" />
                                    <TreeViewItem Header="MainWindow.xaml"
                                                  Foreground="{DynamicResource AppForeground}" />
                                </TreeViewItem>
                            </TreeView>
                        </avalonDock:LayoutAnchorable>
                    </avalonDock:LayoutAnchorablePane>

                    <!-- Center Document Well + Bottom Output -->
                    <avalonDock:LayoutPanel Orientation="Vertical">
                        <avalonDock:LayoutDocumentPane>
                            <avalonDock:LayoutDocument Title="Program.cs" ContentId="doc-program">
                                <TextBox Background="{DynamicResource AppEditorBackground}"
                                         Foreground="{DynamicResource AppForeground}"
                                         BorderThickness="0"
                                         FontFamily="Consolas"
                                         FontSize="13"
                                         AcceptsReturn="True"
                                         Text="// Switch themes in the toolbar above, float a pane, or press Ctrl+Tab!" />
                            </avalonDock:LayoutDocument>
                            <avalonDock:LayoutDocument Title="README.md" ContentId="doc-readme">
                                <TextBox Background="{DynamicResource AppEditorBackground}"
                                         Foreground="{DynamicResource AppForeground}"
                                         BorderThickness="0"
                                         FontFamily="Consolas"
                                         FontSize="13"
                                         AcceptsReturn="True"
                                         Text="# Theme Demo&#x0a;Selected theme persists automatically across restarts." />
                            </avalonDock:LayoutDocument>
                        </avalonDock:LayoutDocumentPane>

                        <avalonDock:LayoutAnchorablePane DockHeight="150">
                            <avalonDock:LayoutAnchorable Title="Output" ContentId="output">
                                <TextBox Background="{DynamicResource AppEditorBackground}"
                                         Foreground="{DynamicResource AppForeground}"
                                         BorderThickness="0"
                                         IsReadOnly="True"
                                         FontFamily="Consolas"
                                         Text="Build succeeded — 0 warnings, 0 errors." />
                            </avalonDock:LayoutAnchorable>
                        </avalonDock:LayoutAnchorablePane>
                    </avalonDock:LayoutPanel>

                </avalonDock:LayoutPanel>
            </avalonDock:LayoutRoot>
        </avalonDock:DockingManager>
    </DockPanel>
</Window>
```

---

## Step 7: Run and Verify

Run the application:

```bash
dotnet run
```

1. Switch between **Arc Dark**, **Visual Studio 2026 Dark**, **Visual Studio 2013 Blue**, **Expression Dark**, **Corporate Teal (XAML)**, and **Nord Frost (JSON)**—notice how tabs, headers, splitters, and host surfaces update immediately without losing open documents or docking positions.
2. Drag **Solution Explorer** out into a floating window and switch themes again—floating windows receive the new theme dictionary automatically.
3. Close and relaunch the app—your last chosen theme is restored from `%LOCALAPPDATA%\AvalonDockThemeDemo\settings.json`.

---

## Next Steps

- [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) — Explore visual details and tab-icon behavior across all seven theme packages.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Full `ResourceKeys` reference and `DictionaryTheme` customization patterns.
- [Tutorial: Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}) — Combine persisted theme settings with XML or JSON docking layout save/restore.
