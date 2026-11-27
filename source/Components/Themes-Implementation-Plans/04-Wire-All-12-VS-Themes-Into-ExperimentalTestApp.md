# Plan 04 (of 05) — Wire All 12 Restored VS Themes into `ExperimentalTestApp` Only

> **Execution Order:** **Step 4 of 5** (Requires [Plan 03](03-Parser-Unit-Tests-And-STA-Theme-Regression-Suite.md) completed; must build and pass all tests before starting [Plan 05](05-Sync-Docs-THEMES-Report-AGENTS-And-ROADMAP.md)).

---

## 1. Objective & Scope

Wire all **12 restored VS themes** (`Vs2013{Dark,Light,Blue}`, `VS2015{Dark,Light,Blue}`, `VS2022{Dark,Light,Blue}`, `VS2026{Dark,Light,Blue}`) plus `GenericTheme` (if desired alongside existing themes) into **`source/ExperimentalTestApp` ONLY**, enabling live interactive testing of all themes across docked panes, tear-off `LayoutFloatingWindowControl` windows, standalone `DetachedAnchorableWindow` OS windows, and `XmlLayoutSerializer` persistence under Caliburn.Micro `6.0.15-beta`.

### Explicit Scope Boundaries (Agreed)
- **Touch `source/ExperimentalTestApp` ONLY** among sample applications.
- **Keep `source/VS2013Test` deleted** (do **not** restore the legacy standalone sample).
- **Do NOT modify** `source/TestApp`, `source/AvalonDockCodeApp`, `source/MVVMTestApp`, `source/WinFormsTestApp`, `source/MLibTest`, or `source/CaliburnDockTestApp` (keeping FlaUI test harnesses on `TestApp` and `AvalonDockCodeApp` untouched).

---

## 2. Prerequisites

- [Plan 03](03-Parser-Unit-Tests-And-STA-Theme-Regression-Suite.md) is complete: all parser unit tests and 12-theme STA regression tests pass on `net48`.

---

## 3. Step-by-Step Implementation Tasks

### Step 4.1 — Add Theme Project References to `ExperimentalTestApp.csproj`

Edit `source/ExperimentalTestApp/ExperimentalTestApp.csproj` to add `<ProjectReference>` entries for the two restored theme packages alongside the existing five theme packages:

```xml
<ProjectReference Include="..\Components\AvalonDock.Themes.VS\AvalonDock.Themes.VS.csproj" />
<ProjectReference Include="..\Components\AvalonDock.Themes.VS2013\AvalonDock.Themes.VS2013.csproj" />
```

### Step 4.2 — Register All 12 Restored VS Themes in `ExperimentalTestApp/Bootstrapper.cs`

Edit `source/ExperimentalTestApp/Bootstrapper.cs`:
1. Add `using AvalonDock.Themes.VS;` alongside `using AvalonDock.Themes;`.
2. Keep `public const string DefaultThemeName = "VS2010";` unchanged as the start-up default.
3. Expand the `new ThemeManager(new Theme[] { ... })` registration in `Configure()` to include all **12** restored themes (bringing `ExperimentalTestApp`'s theme selector from 7 themes to **19** — or **20** including `GenericTheme` at the bottom):
   ```csharp
   var themeManager = new ThemeManager(new Theme[]
   {
   	new VS2010Theme(),
   	new Vs2013LightTheme(),
   	new Vs2013BlueTheme(),
   	new Vs2013DarkTheme(),
   	new VS2015LightTheme(),
   	new VS2015BlueTheme(),
   	new VS2015DarkTheme(),
   	new VS2022LightTheme(),
   	new VS2022BlueTheme(),
   	new VS2022DarkTheme(),
   	new VS2026LightTheme(),
   	new VS2026BlueTheme(),
   	new VS2026DarkTheme(),
   	new ArcLightTheme(),
   	new ArcDarkTheme(),
   	new MetroTheme(),
   	new ExpressionLightTheme(),
   	new ExpressionDarkTheme(),
   	new AeroTheme(),
   	new GenericTheme(),
   });
   ```
   *(Verification note: `ShellViewModel` already populates `Themes` dynamically via `themeManager.AvailableThemes.Select(t => t.Name).ToList().AsReadOnly()` and applies selections via `_themeManager.ApplyTheme(value)`. Because every concrete subclass has a distinct `GetType()` and unique `Theme.Name` — `"Vs2013Dark"`, `"VS2015Dark"`, `"VS2022Dark"`, `"VS2026Dark"`, etc. — zero changes are required in `ShellViewModel.cs` or `ShellView.xaml`!)*

### Step 4.3 — Update `ExperimentalTestApp/README.md`

Update line 14 of `source/ExperimentalTestApp/README.md` from *"and the five theme packages"* to *"and all seven theme packages (`VS2010`, `VS2013`, `VS` [2015/2022/2026], `Arc`, `Metro`, `Expression`, `Aero`, plus built-in `GenericTheme`)"*, confirming that zero `System.Text.Json` remains in `ExperimentalTestApp`'s `net48` output directory.

---

## 4. Verification & Exit Gate to Plan 05

Build the full solution, verify `ExperimentalTestApp`'s output folder has zero `System.Text.Json` or unexpected transitive DLLs, and run the test suites:

```powershell
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

Get-ChildItem "source/ExperimentalTestApp/bin/Release/net48/*.dll" | Select-Object Name

dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category!=FlaUI" -m:1
```

**Expected Result:**
- `ExperimentalTestApp` compiles cleanly (`<Nullable>enable</Nullable>`, `--warnaserror`).
- `source/ExperimentalTestApp/bin/Release/net48/` contains `AvalonDock.Themes.VS.dll` and `AvalonDock.Themes.VS2013.dll` alongside the 5 existing theme DLLs, `Caliburn.Micro*.dll`, and `Microsoft.Xaml.Behaviors.dll` — with **zero** `System.Text.Json*`, `System.Memory*`, or `Microsoft.Bcl*` assemblies.
- All unit and STA tests remain 100% green, and `git status` confirms no unintended edits to `TestApp`, `AvalonDockCodeApp`, or other samples.
