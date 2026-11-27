# Where We Last Left Off (`LAST_LEFT_OFF_HERE.md`)

**Date:** 2026-11-26  
**Branch:** `master` (`origin` → `https://github.com/NTLDR2/AvalonDock.git`)  
**Required Tooling:** `.NET 8.0 SDK` + `.NET Framework 4.8 Developer Pack` **only** (zero `.NET 9` / `.NET 10` / `.NET 11` SDKs, zero `System.Text.Json`)

---

## 1. What Was Just Completed

### A. Strict `net48` + `netstandard2.0` Consolidation Across Entire Repository
- **UI-Agnostic Libraries Kept on `netstandard2.0` (`source/Components/`):** `AvalonDock.Core`, `AvalonDock.Mvvm`, `AvalonDock.Mvvm.CommunityToolkit`, `AvalonDock.DependencyInjection`, and `AvalonDock.Serializer.Xml` remain strictly `netstandard2.0` with zero WPF dependencies.
- **WPF Libraries, Themes, Samples & All Tests Retargeted Strictly to `net48`:**
  - Centralized `ComponentTargetFrameworks` and `TestTargetFrameworks` in `source/Directory.Build.props` to **`net48`**.
  - Retargeted `TestApp`, `AvalonDockCodeApp`, `ExperimentalTestApp`, `MVVMTestApp`, `WinFormsTestApp`, standalone `CaliburnDockTestApp`, and `MLibTest` (`MLibTest`, `ServiceLocator`, `SettingsModel`) strictly to **`net48`**.
  - Applied minimal mechanical `net48` compatibility replacements in `source/AvalonDockCodeApp/` (WinForms `FolderBrowserDialog`, `Split(new[] { ... })`, `.Substring(...)`, and `Dispatcher.BeginInvoke(new Action(...))` — zero optimization bloat) so the flagship `ToggleDockingManager` IDE demo and its FlaUI `ToggleDock` automation suite run natively on `.NET Framework 4.8`.
- **CI & Release Workflows (`.github/workflows/ci.yml`, `release.yml`):** Updated FlaUI UI automation steps to run on `--framework net48`.
- **Documentation Synced:** Updated `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `REPO_MAP.md`, `ROADMAP.md`, `source/ExperimentalTestApp/README.md`, and `documentation/**` to reflect strict `net48` + `netstandard2.0` targeting while preserving explicit `Upstream v5.0.0`–`v5.1.0` vs. `NTLDR2 Fork v5.1.0` scope labels.

### B. Previous Milestones Preserved
- **`.NET 8.0` SDK-Only Compilation:** `<LangVersion>12</LangVersion>` in `source/Directory.Build.props` with `NUnit` pinned to `4.3.2` in `AvalonDockTest.csproj`.
- **STA Worker Dispatcher Hygiene:** `ThreadExecutor.cs` and `AutomationTestBase.cs` cleanly close worker-owned windows and shut down worker STA dispatchers without calling `Application.Current.Shutdown()`.
- **Side-by-Side Caliburn.Micro Samples:** `source/ExperimentalTestApp/` (`Caliburn.Micro 6.0.15-beta` + `DockLayout` + `IDockLayoutService` in `AvalonDock.sln`) alongside restored upstream `source/CaliburnDockTestApp/` (`Caliburn.Micro 4.0.136-rc` standalone).

---

## 2. Current Verified Health Snapshot

```powershell
# Restore & build entire solution on netstandard2.0 + net48 (0 warnings, 0 errors)
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# Build standalone restored upstream CaliburnDockTestApp on net48 (0 warnings, 0 errors)
dotnet restore source/CaliburnDockTestApp/CaliburnDockTestApp.csproj
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Release --no-restore

# Run unit & STA regression suite on net48 (413/413 pass)
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1

# Run FlaUI desktop UI automation suite on net48
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net48 -m:1
```

> **Environment Notes for Next Session:**
> 1. **Disk Space on `C:`:** Free space is tight. Avoid accumulating unnecessary `Debug` / temporary build folders and clean scratch artifacts promptly.
> 2. **PowerShell Execution Policy:** Local `.ps1` scripts are blocked by machine policy (`Restricted`); run inline commands directly in PowerShell (`;` + `$LASTEXITCODE` checks).
> 3. **Testing Rule (`AGENTS.md`):** Always pass `-m:1` to `dotnet test`, and **never** call `Application.Current.Shutdown()` in tests.

---

## 3. Where to Pick Up Next

1. **Remaining Legacy Standalone Sample Cleanup (`ROADMAP.md` Phase 4):**
   - `source/MLibTest/`: `MLibTest`, `ServiceLocator`, `SettingsModel`, and non-SDK `Settings.csproj` now all target `.NET Framework 4.8`, while still carrying dead pre-v5 MSBuild `<ItemGroup Condition>` blocks (`net5.0`, `net40`).
   - `source/AvalonDocPanelMemoryLeaks/`: Legacy non-SDK `v4.8` `.csproj` referencing old `packages.config` `v4.30.0`.
2. **Broader Fork Roadmap (`ROADMAP.md`):**
   - **Phase 2:** Vendoring into `NTLDR2/SomaFrameQD`.
   - **Phase 3:** Optional `AvalonDock.Mvvm.CaliburnMicro` (`netstandard2.0`) BCL paired with `NTLDR2/Caliburn.Micro`.
   - **Phase 5:** `AvalonDock` → **`AvalonDox`** rebranding & assembly/NuGet identity isolation.
