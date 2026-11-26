---
title: Custom Themes
layout: default
parent: Guides
nav_order: 6
description: "Create custom AvalonDock themes using VS2026 JSON palettes, DictionaryTheme subclasses, or Arc XAML ResourceKeys overrides."
---

# Custom Themes

AvalonDock gives you three progressively deeper ways to create a custom theme—ranging from a 10-line JSON color override file to a reusable custom `Theme` package with tailored control templates.

| Approach | Effort | Best When You Want To… |
|:---------|:-------|:-----------------------|
| **1. VS2026 JSON or `.vstheme` Palette (`VsTheme`)** | Lowest (zero XAML) | Re-color headers, tabs, borders, and accents on top of Visual Studio 2015/2022/2026 templates |
| **2. `DictionaryTheme` Subclass in C#** | Low | Override specific `ComponentResourceKey` brushes dynamically at runtime (e.g., user-chosen accent colors) |
| **3. Custom `Theme` Subclass + XAML ResourceDictionary** | Medium | Ship a reusable XAML theme class across projects or customize control templates |

---

## Approach 1: Custom Palette via VS2026 JSON or `.vstheme` (Zero XAML)

The fastest way to build a custom dark or light theme without writing any WPF XAML is [`VsTheme`]({{ site.baseurl }}{% link themes/vs.md %}) from `Dirkster.AvalonDock.Themes.VS`.

### Layering a JSON Override on Top of VS2022 / VS2026 Templates

Create a JSON file (for example, `brand-dark.json`) listing only the Visual Studio color tokens you want to override in 8-digit `AARRGGBB` hex format:

```json
[
  { "Name": "EnvironmentBackground",   "Background": "FF181A1F" },
  { "Name": "EnvironmentHeader",       "Background": "FF21252B" },
  { "Name": "EnvironmentTab",          "Background": "FF21252B" },
  { "Name": "EnvironmentTabInactive",  "Background": "FF1B1D23" },
  { "Name": "FileTabSelectedBorder",   "Background": "FF00B4D8" },
  { "Name": "FileTabSelectedText",     "Background": "FFFFFFFF" },
  { "Name": "ToolWindowTabSelectedTab","Background": "FF00B4D8" },
  { "Name": "ToolWindowBorder",        "Background": "FF2C313A" },
  { "Name": "TitleBarActiveBorder",    "Background": "FF00B4D8" },
  { "Name": "TitleBarActiveText",      "Background": "FFFFFFFF" }
]
```

Load it with `VsTheme.FromJson(...)`, optionally specifying the VS2022 rounded-tab `Generic.xaml` template dictionary:

```csharp
using System;
using AvalonDock.Themes.VS;

var vs2022Templates = new Uri(
    "/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml",
    UriKind.Relative);

dockManager.Theme = VsTheme.FromJson(
    filePath: "Themes/brand-dark.json",
    basePalette: null,
    genericXamlUri: vs2022Templates);
```

{: .tip }
If you already have a Visual Studio `.vstheme` file exported from the Visual Studio Color Theme Designer (or a `.vstheme.gz` compressed stream), pass it directly to `new VsTheme("Themes/custom.vstheme")` or `{vs:VsTheme Path=Themes/custom.vstheme}` in XAML. See [VS Theme]({{ site.baseurl }}{% link themes/vs.md %}) for all `VsTheme` and `VsThemeColorPalette` overloads.

---

## Approach 2: Dynamic Brush Overrides with `DictionaryTheme`

When you want to start from an existing theme (such as `ArcDarkTheme` or `Vs2013DarkTheme`) and override brushes in C#—for example, applying a user-picked accent color at runtime—subclass `DictionaryTheme`.

{: .important }
`DictionaryTheme` is an **`abstract`** class (`public abstract class DictionaryTheme : Theme`). Always define a small concrete subclass that passes your `ResourceDictionary` to `base(dictionary)`.

### Example: Dynamic Accent Override on Top of Arc Dark

`Dirkster.AvalonDock.Themes.Arc` exposes every themeable color and brush as a `ComponentResourceKey` on `AvalonDock.Themes.Arc.Themes.ResourceKeys`:

```csharp
using System;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Controls;
using AvalonDock.Themes;
using ArcKeys = AvalonDock.Themes.Arc.Themes.ResourceKeys;

public sealed class AccentedArcDarkTheme : DictionaryTheme
{
    public AccentedArcDarkTheme(Color accentColor)
        : base(BuildDictionary(accentColor))
    {
    }

    private static ResourceDictionary BuildDictionary(Color accent)
    {
        var dict = new ResourceDictionary();

        // 1. Merge the complete Arc Dark base theme (brushes + control templates)
        dict.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/AvalonDock.Themes.Arc;component/DarkTheme.xaml", UriKind.Relative)
        });

        // 2. Override accent and active tab/caption keys AFTER MergedDictionaries
        //    (local dictionary entries take precedence over MergedDictionaries in WPF)
        var accentBrush = CreateFrozenBrush(accent);
        var whiteBrush = CreateFrozenBrush(Colors.White);
        var previewFill = CreateFrozenBrush(Color.FromArgb(0x66, accent.R, accent.G, accent.B));

        dict[ArcKeys.ControlAccentColorKey] = accent;
        dict[ArcKeys.ControlAccentBrushKey] = accentBrush;
        dict[ArcKeys.DocumentWellTabSelectedActiveBackground] = accentBrush;
        dict[ArcKeys.DocumentWellTabSelectedActiveText] = whiteBrush;
        dict[ArcKeys.ToolWindowCaptionActiveBackground] = accentBrush;
        dict[ArcKeys.ToolWindowCaptionActiveText] = whiteBrush;
        dict[ArcKeys.ToolWindowTabSelectedActiveText] = accentBrush;
        dict[ArcKeys.AutoHideTabHoveredBorder] = accentBrush;
        dict[ArcKeys.AutoHideTabHoveredText] = accentBrush;
        dict[ArcKeys.DockingButtonForegroundBrushKey] = accentBrush;
        dict[ArcKeys.PreviewBoxBorderBrushKey] = accentBrush;
        dict[ArcKeys.PreviewBoxBackgroundBrushKey] = previewFill;

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
// Emerald accent on Arc Dark
dockManager.Theme = new AccentedArcDarkTheme(Color.FromRgb(0x10, 0xB9, 0x81));
```

---

## Approach 3: Reusable XAML `Theme` Subclass

To package a custom theme as compiled XAML resources (either inside your application or in a dedicated class library), create a XAML `ResourceDictionary` and a `Theme` subclass returning its pack URI.

{: .warning }
In AvalonDock v5, `Dirkster.AvalonDock.Themes.VS2013` migrated from loose XAML brush files (`DarkBrushs.xaml` and `DarkTheme.xaml`) to embedded `.vstheme.gz` resources (`#572`). Do **not** merge those removed `AvalonDock.Themes.VS2013` XAML files—they no longer exist and will throw `IOException` at runtime. For XAML-merged custom themes, base your dictionary on **`AvalonDock.Themes.Arc`** (`DarkBrushs.xaml` + `Themes/Generic.xaml`), **`AvalonDock.Themes.Expression`**, **`AvalonDock.Themes.Metro`**, or **`AvalonDock.Themes.Aero`**.

### Step 1: Create the Theme `ResourceDictionary`

Create `Themes/BrandDarkTheme.xaml` (ensure Build Action is **`Page`**, the default for WPF XAML files):

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:options="http://schemas.microsoft.com/winfx/2006/xaml/presentation/options"
                    xmlns:avalonDockControls="clr-namespace:AvalonDock.Controls;assembly=AvalonDock"
                    xmlns:arcKeys="clr-namespace:AvalonDock.Themes.Arc.Themes;assembly=AvalonDock.Themes.Arc">

    <!-- 1. Pull in Arc Dark brushes and control templates -->
    <ResourceDictionary.MergedDictionaries>
        <ResourceDictionary Source="/AvalonDock.Themes.Arc;component/DarkBrushs.xaml" />
        <ResourceDictionary Source="/AvalonDock.Themes.Arc;component/Themes/Generic.xaml" />
    </ResourceDictionary.MergedDictionaries>

    <!-- 2. Override brand colors & brushes (entries in outer dictionary win over MergedDictionaries) -->
    <Color x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentColorKey}">#FF7C4DFF</Color>
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ControlAccentBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />

    <!-- Frame & panel surfaces -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.Background}"
                     options:Freeze="True" Color="#FF161822" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.TabBackground}"
                     options:Freeze="True" Color="#FF1E2030" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PanelBorderBrush}"
                     options:Freeze="True" Color="#FF2E3248" />

    <!-- Active document tab & tool window header -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DocumentWellTabSelectedActiveBackground}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DocumentWellTabSelectedActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ToolWindowCaptionActiveBackground}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.ToolWindowCaptionActiveText}"
                     options:Freeze="True" Color="#FFFFFFFF" />

    <!-- Drag-drop indicators & preview box -->
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.DockingButtonForegroundBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PreviewBoxBorderBrushKey}"
                     options:Freeze="True" Color="#FF7C4DFF" />
    <SolidColorBrush x:Key="{x:Static arcKeys:ResourceKeys.PreviewBoxBackgroundBrushKey}"
                     options:Freeze="True" Color="#447C4DFF" />

    <!-- ToggleDockingManager sidebar button foreground -->
    <SolidColorBrush x:Key="{x:Static avalonDockControls:ToggleDockButton.ForegroundBrushKey}"
                     options:Freeze="True" Color="#FFC8D3F5" />
</ResourceDictionary>
```

{: .note }
Notice the pack URI spelling in `AvalonDock.Themes.Arc`: `DarkBrushs.xaml` and `LightBrushs.xaml` (without an `e`). Merging `DarkTheme.xaml` directly is equivalent to merging `DarkBrushs.xaml` followed by `Themes/Generic.xaml`.

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

## Key `ResourceKeys` Reference (`Arc`, `VS`, `VS2013`)

`AvalonDock.Themes.Arc.Themes.ResourceKeys`, `AvalonDock.Themes.VS.Themes.ResourceKeys`, and `AvalonDock.Themes.VS2013.Themes.ResourceKeys` share a consistent set of `ComponentResourceKey` names grouped by UI area:

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

{: .important }
Each assembly's `ComponentResourceKey` is scoped to its own `typeof(ResourceKeys)` (`Arc.Themes.ResourceKeys`, `VS.Themes.ResourceKeys`, or `VS2013.Themes.ResourceKeys`). Always reference the `ResourceKeys` class from the **same** assembly whose `Generic.xaml` control templates you merged.

---

## Controls Styled by AvalonDock Themes

If you build a completely custom theme from scratch (copying `/AvalonDock;component/Themes/generic.xaml` or `/AvalonDock.Themes.Arc;component/Themes/Generic.xaml`), ensure styles and templates exist for all public docking controls:

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
- [Arc Theme]({{ site.baseurl }}{% link themes/arc.md %}) — Pack URIs and `ResourceKeys` details for `Dirkster.AvalonDock.Themes.Arc`.
- [VS Theme]({{ site.baseurl }}{% link themes/vs.md %}) — `.vstheme`, `.vstheme.gz`, and VS2026 JSON theme reference.
- [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}) — Step-by-step walkthrough building a multi-theme app with custom corporate branding.
