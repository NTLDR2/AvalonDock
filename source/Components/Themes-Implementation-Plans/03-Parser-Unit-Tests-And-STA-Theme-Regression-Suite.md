# Plan 03 (of 05) — Unit Tests for Pure-C# JSON/XML Parsers & 12-Theme STA Regression Suite

> **Execution Order:** **Step 3 of 5** (Requires [Plan 02](02-Restore-Themes-VS2013-Brush-Freezing-And-Solution-CI.md) completed; all unit and STA tests must pass before starting [Plan 04](04-Wire-All-12-VS-Themes-Into-ExperimentalTestApp.md)).

---

## 1. Objective & Scope

Lock down automated test coverage in `source/AutomationTest/AvalonDockTest/` for both restored packages:
1. Restore upstream `VsThemeParserTests.cs` and `VsJsonThemeParserTests.cs` from `b706128`, and **expand `VsJsonThemeParserTests.cs`** to thoroughly exercise every branch of the new pure-C# recursive-descent parser (comments, trailing commas, escapes, UTF-8 BOM, unknown nested objects/arrays, duplicate tokens, stream/file overloads, and `FormatException` with line/column diagnostics on malformed JSON).
2. Expand STA theme regression coverage (`VsThemeToggleDockingManagerTests.cs` and a comprehensive 12-theme matrix test) verifying that **all 12 restored themes** (`Vs2013{Blue,Dark,Light}`, `VS2015{Blue,Dark,Light}`, `VS2022{Blue,Dark,Light}`, `VS2026{Blue,Dark,Light}`):
   - Construct their `ThemeResourceDictionary` cleanly without missing embedded resources or unresolved keys.
   - Freeze all `SolidColorBrush` entries (`brush.IsFrozen == true`).
   - Provide complete `ToggleDockingManager` styles (`ToggleAnchorablePaneControlStyle`, `ToggleDockButton`, `ToggleDockButton.ForegroundBrushKey`, `ToggleDockingManager`) and resolve `ToggleAnchorablePaneTitle` templates cleanly (`#683`).
   - Resolve `MenuItemEx` document dropdown templates cleanly (`#684`) and include `DetachToWindowCommand` in `AnchorableContextMenu`.
   - Switch dynamically at runtime on both `DockingManager` (including open floating/detached windows) and `ToggleDockingManager` without throwing.

> **Critical STA Testing Rule (`AGENTS.md` Section 6):** Never call `Application.Current.Shutdown()` in any test or teardown! Always instantiate standalone controls in `[Apartment(ApartmentState.STA)]` fixtures or close individual `Window` instances if hosted. Ensure NUnit assertions stay compatible with NUnit `4.3.x` + C# 12 (`SDK 8.0`).

---

## 2. Prerequisites

- [Plan 02](02-Restore-Themes-VS2013-Brush-Freezing-And-Solution-CI.md) is complete: both `AvalonDock.Themes.VS` and `AvalonDock.Themes.VS2013` are in `AvalonDock.sln` and build cleanly with `--warnaserror`.

---

## 3. Step-by-Step Implementation Tasks

### Step 3.1 — Add Project References to `AvalonDockTest.csproj`

Edit `source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj` to add `<ProjectReference>` entries for:
- `..\..\Components\AvalonDock.Themes.VS\AvalonDock.Themes.VS.csproj`
- `..\..\Components\AvalonDock.Themes.VS2013\AvalonDock.Themes.VS2013.csproj`

### Step 3.2 — Restore & Expand `VsThemeParserTests.cs` and `VsJsonThemeParserTests.cs`

1. Restore both baseline test files from `b706128`:
   ```powershell
   git checkout b706128183a8edce302b1b633396e2c98d48ba32 -- `
     source/AutomationTest/AvalonDockTest/VsThemeParserTests.cs `
     source/AutomationTest/AvalonDockTest/VsJsonThemeParserTests.cs
   ```
2. Expand `VsJsonThemeParserTests.cs` with dedicated unit tests covering the new pure-C# parser contract:
   - **Comments & Trivia:** Single-line `//` comments (at start, inline, and EOF without trailing newline), `/* ... */` block comments (including multi-line and `*` inside comments), and leading UTF-8 BOM (`\uFEFF`).
   - **Trailing Commas:** Trailing comma after the last property in an object `{ "Name": "Foo", "Background": "FF112233", }` and after the last object in the root array `[ { ... }, ]`.
   - **String Escapes:** Escaped quotes/backslashes (`\"`, `\\`, `\/`) and `\uXXXX` hex escapes in token names or ignored string fields.
   - **Forward Compatibility (Unknown / Nested Fields):** Entry objects containing extra numbers, booleans (`true`/`false`), `null`, nested objects (`"Metadata": { "Version": 2, "Tags": ["a", "b"] }`), and `null` elements inside the root array.
   - **Duplicate Token Precedence & Foreground Support:** Duplicate `"Name"` entries where the later entry wins, and entries setting both `"Background"` and `"Foreground"`.
   - **Empty / `null` Root & Stream/File Overloads:** `"[]"`, `"null"`, `Parse(Stream)` (UTF-8 stream with and without BOM), `ParseFile(tempFilePath)`, and `ArgumentNullException` guards on `null` inputs.
   - **Malformed JSON (`FormatException` Contract):**
     - Non-array root (`"{}"`, `"123"`, `"\"str\""`) → throws `FormatException` with line/column info.
     - Unterminated string, invalid `\u12G4` escape, unclosed `/*` comment, missing `:` or `,`, unclosed `[` or `{`, or trailing garbage after `]` (`"[] trailing"`) → throws `FormatException`.

### Step 3.3 — Expand STA Theme Regression Tests Across All 12 Restored Themes

Update `source/AutomationTest/AvalonDockTest/VsThemeToggleDockingManagerTests.cs` (and check `MenuItemExContentAlignmentTests.cs` / `DependencyInjectionTests.cs` if `d0135f0` retargeted VS theme cases there):
1. Add a parameterized `[TestCaseSource]` covering all **12 restored theme factories**:
   - `Vs2013BlueTheme`, `Vs2013DarkTheme`, `Vs2013LightTheme` (`AvalonDock.Themes`)
   - `VS2015BlueTheme`, `VS2015DarkTheme`, `VS2015LightTheme` (`AvalonDock.Themes.VS`)
   - `VS2022BlueTheme`, `VS2022DarkTheme`, `VS2022LightTheme` (`AvalonDock.Themes.VS`)
   - `VS2026BlueTheme`, `VS2026DarkTheme`, `VS2026LightTheme` (`AvalonDock.Themes.VS`)
2. Assert for each of the 12 themes on an STA thread:
   - `theme.ThemeResourceDictionary` is non-null, merges `Generic.xaml` cleanly, and every `SolidColorBrush` directly stored in `theme.ThemeResourceDictionary.Values` has `IsFrozen == true`.
   - `theme.ThemeResourceDictionary` (including its `MergedDictionaries` lookup via a host `FrameworkElement`) resolves:
     - `"ToggleAnchorablePaneControlStyle"` (`Style`)
     - `ToggleDockButton.ForegroundBrushKey` (`Brush`)
     - `typeof(ToggleDockButton)` (`Style`)
     - `typeof(ToggleDockingManager)` (`Style`)
     - `"AnchorableContextMenu"` (`ContextMenu`) containing a `MenuItem` bound to `DetachToWindowCommand`.
   - Applying `ToggleAnchorablePaneTitle` template inside a host `Border` with `theme.ThemeResourceDictionary` merged succeeds without throwing and finds `PART_MinimizeButton`.
   - Switching `dockManager.Theme` across `VS2010Theme` → `Vs2013DarkTheme` → `VS2015BlueTheme` → `VS2022DarkTheme` → `VS2026LightTheme` → `ArcDarkTheme` → `GenericTheme` updates `DockingManager.Resources.MergedDictionaries` cleanly without leaking stale `DictionaryTheme` dictionaries! *(Specifically verify `DictionaryTheme` → `DictionaryTheme` transitions, e.g., `Vs2013DarkTheme` → `VS2026DarkTheme`, properly remove the previous `currentThemeResourceDictionary`.)*
3. Add a test for `VsTheme.FromJson(stream, basePalette)` verifying custom runtime VS2026 JSON override loading produces a valid `DictionaryTheme`.

---

## 4. Verification & Exit Gate to Plan 04

Build the solution under `--warnaserror` and execute both the targeted VS theme fixtures and the full non-FlaUI regression suite single-threaded (`-m:1`):

```powershell
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj `
  --configuration Release --no-build --framework net48 `
  --filter "FullyQualifiedName~VsJsonThemeParserTests|FullyQualifiedName~VsThemeParserTests|FullyQualifiedName~VsThemeToggleDockingManagerTests" -m:1

dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category!=FlaUI" -m:1
```

**Expected Result:**
- `0` compiler/analyzer warnings.
- `100%` of parser unit tests and 12-theme STA regression tests pass, and `0` regressions in the existing STA suite (confirming no `Application.Current` corruption).
