# Plan 10 (of 10) — Antigravity Workspace Skill (`.agents/skills/`), STA Bridge Unit Tests & Documentation Sync

> **Execution Order:** **Step 10 of 10** (Final stage — requires [Plan 09](09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; zero `System.Text.Json` / `System.Memory` / `Microsoft.Bcl.*`; **zero** edits to existing AvalonDock theme libraries; **never** call `Application.Current.Shutdown()` in STA tests; always run `dotnet test` with **`-m:1`**.

---

## 1. Objective & Scope

Complete the Antigravity <-> `ThemeSuperTestApp` integration by authoring a permanent workspace skill (`.agents/skills/theme-super-test-bridge/SKILL.md`) so Antigravity automatically knows how to inspect `F12` snapshots and drive a live `ThemeSuperTestApp.exe` session in any conversation, adding isolated temp-directory STA unit tests in `AvalonDockTest`, updating `source/ThemeSuperTestApp/README.md`, and running the full 20-project solution build and regression test gates.

---

## 2. Step-by-Step Implementation Tasks

### Step 10.1 — Create Workspace Skill `.agents/skills/theme-super-test-bridge/SKILL.md`
Create `.agents/skills/theme-super-test-bridge/SKILL.md` (with valid YAML frontmatter `name: theme-super-test-bridge` and `description: ...`, automatically discovered by Antigravity and tracked by `!/.agents/**` in `.gitignore`) documenting:
1. **When to Activate:** Whenever visually inspecting, debugging, benchmarking, or refactoring AvalonDock themes, checking a user-pushed `F12` snapshot from `ThemeSuperTestApp`, or driving a live `ThemeSuperTestApp.exe` session.
2. **Presence Check (`heartbeat.xml`):** How to read `source/ThemeSuperTestApp/AgentBridge/heartbeat.xml` (`State == "Listening"`, `UpdatedAtUtc` recency, `ProcessId`) to verify whether `ThemeSuperTestApp.exe` is currently running on the user's desktop (and how to ask the user to launch it or read the most recent `latest/` snapshot if stopped).
3. **Reading User-Pushed `F12` Snapshots:** Exact paths under `source/ThemeSuperTestApp/AgentBridge/latest/`:
   - `latest-context.md` (user note + OS state + active theme + unfrozen `Freezable` audit + multi-window sharing + WPF binding errors)
   - `latest-composite.png` (single-call multi-window visual overview via `read_file`)
   - `latest-main-wpf.png` & `latest-main-dwm.png` (high-res client vs. native DWM title-bar images via `read_file`).
4. **Sending Live Remote Commands (`inbox/<requestId>.xml` -> `outbox/<requestId>-response.xml`):**
   - Exact `<AgentCommand>` XML templates for all allow-listed verbs (`ping`, `get-state`, `apply-theme`, `set-dwm-mode`, `set-shell-options`, `spawn-overflow-tabs`, `close-overflow-tabs`, `spawn-floating`, `spawn-detached`, `dock-all`, `reset-layout`, `benchmark-cold-ctors`, `load-inline-json`, `capture-snapshot`, `sweep-all-20-screenshots`).
   - Atomic write pattern (or direct `write_file` + waiting briefly via `schedule` / command check if needed, reading `<requestId>-response.xml`, and calling `read_file` on the returned `.png` paths).

### Step 10.2 — Add Isolated Temp-Folder STA Unit Tests (`ThemeSuperTestAppAgentBridgeTests.cs`)
Create `source/AutomationTest/AvalonDockTest/ThemeSuperTestAppAgentBridgeTests.cs` (`[TestFixture]`, `[Apartment(ApartmentState.STA)]` — using a unique `Path.Combine(Path.GetTempPath(), ...)` directory deleted in `[TearDown]` so tests never touch the workspace's real `source/ThemeSuperTestApp/AgentBridge/` folder, and **never** calling `Application.Current.Shutdown()`!):
1. **`ScreenshotService_CapturesValidWpfPngAndCompositePng_AndEnforcesRollingRetentionCap`:**
   - Measures/arranges an off-screen `Border` / `DockingManager`, captures WPF `RenderTargetBitmap` + composite PNGs via `WindowScreenshotService`, asserts files exist with valid PNG magic bytes (`0x89, 0x50, 0x4E, 0x47`), and verifies rolling retention prunes archived captures above the configured cap while keeping `latest/*` intact.
2. **`AgentBridgeService_WritesHeartbeat_AndProcessesPingAndApplyThemeXmlCommands`:**
   - Starts `AgentBridgeService` against an isolated `AgentBridgePaths(tempDir)`, asserts `heartbeat.xml` exists with `State="Listening"`.
   - Drops `<AgentCommand Id="test-ping" Action="ping" />` and `<AgentCommand Id="test-theme" Action="apply-theme"><Param Name="ThemeId" Value="VS2022Dark" /><Param Name="Capture" Value="false" /></AgentCommand>`, invokes deterministic inbox processing (`ProcessPendingInboxAsync()`), and asserts `outbox/test-ping-response.xml` and `outbox/test-theme-response.xml` report `Status="Ok"` with `ActiveTheme == "VS2022Dark"`.
   - Disposes `AgentBridgeService` and asserts `heartbeat.xml` transitions to `State="Stopped"`.
3. **`AgentBridgeService_LoadInlineJson_ReportsOkOnValidJson_AndErrorOnMalformedJson`:**
   - Sends valid and malformed `load-inline-json` XML commands and verifies `Status="Ok"` vs. `Status="Error"` containing `FormatException` line/column coordinates.

### Step 10.3 — Update `source/ThemeSuperTestApp/README.md` & Plan `01` Sequence Table
- Add Section 5 (`Antigravity Multimodal Bridge & F12 Snapshot Workflow`) to `source/ThemeSuperTestApp/README.md` documenting `F12` snapshots, Tool 6 (`Ctrl+Alt+A`), the XML `inbox/`/`outbox/` command reference, and `.agents/skills/theme-super-test-bridge/SKILL.md`.
- Update the master plan table in `01-Project-Scaffold-Caliburn-Docking-Foundation-And-Theme-Catalog.md` so all 10 plans (`01`–`10`) are indexed cleanly.

---

## 3. Final End-to-End Verification Gate

```powershell
# 1. Full 20-project Solution Build (Debug & Release, zero warnings allowed)
dotnet build source/AvalonDock.sln --configuration Debug --no-restore --warnaserror
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# 2. Targeted AgentBridge STA Unit Tests (-m:1)
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net48 --filter "FullyQualifiedName~ThemeSuperTestAppAgentBridgeTests" -m:1

# 3. Full Non-FlaUI Regression Suite (-m:1)
dotnet test source/AvalonDock.sln --configuration Release --no-build --filter "Category!=FlaUI" -m:1

# 4. Binary Cleanliness, .gitignore Hygiene & Zero-Theme-Edit Checks
Get-ChildItem "source/ThemeSuperTestApp/bin/Release/net48/*.dll" | Select-Object Name
git check-ignore -v source/ThemeSuperTestApp/AgentBridge/heartbeat.xml
git check-ignore -v .agents/skills/theme-super-test-bridge/SKILL.md
```

**Completion Criteria:**
- All 20 projects in `source/AvalonDock.sln` build in both `Debug` and `Release` with **0 warnings, 0 errors**.
- All unit/STA tests (including `ThemeSuperTestAppDiagnosticsTests` and `ThemeSuperTestAppAgentBridgeTests`) pass with `-m:1`.
- `git check-ignore` confirms `source/ThemeSuperTestApp/AgentBridge/*` is ignored while `.agents/skills/theme-super-test-bridge/SKILL.md` is tracked.
- `ThemeSuperTestApp` retains its exact `15` clean DLLs (zero `System.Text.Json`, `System.Memory`, or `Microsoft.Bcl.*`), and zero existing theme files were modified.
