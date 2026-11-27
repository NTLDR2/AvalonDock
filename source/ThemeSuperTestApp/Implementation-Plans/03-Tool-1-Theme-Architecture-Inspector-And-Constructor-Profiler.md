# Plan 03 (of 07) — Tool 1: Theme Architecture Inspector & Cold Constructor / Apply Profiler

> **Execution Order:** **Step 3 of 7** (Requires [Plan 02](02-Windows-OS-Theme-Detection-DWM-Chrome-And-Pinned-Shell-Header.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Build **Tool 1 (`ThemeProfilerViewModel` / `ThemeProfilerView.xaml`)** — a dockable `ToolboxScreen` hosted inside `DockingManager` that empirically measures and exposes two major architectural findings from [THEMES-ANALYSIS-AND-BEST-PRACTICES.md](../../Components/THEMES-ANALYSIS-AND-BEST-PRACTICES.md):
1. **Eager Constructor Overhead on `DictionaryTheme` / `VsTheme`:** Quantifies cold `new XxxTheme()` instantiation time (`ms`) and managed memory allocation (`bytes`) versus URI-based `Theme` (`~0.01 ms`), giving you a repeatable before/after benchmark for when `DictionaryTheme` is eventually made lazy.
2. **Namespace & `[XmlnsDefinition]` Audit Across All 20 Themes:** Inspects every theme assembly's `[XmlnsDefinitionAttribute]` declarations, `GetResourceUri()` return value (`IThemeInfo.ResourceUri`), and `ThemeResourceDictionary` state in one sortable table.

---

## 2. Step-by-Step Implementation Tasks

### Step 3.1 — Implement Cold Constructor & Apply Sweep Benchmarking in `ThemeCatalogService`
Extend `Services/ThemeCatalogService.cs` with two deterministic profiling routines:
1. **`BenchmarkAllColdConstructors()`:**
   - Iterates through all registered `ThemeDescriptor` entries.
   - Calls `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();`, captures `GC.GetTotalMemory(false)` and `Stopwatch.GetTimestamp()`, invokes `descriptor.Factory()` to construct a **brand-new** `Theme` instance without mutating `DockingManager.Theme`, and records:
     - `LastColdConstructorMs` (`(endTicks - startTicks) * 1000.0 / Stopwatch.Frequency`)
     - `LastColdAllocatedBytes` (`Math.Max(0, memAfter - memBefore)`)
     - `ResourceUriText` (`instance.GetResourceUri()?.ToString() ?? "<null>"`)
     - `DirectEntryCount` & `MergedDictionaryCount` (if `instance is DictionaryTheme dt && dt.ThemeResourceDictionary != null`).
2. **`SweepAndBenchmarkApplyAllThemesAsync()`:**
   - Sequentially applies each of the 20 themes to `DockingManager.Theme`, pumps `DispatcherPriority.Render` (`await Dispatcher.Yield(DispatcherPriority.Render)` so WPF actually resolves `{DynamicResource}` bindings and renders the visual tree!), and records exact `LastApplyMs` (layout + render pass included) for every theme before restoring or stopping on the final theme.

### Step 3.2 — Create `ViewModels/Tools/ThemeProfilerViewModel.cs` & `Views/Tools/ThemeProfilerView.xaml`
Implement `ThemeProfilerViewModel : ToolboxScreen` (`PreferredSide = DockSide.Bottom` or `Right`, `IsOpenByDefault = true`, `Shortcut = "Ctrl+Alt+P"`):
1. **Summary KPI Strip at Top of Toolbox:**
   - Average Cold `.ctor` (`Theme (URI)`): `X.XXX ms` vs. Average Cold `.ctor` (`DictionaryTheme` / `VsTheme`): `Y.YYY ms` (shows exact multiplier, e.g., `120x slower eager .ctor`).
   - Count of themes missing `[XmlnsDefinition]` CLR namespace mapping (`9 / 20` — all in `AvalonDock.Themes.VS`).
   - Count of themes returning `null` from `IThemeInfo.ResourceUri` (`12 / 20` — all `DictionaryTheme` subclasses).
2. **Action Toolbar (Caliburn.Micro `cal:Message.Attach`):**
   - `BenchmarkColdConstructors` — Runs cold `.ctor` benchmark across all 20 themes.
   - `SweepAllThemes` — Cycles live through all 20 themes on `DockingManager` measuring full apply + render latency (with `CanSweepAllThemes` guard preventing re-entrancy).
   - `ApplySelectedRowTheme` — Applies whichever row is double-clicked or selected in the profiler grid.
   - `CopyBenchmarkMarkdown` — Formats the entire 20-row architecture + timing table as GitHub-Flavored Markdown onto `Clipboard.SetText(...)`.
3. **20-Theme Architecture & Timing `DataGrid` (`AutoGenerateColumns="False"`, `IsReadOnly="True"`):**
   - Columns: `Theme`, `Base Class` (`Theme` / `DictionaryTheme` / `VsTheme`), `Inheritance Chain`, `Assembly`, `CLR Namespace`, `Xmlns Mapped?`, `GetResourceUri()`, `Cold .ctor (ms)`, `Cold Alloc (KB)`, `Apply + Render (ms)`, `Root Dict Entries`, `Merged Dicts`.
   - Row highlight or indicator for the currently active theme.

### Step 3.3 — Register `ThemeProfilerViewModel` in `Bootstrapper.cs`
Register `RegisterToolbox<ThemeProfilerViewModel>()` in `Bootstrapper.Configure()` so it appears automatically in the `_View` menu, responds to `Ctrl+Alt+P`, and docks cleanly via `ZonePlacementStrategy`.

---

## 3. Verification & Exit Gate to Plan 04

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git diff --name-only -- source/Components/
```

**Expected Result:**
- Builds cleanly with **0 warnings, 0 errors**.
- Clicking `Benchmark Cold Constructors` populates all 20 rows, clearly contrasting `0.00–0.02 ms` on URI `Theme` classes against `1–10 ms` eager construction on `Vs2013*Theme` / `VS2015*` / `VS2022*` / `VS2026*`.
- Clicking `Copy Markdown` produces a ready-to-paste Markdown benchmark table.
- Zero edits to `source/Components/`.
