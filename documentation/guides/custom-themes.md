---
title: Custom Themes
layout: default
parent: Guides
nav_order: 6
description: "Create custom AvalonDock themes using DictionaryTheme / VsTheme subclasses or XAML ResourceDictionary overrides."
---

# Custom Themes

AvalonDock gives you two clean ways to create a custom theme—ranging from a runtime C# `DictionaryTheme` / `VsTheme` accent override to a reusable compiled XAML `Theme` class with tailored brushes or control templates.

| Approach | Effort | Best When You Want To… |
|:---------|:-------|:-----------------------|
| **1. `DictionaryTheme` / `VsTheme` Subclass in C#** | Low (zero XAML) | Override specific `ComponentResourceKey` brushes dynamically at runtime (e.g., user-chosen accent colors) |
| **2. Custom `Theme` Subclass + XAML ResourceDictionary** | Medium | Ship a reusable XAML theme class across projects or customize control templates |

---

## Approach 1: Dynamic Brush Overrides with `DictionaryTheme`

When you want to start from an existing theme (such as `VS2022DarkTheme` or `Vs2013DarkTheme`) and override brushes in C#—for example, applying a user-picked accent color at runtime—subclass `DictionaryTheme` (or `VsTheme`).

{: .important }
`DictionaryTheme` is an **`abstract`** class (`public abstract class DictionaryTheme : Theme`). Always define a small concrete subclass that passes your `ResourceDictionary` to `base(dictionary)`.

### Example: Dynamic Accent Override on Top of VS 2022 Dark

`Dirkster.AvalonDock.Themes.Type3New` exposes every themeable color and brush as a `ComponentResourceKey` on `AvalonDock.Themes.Type3New.Themes.ResourceKeys`:

```csharp
using System.Windows;
using System.Windows.Media;
using AvalonDock.Controls;
using AvalonDock.Themes;
using AvalonDock.Themes.Type3New;
using VsKeys = AvalonDock.Themes.Type3New.Themes.ResourceKeys;

public sealed class AccentedVsDarkTheme : DictionaryTheme
{
    public AccentedVsDarkTheme(Color accentColor)
        : base(BuildDictionary(accentColor))
    {
    }

    private static ResourceDictionary BuildDictionary(Color accent)
    {
        // 1. Start from the complete VS 2022 Dark dictionary (frozen brushes + VS2022 Generic.xaml)
        var dict = new VS2022DarkTheme().ThemeResourceDictionary;

        // 2. Override accent and active tab/caption keys in place
        var accentBrush = CreateFrozenBrush(accent);
        var whiteBrush = CreateFrozenBrush(Colors.White);
        var previewFill = CreateFrozenBrush(Color.FromArgb(0x66, accent.R, accent.G, accent.B));

        dict[VsKeys.ControlAccentColorKey] = accent;
        dict[VsKeys.ControlAccentBrushKey] = accentBrush;
        dict[VsKeys.DocumentWellTabSelectedActiveBackground] = accentBrush;
        dict[VsKeys.DocumentWellTabSelectedActiveText] = whiteBrush;
        dict[VsKeys.ToolWindowCaptionActiveBackground] = accentBrush;
        dict[VsKeys.ToolWindowCaptionActiveText] = whiteBrush;
        dict[VsKeys.ToolWindowTabSelectedActiveText] = accentBrush;
        dict[VsKeys.AutoHideTabHoveredBorder] = accentBrush;
        dict[VsKeys.AutoHideTabHoveredText] = accentBrush;
        dict[VsKeys.DockingButtonForegroundBrushKey] = accentBrush;
        dict[VsKeys.PreviewBoxBorderBrushKey] = accentBrush;
        dict[VsKeys.PreviewBoxBackgroundBrushKey] = previewFill;

        // Optional: customize ToggleDockingManager sidebar button text
        dict[ToggleDockButton.ForegroundBrushKey] = CreateFrozenBrush(Color.FromRgb(0xD0, 0xD4, 0xDC));

        return dict;
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
```

Apply it at runtime whenever the user picks a new accent color:

```csharp
// Emerald accent on VS 2022 Dark
dockManager.Theme = new AccentedVsDarkTheme(Color.FromRgb(0x10, 0xB9, 0x81));
```

---

## Approach 2: Reusable XAML `Theme` Subclass

To package a custom theme as compiled XAML resources (either inside your application or in a dedicated class library), create a XAML `ResourceDictionary` and a `Theme` subclass returning its pack URI. For rounded modern tabs, merge `/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml` and define `AvalonDock.Themes.Type3New.Themes.ResourceKeys` tokens (or merge **`/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/DarkTheme.xaml`**, `Themes/Metro/Theme.xaml`, or `Themes/Aero/Theme.xaml`).

### Step 1: Create the Theme `ResourceDictionary`

Create `Themes/BrandDarkTheme.xaml` (ensure Build Action is **`Page`**, the default for WPF XAML files):

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:options="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"
                    xmlns:avalonDockControls="clr-namespace:AvalonDock.Controls;assembly=AvalonDock"
                    xmlns:vsKeys="clr-namespace:AvalonDock.Themes.Type3New.Themes;assembly=AvalonDock.Themes.Type3New">

    <!-- 1. Pull in VS 2022 rounded-tab control templates -->
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <!-- 2. Define brand colors & brushes (entries in outer dictionary win over MergedDictionaries) -->
    <Color x:Key="{x:Static vsKeys:ResourceKeys.ControlAccentColorKey}">#FF7C4DFF</Color>
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.ControlAccentBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />

    <!-- Frame & panel surfaces -->
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.Background}"
                     options:Freeze="True" Color="#FF161822" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.TabBackground}"
                     options:Freeze="True" Color="#FF1E2030" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.PanelBorderBrush}"
                     options:Freeze="True" Color="#FF2E3248" />

    <!-- Active document tab & tool window header -->
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.DocumentWellTabSelectedActiveBackground}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.DocumentWellTabSelectedActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.ToolWindowCaptionActiveBackground}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.ToolWindowCaptionActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />

    <!-- Drag-drop indicators & preview box -->
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.DockingButtonForegroundBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.PreviewBoxBorderBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static vsKeys:ResourceKeys.PreviewBoxBackgroundBrushKey}"
                     options:Freeze="True" Color="#447C4DFF" />

    <!-- ToggleDockingManager sidebar button foreground -->
    <SolidColorBrush x:Key="{x:Static avalonDockControls:ToggleDockButton.ForegroundBrushKey}"
                     options:Freeze="True" Color="#FFC8D3F5" />
</ResourceDictionary>
```

### Step 2: Implement the `Theme` Subclass

Replace `MyApp` in the pack URI with your assembly name:

```csharp
using System;
using AvalonDock.Themes;

namespace MyApp.Themes;

public sealed class BrandDarkTheme : Theme
{
    public override string Name => "Brand Dark";

    public override Uri GetResourceUri()
    {
        return new Uri(
            "/MyApp;component/Themes/BrandDarkTheme.xaml",
            UriKind.Relative);
    }
}
```

### Step 3: Apply in XAML or C#

```xml
<avalonDock:DockingManager xmlns:localThemes="clr-namespace:MyApp.Themes">
    <avalonDock:DockingManager.Theme>
        <localThemes:BrandDarkTheme />
    </avalonDock:DockingManager.Theme>
</avalonDock:DockingManager>
```

---

## Key `ResourceKeys` Reference (`AvalonDock.Themes.Type3New`)

`AvalonDock.Themes.Type3New.Themes.ResourceKeys` (and `AvalonDock.Themes.Type2Hybrid.Themes.ResourceKeys`) exposes a comprehensive set of `ComponentResourceKey` properties grouped by UI area:

| UI Area | Key Properties on `ResourceKeys` | Value Type |
|:--------|:---------------------------------|:-----------|
| **Accent** | `ControlAccentColorKey` (`Color`), `ControlAccentBrushKey` (`Brush`) | `Color` / `Brush` |
| **Workspace & Borders** | `Background`, `PanelBorderBrush`, `TabBackground` | `Brush` |
| **Document Tabs** | `DocumentWellTabSelectedActiveBackground`, `DocumentWellTabSelectedActiveText`, `DocumentWellTabSelectedInactiveBackground`, `DocumentWellTabSelectedInactiveText`, `DocumentWellTabUnselectedBackground`, `DocumentWellTabUnselectedText`, `DocumentWellTabUnselectedHoveredBackground`, `DocumentWellTabUnselectedHoveredText` | `Brush` |
| **Document Close & Overflow Buttons** | `DocumentWellOverflowButtonDefaultGlyph`, `DocumentWellOverflowButtonHoveredBackground`, `DocumentWellTabButtonSelectedActiveGlyph`, `DocumentWellTabButtonSelectedActiveHoveredBackground`, … | `Brush` |
| **Tool Window Headers** | `ToolWindowCaptionActiveBackground`, `ToolWindowCaptionActiveGrip`, `ToolWindowCaptionActiveText`, `ToolWindowCaptionInactiveBackground`, `ToolWindowCaptionInactiveGrip`, `ToolWindowCaptionInactiveText` | `Brush` |
| **Tool Window Header Buttons** | `ToolWindowCaptionButtonActiveGlyph`, `ToolWindowCaptionButtonActiveHoveredBackground`, `ToolWindowCaptionButtonInactiveGlyph`, … | `Brush` |
| **Tool Window Tabs** | `ToolWindowTabSelectedActiveBackground`, `ToolWindowTabSelectedActiveText`, `ToolWindowTabSelectedInactiveBackground`, `ToolWindowTabSelectedInactiveText`, `ToolWindowTabUnselectedBackground`, `ToolWindowTabUnselectedText`, `ToolWindowTabUnselectedHoveredBackground`, `ToolWindowTabUnselectedHoveredText` | `Brush` |
| **Auto-Hide Strips** | `AutoHideTabDefaultBackground`, `AutoHideTabDefaultBorder`, `AutoHideTabDefaultText`, `AutoHideTabHoveredBackground`, `AutoHideTabHoveredBorder`, `AutoHideTabHoveredText` | `Brush` |
| **Toggle Sidebar (`ToggleDockingManager`)** | `AvalonDock.Controls.ToggleDockButton.ForegroundBrushKey` | `Brush` |
| **Floating Windows** | `FloatingDocumentWindowBackground`, `FloatingDocumentWindowBorder`, `FloatingToolWindowBackground`, `FloatingToolWindowBorder` | `Brush` |
| **Ctrl+Tab Navigator** | `NavigatorWindowBackground`, `NavigatorWindowForeground`, `NavigatorWindowSelectedBackground`, `NavigatorWindowSelectedText` | `Brush` |
| **Docking Drop Indicators** | `DockingButtonWidthKey` (`double`), `DockingButtonHeightKey` (`double`), `DockingButtonForegroundBrushKey`, `DockingButtonForegroundArrowBrushKey`, `DockingButtonStarBorderBrushKey`, `DockingButtonStarBackgroundBrushKey`, `PreviewBoxBorderBrushKey`, `PreviewBoxBackgroundBrushKey` | `double` / `Brush` |

---

## Controls Styled by AvalonDock Themes

If you build a completely custom theme from scratch (copying `/AvalonDock;component/Themes/generic.xaml` or `/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml`), ensure styles and templates exist for all public docking controls:

| Control (`AvalonDock.Controls`) | Role |
|:--------------------------------|:-----|
| `DockingManager` / `ToggleDockingManager` | Root docking host and optional toggle-sidebar host |
| `ToggleDockButton` | Sidebar toggle button in `ToggleDockingManager` |
| `LayoutDocumentPaneControl` / `DocumentPaneTabPanel` | Document tab well |
| `LayoutAnchorablePaneControl` / `AnchorablePaneTabPanel` | Tool window tab strip and docked container |
| `AnchorablePaneTitle` | Tool window header bar (drag handle, pin, close, menu buttons) |
| `LayoutDocumentTabItem` / `LayoutAnchorableTabItem` | Individual document and tool window tabs |
| `LayoutDocumentControl` / `LayoutAnchorableControl` | Content presenters inside panes |
| `LayoutAutoHideWindowControl` / `AnchorablePaneControlOverlayArea` | Auto-hide flyout window |
| `LayoutGridResizerControl` | Splitter thumb between panes |
| `LayoutDocumentFloatingWindowControl` / `LayoutAnchorableFloatingWindowControl` | Floating OS windows |
| `OverlayWindow` | Drag-and-drop docking target indicators and preview box |
| `NavigatorWindow` | `Ctrl+Tab` switcher popup |

---

## Testing Checklist for Custom Themes

Before shipping a custom theme, verify every visual state:

1. **Active vs. inactive focus:** Click inside an editor, then click inside a tool window—verify active/inactive caption and tab contrast.
2. **Tab hover, close, and overflow:** Open enough documents to trigger the overflow dropdown button and test hover/pressed button states.
3. **Auto-hide and `ToggleDockingManager`:** Unpin a tool window to the edge strip, and test sidebar buttons if your app uses `ToggleDockingManager`.
4. **Floating windows and drag indicators:** Drag a pane out to float, then drag it back over the center/edge drop targets to verify `OverlayWindow` visibility and `PreviewBox` contrast.
5. **`Ctrl+Tab` navigator:** Press `Ctrl+Tab` with multiple documents and tool windows open to check `NavigatorWindow` legibility.

---

## See Also

- [Theming Guide]({{ site.baseurl }}{% link guides/theming.md %}) — Applying and switching built-in themes.
- [VS Theme]({{ site.baseurl }}{% link themes/vs.md %}) — Loading custom `.vstheme`, `.vstheme.gz`, and VS2026 JSON color overrides via `VsTheme`.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — Step-by-step walkthrough building a multi-theme app with custom corporate branding.
