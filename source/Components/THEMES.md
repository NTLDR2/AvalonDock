# AvalonDock v5.1.0 — Themes Architecture & Inventory Report

Authoritative technical deep dive into the **AvalonDock v5.1.0** theming system across `source/Components/` (`NTLDR2/AvalonDock` fork).

---

## Executive Summary

1. **Three Consolidated Theme Category Packages (3 Assemblies, 17 Packaged Theme Variants — All `net48` Exclusively, Zero `System.Text.Json`):**
   The three `AvalonDock.Themes.*` directories under `source/Components/` (`AvalonDock.Themes.Type1Legacy`, `AvalonDock.Themes.Type2Hybrid`, and `AvalonDock.Themes.Type3New`; *`Themes.Arc` is removed in the NTLDR2 fork*) target **`.NET Framework 4.8` (`net48`) exclusively** with **zero external NuGet dependencies** and together deliver **17 concrete `Theme` classes** (plus `VsTheme` and `{avalonDock:VsTheme}` for custom `.vstheme` / `.vstheme.gz` / VS2026 `.json` files), aligned 1:1:1 across direct base class, CLR namespace, and compiled assembly.
   > **Fork Note (`NTLDR2 Fork v5.1.0` vs Upstream `v5.0.0`–`v5.1.0`):** Upstream shipped 7 separate theme assemblies, mapped XAML to `https://github.com/Dirkster99/AvalonDock` (omitting `Themes.VS` from `[XmlnsDefinition]`), and pulled `System.Text.Json` `10.0.12` into `AvalonDock.Themes.VS` on `net48`. In this fork:
   > - `AvalonDock.Themes.Arc` is removed (`20 -> 18` built-in themes).
   > - Concrete themes are split into 3 explicit CLR namespaces and consolidated into 3 matching assemblies: `AvalonDock.Themes.Type1Legacy` (`Theme` URI base), `AvalonDock.Themes.Type2Hybrid` (`DictionaryTheme` base), and `AvalonDock.Themes.Type3New` (`VsTheme : DictionaryTheme` base), while abstract `Theme` and `DictionaryTheme` remain in `AvalonDock.Themes`.
   > - All 18 built-in themes and `{avalonDock:VsTheme}` map uniformly to **`https://github.com/NTLDR2/AvalonDock`** via `[assembly: XmlnsDefinition]`.
   > - [`VsJsonThemeParser.cs`](AvalonDock.Themes.Type3New/VsJsonThemeParser.cs) is implemented as a self-contained, zero-dependency pure-C# recursive-descent reader using inbox `net48` `mscorlib` types only, and [`VsThemePaletteFactory.cs`](AvalonDock.Themes.Type2Hybrid/VsThemePaletteFactory.cs) freezes 100% of generated `SolidColorBrush` instances (`IsFrozen == true`, including `MenuKeys` and `ControlAccentColorKey`).

2. **Built-in `GenericTheme` (18th Concrete Theme):**
   The core `AvalonDock` assembly (`Dirkster.AvalonDock`, `net48`) ships **both**:
   - [`GenericTheme`](AvalonDock/Themes/GenericTheme.cs) (`AvalonDock.Themes.Type1Legacy.GenericTheme`) — a concrete `Theme` class pointing to `/AvalonDock;component/Themes/generic.xaml`.
   - [`Themes/generic.xaml`](AvalonDock/Themes/generic.xaml) (1,763 lines) + 22 raster icons under `AvalonDock/Themes/Generic/Images/` — the WPF default `ThemeInfo` fallback dictionary for every AvalonDock control.

3. **Programmatic Custom Themes (`DictionaryTheme` & `VsTheme`):**
   [`DictionaryTheme`](AvalonDock/Themes/DictionaryTheme.cs) (`public abstract class DictionaryTheme : Theme` in `AvalonDock.Themes`) powers both the built-in `Type2Hybrid` (`Vs2013*Theme`) and `Type3New` ([`VsTheme`](AvalonDock.Themes.Type3New/VsTheme.cs)) palette builders as well as user-defined runtime C# themes.

---

## 1. Complete Inventory of Themes (18 Concrete Presets Across 4 Assemblies)

| Component Directory | NuGet Package (`net48`) | Concrete `Theme` Class(es) (`Theme.Name`) | Direct Base Class & CLR Namespace | Palette / Resource Mechanism | Assets & Template Highlights |
| :--- | :--- | :--- | :--- | :--- | :--- |
| [`AvalonDock`](AvalonDock/Themes/) *(built-in core)* | `Dirkster.AvalonDock` | [`GenericTheme`](AvalonDock/Themes/GenericTheme.cs) (`"Generic"`) | `Theme` (`AvalonDock.Themes.Type1Legacy`) | `/AvalonDock;component/Themes/generic.xaml` (`SystemColors.*` + `#007ACC` toggle accents) | 22 raster PNGs in `Themes/Generic/Images/`; WPF `DefaultStyleKey` fallback for all controls |
| [`AvalonDock.Themes.Type1Legacy`](AvalonDock.Themes.Type1Legacy/) | `Dirkster.AvalonDock.Themes.Type1Legacy` | [`AeroTheme`](AvalonDock.Themes.Type1Legacy/AeroTheme.cs), [`ExpressionDarkTheme`](AvalonDock.Themes.Type1Legacy/ExpressionDarkTheme.cs), [`ExpressionLightTheme`](AvalonDock.Themes.Type1Legacy/ExpressionLightTheme.cs), [`MetroTheme`](AvalonDock.Themes.Type1Legacy/MetroTheme.cs), [`VS2010Theme`](AvalonDock.Themes.Type1Legacy/VS2010Theme.cs) | `Theme` (`AvalonDock.Themes.Type1Legacy`) | Compiled BAML skin dictionaries under `/AvalonDock.Themes.Type1Legacy;component/Themes/{Aero,Expression,Metro,VS2010}/...` (+ [`AeroColors.cs`](AvalonDock.Themes.Type1Legacy/AeroColors.cs)) | Distinct curved tab borders ([`AeroSplineBorder.cs`](AvalonDock.Themes.Type1Legacy/Controls/AeroSplineBorder.cs) & [`ExpressionSplineBorder.cs`](AvalonDock.Themes.Type1Legacy/Controls/ExpressionSplineBorder.cs)) + 146 raster PNG dock/pin icons |
| [`AvalonDock.Themes.Type2Hybrid`](AvalonDock.Themes.Type2Hybrid/) | `Dirkster.AvalonDock.Themes.Type2Hybrid` | [`Vs2013BlueTheme`](AvalonDock.Themes.Type2Hybrid/Vs2013BlueTheme.cs), [`Vs2013DarkTheme`](AvalonDock.Themes.Type2Hybrid/Vs2013DarkTheme.cs), [`Vs2013LightTheme`](AvalonDock.Themes.Type2Hybrid/Vs2013LightTheme.cs) | `DictionaryTheme` (`AvalonDock.Themes.Type2Hybrid`) | Embedded GZIP `AvalonDock.Themes.Type2Hybrid.Resources.vs2013*.vstheme.gz` → [`VsThemePaletteFactory.cs`](AvalonDock.Themes.Type2Hybrid/VsThemePaletteFactory.cs) (100% frozen brushes on [`ResourceKeys.cs`](AvalonDock.Themes.Type2Hybrid/Themes/ResourceKeys.cs) + [`MenuKeys.cs`](AvalonDock.Themes.Type2Hybrid/Themes/Menu/MenuKeys.cs)) | **100% vector** ([`IconGeometry.xaml`](AvalonDock.Themes.Type2Hybrid/Themes/Icons/IconGeometry.xaml), [`OverlayButtons.xaml`](AvalonDock.Themes.Type2Hybrid/OverlayButtons.xaml), [`Themes/Generic.xaml`](AvalonDock.Themes.Type2Hybrid/Themes/Generic.xaml)); `MergedDictionaries.Count == 1` |
| [`AvalonDock.Themes.Type3New`](AvalonDock.Themes.Type3New/) | `Dirkster.AvalonDock.Themes.Type3New` | `VS2015{Blue,Dark,Light}Theme`, `VS2022{Blue,Dark,Light}Theme`, `VS2026{Blue,Dark,Light}Theme` (+ [`VsTheme`](AvalonDock.Themes.Type3New/VsTheme.cs)) | `VsTheme : DictionaryTheme` (`AvalonDock.Themes.Type3New`) | Embedded XML `.vstheme` ([`VsThemeParser.cs`](AvalonDock.Themes.Type3New/VsThemeParser.cs)) + VS2026 `.json` overrides ([`VsJsonThemeParser.cs`](AvalonDock.Themes.Type3New/VsJsonThemeParser.cs), pure C#) → frozen brushes on [`ResourceKeys.cs`](AvalonDock.Themes.Type3New/Themes/ResourceKeys.cs) + [`MenuKeys.cs`](AvalonDock.Themes.Type3New/Themes/Menu/MenuKeys.cs) | **100% vector**; two template sets: flat [`Themes/Generic.xaml`](AvalonDock.Themes.Type3New/Themes/Generic.xaml) (`VS2015*`) and rounded-corner [`Themes/VS2022/Generic.xaml`](AvalonDock.Themes.Type3New/Themes/VS2022/Generic.xaml) (`VS2022*` & `VS2026*`); `{avalonDock:VsTheme}` XAML extension |

### Feature & Tab Icon Matrix

| Theme Family | Category Assembly | Dark | Light | Blue | Document Tab Icons (`IconSource`) | `Ctrl+Tab` `NavigatorWindow` Icons | `ToggleDockingManager` Native Styling |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **VS 2015 / 2022 / 2026** | `AvalonDock.Themes.Type3New` | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS 2013** | `AvalonDock.Themes.Type2Hybrid` | Yes | Yes | Yes | Yes | Yes | Yes |
| **VS 2010** | `AvalonDock.Themes.Type1Legacy` | — | — | Yes | Yes | Yes | Yes |
| **Expression** | `AvalonDock.Themes.Type1Legacy` | Yes | Yes | — | Yes | Yes | Yes |
| **Metro** | `AvalonDock.Themes.Type1Legacy` | — | Yes | — | Yes | Yes | Yes |
| **Aero** | `AvalonDock.Themes.Type1Legacy` | — | Yes | — | Yes | Yes | Yes |
| **GenericTheme** | `AvalonDock` *(core)* | System | System | — | Yes | Yes | Yes |

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
[`GenericTheme.cs`](AvalonDock/Themes/GenericTheme.cs) (`AvalonDock.Themes.Type1Legacy.GenericTheme`) is a concrete `Theme` subclass returning `/AvalonDock;component/Themes/generic.xaml`:

- **MVVM & DI Selectors:** Gives `ComboBox` theme pickers (`DisplayMemberPath="Name"` → `"Generic"`) and `IThemeManager` a concrete `Theme` object to revert to the default look at runtime.
- **Lookup Precedence Difference vs `Theme = null`:**
  - `Theme = null` leaves `DockingManager.Resources.MergedDictionaries` empty, so implicit styles in `Application.Current.Resources` take precedence over `Themes/generic.xaml`.
  - `Theme = new GenericTheme()` merges `/AvalonDock;component/Themes/generic.xaml` directly into `DockingManager.Resources.MergedDictionaries` (plus open floating and detached windows), placing it **above** `Application.Current.Resources` in WPF resource lookup order.

### Role 3: Structural Fallback Behind Theme Packages (`ToggleDockingManager` & Shared Styles)
Even when an external theme package is active, any style or template not explicitly redefined by that theme falls back to `AvalonDock/Themes/generic.xaml`:

- All 3 theme category packages define `ToggleAnchorablePaneControlStyle`, `ToggleDockButton`, `ToggleDockButton.ForegroundBrushKey`, and `ToggleDockingManager` styles.
- **`Type3New` (`VS2015*`/`VS2022*`/`VS2026*`) and `Type2Hybrid` (`Vs2013*`)** define their own custom `ToggleAnchorablePaneTitle` style, whereas `Type1Legacy` (`VS2010`, `Aero`, `Expression`, `Metro`) inherits the `ToggleAnchorablePaneTitle` template (with `PART_OptionsButton` and `PART_MinimizeButton`) from [`AvalonDock/Themes/generic.xaml`](AvalonDock/Themes/generic.xaml).
- [`ToggleDockingManager.LoadToggleStyle()`](AvalonDock/ToggleDockingManager.cs) loads `ToggleAnchorablePaneControlStyle` directly from `pack://application:,,,/AvalonDock;component/Themes/generic.xaml` into a `[ThreadStatic]` cache whenever an active or custom theme does not provide its own `ToggleAnchorablePaneControlStyle`.

---

## 3. Deep Dive: `AvalonDock.Themes.Type3New` & `AvalonDock.Themes.Type2Hybrid` Runtime Palette Engine

Unlike the six URI-based XAML themes in `Type1Legacy`, `AvalonDock.Themes.Type3New` and `AvalonDock.Themes.Type2Hybrid` derive from [`DictionaryTheme`](AvalonDock/Themes/DictionaryTheme.cs) and construct frozen `SolidColorBrush` dictionaries in memory from authentic Visual Studio theme files:

1. **XML `.vstheme` & GZIP `.vstheme.gz` Parsing ([`VsThemeParser.cs`](AvalonDock.Themes.Type3New/VsThemeParser.cs)):**
   - Auto-detects GZIP magic bytes (`0x1F 0x8B`) via `System.IO.Compression.GZipStream` and parses the `Environment` `<Category>` XML via `System.Xml.Linq.XDocument` into an immutable [`VsThemeColorPalette`](AvalonDock.Themes.Type3New/VsThemeColorPalette.cs).
2. **Zero-Dependency VS2026 JSON Override Parsing ([`VsJsonThemeParser.cs`](AvalonDock.Themes.Type3New/VsJsonThemeParser.cs)):**
   - Parses Visual Studio 2026 JSON color-override arrays (`Name`, `Background`, `Foreground`) using a pure-C# recursive-descent `JsonThemeReader` built strictly on `.NET Framework 4.8` inbox `mscorlib` types (zero `System.Text.Json`, zero `Span<T>` / `System.Memory`).
   - Supports `//` and `/* ... */` comments, trailing commas, UTF-8 BOM, `\uXXXX` escapes, case-insensitive keys, and skipping unknown/nested fields; throws `FormatException` with 1-based `(Line X, Column Y)` coordinates on malformed syntax.
   - [`Vs2026ThemeFactory.cs`](AvalonDock.Themes.Type3New/Vs2026ThemeFactory.cs) layers `AvalonDock.Themes.Type3New.Resources.vs2026{blue,dark,light}.json` overrides onto `AvalonDock.Themes.Type3New.Resources.vs2022{blue,dark,light}.vstheme` via `VsThemeColorPalette.Merge(...)`.
3. **Frozen `ResourceDictionary` Construction:**
   - [`VsThemeResourceBuilder.cs`](AvalonDock.Themes.Type3New/VsThemeResourceBuilder.cs) maps palette tokens (including VS2026 independent `EnvironmentHeader*` and `EnvironmentTab*` tokens) onto `AvalonDock.Themes.Type3New.Themes.ResourceKeys` and `MenuKeys`, freezes every `SolidColorBrush`, and merges either `/AvalonDock.Themes.Type3New;component/Themes/Generic.xaml` (`VS2015*`) or `/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml` (`VS2022*` & `VS2026*`).
   - [`VsThemePaletteFactory.cs`](AvalonDock.Themes.Type2Hybrid/VsThemePaletteFactory.cs) decompresses `AvalonDock.Themes.Type2Hybrid.Resources.vs2013{blue,dark,light}.vstheme.gz`, freezes all `SolidColorBrush` entries on `AvalonDock.Themes.Type2Hybrid.Themes.ResourceKeys` and `MenuKeys` (plus `ControlAccentColorKey`), and merges `/AvalonDock.Themes.Type2Hybrid;component/Themes/Generic.xaml` (which itself merges `OverlayButtons.xaml`, `Themes/Menu/MenuItem.xaml`, and `Themes/Icons/IconGeometry.xaml`).

---

## 4. Class Hierarchy & Runtime Theme-Switching Pipeline

```text
AvalonDock.Core.IThemeInfo (netstandard2.0 — Name, ResourceUri)
  └── AvalonDock.Themes.Theme (abstract DependencyObject — Name strips "Theme" suffix, abstract GetResourceUri())
        ├── AvalonDock.Themes.Type1Legacy:
        │     GenericTheme (in AvalonDock.dll), AeroTheme, ExpressionDarkTheme,
        │     ExpressionLightTheme, MetroTheme, VS2010Theme  (URI-based)
        └── AvalonDock.Themes.DictionaryTheme (abstract — holds in-memory ThemeResourceDictionary; GetResourceUri() => null)
              ├── AvalonDock.Themes.Type2Hybrid:
              │     Vs2013BlueTheme, Vs2013DarkTheme, Vs2013LightTheme
              └── AvalonDock.Themes.Type3New.VsTheme
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