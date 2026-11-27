# Plan 09 (of 10) — Live `AgentBridgeService` XML Mailbox, 20-Theme Screenshot Sweep & Tool 6 UI

> **Execution Order:** **Step 9 of 10** (Requires [Plan 08](08-Dual-Mode-Screenshot-Engine-Composite-Stitcher-And-F12-Snapshot.md) completed; precedes [Plan 10](10-Antigravity-Workspace-Skill-STA-Bridge-Tests-And-Docs-Sync.md)).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; pure inbox `System.Xml.Linq` (`XDocument`) XML protocol (**zero** `System.Text.Json`); **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Build the bidirectional **`AgentBridgeService`** that runs automatically on `ThemeSuperTestApp` startup, publishes a live `heartbeat.xml` presence file, watches `AgentBridge/inbox/*.xml` for allow-listed commands from Antigravity, executes them on the live WPF UI `Dispatcher` (awaiting `DispatcherPriority.Render` before capturing screenshots), writes structured XML responses to `AgentBridge/outbox/`, and surfaces a dedicated dockable **Tool 6: `6. Antigravity Live Bridge`** (`Ctrl+Alt+A`) toolbox inside `DockingManager`.

---

## 2. Step-by-Step Implementation Tasks

### Step 9.1 — Define Pure-XML Protocol Models (`Models/AgentBridgeModels.cs`)
Using inbox `System.Xml.Linq` (`XDocument` / `XElement` — zero `System.Text.Json`):
1. **`AgentBridgeCommand`:**
   - Parsed from `<AgentCommand Id="..." Action="..."> <Param Name="..." Value="..." /> <Payload>...</Payload> </AgentCommand>`:
   - `RequestId` (`string`), `Action` (`string`, case-insensitive), `IReadOnlyDictionary<string, string> Parameters`, `string? Payload` (used for multi-line inline JSON overrides or notes), `SourceFilePath`.
2. **`AgentBridgeResponse`:**
   - Serialized atomically (`.tmp` -> rename) to `AgentBridge/outbox/<RequestId>-response.xml`:
   - Attributes/elements: `Id`, `Action`, `Status` (`"Ok"` | `"Error"`), `DurationMs`, `Timestamp`, `Message`, `ActiveTheme` (`Id`, `DisplayName`, `BaseClass`, `ClrNamespace`), `Screenshots` (`MainWpfPath`, `MainDwmPath`, `CompositePath`), `ReportPath`, and optional `<Items>` payload.
3. **`AgentBridgeLogEntry`:**
   - UI log record shown in Tool 6 (`TimestampText`, `Direction` `[Agent -> App | User F12 -> Agent]`, `RequestId`, `ActionSummary`, `Status`, `DurationMs`, `PrimaryArtifactPath`).

### Step 9.2 — Implement `Services/AgentBridgeService.cs` (`IDisposable`)
1. **Auto-Start & Live Heartbeat (`AgentBridge/heartbeat.xml`):**
   - Starts listening automatically once attached to `ShellView` / `ShellViewModel` (`IsListening = true` by default; toggleable at runtime).
   - Writes `heartbeat.xml` containing `ProcessId`, `State` (`"Listening"` | `"Paused"` | `"Stopped"`), `StartedAt`, `UpdatedAtUtc`, `ActiveThemeId`, `ActiveThemeName`, `ActiveBaseClass`, `OpenFloatingOrDetachedWindows`, and `CapturedWpfErrors` on startup, on every command/theme change, on a `2.5s` heartbeat timer, and writes `State="Stopped"` cleanly inside `Dispose()` on shutdown.
2. **Resilient Dual Watcher (`FileSystemWatcher` + `350 ms` Fallback Poll):**
   - Watches `AgentBridge/inbox/*.xml` (ignoring `*.tmp` files so Antigravity can write `.tmp` and rename atomically, or write `.xml` directly with brief retry on `IOException` file-share locks).
   - Processes inbox files FIFO one-at-a-time on the UI `Dispatcher` (`SemaphoreSlim(1, 1)`), deletes the processed inbox `.xml` file once its response is written to `outbox/`, and prunes `outbox/` responses older than the most recent `50` requests.
3. **Allow-Listed Command Verbs (Zero Arbitrary Code/Shell Execution):**
   - `ping` / `get-state`: Returns live OS snapshot, active theme metadata, open host window count, unfrozen `Freezable` count, and trace error count.
   - `capture-snapshot` (`Note`, `IncludeDwm`): Triggers full screenshot + `latest-context.md` capture and returns exact `.png` & `.md` paths.
   - `apply-theme` (`ThemeId`, `ForceCold`, `Capture`): Applies any of the 20+ themes by `Id` (or display name), awaits `DispatcherPriority.Render`, and (when `Capture != "false"`, default `true`) captures screenshots so Antigravity immediately sees the newly applied theme!
   - `set-dwm-mode` (`Mode`: `DefaultUntouched` | `MatchWindowsOsMode` | `MatchActiveAvalonDockTheme`, optional `Capture`).
   - `set-shell-options` (`StyleOuterShell`, `UseCustomHeader`, `AutoSyncOs`, optional `Capture`).
   - `spawn-overflow-tabs`, `close-overflow-tabs`, `spawn-floating`, `spawn-detached`, `dock-all`, `reset-layout` (each awaiting `DispatcherPriority.Render` and optionally capturing screenshots!).
   - `benchmark-cold-ctors`: Runs `ThemeCatalogService.BenchmarkAllColdConstructors()`, writes the Markdown benchmark table to `outbox/<id>-benchmark.md`, and returns its path.
   - `load-inline-json` (`BasePalette`, `TemplateSet`, `<Payload>` containing VS2026 JSON, optional `Capture`): Parses and applies a custom JSON override live and returns either the rendered screenshot paths or the exact `FormatException (Line X, Column Y)` error!
   - `sweep-all-20-screenshots`: Iterates through all 20 built-in themes on `DockingManager`, awaits `DispatcherPriority.Render` on each, writes 20 labeled PNGs + `sweep-index.md` into `AgentBridge/captures/sweep-<id>/`, restores the prior theme, and returns the sweep index path!

### Step 9.3 — Build Tool 6 (`AgentBridgeToolboxViewModel` / `AgentBridgeToolboxView.xaml`, `Ctrl+Alt+A`) & Header Status Pill
1. **`ViewModels/Tools/AgentBridgeToolboxViewModel.cs` (`Zone = DockZone.LeftBottom`, `IsOpenByDefault = true`, `Shortcut = "Ctrl+Alt+A"`):**
   - `IsListening` two-way toggle (`Listening` green pill vs. `Paused` amber pill).
   - `BridgeDirectoryPath` copyable textbox + `OpenBridgeFolder` action (`Process.Start("explorer.exe", path)`).
   - `UserSnapshotNote` textbox bound directly to `ShellViewModel.UserSnapshotNote` + **`Send Snapshot to Antigravity (F12)`** button + `CleanOldCaptures` button + `ClearLog` button.
   - Live `DataGrid` displaying `LogEntries` (newest first: Time, Direction, Request ID, Action, Status, Duration ms, Output File).
2. **Pinned Header Bar Indicator (`ShellView.xaml`):**
   - Add a compact live `Bridge: Listening (N cmds)` pill + the **`Send Snapshot (F12)`** button in `ShellView.xaml`, and register `AgentBridgeService` + `AgentBridgeToolboxViewModel` in `Bootstrapper.cs` (disposing `AgentBridgeService` cleanly in `Bootstrapper.OnExit`).

---

## 3. Verification & Exit Gate to Plan 10

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Debug --no-restore --warnaserror
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git status --short -- source/Components/
```

**Expected Result:**
- Both `Debug` and `Release` compile with **0 warnings, 0 errors**.
- STA smoke test drops XML commands (`ping`, `apply-theme`, `capture-snapshot`) into an isolated `AgentBridge/inbox/` folder and verifies `AgentBridgeService` processes them, writes valid XML responses to `outbox/` + PNG captures to `latest/`, and updates `heartbeat.xml`.
- Zero edits to `source/Components/`.
