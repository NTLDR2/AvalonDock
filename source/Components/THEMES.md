# AvalonDock v5.1.0 — Themes Architecture & Inventory Report

Authoritative technical deep dive into the **AvalonDock v5.1.0** theming system across `source/Components/`.

---

## Executive Summary

1. **Standalone Theme Packages (7 Assemblies, 19 Built-in Theme Variants — All `net48` Exclusively, Zero `System.Text.Json`):**
   The seven `AvalonDock.Themes.*` directories under `source/Components/` (`Arc`, `VS`, `VS2013`, `VS2010`, `Expression`, `Metro`, `Aero`) target **`.NET Framework 4.8` (`net48`) exclusively** with **zero external NuGet dependencies** and together deliver **19 concrete `Theme` classes** (plus `VsTheme` and `{vs:VsTheme}` for custom `.vstheme` / `.vstheme.gz` / VS2026 `.json` files).
   > **Fork Note (`NTLDR2 Fork v5.1.0` vs Upstream `v5.0.0`–`v5.1.0`):** Upstream `AvalonDock.Themes.VS` pulled `System.Text.Json` `10.0.12` on `net48` solely to parse Visual Studio 2026 JSON color-override arrays (transitively infecting `AvalonDock.Themes.VS2013` as well). In this fork, [`VsJsonThemeParser.cs`](AvalonDock.Themes.VS/VsJsonThemeParser.cs) is implemented as a self-contained, zero-dependency pure-C# recursive-descent reader using inbox `net48` `mscorlib` types only (throwing `FormatException` with `(Line X, Column Y)` on invalid syntax), and [`VsThemePaletteFactory.cs`](AvalonDock.Themes.VS2013/VsThemePaletteFactory.cs) freezes all ~75 generated `SolidColorBrush` instances (`IsFrozen == true`). (`AvalonDock.Serializer.Json` remains permanently omitted.)

2. **Built-in `GenericTheme` (20th Concrete Theme):**
   The core `AvalonDock` assembly (`Dirkster.AvalonDock`, `net48`) ships **both**:
   - [`GenericTheme`](AvalonDock/Themes/GenericTheme.cs) (`AvalonDock.Themes.GenericTheme`) — a concrete `Theme` class pointing to `/AvalonDock;component/Themes/generic.xaml`.
   - [`Themes/generic.xaml`](AvalonDock/Themes/generic.xaml) (1,763 lines) + 22 raster icons under `AvalonDock/Themes/Generic/Images/` — the WPF default `ThemeInfo` fallback dictionary for every AvalonDock control.

3. **Programmatic Custom Themes (`DictionaryTheme`):**
   [`DictionaryTheme`](AvalonDock/Themes/DictionaryTheme.cs) (`public abstract class DictionaryTheme : Theme`) powers both the built-in `VsTheme` / `Vs2013*Theme` palette builders and user-defined runtime C# themes (for example, merging `Arc` XAML resources and overriding accent `ComponentResourceKey`s in memory).

---

## 1. Complete Inventory of Themes (20 Concrete Classes Across 8 Assemblies)

> **CLR Namespace Note:** `AvalonDock.Themes.VS` places all of its public types (`VS2015*Theme`, `VS2022*Theme`, `VS2026*Theme`, `VsTheme`, `VsThemeParser`, `VsJsonThemeParser`) in **`AvalonDock.Themes.VS`**. Every other built-in theme class across the remaining 7 assemblies (including `Vs2013*Theme`) lives in **`AvalonDock.Themes`**.

| Component Directory | NuGet Package (`net48`) | Concrete `Theme` Class(es) (`Theme.Name`) | Base Class & Namespace | Palette / Resource Mechanism | Assets & Template Highlights |
| :--- | :--- | :--- | :--- | :--- | :--- |
| [`AvalonDock`](AvalonDock/Themes/) *(built-in)* | `Dirkster.AvalonDock` | [`GenericTheme`](AvalonDock/Themes/GenericTheme.cs) (`"Generic"`) | `Theme` (`AvalonDock.Themes`) | `/AvalonDock;component/Themes/generic.xaml` (`SystemColors.*` + `#007ACC` toggle accents) | 22 raster PNGs in `Themes/Generic/Images/`; WPF `DefaultStyleKey` fallback for all controls |
| [`AvalonDock.Themes.Arc`](AvalonDock.Themes.Arc/) | `Dirkster.AvalonDock.Themes.Arc` | [`ArcDarkTheme`](AvalonDock.Themes.Arc/ArcDarkTheme.cs) (`"ArcDark"`), [`ArcLightTheme`](AvalonDock.Themes.Arc/ArcLightTheme.cs) (`"ArcLight"`) | `Theme` (`AvalonDock.Themes`) | [`DarkTheme.xaml`](AvalonDock.Themes.Arc/DarkTheme.xaml), [`LightTheme.xaml`](AvalonDock.Themes.Arc/LightTheme.xaml) → `DarkBrushs.xaml` / `LightBrushs.xaml` + [`ResourceKeys.cs`](AvalonDock.Themes.Arc/Themes/ResourceKeys.cs) *(note spelling `Brushs`)* | **100% vector** ([`IconGeometry.xaml`](AvalonDock.Themes.Arc/Themes/Icons/IconGeometry.xaml), [`OverlayButtons.xaml`](AvalonDock.Themes.Arc/OverlayButtons.xaml), zero PNGs); Rider/Fleet-style title-only document tabs; custom `MenuItem` & `ToggleAnchorablePaneTitle` |
| [`AvalonDock.Themes.VS`](AvalonDock.Themes.VS/) | `Dirkster.AvalonDock.Themes.VS` | `VS2015{Blue,Dark,Light}Theme`, `VS2022{Blue,Dark,Light}Theme`, `VS2026{Blue,Dark,Light}Theme` (+ [`VsTheme`](AvalonDock.Themes.VS/VsTheme.cs)) | `VsTheme : DictionaryTheme` (**`AvalonDock.Themes.VS`**) | Embedded XML `.vstheme` ([`VsThemeParser.cs`](AvalonDock.Themes.VS/VsThemeParser.cs)) + VS2026 `.json` overrides ([`VsJsonThemeParser.cs`](AvalonDock.Themes.VS/VsJsonThemeParser.cs), pure C#) → frozen brushes on [`ResourceKeys.cs`](AvalonDock.Themes.VS/Themes/ResourceKeys.cs) | **100% vector**; two template sets: flat [`Themes/Generic.xaml`](AvalonDock.Themes.VS/Themes/Generic.xaml) (`VS2015*`) and rounded-corner [`Themes/VS2022/Generic.xaml`](AvalonDock.Themes.VS/Themes/VS2022/Generic.xaml) (`VS2022*` & `VS2026*`); `{vs:VsTheme}` XAML extension |
| [`AvalonDock.Themes.VS2013`](AvalonDock.Themes.VS2013/) | `Dirkster.AvalonDock.Themes.VS2013` | [`Vs2013BlueTheme`](AvalonDock.Themes.VS2013/Vs2013BlueTheme.cs) (`"Vs2013Blue"`), [`Vs2013DarkTheme`](AvalonDock.Themes.VS2013/Vs2013DarkTheme.cs) (`"Vs2013Dark"`), [`Vs2013LightTheme`](AvalonDock.Themes.VS2013/Vs2013LightTheme.cs) (`"Vs2013Light"`) | `DictionaryTheme` (`AvalonDock.Themes`) | Embedded GZIP `vs2013*.vstheme.gz` → [`VsThemePaletteFactory.cs`](AvalonDock.Themes.VS2013/VsThemePaletteFactory.cs) (100% frozen brushes on [`ResourceKeys.cs`](AvalonDock.Themes.VS2013/Themes/ResourceKeys.cs)) | **100% vector** ([`IconGeometry.xaml`](AvalonDock.Themes.VS2013/Themes/Icons/IconGeometry.xaml), [`OverlayButtons.xaml`](AvalonDock.Themes.VS2013/OverlayButtons.xaml), [`Themes/Generic.xaml`](AvalonDock.Themes.VS2013/Themes/Generic.xaml)); custom `MenuItemEx` & `ToggleAnchorablePaneTitle` |
| [`AvalonDock.Themes.VS2010`](AvalonDock.Themes.VS2010/) | `Dirkster.AvalonDock.Themes.VS2010` | [`VS2010Theme`](AvalonDock.Themes.VS2010/VS2010Theme.cs) (`"VS2010"`) | `Theme` (`AvalonDock.Themes`) | [`Theme.xaml`](AvalonDock.Themes.VS2010/Theme.xaml) + [`Brushes.xaml`](AvalonDock.Themes.VS2010/Brushes.xaml) (`AvalonDock_ThemeVS2010_BaseColor1`..`34`) | Classic Visual Studio 2010 blue-gold active gradients + PNG dock/pin images |
| [`AvalonDock.Themes.Expression`](AvalonDock.Themes.Expression/) | `Dirkster.AvalonDock.Themes.Expression` | [`ExpressionDarkTheme`](AvalonDock.Themes.Expression/ExpressionDarkTheme.cs) (`"ExpressionDark"`), [`ExpressionLightTheme`](AvalonDock.Themes.Expression/ExpressionLightTheme.cs) (`"ExpressionLight"`) | `Theme` (`AvalonDock.Themes`) | [`DarkTheme.xaml`](AvalonDock.Themes.Expression/DarkTheme.xaml), [`LightTheme.xaml`](AvalonDock.Themes.Expression/LightTheme.xaml) (`AvalonDock_Expression_BaseColor1`..`15`) | Custom curved tab border control ([`SplineBorder.cs`](AvalonDock.Themes.Expression/Controls/SplineBorder.cs)) + PNG icons; places tool-window tabs at the **top** of anchorable panes |
| [`AvalonDock.Themes.Metro`](AvalonDock.Themes.Metro/) | `Dirkster.AvalonDock.Themes.Metro` | [`MetroTheme`](AvalonDock.Themes.Metro/MetroTheme.cs) (`"Metro"`) | `Theme` (`AvalonDock.Themes`) | [`Theme.xaml`](AvalonDock.Themes.Metro/Theme.xaml) + [`Brushes.xaml`](AvalonDock.Themes.Metro/Brushes.xaml) (`AvalonDock_ThemeMetro_BaseColor1`..`7`, `#41B1E1` accent) | Flat Windows 8 light chrome + PNG dock/pin images |
| [`AvalonDock.Themes.Aero`](AvalonDock.Themes.Aero/) | `Dirkster.AvalonDock.Themes.Aero` | [`AeroTheme`](AvalonDock.Themes.Aero/AeroTheme.cs) (`"Aero"`) | `Theme` (`AvalonDock.Themes`) | [`Theme.xaml`](AvalonDock.Themes.Aero/Theme.xaml) + [`Brushes.xaml`](AvalonDock.Themes.Aero/Brushes.xaml) (`ComponentResourceKey`s on [`AeroColors.cs`](AvalonDock.Themes.Aero/AeroColors.cs)) | Custom curved tab border control ([`SplineBorder.cs`](AvalonDock.Themes.Aero/Controls/SplineBorder.cs)) + Aero gradient brushes & PNG icons |

### Feature & Tab Icon Matrix

| Theme | Dark | Light | Blue | Document Tab Icons (`IconSource`) | `Ctrl+Tab` `NavigatorWindow` Icons | `ToggleDockingManager` Native Styling |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Arc** | Yes | Yes | — | Title-only (Rider/Fleet style) | Yes | Yes |
| **VS (2015 / 2022 / 2026)** | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS2013** | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS2010** | — | — | Yes | Yes | Yes | Yes |
| **Expression** | Yes | Yes | — | Yes | Yes | Yes |
| **Metro** | — | Yes | — | Yes | Yes | Yes |
| **Aero** | — | Yes | — | Yes | Yes | Yes |
| **GenericTheme** | System | System | — | Yes | Yes | Yes |

---

## 2. Deep Dive: `GenericTheme` & `Themes/generic.xaml`

[`AvalonDock/Themes/generic.xaml`](AvalonDock/Themes/generic.xaml) serves **three distinct roles** in AvalonDock:

### Role 1: Automatic WPF Default Style Lookup (`DockingManager.Theme == null`)
[`AvalonDock/Properties/AssemblyInfo.cs`](AvalonDock/Properties/AssemblyInfo.cs) declares:

```csharp
[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,
	ResourceDictionaryLocation.SourceAssembly)]
```

When `DockingManager.Theme` is left `null`, WPF automatically resolves every AvalonDock control's `DefaultStyleKey` from `Themes/generic.xaml` at the bottom of the WPF resource lookup chain, styling panes, tabs, floating windows, and menus with `SystemColors.*`.

### Role 2: Explicit `new GenericTheme()` Selection & Resource Precedence
[`GenericTheme.cs`](AvalonDock/Themes/GenericTheme.cs) is a concrete `Theme` subclass returning `/AvalonDock;component/Themes/generic.xaml`:

- **MVVM & DI Selectors:** Gives `ComboBox` theme pickers (`DisplayMemberPath="Name"` → `"Generic"`) and `IThemeManager` a concrete `Theme` object to revert to the default look at runtime.
- **Lookup Precedence Difference vs `Theme = null`:**
  - `Theme = null` leaves `DockingManager.Resources.MergedDictionaries` empty, so implicit styles in `Application.Current.Resources` take precedence over `Themes/generic.xaml`.
  - `Theme = new GenericTheme()` merges `/AvalonDock;component/Themes/generic.xaml` directly into `DockingManager.Resources.MergedDictionaries` (plus open floating and detached windows), placing it **above** `Application.Current.Resources` in WPF resource lookup order.

### Role 3: Structural Fallback Behind Theme Packages (`ToggleDockingManager` & Shared Styles)
Even when an external theme package is active, any style or template not explicitly redefined by that theme falls back to `AvalonDock/Themes/generic.xaml`:

- All 7 theme packages define `ToggleAnchorablePaneControlStyle`, `ToggleDockButton`, `ToggleDockButton.ForegroundBrushKey`, and `ToggleDockingManager` styles.
- **`Arc`, `VS` (`2015`/`2022`/`2026`), and `VS2013`** define their own custom `ToggleAnchorablePaneTitle` style, whereas `VS2010`, `Aero`, `Expression`, and `Metro` inherit the `ToggleAnchorablePaneTitle` template (with `PART_OptionsButton` and `PART_MinimizeButton`) from [`AvalonDock/Themes/generic.xaml`](AvalonDock/Themes/generic.xaml).
- [`ToggleDockingManager.LoadToggleStyle()`](AvalonDock/ToggleDockingManager.cs) loads `ToggleAnchorablePaneControlStyle` directly from `pack://application:,,,/AvalonDock;component/Themes/generic.xaml` into a `[ThreadStatic]` cache whenever an active or custom theme does not provide its own `ToggleAnchorablePaneControlStyle`.

---

## 3. Deep Dive: `AvalonDock.Themes.VS` & `AvalonDock.Themes.VS2013` Runtime Palette Engine

Unlike the five URI-based XAML themes, `AvalonDock.Themes.VS` and `AvalonDock.Themes.VS2013` derive from [`DictionaryTheme`](AvalonDock/Themes/DictionaryTheme.cs) and construct frozen `SolidColorBrush` dictionaries in memory from authentic Visual Studio theme files:

1. **XML `.vstheme` & GZIP `.vstheme.gz` Parsing ([`VsThemeParser.cs`](AvalonDock.Themes.VS/VsThemeParser.cs)):**
   - Auto-detects GZIP magic bytes (`0x1F 0x8B`) via `System.IO.Compression.GZipStream` and parses the `Environment` `<Category>` XML via `System.Xml.Linq.XDocument` into an immutable [`VsThemeColorPalette`](AvalonDock.Themes.VS/VsThemeColorPalette.cs).
2. **Zero-Dependency VS2026 JSON Override Parsing ([`VsJsonThemeParser.cs`](AvalonDock.Themes.VS/VsJsonThemeParser.cs)):**
   - Parses Visual Studio 2026 JSON color-override arrays (`Name`, `Background`, `Foreground`) using a pure-C# recursive-descent `JsonThemeReader` built strictly on `.NET Framework 4.8` inbox `mscorlib` types (zero `System.Text.Json`, zero `Span<T>` / `System.Memory`).
   - Supports `//` and `/* ... */` comments, trailing commas, UTF-8 BOM, `\uXXXX` escapes, case-insensitive keys, and skipping unknown/nested fields; throws `FormatException` with 1-based `(Line X, Column Y)` coordinates on malformed syntax.
   - [`Vs2026ThemeFactory.cs`](AvalonDock.Themes.VS/Vs2026ThemeFactory.cs) layers `Resources/vs2026{blue,dark,light}.json` overrides onto `Resources/vs2022{blue,dark,light}.vstheme` via `VsThemeColorPalette.Merge(...)`.
3. **Frozen `ResourceDictionary` Construction:**
   - [`VsThemeResourceBuilder.cs`](AvalonDock.Themes.VS/VsThemeResourceBuilder.cs) maps palette tokens (including VS2026 independent `EnvironmentHeader*` and `EnvironmentTab*` tokens) onto `AvalonDock.Themes.VS.Themes.ResourceKeys`, freezes every `SolidColorBrush`, and merges either `/AvalonDock.Themes.VS;component/Themes/Generic.xaml` (`VS2015*`) or `/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml` (`VS2022*` & `VS2026*`).
   - [`VsThemePaletteFactory.cs`](AvalonDock.Themes.VS2013/VsThemePaletteFactory.cs) decompresses `Resources/vs2013{blue,dark,light}.vstheme.gz`, freezes all ~75 `SolidColorBrush` entries on `AvalonDock.Themes.VS2013.Themes.ResourceKeys`, and merges `/AvalonDock.Themes.VS2013;component/OverlayButtons.xaml` + `/AvalonDock.Themes.VS2013;component/Themes/Generic.xaml`.

---

## 4. Class Hierarchy & Runtime Theme-Switching Pipeline

```text
AvalonDock.Core.IThemeInfo (netstandard2.0 — Name, ResourceUri)
  └── AvalonDock.Themes.Theme (abstract DependencyObject — Name strips "Theme" suffix, abstract GetResourceUri())
        ├── GenericTheme, ArcDarkTheme, ArcLightTheme, VS2010Theme,
        │   ExpressionDarkTheme, ExpressionLightTheme, MetroTheme, AeroTheme  (URI-based)
        └── DictionaryTheme (abstract — holds in-memory ThemeResourceDictionary; GetResourceUri() => null)
              ├── Vs2013BlueTheme, Vs2013DarkTheme, Vs2013LightTheme  (AvalonDock.Themes)
              └── VsTheme  (AvalonDock.Themes.VS)
                    ├── VS2015BlueTheme, VS2015DarkTheme, VS2015LightTheme
                    ├── VS2022BlueTheme, VS2022DarkTheme, VS2022LightTheme
                    └── VS2026BlueTheme, VS2026DarkTheme, VS2026LightTheme
```

### 5-Target Dictionary Swap on `DockingManager.Theme` Change

Top-level OS windows own separate WPF visual trees and do not inherit `DockingManager.Resources`. Consequently, [`DockingManager.OnThemeChanged`](AvalonDock/DockingManager.cs) removes the previous theme dictionary (by `currentThemeResourceDictionary` reference for `DictionaryTheme`, or matching `r.Source == oldTheme.GetResourceUri()` for URI-based themes) and merges the new dictionary across **five** visual roots:

1. `DockingManager.Resources.MergedDictionaries`
2. Every standalone [`DetachedAnchorableWindow`](AvalonDock/Controls/DetachedAnchorableWindow.cs) (`_detachedAnchorables` via `UpdateThemeResources(oldTheme, newTheme)`)
3. Every tear-off [`LayoutFloatingWindowControl`](AvalonDock/Controls/LayoutFloatingWindowControl.cs) (`_fwList` via `UpdateThemeResources(oldTheme)`)
4. `_navigatorWindow` (`Ctrl+Tab` [`NavigatorWindow`](AvalonDock/Controls/NavigatorWindow.cs))
5. `_overlayWindow` (drag-and-drop [`OverlayWindow`](AvalonDock/Controls/OverlayWindow.cs))

When hosted in [`ToggleDockingManager`](AvalonDock/ToggleDockingManager.cs), `OnThemeChanged` also dispatches `ReapplyThemeStyles()` at `DispatcherPriority.Loaded` to re-resolve `ToggleAnchorablePaneControlStyle`, clear and rebuild the 6 sidebar `ToggleDockButtonBar`s (refreshing vector icon foreground bindings), and preserve open `IToolbox` states across live theme switches.
