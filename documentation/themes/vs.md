---
title: VS Theme
layout: default
parent: Themes
nav_order: 2
description: "Visual Studio 2015, 2022, and 2026 themes plus custom .vstheme, .vstheme.gz, and zero-dependency VS2026 JSON palette loading."
---

# VS Theme (`.vstheme` & VS2026 JSON)

`Dirkster.AvalonDock.Themes.VS` brings authentic Visual Studio 2015, Visual Studio 2022, and Visual Studio 2026 docking themes to AvalonDock, along with a runtime theme engine (`VsTheme`) capable of loading custom `.vstheme` XML files, gzip-compressed `.vstheme.gz` resources, and Visual Studio 2026 JSON override files — targeting `.NET Framework 4.8` (`net48`) exclusively with **zero external NuGet dependencies**.

{: .note }
**Fork Scope (`NTLDR2 Fork v5.1.0` vs. Upstream `v5.0.0`–`v5.1.0`):** Upstream `Dirkster.AvalonDock.Themes.VS` referenced `System.Text.Json` on `net48` to parse VS2026 JSON overrides. In **`NTLDR2 Fork v5.1.0`**, `VsJsonThemeParser` uses a self-contained, zero-dependency pure-C# recursive-descent parser (retaining 100% public API compatibility while throwing `FormatException` with `(Line X, Column Y)` diagnostics on malformed JSON syntax), keeping `net48` completely free of `System.Text.Json` and `System.Memory` assemblies.

---

## Installation

```bash
dotnet add package Dirkster.AvalonDock.Themes.VS
```

| Property | Value |
|:---------|:------|
| **NuGet Package** | `Dirkster.AvalonDock.Themes.VS` |
| **Assembly** | `AvalonDock.Themes.VS` |
| **Target Framework** | `net48` *(zero external NuGet dependencies)* |
| **CLR Namespace** | **`AvalonDock.Themes.VS`** |
| **Resource Keys Class** | `AvalonDock.Themes.VS.Themes.ResourceKeys` |
| **Built-in Variants** | `VS2026DarkTheme`, `VS2026LightTheme`, `VS2026BlueTheme`, `VS2022DarkTheme`, `VS2022LightTheme`, `VS2022BlueTheme`, `VS2015DarkTheme`, `VS2015LightTheme`, `VS2015BlueTheme` |

{: .important }
All types in this package reside in the **`AvalonDock.Themes.VS`** namespace (not `AvalonDock.Themes`). In XAML, always map an explicit `xmlns:vs="clr-namespace:AvalonDock.Themes.VS;assembly=AvalonDock.Themes.VS"` prefix.

---

## Built-in Theme Variants

### Visual Studio 2026 (`VS2026*Theme`)

Visual Studio 2026 introduced Fluent accents and independent color tokens for tool headers and tab strips (`EnvironmentHeader`, `EnvironmentHeaderInactive`, `EnvironmentTab`, `EnvironmentTabInactive`) layered via JSON overrides on top of modern rounded-corner VS2022 control templates (`/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml`):

```csharp
using AvalonDock.Themes.VS;

dockManager.Theme = new VS2026DarkTheme();
dockManager.Theme = new VS2026LightTheme();
dockManager.Theme = new VS2026BlueTheme();
```

### Visual Studio 2022 (`VS2022*Theme`)

Uses VS2022 control templates (`/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml`) with embedded `.vstheme` color palettes:

```csharp
using AvalonDock.Themes.VS;

dockManager.Theme = new VS2022DarkTheme();
dockManager.Theme = new VS2022LightTheme();
dockManager.Theme = new VS2022BlueTheme();
```

### Visual Studio 2015 (`VS2015*Theme`)

Uses classic flat VS2015 control templates (`/AvalonDock.Themes.VS;component/Themes/Generic.xaml`) with embedded `.vstheme` color palettes:

```csharp
using AvalonDock.Themes.VS;

dockManager.Theme = new VS2015DarkTheme();
dockManager.Theme = new VS2015LightTheme();
dockManager.Theme = new VS2015BlueTheme();
```

### Applying Built-in Variants in XAML

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

        <!-- Layout -->
    </avalonDock:DockingManager>
</Window>
```

---

## Loading Custom `.vstheme` and `.vstheme.gz` Files

`VsTheme` extends `DictionaryTheme` and constructs a complete WPF `ResourceDictionary` of frozen `SolidColorBrush` entries at runtime from Visual Studio `Environment` category tokens (including `ToggleDockButton.ForegroundBrushKey` for [`ToggleDockingManager`]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %})).

### From a File Path or Stream

`VsTheme(Stream)` and `VsTheme(string)` automatically detect GZIP compression via magic bytes (`0x1F 0x8B`), so both plain `.vstheme` XML files and compressed `.vstheme.gz` files work seamlessly:

```csharp
using System.IO;
using AvalonDock.Themes.VS;

// From file path
dockManager.Theme = new VsTheme("Themes/solarized-dark.vstheme");

// From stream (.vstheme or .vstheme.gz)
using var stream = File.OpenRead("Themes/solarized-dark.vstheme.gz");
dockManager.Theme = new VsTheme(stream);
```

### From an Embedded `.vstheme.gz` Assembly Resource

Embed compressed themes as `<EmbeddedResource Include="Resources\mytheme.vstheme.gz" />` in your `.csproj` and load them with `VsThemeResourceLoader`:

```csharp
using System.Reflection;
using AvalonDock.Themes.VS;

byte[] gzipBytes = VsThemeResourceLoader.Load(
    Assembly.GetExecutingAssembly(),
    "MyApp.Resources.mytheme.vstheme.gz");

dockManager.Theme = new VsTheme(gzipBytes);
```

### XAML Markup Extension (`VsThemeExtension`)

Load a `.vstheme` file directly in XAML using `{vs:VsTheme Path=...}` (paths may be absolute or relative to the entry assembly directory):

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:vs="clr-namespace:AvalonDock.Themes.VS;assembly=AvalonDock.Themes.VS">

    <avalonDock:DockingManager Theme="{vs:VsTheme Path=Themes/custom.vstheme}" />
</Window>
```

---

## Loading Custom Visual Studio 2026 JSON Overrides

Visual Studio 2026 stores custom themes as flat JSON arrays of override entries (`Name`, optional `Category`, and 8-digit `AARRGGBB` `Background` / `Foreground` hex strings):

```json
[
  { "Name": "EnvironmentBackground",  "Category": "5af241b7-...", "Background": "FF1E222A" },
  { "Name": "EnvironmentHeader",      "Category": "5af241b7-...", "Background": "FF282C34" },
  { "Name": "EnvironmentTab",         "Category": "5af241b7-...", "Background": "FF282C34" },
  { "Name": "FileTabSelectedBorder",  "Category": "5af241b7-...", "Background": "FF61AFEF" }
]
```

Because VS2026 JSON files are override-only, pass an optional `basePalette` (and optionally the VS2022 rounded-tab `Generic.xaml` URI) to `VsTheme.FromJson`:

```csharp
using System;
using AvalonDock.Themes.VS;

var basePalette = VsThemeParser.ParseFile("Themes/vs2022dark.vstheme");
var vs2022Templates = new Uri(
    "/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml",
    UriKind.Relative);

dockManager.Theme = VsTheme.FromJson(
    filePath: "Themes/one-dark-overrides.json",
    basePalette: basePalette,
    genericXamlUri: vs2022Templates);
```

If `basePalette` is `null`, `VsThemeResourceBuilder` supplies sensible dark-theme fallback colors for any omitted tokens.

---

## Public Parsing & Resource Builder API

For advanced scenarios—such as inspecting theme colors to style your own application's `Menu`, `ToolBar`, or `StatusBar`, or merging multiple override layers—use the public parser and builder types in `AvalonDock.Themes.VS`:

| Type | Key Public Members | Purpose |
|:-----|:-------------------|:--------|
| `VsThemeParser` | `Parse(Stream)`, `ParseFile(string)`, `ParseGZip(byte[])` | Parses `.vstheme` / `.vstheme.gz` XML (`Environment` category) into a `VsThemeColorPalette` |
| `VsJsonThemeParser` | `Parse(string)`, `Parse(Stream)`, `ParseFile(string)` | Zero-dependency pure-C# parser for VS2026 JSON color arrays (supports `//` & `/* */` comments, trailing commas, UTF-8 BOM, and skips unknown fields; throws `FormatException` on invalid JSON syntax) |
| `VsThemeColorPalette` | `GetBackground(string)`, `GetForeground(string)`, `GetBackgroundOrDefault(string, Color)`, `GetForegroundOrDefault(string, Color)`, `Merge(VsThemeColorPalette)`, `Contains(string)`, `Count` | Immutable-mergeable lookup table of parsed VS color tokens |
| `VsThemeColorEntry` | `Background` (`Color?`), `Foreground` (`Color?`) | Single token's background and foreground colors |
| `VsThemeResourceLoader` | `Load(Assembly, string)` | Reads an embedded manifest resource into a `byte[]` |
| `VsThemeResourceBuilder` | `Build(VsThemeColorPalette)`, `Build(VsThemeColorPalette, Uri)` | Maps a `VsThemeColorPalette` to `AvalonDock.Themes.VS.Themes.ResourceKeys` and merges `Generic.xaml` |

```csharp
using System.Windows.Media;
using AvalonDock.Themes.VS;

var basePalette = VsThemeParser.ParseFile("Themes/vs2022dark.vstheme");
var userOverrides = VsJsonThemeParser.ParseFile("Themes/user-accent.json");
var merged = basePalette.Merge(userOverrides);

// Reuse parsed colors in your own host window UI:
Color headerBg = merged.GetBackgroundOrDefault("EnvironmentHeader", Color.FromRgb(0x2D, 0x2D, 0x30));
Color accent = merged.GetBackgroundOrDefault("FileTabSelectedBorder", Color.FromRgb(0x00, 0x7A, 0xCC));

// Apply to DockingManager:
dockManager.Theme = new VsTheme(merged);
```

---

## See Also

- [VS2013 Theme]({{ site.baseurl }}{% link themes/vs2013.md %}) — Dedicated Visual Studio 2013 Dark, Light, and Blue themes.
- [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) — Compare JSON palettes, `DictionaryTheme`, and XAML `ResourceKeys` overrides.
