# Plan 05 (of 05) — Synchronize Documentation, `THEMES.md`, `AGENTS.md`, Skill & `ROADMAP.md`

> **Execution Order:** **Step 5 of 5** (Final stage — requires [Plan 04](04-Wire-All-12-VS-Themes-Into-ExperimentalTestApp.md) completed).
> **Roadmap Guardrail (`ROADMAP.md` #4):** *"Architectural changes must simultaneously update `documentation/`, `AGENTS.md`, and `.agents/skills/avalondock-internals/SKILL.md` so AI coding agents always operate from ground-truth invariants."*

---

## 1. Objective & Scope

Bring every technical report, documentation site page, AI agent rule file, and roadmap milestone into 100% alignment with the restored, zero-`System.Text.Json` **7-package / 20-theme** architecture (`13` total shipped NuGet packages):

- **7 Theme NuGet Packages (19 concrete classes, all targeting `net48` exclusively):**
  - `Dirkster.AvalonDock.Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`) — `net48`
  - `Dirkster.AvalonDock.Themes.VS` (`VS2015{Blue,Dark,Light}Theme`, `VS2022{Blue,Dark,Light}Theme`, `VS2026{Blue,Dark,Light}Theme`, plus `VsTheme` & `{vs:VsTheme}`) — **`net48` exclusively, in `AvalonDock.Themes.VS` namespace, powered by zero-dependency XML & pure-C# JSON parsers**
  - `Dirkster.AvalonDock.Themes.VS2013` (`Vs2013{Blue,Dark,Light}Theme`) — **`net48` exclusively, in `AvalonDock.Themes` namespace**
  - `Dirkster.AvalonDock.Themes.VS2010` (`VS2010Theme`) — `net48`
  - `Dirkster.AvalonDock.Themes.Expression` (`ExpressionDarkTheme`, `ExpressionLightTheme`) — `net48`
  - `Dirkster.AvalonDock.Themes.Metro` (`MetroTheme`) — `net48`
  - `Dirkster.AvalonDock.Themes.Aero` (`AeroTheme`) — `net48`
- **Plus Built-in Core Theme (20th concrete class):** `GenericTheme` (+ abstract `DictionaryTheme`) in `Dirkster.AvalonDock`.

---

## 2. Prerequisites

- [Plan 01](01-Restore-Themes-VS-And-Zero-Dep-Json-Parser.md) through [Plan 04](04-Wire-All-12-VS-Themes-Into-ExperimentalTestApp.md) are complete and verified.

---

## 3. Step-by-Step Implementation Tasks

### Step 5.1 — Update `source/Components/THEMES.md`

Update `source/Components/THEMES.md` (created during our initial theme audit) so it reflects the full post-refactoring state:
1. Update the Executive Summary and Inventory Table from **8 concrete themes across 6 assemblies** to **20 concrete themes across 8 assemblies** (7 `AvalonDock.Themes.*` packages + built-in `GenericTheme` in `AvalonDock`).
2. Document the internal architecture of `AvalonDock.Themes.VS` (`VsTheme : DictionaryTheme`, `VsThemeParser` for `.vstheme`/`.vstheme.gz`, pure-C# zero-dependency `VsJsonThemeParser` for VS2026 `.json` overrides, `VsThemeResourceBuilder`, and dual `Themes/Generic.xaml` vs `Themes/VS2022/Generic.xaml` templates) and `AvalonDock.Themes.VS2013` (`Vs2013*Theme : DictionaryTheme`, `.vstheme.gz` resources, frozen `VsThemePaletteFactory` brushes).
3. Highlight the namespace distinction: `AvalonDock.Themes.VS` types reside in `AvalonDock.Themes.VS`, whereas all other built-in theme classes (including `Vs2013*Theme`) reside in `AvalonDock.Themes`.

### Step 5.2 — Restore & Update `documentation/themes/vs.md` and `vs2013.md` + Gallery/Guides

1. **Extract baseline docs from `b706128` (`new-docs-carbon4/themes/vs.md` and `vs2013.md`) into `documentation/themes/`:**
   ```powershell
   git show b706128183a8edce302b1b633396e2c98d48ba32:new-docs-carbon4/themes/vs.md > documentation/themes/vs.md
   git show b706128183a8edce302b1b633396e2c98d48ba32:new-docs-carbon4/themes/vs2013.md > documentation/themes/vs2013.md
   ```
   *(Ensure UTF-8 without BOM and LF line endings!)*
2. **Update `documentation/themes/vs.md`:**
   - Add a `Fork Scope (NTLDR2 Fork v5.1.0)` note explaining that `VsJsonThemeParser` uses a self-contained, zero-dependency pure-C# parser (eliminating upstream's `System.Text.Json` dependency on `net48` while keeping 100% API compatibility, throwing `FormatException` on invalid JSON syntax).
3. **Renumber `nav_order` cleanly in `documentation/themes/`:**
   - `arc.md` → `nav_order: 1`
   - `vs.md` → `nav_order: 2`
   - `vs2013.md` → `nav_order: 3`
   - `vs2010.md` → `nav_order: 4`
   - `expression.md` → `nav_order: 5`
   - `metro.md` → `nav_order: 6`
   - `aero.md` → `nav_order: 7`
4. **Update cross-referencing documentation pages in `documentation/`:**
   - `documentation/themes/index.md` — Update package comparison table, feature/tab-icon matrix (`VS` 2015/2022/2026 & `VS2013` support tab icons, `NavigatorWindow` icons, and `ToggleDockingManager`), and fork note (noting `Serializer.Json` remains omitted while `Themes.VS` and `Themes.VS2013` ship zero-`System.Text.Json` in `v5.1.0`).
   - `documentation/guides/theming.md`, `documentation/guides/custom-themes.md`, `documentation/tutorials/styling-and-theming.md`, `documentation/getting-started/installation.md`, `documentation/concepts/architecture.md`, `documentation/migration/index.md`, `documentation/migration/breaking-changes.md`, `documentation/guides/caliburn-micro.md`, and `documentation/index.md` — Update package counts (`11` → `13` total packages; `5` → `7` theme packages), keep the v4 → v5 migration warning that `AvalonDock.Themes.VS2013` loose XAML files (`DarkBrushs.xaml` / `DarkTheme.xaml`) were replaced by embedded `.vstheme.gz` resources in v5 (`#572`), and keep `System.Text.Json` / `Dirkster.AvalonDock.Serializer.Json` listed as permanently omitted.

### Step 5.3 — Update `AGENTS.md`, `.agents/skills/avalondock-internals/SKILL.md`, `README.md` & `ROADMAP.md`

1. **`AGENTS.md` *(keep total file size concise — under ~24 KB)*:**
   - Update package count (`11` → `13 NuGet Packages`) and Section 3 architecture diagram/table to include `AvalonDock.Themes.VS` (`AvalonDock.Themes.VS`) and `AvalonDock.Themes.VS2013` (`AvalonDock.Themes`).
   - In Section 2 (`v4 → v5 Differences & LLM Anti-Hallucination Guardrails`):
     - Under **Removed `System.Text.Json` Modules**: keep `System.Text.Json` and `Dirkster.AvalonDock.Serializer.Json` strictly banned; note that `AvalonDock.Themes.VS` and `AvalonDock.Themes.VS2013` are **restored and refactored to zero `System.Text.Json`**.
     - Under **Custom XAML Dictionary Merges**: retain the critical v5 guardrail that `/AvalonDock.Themes.VS2013;component/DarkBrushs.xaml` / `DarkTheme.xaml` do **not** exist in v5 (VS2013 uses embedded `.vstheme.gz` + `DictionaryTheme`; use `Themes.Arc` for XAML brush merges or override `Vs2013*Theme.ThemeResourceDictionary` in C#).
     - Under **Theme Namespaces**: clarify that `Arc*`, `Vs2013*`, `VS2010`, `Expression*`, `Metro`, `Aero`, and `GenericTheme` live in **`AvalonDock.Themes`**, while `VS2015*`, `VS2022*`, `VS2026*`, and `VsTheme` live in **`AvalonDock.Themes.VS`**.
2. **`.agents/skills/avalondock-internals/SKILL.md` (Section 9 — Theme Engine Internals):**
   - Note that `Theme` has both URI-based subclasses (`Arc*`, `VS2010`, `Expression*`, `Metro`, `Aero`, `GenericTheme`) and built-in `DictionaryTheme` subclasses (`VsTheme` / `VS2015*` / `VS2022*` / `VS2026*` in `AvalonDock.Themes.VS` using XML `.vstheme` + pure-C# `VsJsonThemeParser`, and `Vs2013*Theme` in `AvalonDock.Themes.VS2013` using `.vstheme.gz`).
3. **`README.md` & `ROADMAP.md`:**
   - Update `README.md` package summary (`13` packages, `7` zero-dependency theme packages, `0` `System.Text.Json` dependencies).
   - Update `ROADMAP.md` Phase 1/2 notes (`7` zero-dependency theme libraries on `net48`), Phase 4 (`ExperimentalTestApp` showcasing all 7 theme packages), and Phase 5 (`AvalonDox.Themes.{Arc,VS,VS2013,VS2010,Expression,Metro,Aero}` — plus note in the Phase 5 `pack://` checklist that `VsThemeResourceBuilder`, `VsThemePaletteFactory`, `VS2022*Theme`, and `VS2026*Theme` also contain `/AvalonDock.Themes.VS*;component/...` pack URIs and `AvalonDock.Themes.Resources.*` manifest resource prefixes that must be updated atomically during the `AvalonDox` cutover!).

---

## 4. Final End-to-End Verification Gate

Run the complete build, unit/STA test suite, FlaUI desktop test suite (if running on an unlocked interactive Windows session), and link/dependency checks:

```powershell
# 1. Clean Release build with zero warnings allowed
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# 2. Fast Unit + STA Regression Suite (single-threaded)
dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category!=FlaUI" -m:1

# 3. FlaUI Desktop Automation Suite on net48 (single-threaded)
dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category=FlaUI" --framework net48 -m:1

# 4. Zero System.Text.Json verification across all build assets & csproj files
Get-ChildItem -Path "source" -Recurse -Include "*.csproj","project.assets.json" |
  Select-String -Pattern "System\.Text\.Json"
```

**Completion Criteria:**
- All 13 component projects, sample apps, and test suites build with **0 warnings, 0 errors**.
- Unit/STA tests (including all new `VsJsonThemeParserTests`, `VsThemeParserTests`, and 12-theme STA tests) and FlaUI tests pass on `net48`.
- `Select-String -Pattern "System\.Text\.Json"` across all `.csproj` and `project.assets.json` files returns **empty**.
- Documentation (`documentation/`, `source/Components/THEMES.md`, `AGENTS.md`, `SKILL.md`, `README.md`, `ROADMAP.md`) is 100% consistent.
