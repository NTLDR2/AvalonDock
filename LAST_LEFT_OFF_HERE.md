# Where We Last Left Off (`LAST_LEFT_OFF_HERE.md`)

**Date:** 2026-11-26  
**Branch:** `master` (`origin` → `https://github.com/NTLDR2/AvalonDock.git`)  
**Prior Baseline Commit:** `78e5157f8f7ea7cb34ae1f1c44d55fa2bbec0e68`  
**Required Tooling:** `.NET 8.0 SDK` + `.NET Framework 4.8 Developer Pack` **only** (zero `.NET 9` / `.NET 10` / `.NET 11` SDKs, zero `System.Text.Json`)

---

## 1. What Was Just Completed

### A. Strict Two-TFM Consolidation (`net8.0-windows` + `net48` Only)
- **Libraries & Themes (`source/Directory.Build.props`):** Centralized `ComponentTargetFrameworks` and `TestTargetFrameworks` to **`net8.0-windows;net48`** (`netstandard2.0` unchanged for `AvalonDock.Core`, `AvalonDock.Mvvm`, `AvalonDock.Mvvm.CommunityToolkit`, `AvalonDock.DependencyInjection`, and `AvalonDock.Serializer.Xml`).
- **`.NET 8.0` SDK-Only Compilation:** Set `<LangVersion>12</LangVersion>` in `source/Directory.Build.props` and pinned `NUnit` to `4.3.2` in `source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj` (avoiding C# 13 overload-resolution ambiguity introduced in `NUnit` `4.6.x`).
- **Samples & Harnesses Aligned:** Retargeted `TestApp`, `MVVMTestApp`, `WinFormsTestApp`, `ExperimentalTestApp`, `CaliburnDockTestApp`, and `MLibTest` (`MLibTest`, `ServiceLocator`, `SettingsModel`) to **`net8.0-windows;net48`**, and `AvalonDockCodeApp` to **`net8.0-windows`**. Zero `.csproj`, `.props`, or `.yml` files anywhere in the repository reference `net9*`, `net10*`, or `net11*`.
- **CI & Release Workflows (`.github/workflows/ci.yml`, `release.yml`, `dependabot.yml`):** Simplified to install only `.NET 8.0.x` and run FlaUI UI automation on `net8.0-windows` (dropped `.NET 9` / `.NET 10` / `.NET 11` SDK setup and duplicate FlaUI jobs).
- **Documentation Synced:** Updated `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `REPO_MAP.md`, `ROADMAP.md`, `SLOC_Report.md`, `.agents/skills/avalondock-internals/SKILL.md`, and `documentation/**` to match the two-TFM policy.

### B. Fixed `net8.0-windows` STA `testhost.exe` Crash in `AvalonDockTest`
- **Root Cause:** Ephemeral STA worker threads in `ThreadExecutor.RunCodeAsSTA` exited without closing windows or shutting down their `Dispatcher`, leaving Win32 `HwndSubclass.SubclassWndProc` delegates pointing at torn-down CLR thread state on `.NET 8`; `AutomationTestBase.After()` also called `window.Close()` cross-thread.
- **Fix:** Updated `ThreadExecutor.cs`, `AutomationTestBase.cs`, `AnchorablePaneTest.cs`, and `ToggleDockLayoutReloadTests.cs` to close thread-owned windows in `finally` blocks, marshal `window.Close()` onto `window.Dispatcher`, and call `Dispatcher.InvokeShutdown()` on worker STA threads.
- **Result:** All **413/413** unit & STA regression tests (`Category!=FlaUI`) pass cleanly on **both** `net8.0-windows` and `net48`.

### C. Added `source/ExperimentalTestApp` & Fixed Caliburn.Micro Window Input Guardrail
- **New Reference Sample (`source/ExperimentalTestApp/`):** Added to `source/AvalonDock.sln` (`{E1F13CDD-60F2-4221-ABCB-EAE18DE5B452}`), targeting `net8.0-windows;net48` with **Caliburn.Micro `6.0.15-beta`** (zero `System.Text.Json`) + classic `DockingManager` (`DockLayout` + `IDockLayoutService`), `SimpleContainer`, `Conductor<IDocument>.Collection.OneActive`, `CanCloseAsync` Save/Discard/Cancel workflow, `IEventAggregator`, `XmlLayoutSerializer` persistence (`%LOCALAPPDATA%\ExperimentalTestApp\layout.xml`), and live theme switching.
- **Critical Bug Fixed & Documented (`source/ExperimentalTestApp/Views/ShellView.xaml`):**
  - Attaching guarded `<i:KeyTrigger><cal:ActionMessage>` triggers directly to `<Window>` causes Caliburn.Micro's `ActionMessage.ApplyAvailabilityEffect` to set `Window.IsEnabled = false` whenever any guard (`CanSave`, `CanSaveAll`, `CanCloseActiveDocument`) evaluates to `false`, freezing all mouse input on the main window.
  - Window-wide `KeyTrigger`s now live on a zero-sized leaf `<FrameworkElement>` inside the root `<DockPanel>`, keeping `ShellView.IsEnabled == true` at all times. Documented in `source/ExperimentalTestApp/README.md`, `documentation/guides/caliburn-micro.md`, and `AGENTS.md`.

### D. Restored Original Upstream `source/CaliburnDockTestApp` Side-by-Side
- Because the `DockLayout`-based `ExperimentalTestApp` differed in UI/feel from what was originally expected of `CaliburnDockTestApp`, the new app was cleanly isolated as **`source/ExperimentalTestApp/`** and all **14 original files** from `Dirkster99/AvalonDock` (`master`) were copied back into **`source/CaliburnDockTestApp/`** (standalone outside `AvalonDock.sln`).
- Only line 5 of `source/CaliburnDockTestApp/CaliburnDockTestApp.csproj` was changed (`net9.0-windows;net48` → `net8.0-windows;net48`).
- Both `CaliburnDockTestApp` and `ExperimentalTestApp` build with **0 warnings / 0 errors** and launch responsive Win32/WPF windows (`Visible=True`, `IsWindowEnabled=True`) on both `net8.0-windows` and `net48` with **zero collisions**.

---

## 2. Current Verified Health Snapshot

```powershell
# Restore & build entire 16-project solution (0 warnings, 0 errors)
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# Build standalone restored upstream CaliburnDockTestApp (0 warnings, 0 errors on net8.0-windows & net48)
dotnet restore source/CaliburnDockTestApp/CaliburnDockTestApp.csproj
dotnet build source/CaliburnDockTestApp/CaliburnDockTestApp.csproj --configuration Release --no-restore

# Run unit & STA regression suite across net8.0-windows and net48 (413/413 pass on each TFM)
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1
```

> **Environment Notes for Next Session:**
> 1. **Disk Space on `C:`:** Free space is tight (~2.3 GB). Avoid accumulating unnecessary `Debug` / temporary build folders and clean scratch artifacts promptly.
> 2. **PowerShell Execution Policy:** Local `.ps1` scripts are blocked by machine policy (`Restricted`); run inline commands directly in PowerShell (`;` + `$LASTEXITCODE` checks).
> 3. **Testing Rule (`AGENTS.md`):** Always pass `-m:1` to `dotnet test`, and **never** call `Application.Current.Shutdown()` in tests.

---

## 3. Where to Pick Up Next

1. **`CaliburnDockTestApp` vs. `ExperimentalTestApp` Next Steps:**
   - Both apps now sit cleanly side-by-side (`source/CaliburnDockTestApp/` = classic upstream `DocumentsSource` + Caliburn.Micro `4.0.136-rc`; `source/ExperimentalTestApp/` = `DockLayout` + `IDockLayoutService` + Caliburn.Micro `6.0.15-beta`).
   - Ready to either modernize `CaliburnDockTestApp` in-place along its classic `DocumentsSource` / `Conductor` style, refine `ExperimentalTestApp`, or extract reusable pieces toward Phase 3 (`AvalonDock.Mvvm.CaliburnMicro`).
2. **Remaining Standalone Sample Cleanup (`ROADMAP.md` Phase 4):**
   - `source/MLibTest/`: `MLibTest`, `ServiceLocator`, and `SettingsModel` target `net8.0-windows;net48`, but still reference legacy non-SDK `Settings.csproj` (`v4.5.2`), carry dead pre-v5 MSBuild `<ItemGroup Condition>` blocks (`net5.0`, `net40`), and trigger analyzer errors when built standalone.
   - `source/AvalonDocPanelMemoryLeaks/`: Legacy non-SDK `v4.6.1` `.csproj` referencing old `packages.config` `v4.30.0`.
   - `source/AvalonDockCodeApp/`: Currently targets `net8.0-windows` only; evaluate `net48` compatibility if desired.
3. **Broader Fork Roadmap (`ROADMAP.md`):**
   - **Phase 1–2:** Vendoring into `NTLDR2/SomaFrameQD`.
   - **Phase 3:** Optional `AvalonDock.Mvvm.CaliburnMicro` (`netstandard2.0`) BCL paired with `NTLDR2/Caliburn.Micro`.
   - **Phase 5:** `AvalonDock` → **`AvalonDox`** rebranding & assembly/NuGet identity isolation.
