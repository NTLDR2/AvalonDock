---
title: Theming
layout: default
parent: Guides
nav_order: 5
description: "Apply and switch between all three AvalonDock theme category packages and GenericTheme in XAML, code-behind, MVVM, and Dependency Injection."
---

# Theming

AvalonDock ships **three consolidated theme category packages** (17 packaged theme variants plus `VsTheme` custom `.vstheme`/JSON loading) alongside the built-in `GenericTheme` and abstract `Theme` / `DictionaryTheme` contracts in `Dirkster.AvalonDock`. Every theme targets `net48` exclusively, supports both `DockingManager` and [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) out of the box, has zero third-party NuGet dependencies, and can be switched dynamically at runtime without recreating the layout.

---

## Available Theme Category Packages

Install one or more theme category packages alongside `Dirkster.AvalonDock`:

| NuGet Package (`net48`) | CLR Namespace & Direct Base | Built-in Theme Classes | Aesthetic |
|:------------------------|:----------------------------|:-----------------------|:----------|
| `Dirkster.AvalonDock.Themes.Type3New` | `AvalonDock.Themes.Type3New` (`VsTheme : DictionaryTheme`) | `VS2026{Dark,Light,Blue}Theme`, `VS2022{Dark,Light,Blue}Theme`, `VS2015{Dark,Light,Blue}Theme`, `VsTheme` | Visual Studio 2015, 2022, and 2026 themes plus custom `.vstheme` / `.vstheme.gz` / VS2026 JSON palettes |
| `Dirkster.AvalonDock.Themes.Type2Hybrid` | `AvalonDock.Themes.Type2Hybrid` (`DictionaryTheme`) | `Vs2013DarkTheme`, `Vs2013LightTheme`, `Vs2013BlueTheme` | Flat Visual Studio 2013 Dark, Light, and Blue palettes built from embedded `.vstheme.gz` resources |
| `Dirkster.AvalonDock.Themes.Type1Legacy` | `AvalonDock.Themes.Type1Legacy` (`Theme`) | `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme` | Classic URI-based Visual Studio 2010, Expression Blend, Metro, and Windows Aero skins |
| `Dirkster.AvalonDock` *(built-in)* | `AvalonDock.Themes.Type1Legacy` (`Theme`) | `GenericTheme` | Default WPF system-color fallback (applied when `Theme` is `null` or explicitly set) |

{: .note }
In C#, abstract `Theme` and `DictionaryTheme` reside in **`AvalonDock.Themes`**, while concrete themes live in **`AvalonDock.Themes.Type1Legacy`**, **`AvalonDock.Themes.Type2Hybrid`**, and **`AvalonDock.Themes.Type3New`**. In XAML on **`NTLDR2 Fork v5.1.0`**, all four assemblies register `[assembly: XmlnsDefinition]` onto **`https://github.com/NTLDR2/AvalonDock`**, so all 18 built-in themes and `{avalonDock:VsTheme Path=...}` resolve uniformly under `xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"` (`Upstream v5.0.0`–`v5.1.0` used `https://github.com/Dirkster99/AvalonDock` across 7 separate theme packages).

See the [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) for screenshots, tab-icon support comparison, and package-specific details.

---

## Applying a Theme in XAML

Reference the theme assembly in your project and assign `DockingManager.Theme` directly in XAML:

```xml
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock">

    <avalonDock:DockingManager x:Name="dockManager">
        <avalonDock:DockingManager.Theme>
            <avalonDock:VS2026DarkTheme />
        </avalonDock:DockingManager.Theme>

        <!-- Layout definition -->
    </avalonDock:DockingManager>
</Window>
```

---

## Applying and Switching Themes at Runtime

### Direct Assignment in Code-Behind

Assigning a new `Theme` instance to `DockingManager.Theme` (or `ToggleDockingManager.Theme`) immediately swaps the active resource dictionary across the docking tree, any open floating windows, and any detached standalone windows:

```csharp
using AvalonDock.Themes;
using AvalonDock.Themes.Type1Legacy;
using AvalonDock.Themes.Type2Hybrid;
using AvalonDock.Themes.Type3New;

// Switch to Visual Studio 2026 / 2022 / 2015 / 2013 / 2010
dockManager.Theme = new VS2026DarkTheme();
dockManager.Theme = new VS2022LightTheme();
dockManager.Theme = new Vs2013DarkTheme();
dockManager.Theme = new VS2010Theme();

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
using AvalonDock.Themes.Type1Legacy;
using AvalonDock.Themes.Type2Hybrid;
using AvalonDock.Themes.Type3New;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class MainViewModel : ObservableObject
{
    public IReadOnlyList<Theme> AvailableThemes { get; } = new Theme[]
    {
        new VS2026DarkTheme(),
        new VS2026LightTheme(),
        new VS2022DarkTheme(),
        new Vs2013DarkTheme(),
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
`Theme.Name` automatically strips the `"Theme"` suffix from the class name (for example, `ExpressionDarkTheme.Name` returns `"ExpressionDark"` and `VS2026DarkTheme.Name` returns `"VS2026Dark"`), making `DisplayMemberPath="Name"` work cleanly in selector controls.

### Switching Themes via `IThemeManager` (Dependency Injection)

When using [`Dirkster.AvalonDock.DependencyInjection`]({{ site.baseurl }}{% link guides/dependency-injection.md %}), register themes with `ThemeManager` and switch them by name via `IThemeManager` (`AvalonDock.Core`):

```csharp
services.AddSingleton<IThemeManager>(_ => new ThemeManager(new Theme[]
{
    new VS2026DarkTheme(),
    new VS2026LightTheme(),
    new Vs2013DarkTheme(),
    new ExpressionDarkTheme(),
    new VS2010Theme()
}));
```

Attach `ThemeManager` to your `DockingManager` once on `Loaded`, then switch themes from any injected service or ViewModel without referencing WPF controls:

```csharp
// In MainWindow.Loaded:
if (themeManager is ThemeManager concreteManager)
{
    concreteManager.Attach(dockManager);
    concreteManager.ApplyTheme("VS2026Dark");
}

// In a ViewModel injected with IThemeManager:
_themeManager.ApplyTheme("ExpressionDark"); // case-insensitive match on Theme.Name
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
            <avalonDock:VS2026DarkTheme />
        </avalonDock:DockingManager.Theme>
        <!-- ... -->
    </avalonDock:DockingManager>
</Window>
```

---

## How the Theme Architecture Works

AvalonDock's theming system is built on two base classes in `AvalonDock.Themes`:

1. **`Theme`** (`public abstract class Theme : DependencyObject, IThemeInfo`):
   - URI-based themes (`VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `GenericTheme`) override `GetResourceUri()` to return a relative pack `Uri` to a compiled XAML `ResourceDictionary`.
2. **`DictionaryTheme`** (`public abstract class DictionaryTheme : Theme`):
   - Runtime-constructed themes (`VsTheme` / `VS2015*` / `VS2022*` / `VS2026*`, `Vs2013*Theme`, and custom subclasses) pass a dynamically built `ResourceDictionary` to `base(themeResourceDictionary)` and return `null` from `GetResourceUri()`.
   - When `DockingManager.Theme` changes, AvalonDock removes the previous theme dictionary and merges either `ThemeResourceDictionary` (if `Theme is DictionaryTheme`) or a new `ResourceDictionary { Source = theme.GetResourceUri() }` into the manager, every active [`LayoutFloatingWindowControl`]({{ site.baseurl }}{% link concepts/floating-windows.md %}), and every open `DetachedAnchorableWindow`.

---

## Next Steps

- [Themes Gallery]({{ site.baseurl }}{% link themes/index.md %}) — Compare all 3 theme category packages (18 built-in themes), tab icon behavior, and visual styles.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Build branded themes via `DictionaryTheme`, `.vstheme`/JSON palettes, or XAML `ResourceKeys` overrides.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — Build an end-to-end runtime theme switcher with persisted preferences and custom corporate branding.
