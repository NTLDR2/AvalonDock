# Plan 04 (of 07) — Tool 2: Live `ResourceDictionary` & `Freezable.IsFrozen` Auditor + Cross-Theme Key Diff

> **Execution Order:** **Step 4 of 7** (Requires [Plan 03](03-Tool-1-Theme-Architecture-Inspector-And-Constructor-Profiler.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Build **Tool 2 (`ResourceAuditorViewModel` / `ResourceAuditorView.xaml`)** backed by `Services/ResourceDictionaryAuditService.cs` to inspect the active theme's `ResourceDictionary` hierarchy in real time and diagnose three critical WPF theme quality issues:
1. **Unfrozen `Freezable` Detection:** Scans every `SolidColorBrush`, `LinearGradientBrush`, `DrawingBrush`, `Geometry`, and `Pen` in the active theme's root dictionary and nested `MergedDictionaries` (plus `Style.Resources` where accessible) and flags any resource where `freezable.IsFrozen == false`!
2. **Lookup Precedence & Dictionary Structure Visibility:** Shows whether each resource lives in the root `ResourceDictionary` (`Direct` — used by `VsThemeResourceBuilder` and `VsThemePaletteFactory`) or inside `MergedDictionaries[i]` (used by `Arc`, `VS2010`, `Expression`, `Metro`, `Aero`).
3. **Cross-Theme Semantic Key Parity Diff:** Compares the keys defined by any two themes (or against known `ComponentResourceKey` catalogs in `AvalonDock.Themes.Arc.Themes.ResourceKeys`, `AvalonDock.Themes.VS.Themes.ResourceKeys`, `AvalonDock.Themes.VS2013.Themes.ResourceKeys`, and `ToggleDockButton.ForegroundBrushKey`) so missing or mismatched keys are immediately visible.

---

## 2. Step-by-Step Implementation Tasks

### Step 4.1 — Implement `Services/ResourceDictionaryAuditService.cs` & Models
Create structured audit models in `Models/`:
1. **`ResourceEntryRecord`:**
   - `KeyDisplay` (`string` — formatted cleanly for `ComponentResourceKey` as `[Type.Name].ResourceId`, for `Type` implicit styles as `Style TargetType={Type.Name}`, or `key.ToString()`).
   - `KeyCategory` (`ComponentResourceKey`, `ImplicitTypeStyle`, `SystemKey`, `StringOrOther`).
   - `ValueTypeName` (`string`, e.g., `SolidColorBrush`, `LinearGradientBrush`, `Color`, `Style`, `ControlTemplate`, `Double`, `Thickness`).
   - `ColorHex` (`string?` — `#AARRGGBB` for `SolidColorBrush`, `Color`, or gradient stops summary) and `SwatchBrush` (`Brush?` — frozen copy for safe UI preview).
   - `FreezeStatus` (`Frozen`, `UnfrozenWarning`, `NotFreezable`).
   - `SourceLocation` (`string`, e.g., `"Root Dictionary (Direct)"` vs. `"Merged[0]: /AvalonDock.Themes.Arc;component/DarkBrushs.xaml"`).
   - `DictionaryDepth` (`int`).
2. **`ThemeResourceAuditReport`:**
   - `ThemeName`, `TotalEntries`, `BrushCount`, `FrozenFreezableCount`, `UnfrozenFreezableCount`, `StyleAndTemplateCount`, `MergedDictionaryTreeSummary`, and `IReadOnlyList<ResourceEntryRecord> Entries`.
3. **Safe Dictionary Resolution for Both `DictionaryTheme` and URI `Theme`:**
   - If active theme is `DictionaryTheme dt`: inspect `dt.ThemeResourceDictionary` (or the live dictionary attached to `DockingManager.Resources.MergedDictionaries`).
   - If active theme is URI `Theme`: locate the live `ResourceDictionary` in `DockingManager.Resources.MergedDictionaries` matching `theme.GetResourceUri()` (or instantiate a read-only inspection `ResourceDictionary { Source = uri }` if inspecting an inactive theme for cross-theme diffing).
   - Enumerate `DictionaryEntry` items safely (note: in WPF BAML dictionaries, iterating `dict.Keys` and reading `dict[key]` realizes deferred BAML entries cleanly; wrap individual `dict[key]` reads in `try/catch` so any malformed BAML resource is recorded as an error row rather than crashing the auditor!).
4. **`DiffThemeKeys(ThemeDescriptor baseline, ThemeDescriptor target)`:**
   - Compares normalized resource key signatures (`ResourceId` for `ComponentResourceKey`, `TargetType` for implicit `Style`s, string key for named resources) between `baseline` and `target`, returning `OnlyInBaseline`, `OnlyInTarget`, and `InBothWithDifferentValueType`.

### Step 4.2 — Create `ViewModels/Tools/ResourceAuditorViewModel.cs` & `Views/Tools/ResourceAuditorView.xaml`
Implement `ResourceAuditorViewModel : ToolboxScreen` (`PreferredSide = DockSide.Right`, `IsOpenByDefault = true`, `Shortcut = "Ctrl+Alt+R"`):
1. **Auto-Refresh on `ThemeCatalogService.ActiveThemeChanged`:** Immediately audits the newly applied theme and updates KPI counters (`Total Keys`, `Brushes`, `Frozen`, and a bright warning badge when `Unfrozen > 0`!).
2. **Tab 1 — Live Resource & `Freezable` Explorer:**
   - Filter bar: `SearchText` (`TextBox` filtering Key, ValueType, ColorHex, or SourceLocation), `FilterMode` ComboBox (`All Entries` | `Unfrozen Freezables Only (!)` | `Brushes & Colors Only` | `Styles & Templates Only` | `ComponentResourceKeys Only`).
   - Virtualized `DataGrid` displaying `[Color Swatch Border]`, `Hex`, `Freeze Status`, `Key`, `Value Type`, and `Dictionary Source`.
3. **Tab 2 — Cross-Theme Key Parity Diff:**
   - `ComparisonTheme` ComboBox (pick any of the 20 themes to diff against the currently active theme — e.g., `ArcDark` vs. `VS2022Dark` or `Vs2013Dark` vs. `VS2015Dark`).
   - Displays counts and filterable list of keys `Missing in Active Theme`, `Extra in Active Theme`, and `Type Mismatches` (e.g., `LinearGradientBrush` in `Aero`/`VS2010` vs. `SolidColorBrush` in `VS`).
   - `CopyAuditSummaryMarkdown` action button.

### Step 4.3 — Register `ResourceAuditorViewModel` in `Bootstrapper.cs`
Register `RegisterToolbox<ResourceAuditorViewModel>()` in `Bootstrapper.Configure()`.

---

## 3. Verification & Exit Gate to Plan 05

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git diff --name-only -- source/Components/
```

**Expected Result:**
- Builds with **0 warnings, 0 errors**.
- Switching across all 20 themes updates the Resource & `Freezable` Explorer live with color swatches, `#AARRGGBB` hex codes, dictionary merge provenance, and `IsFrozen` status (flagging any unfrozen brushes left in legacy XAML themes like `Aero`/`Metro`/`Expression`/`VS2010`!).
- Zero edits to `source/Components/`.
