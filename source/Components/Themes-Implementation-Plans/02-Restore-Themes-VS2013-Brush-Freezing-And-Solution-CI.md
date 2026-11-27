# Plan 02 (of 05) — Restore `AvalonDock.Themes.VS2013`, Freeze Palette Brushes & Wire Solution / CI

> **Execution Order:** **Step 2 of 5** (Requires [Plan 01](01-Restore-Themes-VS-And-Zero-Dep-Json-Parser.md) completed; must pass full solution `--warnaserror` build and `dotnet pack` check before starting [Plan 03](03-Parser-Unit-Tests-And-STA-Theme-Regression-Suite.md)).
> **Core Constraint:** Both `AvalonDock.Themes.VS` and `AvalonDock.Themes.VS2013` must target **`net48` exclusively** (matching `Arc`, `VS2010`, `Expression`, `Metro`, and `Aero`) and pack **only** `lib/net48/` in their `.nupkg` archives with zero external NuGet dependencies.

---

## 1. Objective & Scope

1. Restore `source/Components/AvalonDock.Themes.VS2013` (delivering **3 built-in themes**: `Vs2013BlueTheme`, `Vs2013DarkTheme`, `Vs2013LightTheme` in `namespace AvalonDock.Themes`) from local Git history (`b706128`).
2. Optimize `VsThemePaletteFactory.cs` so every runtime-constructed `SolidColorBrush` is frozen via `.Freeze()` (matching `VsThemeResourceBuilder.SetBrush` in `AvalonDock.Themes.VS` and eliminating WPF `Freezable` tracking overhead across ~75 brushes per theme instance).
3. Register both `AvalonDock.Themes.VS` and `AvalonDock.Themes.VS2013` in `source/AvalonDock.sln`, `source/CleanAll.bat`, and `.github/workflows/release.yml` (bringing shipped component NuGet packages from **11 → 13**), and audit the entire solution's `net48` dependency graph to prove zero `System.Text.Json` leakage.

---

## 2. Prerequisites

- [Plan 01](01-Restore-Themes-VS-And-Zero-Dep-Json-Parser.md) is complete: `AvalonDock.Themes.VS.csproj` compiles cleanly under `--warnaserror` on `net48` with zero `System.Text.Json` references.

---

## 3. Step-by-Step Implementation Tasks

### Step 2.1 — Extract `AvalonDock.Themes.VS2013` from Local Git History

Restore the 20 upstream files of `source/Components/AvalonDock.Themes.VS2013` from `b706128`:

```powershell
git checkout b706128183a8edce302b1b633396e2c98d48ba32 -- source/Components/AvalonDock.Themes.VS2013
```

Verify the extracted directory contains:
- `AvalonDock.Themes.VS2013.csproj`:
  - Confirm it targets **`net48` exclusively** via `<TargetFrameworks>$(ComponentTargetFrameworks)</TargetFrameworks>` (matching `source/Directory.Build.props` `<ComponentTargetFrameworks>net48</ComponentTargetFrameworks>`, with zero `net8.0-windows`/`net9.0-windows`/`net10.0-windows`/`net11.0-windows` targets and zero conditional TFM `<ItemGroup>` blocks).
  - Confirm it references only `..\AvalonDock\AvalonDock.csproj` and `..\AvalonDock.Themes.VS\AvalonDock.Themes.VS.csproj` (zero NuGet `<PackageReference>` items).
- `Resources/vs2013{blue,dark,light}.vstheme.gz`
- `OverlayButtons.xaml`, `Themes/Generic.xaml`, `Themes/Icons/IconGeometry.xaml`, `Themes/Menu/*`, `Themes/ResourceKeys.cs`
- `Vs2013BlueTheme.cs`, `Vs2013DarkTheme.cs`, `Vs2013LightTheme.cs` (in `namespace AvalonDock.Themes`), `VsThemePaletteFactory.cs`, `VsThemeResources.cs`

### Step 2.2 — Freeze All Generated `SolidColorBrush` Instances in `VsThemePaletteFactory.cs`

In `source/Components/AvalonDock.Themes.VS2013/VsThemePaletteFactory.cs`, update the three private brush factory helpers (`Brush`, `BrushWithAlpha`, and `Raw`) so every brush is frozen before insertion into the `ResourceDictionary`:

```csharp
private SolidColorBrush Brush(string key)
	=> CreateFrozenBrush(_palette.GetBackgroundOrDefault(key, Colors.Transparent));

private SolidColorBrush BrushWithAlpha(string key, byte alpha)
{
	var c = _palette.GetBackgroundOrDefault(key, Colors.Transparent);
	return CreateFrozenBrush(Color.FromArgb(alpha, c.R, c.G, c.B));
}

private static SolidColorBrush Raw(byte a, byte r, byte g, byte b)
	=> CreateFrozenBrush(Color.FromArgb(a, r, g, b));

private static SolidColorBrush CreateFrozenBrush(Color color)
{
	var brush = new SolidColorBrush(color);
	brush.Freeze();
	return brush;
}
```

*(Also double-check `VsThemeResourceBuilder.cs` in `AvalonDock.Themes.VS` to confirm `SetBrush` and `previewBrush` already call `.Freeze()` on every brush entry.)*

### Step 2.3 — Add Both Projects Back to `source/AvalonDock.sln`

Register both restored theme projects in `source/AvalonDock.sln` (preserving original project GUIDs and solution folder nesting from `b706128` — inspect `git show d0135f0 -- source/AvalonDock.sln` or run `dotnet sln`):
- `Components\AvalonDock.Themes.VS\AvalonDock.Themes.VS.csproj` (nested under the `Components` / `AvalonDock` solution folder alongside `AvalonDock.Themes.Arc` and `AvalonDock.Themes.VS2010`)
- `Components\AvalonDock.Themes.VS2013\AvalonDock.Themes.VS2013.csproj`
- Ensure `Debug|Any CPU`, `Debug|x64`, `Debug|x86`, `Release|Any CPU`, `Release|x64`, and `Release|x86` configuration mappings match the other theme projects in `GlobalSection(ProjectConfigurationPlatforms)`.
- **Important:** Do **not** restore `source/VS2013Test` or `AvalonDock.Serializer.Json` into `AvalonDock.sln`.

### Step 2.4 — Update `source/CleanAll.bat` & `.github/workflows/release.yml`

1. **`source/CleanAll.bat`:**
   - Add `ECHO Components\AvalonDock.Themes.Arc`, `ECHO Components\AvalonDock.Themes.VS`, and `ECHO Components\AvalonDock.Themes.VS2013` to the header banner and corresponding `RMDIR /S /Q` blocks for `bin` and `obj`.
2. **`.github/workflows/release.yml`:**
   - In the **`Pack NuGet packages`** step (`projects=(...)`), add:
     - `"source/Components/AvalonDock.Themes.VS/AvalonDock.Themes.VS.csproj"`
     - `"source/Components/AvalonDock.Themes.VS2013/AvalonDock.Themes.VS2013.csproj"`
   - In the **`Unlist preview NuGet packages`** step (`packages=(...)`), add:
     - `"Dirkster.AvalonDock.Themes.VS"`
     - `"Dirkster.AvalonDock.Themes.VS2013"`

---

## 4. Verification & Exit Gate to Plan 03

Run full solution restore, Release `--warnaserror` build, dry-run NuGet pack of both restored packages, and a repository-wide `System.Text.Json` lockfile audit:

```powershell
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
dotnet pack source/Components/AvalonDock.Themes.VS/AvalonDock.Themes.VS.csproj --configuration Release --no-build -o artifacts/nupkg-check
dotnet pack source/Components/AvalonDock.Themes.VS2013/AvalonDock.Themes.VS2013.csproj --configuration Release --no-build -o artifacts/nupkg-check

# Verify both .nupkg archives ship ONLY lib/net48/ (no net8/net9/net10/net11 folders)
Add-Type -AssemblyName System.IO.Compression.FileSystem
Get-ChildItem "artifacts/nupkg-check/*.nupkg" | ForEach-Object {
  Write-Output "=== $($_.Name) ==="
  [System.IO.Compression.ZipFile]::OpenRead($_.FullName).Entries |
    Where-Object { $_.FullName -like "lib/*" -or $_.FullName -like "*.nuspec" } |
    Select-Object -ExpandProperty FullName
}

Remove-Item -Recurse -Force artifacts/nupkg-check
```

Audit `bin/Release` directories of both theme projects (must contain **only** `net48`) and every `project.assets.json` across `source/` for `System.Text.Json`:

```powershell
Get-ChildItem "source/Components/AvalonDock.Themes.VS/bin/Release","source/Components/AvalonDock.Themes.VS2013/bin/Release" -Directory | Select-Object FullName
Get-ChildItem -Path "source" -Recurse -Filter "project.assets.json" |
  Select-String -Pattern "System\.Text\.Json"
```

**Expected Result:**
- Whole-solution `dotnet build --warnaserror` succeeds with **0 warnings, 0 errors**.
- Both `bin/Release/` folders contain **only** `net48`, and both `.nupkg` archives contain **only** `lib/net48/` (zero modern `.NET` TFM folders) and zero external NuGet dependencies.
- Repository-wide `project.assets.json` scan for `System.Text.Json` returns **zero matches**.
