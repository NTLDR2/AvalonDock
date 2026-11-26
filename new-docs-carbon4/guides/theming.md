---
title: Theming
layout: default
parent: Guides
nav_order: 5
description: "Apply and switch between all seven AvalonDock theme packages and GenericTheme in XAML, code-behind, MVVM, and Dependency Injection."
---

# Theming

AvalonDock ships **seven NuGet theme packages** (19 built-in theme variants plus custom `.vstheme` and JSON theme loading) alongside the built-in `GenericTheme` in `Dirkster.AvalonDock`. Every theme supports both `DockingManager` and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) out of the box, and themes can be switched dynamically at runtime without recreating the layout.

---

## Available Theme Packages

Install one or more theme packages alongside `Dirkster.AvalonDock`:

| NuGet Package | Namespace | Built-in Theme Classes | Aesthetic |
|:--------------|:----------|:-----------------------|:----------|
| `Dirkster.AvalonDock.Themes.Arc` | `AvalonDock.Themes` | `ArcDarkTheme`, `ArcLightTheme` | Modern JetBrains Rider / Fleet styling with rounded tabs and semi-transparent overlays |
| `Dirkster.AvalonDock.Themes.VS` | `AvalonDock.Themes.VS` | `VS2026DarkTheme`, `VS2026LightTheme`, `VS2026BlueTheme`, `VS2022DarkTheme`, `VS2022LightTheme`, `VS2022BlueTheme`, `VS2015DarkTheme`, `VS2015LightTheme`, `VS2015BlueTheme`, `VsTheme` | Visual Studio 2015 / 2022 / 2026 styling plus custom `.vstheme`, `.vstheme.gz`, and VS2026 JSON loading |
| `Dirkster.AvalonDock.Themes.VS2013` | `AvalonDock.Themes` | `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` | Flat Visual Studio 2013 styling backed by embedded `.vstheme.gz` palettes |
| `Dirkster.AvalonDock.Themes.VS2010` | `AvalonDock.Themes` | `VS2010Theme` | Classic Visual Studio 2010 blue-gray gradient chrome |
| `Dirkster.AvalonDock.Themes.Expression` | `AvalonDock.Themes` | `ExpressionDarkTheme`, `ExpressionLightTheme` | High-contrast Expression Blend dark charcoal and light palettes |
| `Dirkster.AvalonDock.Themes.Metro` | `AvalonDock.Themes` | `MetroTheme` | Clean, flat Windows 8 / Metro light theme with `#41B1E1` accents |
| `Dirkster.AvalonDock.Themes.Aero` | `AvalonDock.Themes` | `AeroTheme` | Windows 7 Aero subtle glass-inspired gradients |
| `Dirkster.AvalonDock` *(built-in)* | `AvalonDock.Themes` | `GenericTheme` | Default WPF system-color fallback (applied when `Theme` is `null` or explicitly set) |

{: .important }
Pay attention to namespace and casing differences across packages:
- **`Dirkster.AvalonDock.Themes.VS`** places its classes in **`AvalonDock.Themes.VS`** (`VS2026DarkTheme`, `VS2022DarkTheme`, `VS2015DarkTheme`, `VsTheme`).
- All other theme packages place their theme classes in **`AvalonDock.Themes`**.
- `Vs2013DarkTheme`, `Vs2013LightTheme`, and `Vs2013BlueTheme` use lowercase `s`, whereas `VS2010Theme`, `VS2015*Theme`, `VS2022*Theme`, and `VS2026*Theme` use uppercase `VS`.

See the [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) for screenshots, tab-icon support comparison, and package-specific details.

---

## Applying a Theme in XAML

### Standard Theme (`AvalonDock.Themes`)

Reference the theme assembly explicitly in XAML (recommended when referencing one or more theme packages) and assign `DockingManager.Theme`:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:arc="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Arc">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <arc:ArcDarkTheme />
        </avalonDock:DockingManager.Theme>

        <!-- Layout definition -->
    </avalonDock:DockingManager>
</Window>
```

### Visual Studio 2015 / 2022 / 2026 Theme (`AvalonDock.Themes.VS`)

Because `Dirkster.AvalonDock.Themes.VS` uses the `AvalonDock.Themes.VS` CLR namespace, map a `vs` prefix to `clr-namespace:AvalonDock.Themes.VS;assembly=AvalonDock.Themes.VS`:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:vs="clr-namespace:AvalonDock.Themes.VS;assembly=AvalonDock.Themes.VS">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <vs:VS2026DarkTheme />
        </avalonDock:DockingManager.Theme>
    </avalonDock:DockingManager>
</Window>
```

You can also load an external `.vstheme` file directly in XAML using `VsThemeExtension`:

```xml
<avalonDock:DockingManager Theme="{vs:VsTheme Path=Themes/custom.vstheme}" />
```

---

## Applying and Switching Themes at Runtime

### Direct Assignment in Code-Behind

Assigning a new `Theme` instance to `DockingManager.Theme` (or `ToggleDockingManager.Theme`) immediately swaps the active resource dictionary across the docking tree and any open floating windows:

```csharp
using AvalonDock.Themes;
using AvalonDock.Themes.VS;

// Switch to Arc Dark
dockManager.Theme = new ArcDarkTheme();

// Switch to VS2026 Dark
dockManager.Theme = new VS2026DarkTheme();

// Switch to VS2013 Blue
dockManager.Theme = new Vs2013BlueTheme();

// Switch to Expression Dark
dockManager.Theme = new ExpressionDarkTheme();

// Revert to the default WPF system look
dockManager.Theme = new GenericTheme(); // or null
```

### Data Binding in MVVM

`DockingManager.Theme` is a dependency property (`ThemeProperty`), so you can bind it directly to a ViewModel property or a `ComboBox`:

```csharp
using System.Collections.Generic;
using AvalonDock.Themes;
using AvalonDock.Themes.VS;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainViewModel : ObservableObject
{
    public IReadOnlyList<Theme> AvailableThemes { get; } = new Theme[]
    {
        new ArcDarkTheme(),
        new ArcLightTheme(),
        new VS2026DarkTheme(),
        new VS2026LightTheme(),
        new VS2026BlueTheme(),
        new VS2022DarkTheme(),
        new VS2022LightTheme(),
        new VS2022BlueTheme(),
        new VS2015DarkTheme(),
        new VS2015LightTheme(),
        new VS2015BlueTheme(),
        new Vs2013DarkTheme(),
        new Vs2013LightTheme(),
        new Vs2013BlueTheme(),
        new VS2010Theme(),
        new ExpressionDarkTheme(),
        new ExpressionLightTheme(),
        new MetroTheme(),
        new AeroTheme(),
        new GenericTheme()
    };

    [ObservableProperty]
    private Theme _selectedTheme;

    public MainViewModel()
    {
        _selectedTheme = AvailableThemes[0];
    }
}
```

```xml
<DockPanel>
    <ToolBar DockPanel.Dock="Top">
        <TextBlock Text="Theme:" VerticalAlignment="Center" Margin="4,0" />
        <ComboBox ItemsSource="{Binding AvailableThemes}"
                  SelectedItem="{Binding SelectedTheme}"
                  DisplayMemberPath="Name"
                  Width="180" />
    </ToolBar>

    <avalonDock:DockingManager Theme="{Binding SelectedTheme}">
        <!-- Layout -->
    </avalonDock:DockingManager>
</DockPanel>
```

{: .tip }
`Theme.Name` automatically strips the `"Theme"` suffix from the class name (for example, `ArcDarkTheme.Name` returns `"ArcDark"` and `VS2026DarkTheme.Name` returns `"VS2026Dark"`), making `DisplayMemberPath="Name"` work cleanly in selector controls.

### Switching Themes via `IThemeManager` (Dependency Injection)

When using [`Dirkster.AvalonDock.DependencyInjection`]({{ site.baseurl }}{% link guides/dependency-injection.md %}), register themes through `AddAvalonDock` and switch them by name via `IThemeManager` (`AvalonDock.Core`):

```csharp
services.AddAvalonDock(options =>
{
    options.DefaultTheme = new ArcDarkTheme();
    options.RegisterTheme(new ArcLightTheme());
    options.RegisterTheme(new VS2026DarkTheme());
    options.RegisterTheme(new Vs2013DarkTheme());
});
```

Attach `ThemeManager` to your `DockingManager` once on `Loaded`, then switch themes from any injected service or ViewModel without referencing WPF controls:

```csharp
// In MainWindow.Loaded:
if (themeManager is ThemeManager concreteManager)
{
    concreteManager.Attach(dockManager);
    concreteManager.ApplyTheme(options.DefaultTheme?.Name ?? "ArcDark");
}

// In a ViewModel injected with IThemeManager:
_themeManager.ApplyTheme("VS2026Dark"); // case-insensitive match on Theme.Name
```

---

## Syncing Host Window Chrome with the Active Theme

AvalonDock themes style the docking chrome, tabs, tool window headers, splitters, auto-hide strips, toggle sidebars, and floating windows. Your surrounding `Window`, `Menu`, `ToolBar`, `StatusBar`, and panel contents remain under your application's control.

To keep your outer window background synchronized with the active theme, bind `Window.Background` to `DockingManager.Background` or update application-level brush resources when the theme changes:

```xml
<Window Background="{Binding Background, ElementName=dockManager}"
        UseLayoutRounding="True">
    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <arc:ArcDarkTheme />
        </avalonDock:DockingManager.Theme>
        <!-- ... -->
    </avalonDock:DockingManager>
</Window>
```

---

## How the Theme Architecture Works

AvalonDock's theming system is built on two base classes in `AvalonDock.Themes`:

1. **`Theme`** (`public abstract class Theme : DependencyObject, IThemeInfo`):
   - URI-based themes (`ArcDarkTheme`, `ArcLightTheme`, `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `GenericTheme`) override `GetResourceUri()` to return a relative pack `Uri` to a compiled XAML `ResourceDictionary`.
2. **`DictionaryTheme`** (`public abstract class DictionaryTheme : Theme`):
   - Runtime-constructed themes (`VsTheme`, `VS2015*Theme`, `VS2022*Theme`, `VS2026*Theme`, `Vs2013*Theme`) pass a dynamically built `ResourceDictionary` to `base(themeResourceDictionary)` and return `null` from `GetResourceUri()`.
   - When `DockingManager.Theme` changes, AvalonDock removes the previous theme dictionary and merges either `ThemeResourceDictionary` (if `Theme is DictionaryTheme`) or a new `ResourceDictionary { Source = theme.GetResourceUri() }` into the manager and every active [`LayoutFloatingWindowControl`]({{ site.baseurl }}{% link concepts/floating-windows.md %}).

---

## Next Steps

- [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) — Compare all 7 theme packages, tab icon behavior, and visual styles.
- [VS Theme (`.vstheme` & JSON)]({{ site.baseurl }}{% link themes/vs.md %}) — Load Visual Studio `.vstheme` files or VS2026 JSON color overrides.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Build branded themes via JSON palettes, `DictionaryTheme`, or XAML `ResourceKeys` overrides.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — Build an end-to-end runtime theme switcher with persisted preferences and custom corporate branding.
