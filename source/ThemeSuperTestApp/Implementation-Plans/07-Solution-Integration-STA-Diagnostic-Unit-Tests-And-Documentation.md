# Plan 07 (of 07) — Solution Integration, STA Diagnostic Unit Tests & `ThemeSuperTestApp` Documentation

> **Execution Order:** **Step 7 of 7** (Final stage — requires [Plan 06](06-Tool-5-Runtime-VsTheme-Loader-Specimens-And-Stress-Harness.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries; **never** call `Application.Current.Shutdown()` in STA tests; always run `dotnet test` with **`-m:1`**.

---

## 1. Objective & Scope

Integrate `ThemeSuperTestApp` cleanly into `source/AvalonDock.sln` and `source/CleanAll.bat`, add automated non-UI `[Apartment(ApartmentState.STA)]` unit tests in `source/AutomationTest/AvalonDockTest/` covering `ThemeSuperTestApp`'s core diagnostic engines, author `source/ThemeSuperTestApp/README.md`, and run the complete build, test, dependency, and zero-theme-modification verification gates.

---

## 2. Step-by-Step Implementation Tasks

### Step 7.1 — Wire `ThemeSuperTestApp.csproj` into `source/AvalonDock.sln` & `source/CleanAll.bat`
1. Add `source/ThemeSuperTestApp/ThemeSuperTestApp.csproj` to `source/AvalonDock.sln` under the Samples folder (matching `ExperimentalTestApp`'s solution placement and `Debug|Any CPU` / `Release|Any CPU` configurations):
   ```powershell
   dotnet sln source/AvalonDock.sln add source/ThemeSuperTestApp/ThemeSuperTestApp.csproj
   ```
   *(Inspect `source/AvalonDock.sln` diff afterward to verify clean GUIDs, folder nesting if used, and line endings.)*
2. Update `source/CleanAll.bat` to clean `ThemeSuperTestApp\bin` and `ThemeSuperTestApp\obj` alongside `ExperimentalTestApp`.

### Step 7.2 — Add STA Diagnostic Unit Tests in `source/AutomationTest/AvalonDockTest/`
1. Add `<ProjectReference Include="..\..\ThemeSuperTestApp\ThemeSuperTestApp.csproj" />` to `source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj`.
2. Create `source/AutomationTest/AvalonDockTest/ThemeSuperTestAppDiagnosticsTests.cs` (`[TestFixture]`, `[Apartment(ApartmentState.STA)]` — **never** calling `Application.Current.Shutdown()`, closing any temporary `Window` in `finally` / `[TearDown]`!):
   - **`Catalog_ContainsAll20BuiltInThemes_LazilyWithoutEagerInstantiation`:**
     - Constructs `new ThemeCatalogService()`, asserts `Themes.Count == 20`, and asserts `Themes.All(t => !t.IsInstantiated)` (proving lazy descriptor registration!).
   - **`Catalog_ClassifiesBaseClassesNamespacesAndXmlnsMappingsAccurately`:**
     - Asserts the `8` URI themes (`Arc*`, `VS2010`, `Expression*`, `Metro`, `Aero`, `Generic`) report `BaseClassKind == ThemeBaseClassKind.UriTheme` and `IsMappedInXmlnsDefinition == true`.
     - Asserts `Vs2013{Blue,Dark,Light}Theme` report `BaseClassKind == ThemeBaseClassKind.DictionaryTheme`, `ClrNamespace == "AvalonDock.Themes"`, and `IsMappedInXmlnsDefinition == true`.
     - Asserts all `9` `AvalonDock.Themes.VS` themes (`VS2015*`, `VS2022*`, `VS2026*`) report `BaseClassKind == ThemeBaseClassKind.VsTheme`, `ClrNamespace == "AvalonDock.Themes.VS"`, and **`IsMappedInXmlnsDefinition == false`** (empirically verifying the XAML `[XmlnsDefinition]` diagnostic!).
   - **`ResourceAuditor_AuditsBothUriThemesAndDictionaryThemes_AndDetectsUnfrozenBrushes`:**
     - Audits `VS2022DarkTheme` and `Vs2013DarkTheme`: asserts `UnfrozenFreezableCount == 0` and `FrozenFreezableCount > 50`.
     - Audits a synthetic `DictionaryTheme` containing one intentionally unfrozen `new SolidColorBrush(Colors.Red)` and one frozen brush: asserts `UnfrozenFreezableCount == 1` (proving the unfrozen-`Freezable` detector works!).
     - Runs `DiffThemeKeys` between `ArcDark` and `VS2022Dark` and asserts a valid non-null diff report is produced.
   - **`WindowSharingTracker_DetectsSharedDictionaryForDictionaryTheme_AndDuplicateInstancesForUriTheme`:**
     - Attaches `DockingManager` + a second themed `DetachedAnchorableWindow` (closed cleanly in `finally`!) under `VS2022DarkTheme` (`DictionaryTheme`): asserts `DistinctDictionaryInstances == 1`.
     - Switches to `ArcDarkTheme` (`UriTheme`) and updates both hosts: asserts `DistinctDictionaryInstances == 2`.
   - **`WindowsOsThemeService_CapturesSnapshotSafelyOnNet48`:**
     - Calls `WindowsOsThemeService.CaptureSnapshot()` and asserts non-empty `OsVersionCaption` and valid `AppsThemeMode` / `SystemThemeMode` enum values without throwing.

### Step 7.3 — Write `source/ThemeSuperTestApp/README.md`
Document `ThemeSuperTestApp` thoroughly for human developers and AI agents:
- Purpose & non-goals (dedicated `net48` Caliburn.Micro `6.0.15-beta` theme visual debugger, profiler, and best-practices auditor for classic `DockingManager`).
- Architecture overview (`ThemeCatalogService`, `WindowsOsThemeService`, `DwmWindowChromeHelper`, and Tools 1–5).
- Step-by-step workflow guide: how to use `ThemeSuperTestApp` to diagnose and later verify fixes for eager `DictionaryTheme` constructors, per-window URI `Theme` BAML re-parsing, unfrozen `Freezable` brushes, missing `[XmlnsDefinition]` mappings, `ResourceKeys` gaps, XAML binding errors, and Windows 10/11 OS Dark/Light/High-Contrast chrome interactions.

---

## 3. Final End-to-End Verification Gate

Run all build, test, binary hygiene, and zero-theme-touch checks:

```powershell
# 1. Full Solution Restore & Build (both Debug and Release, zero warnings allowed)
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Debug --no-restore --warnaserror
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# 2. Targeted ThemeSuperTestApp Diagnostic STA Unit Tests (single-threaded)
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net48 --filter "FullyQualifiedName~ThemeSuperTestAppDiagnosticsTests" -m:1

# 3. Full Non-FlaUI Regression Suite (single-threaded)
dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category!=FlaUI" -m:1

# 4. Verify ZERO System.Text.Json / System.Memory / Microsoft.Bcl in ThemeSuperTestApp output
Get-ChildItem "source/ThemeSuperTestApp/bin/Release/net48/*.dll" | Select-Object Name
Select-String -Path "source/ThemeSuperTestApp/ThemeSuperTestApp.csproj" -Pattern "System\.Text\.Json|System\.Memory|Microsoft\.Bcl"

# 5. Verify ZERO modifications to any existing theme or core component file
git status --short -- source/Components/AvalonDock source/Components/AvalonDock.Themes.*
```

**Completion Criteria:**
- Entire 20-project `source/AvalonDock.sln` builds in both `Debug` and `Release` with **0 warnings, 0 errors**.
- All new `ThemeSuperTestAppDiagnosticsTests` and all existing `512` unit/STA tests pass under `-m:1`.
- `ThemeSuperTestApp/bin/{Debug,Release}/net48/` contains **only** clean `net48`/`netstandard2.0` AvalonDock + Caliburn.Micro `6.0.15-beta` + `Microsoft.Xaml.Behaviors` assemblies.
- **Zero** existing theme files in `source/Components/` were modified.
