---
name: theme-super-test-bridge
description: >-
  Multimodal inspection and live XML remote-control guide for ThemeSuperTestApp
  (source/ThemeSuperTestApp/) on .NET Framework 4.8 (net48). Activate this skill
  whenever visually inspecting, debugging, profiling, auditing, or comparing
  AvalonDock v5 themes, reading user-pushed F12 screenshot + diagnostic bundles
  in source/ThemeSuperTestApp/AgentBridge/latest/, or driving a live
  ThemeSuperTestApp.exe session via the AgentBridge XML inbox/outbox mailbox.
---

# ThemeSuperTestApp Multimodal Bridge (`theme-super-test-bridge`)

`ThemeSuperTestApp` (`source/ThemeSuperTestApp/`) is AvalonDock v5's dedicated `net48` + `Caliburn.Micro 6.0.15-beta` visual debugger, performance profiler, and best-practices auditor for all **18 built-in themes** (plus runtime `.vstheme` / `.vstheme.gz` / VS2026 `.json` themes).

It exposes a workspace-local, zero-`System.Text.Json`, pure-XML (`System.Xml.Linq.XDocument`) file mailbox and dual-mode screenshot engine rooted at:

```text
source/ThemeSuperTestApp/AgentBridge/
├── heartbeat.xml                 # Live process presence, listener state & active theme
├── latest/                       # Predictable fixed paths updated on F12 or capture commands
│   ├── latest-context.md         # Operator note + image paths + full 6-section diagnostic report
│   ├── latest-composite.png      # Vertically stitched multi-window overview PNG (inspect first)
│   ├── latest-main-wpf.png       # High-DPI WPF RenderTargetBitmap of ShellView (occlusion-safe)
│   └── latest-main-dwm.png       # Win32 PrintWindow (PW_RENDERFULLCONTENT) with OS DWM title bar
├── inbox/                        # Drop <requestId>.xml commands here for live UI execution
├── outbox/                       # Read <requestId>-response.xml (+ benchmark/report .md files)
└── captures/                     # Rolling archive (40 capture sets max) + sweep-<id>/ folders
```

---

## 1. When to Activate & Core Workflows

Use this skill in either of two workflows:

1. **Passive Inspection (User-Pushed `F12` Snapshot):**
   - The user presses **`F12`** (or clicks **Send Snapshot to Antigravity (F12)** in Row 2 or Tool 6 `Ctrl+Alt+A`) inside `ThemeSuperTestApp` and asks you to inspect a visual glitch, contrast issue, tab header template, DWM title bar, or resource/binding warning.
   - Read `latest-context.md` and `latest-composite.png` in parallel via `read_file` (no live commands required unless you want follow-up comparisons).
2. **Active Remote Control (Live `inbox/` -> `outbox/` XML Commands):**
   - You want to switch themes, toggle DWM dark title bars or custom tab headers, spawn overflow tabs / floating windows / standalone OS `DetachedAnchorableWindow`s, test a live VS2026 inline JSON palette override, benchmark cold constructors, or run an automated 18-theme screenshot sweep.
   - Check `heartbeat.xml` first, write `<AgentCommand>` XML to `inbox/<requestId>.xml`, wait briefly for `outbox/<requestId>-response.xml`, and inspect the resulting XML + PNGs.

---

## 2. Step 1 — Presence & Liveness Check (`heartbeat.xml`)

Before dropping commands into `inbox/`, read `source/ThemeSuperTestApp/AgentBridge/heartbeat.xml`:

```xml
<AgentBridgeHeartbeat
  ProcessId="24180"
  State="Listening"
  StartedAtUtc="2026-11-27T17:45:10.1234567+00:00"
  UpdatedAtUtc="2026-11-27T17:52:35.8912345+00:00"
  ProcessedCommandCount="3"
  ActiveThemeId="ExpressionDark"
  ActiveThemeName="Expression Dark"
  ActiveBaseClass="Theme (URI)"
  WpfTraceErrorCount="0"
  InboxDirectory="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\inbox"
  OutboxDirectory="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\outbox"
  LatestDirectory="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest" />
```

### Presence Decision Rules

- **`State == "Listening"` and `UpdatedAtUtc` is recent:** `AgentBridgeService` refreshes `heartbeat.xml` every **`2.5 s`** (and on every command, theme switch, or OS mode change). If `UpdatedAtUtc` is within ~10–15 seconds of current UTC time (or `Get-Process -Id <ProcessId> -ErrorAction SilentlyContinue` confirms `ThemeSuperTestApp` is running), the bridge is live and ready to execute `inbox/*.xml` commands immediately.
- **`State == "Paused"`:** The app is running, but the user unchecked **Auto-Listen on `AgentBridge/inbox/`** in **Tool 6 (`6. Antigravity Live Bridge`, `Ctrl+Alt+A`)**. You can still read `latest/*` snapshots, or ask the user to re-enable the listener checkbox in Tool 6.
- **`heartbeat.xml` missing, `State == "Stopped"`, or `UpdatedAtUtc` is stale:** `AgentBridgeService.Dispose()` writes `State="Stopped"` on clean exit (a crashed/killed process leaves a stale `UpdatedAtUtc`).
  - If you only need to inspect the user's last `F12` push, read the existing files in `source/ThemeSuperTestApp/AgentBridge/latest/` directly.
  - If you need live command execution, ask the user to launch `.\source\ThemeSuperTestApp\bin\Debug\net48\ThemeSuperTestApp.exe` (or `Release\net48\ThemeSuperTestApp.exe`).

---

## 3. Step 2 — Reading User-Pushed `F12` Snapshots (`AgentBridge/latest/`)

Antigravity's `read_file` tool supports multimodal `.png` inspection alongside `.md` text. Always batch your reads in a single turn:

1. **Primary Read Pair (call `read_file` on both in parallel):**
   - `source/ThemeSuperTestApp/AgentBridge/latest/latest-context.md` — Contains:
     - `Capture ID` & `Captured At` timestamp
     - Optional **`## User / Operator Note for Antigravity`** typed in Tool 6 (`UserSnapshotNote`)
     - Exact file paths to all captured primary and secondary window PNGs
     - Full 6-section diagnostic report: Windows OS build / `AppsUseLightTheme` / `SystemUsesLightTheme` / `HighContrast` / DWM `AccentColor`, active theme CLR inheritance & `[XmlnsDefinition]` status, unfrozen `Freezable` count (`!IsFrozen`), multi-window `ResourceDictionary` object-identity sharing (`RuntimeHelpers.GetHashCode`), cold `.ctor` / `Apply` timings, and in-process `PresentationTraceSources` binding/resource errors.
   - `source/ThemeSuperTestApp/AgentBridge/latest/latest-composite.png` — A single dark-slate vertically stitched overview image containing labeled header bars (`[1] Main ShellView — WPF Visual ...`, `[2] Main ShellView — Win32 DWM Title Bar & Chrome ...`, plus any open `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, or `NavigatorWindow` windows).
2. **Optional High-Resolution Zoom Reads (call `read_file` when pixel-level detail is needed):**
   - `source/ThemeSuperTestApp/AgentBridge/latest/latest-main-wpf.png` — Unscaled DPI-aware WPF `RenderTargetBitmap` (`VisualBrush` at `(0,0)`). Works reliably even when `ThemeSuperTestApp` is behind another window.
   - `source/ThemeSuperTestApp/AgentBridge/latest/latest-main-dwm.png` — Unscaled Win32 `PrintWindow` (`PW_RENDERFULLCONTENT`) capture showing the real Windows 10/11 DWM title bar, caption buttons, and window border. (Omitted/deleted only when the window is minimized or off-screen without an HWND.)
   - Secondary floating/detached window PNGs listed under `## Captured Image Files` in `latest-context.md` (stored in `AgentBridge/captures/<captureId>-win01-wpf.png` / `-win01-dwm.png`).

---

## 4. Step 3 — Sending Live XML Commands (`inbox/` -> `outbox/`)

### Request Schema (`AgentBridge/inbox/<requestId>.xml`)

- Use only ASCII letters, digits, `-`, and `_` in `<requestId>` (e.g., `req-01-vs2022dark`) and match the `Id` attribute to the filename `<requestId>.xml` so the response is written to `outbox/<requestId>-response.xml`.
- Element and attribute names (`AgentCommand`, `Id`, `Action`, `Param`, `Name`, `Value`, `Payload`) are parsed case-insensitively via `XDocument.Parse`.
- Boolean parameters accept `true` / `false` (case-insensitive) or `1` / `0`.
- `AgentBridgeService` watches `inbox/*.xml` via both `FileSystemWatcher` and a `350 ms` fallback `DispatcherTimer` (ignoring `*.tmp` files and retrying transient `IOException` locks), executes commands FIFO on the WPF UI `Dispatcher` (awaiting `DispatcherPriority.Render` before screenshots), deletes the processed inbox `.xml` file, and writes the response atomically.

```xml
<AgentCommand Id="req-01-vs2022dark" Action="apply-theme">
  <Param Name="ThemeId" Value="VS2022Dark" />
  <Param Name="ForceCold" Value="false" />
  <Param Name="Capture" Value="true" />
  <Payload>Inspecting VS2022Dark active tab border and tool pane header contrast.</Payload>
</AgentCommand>
```

### Execution Pattern for Antigravity

1. Write `source/ThemeSuperTestApp/AgentBridge/inbox/<requestId>.xml` using `write_file` (`Overwrite: true`).
2. Wait cleanly without shell-sleep polling: schedule a short one-shot wakeup via `schedule` (`DurationSeconds: 2` for single commands; `DurationSeconds: 8` for `sweep-all-18-screenshots`) and end your turn so `ThemeSuperTestApp` renders and flushes PNGs/XML on the STA thread.
3. On wakeup, call `read_file` on `source/ThemeSuperTestApp/AgentBridge/outbox/<requestId>-response.xml` (and in parallel on `latest/latest-composite.png` when `Capture="true"`).

### Response Schema (`AgentBridge/outbox/<requestId>-response.xml`)

**Success (`Status="Ok"`):**

```xml
<AgentResponse Id="req-01-vs2022dark" Action="apply-theme" Status="Ok" DurationMs="44.85" TimestampUtc="2026-11-27T17:55:02.1140000+00:00">
  <Message>Applied 'VS 2022 Dark' [VsTheme : DictionaryTheme].</Message>
  <ActiveTheme Id="VS2022Dark" DisplayName="VS 2022 Dark" BaseClass="VsTheme : DictionaryTheme" ClrNamespace="AvalonDock.Themes.Type3New" XmlnsMapped="true" />
  <OsState Version="Windows 11 (Build 26100)" AppsMode="Explicit Dark" SystemMode="Explicit Dark" HighContrast="Off" AccentHex="#FF0078D4" />
  <Diagnostics UnfrozenFreezables="0" ThemedWindowHosts="1" DistinctDictionaries="1" WpfTraceErrors="0" />
  <Artifacts
    LatestCompositePng="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest\latest-composite.png"
    LatestMainWpfPng="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest\latest-main-wpf.png"
    LatestMainDwmPng="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest\latest-main-dwm.png"
    LatestContextMd="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest\latest-context.md"
    ArchivedCompositePng="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\captures\20261127-125502090-req-01-vs2022dark-VS2022Dark-composite.png"
    ReportPath="c:\SOFTDEV\AvalonDock-V5-NEW\source\ThemeSuperTestApp\AgentBridge\latest\latest-context.md" />
</AgentResponse>
```

**Error (`Status="Error"`):**

```xml
<AgentResponse Id="req-bad-json" Action="load-inline-json" Status="Error" DurationMs="2.15" TimestampUtc="2026-11-27T17:56:10.0000000+00:00">
  <Message>Unexpected character 'U' at line 3, column 13.</Message>
</AgentResponse>
```

---

## 5. All 16 Allow-Listed Command Verbs

| # | `Action` Verb | Parameters & Defaults | Behavior & Returned `<Artifacts>` |
| ---: | :--- | :--- | :--- |
| 1 | `ping` | *(none)* | Lightweight liveness check; returns `<ActiveTheme>`, `<OsState>`, and `<Diagnostics>` with no screenshot overhead. |
| 2 | `get-state` | *(none)* | Alias of `ping`; returns live theme, OS Dark/Light/High-Contrast/Accent state, unfrozen `Freezable` count, window dictionary sharing counts, and WPF trace error count. |
| 3 | `capture-snapshot` | `Note` (`string?`, or `<Payload>`), `IncludeDwm` (`bool`, default `true`) | Captures dual-mode WPF + DWM screenshots, open floating/detached windows, `latest-composite.png`, and `latest-context.md`. |
| 4 | `apply-theme` | `ThemeId` (**required**; alias `Theme` — ID or `DisplayName`), `ForceCold` (`bool`, default `false`), `Capture` (`bool`, default `true`), optional `<Payload>` note | Applies the specified theme on `DockingManager`, awaits `DispatcherPriority.Render`, and (when `Capture=true`) writes `latest/*` screenshots + `latest-context.md`. |
| 5 | `set-dwm-mode` | `Mode` (**required**: `DefaultUntouched` \| `MatchWindowsOsMode` \| `MatchActiveAvalonDockTheme`), `Capture` (`bool`, default `false`) | Updates `DWMWA_USE_IMMERSIVE_DARK_MODE` across `ShellView` and standalone `DetachedAnchorableWindow`s. Pass `Capture="true"` to inspect `latest-main-dwm.png`. |
| 6 | `set-shell-options` | `StyleOuterShell` (`bool?`), `UseCustomHeader` (`bool?`), `AutoSyncOs` (`bool?`), `Capture` (`bool`, default `false`) | Toggles outer Menu/Header/StatusBar polarity tinting, `IconDocumentHeaderTemplate` vs. `<null>` native theme tab header, and OS auto-sync (omitted flags keep current state). |
| 7 | `spawn-overflow-tabs` | `Capture` (`bool`, default `true`) | Opens 10 icon + dirty-dot specimen document tabs to test `MenuItemEx` overflow dropdowns and `Ctrl+Tab` `NavigatorWindow`. |
| 8 | `close-overflow-tabs` | `Capture` (`bool`, default `false`) | Closes all spawned overflow specimen tabs. |
| 9 | `spawn-floating` | `Capture` (`bool`, default `true`) | Tears off a specimen document and toolbox into `LayoutFloatingWindowControl` windows and captures a multi-window `latest-composite.png`. |
| 10 | `spawn-detached` | `Capture` (`bool`, default `true`) | Detaches the specimen toolbox into a standalone top-level OS `DetachedAnchorableWindow` (`"Window"` mode) and captures both its WPF visual and Win32 DWM title bar. |
| 11 | `dock-all` | `Capture` (`bool`, default `false`) | Re-docks all open floating and detached windows back into `ShellView`. |
| 12 | `reset-layout` | `Capture` (`bool`, default `false`) | Restores the startup docking layout snapshot. |
| 13 | `benchmark-cold-ctors` | *(none)* | Constructs fresh instances of all registered themes, measuring cold `.ctor` ms and managed KB delta; writes Markdown table to `outbox/<Id>-benchmark.md` (`ReportPath`). |
| 14 | `export-full-report` | *(none)* | Exports the complete 6-section Markdown diagnostic report to `outbox/<Id>-full-report.md` (`ReportPath`). |
| 15 | `load-inline-json` | `<Payload>` (**required**; or `Json` param), `BasePalette` (`VS2022Dark` [default] \| `VS2022Light` \| `VS2022Blue` \| `VS2015Dark` \| `None`), `TemplateSet` (`VS2022` [default] \| `VS2015`), `DisplayName` (default `Agent Inline JSON`), `Capture` (`bool`, default `true`) | Parses a VS2026 JSON color-token array via zero-dependency `VsJsonThemeParser`, merges onto `BasePalette`, registers `Runtime.AgentInlineJson`, applies it live, and captures screenshots (or returns line/column `FormatException` in `Status="Error"`). |
| 16 | `sweep-all-18-screenshots` *(aliases: `sweep-all-screenshots`, `sweep-all-20-screenshots`)* | *(none)* | Applies all 18 built-in themes sequentially on `DockingManager` (`Render` pass + unfrozen `Freezable` audit each), saves `01..<18>-<ThemeId>-wpf.png` & `-dwm.png` + `sweep-index.md` under `captures/sweep-<Id>/` (`ReportPath`), and restores the prior active theme. |

### Complete Catalog of 18 Built-In `ThemeId` Values

- **Visual Studio 2026 (`VsTheme : DictionaryTheme`, `AvalonDock.Themes.Type3New`):** `VS2026Dark`, `VS2026Blue`, `VS2026Light`
- **Visual Studio 2022 (`VsTheme : DictionaryTheme`, `AvalonDock.Themes.Type3New`):** `VS2022Dark`, `VS2022Blue`, `VS2022Light`
- **Visual Studio 2015 (`VsTheme : DictionaryTheme`, `AvalonDock.Themes.Type3New`):** `VS2015Dark`, `VS2015Blue`, `VS2015Light`
- **Visual Studio 2013 (`DictionaryTheme`, `AvalonDock.Themes.Type2Hybrid`):** `Vs2013Dark`, `Vs2013Blue`, `Vs2013Light`
- **Classic URI & Built-In Core (`Theme`, `AvalonDock.Themes.Type1Legacy`):** `VS2010`, `ExpressionDark` *(startup default)*, `ExpressionLight`, `Metro`, `Aero`, `Generic`

---

## 6. Ready-to-Copy XML Command Examples

### Ping / State Check (`ping` & `get-state`)
```xml
<AgentCommand Id="req-ping" Action="ping" />
```
```xml
<AgentCommand Id="req-state" Action="get-state" />
```

### Capture Current Snapshot with Operator/Agent Note (`capture-snapshot`)
```xml
<AgentCommand Id="req-snap-01" Action="capture-snapshot">
  <Param Name="IncludeDwm" Value="true" />
  <Payload>Baseline capture before toggling native tab headers.</Payload>
</AgentCommand>
```

### Switch Theme & Capture (`apply-theme`)
```xml
<AgentCommand Id="req-theme-vs2026blue" Action="apply-theme">
  <Param Name="ThemeId" Value="VS2026Blue" />
  <Param Name="ForceCold" Value="true" />
  <Param Name="Capture" Value="true" />
</AgentCommand>
```

### Set DWM Dark Title-Bar Mode & Shell Chrome Options (`set-dwm-mode`, `set-shell-options`)
```xml
<AgentCommand Id="req-dwm-match-theme" Action="set-dwm-mode">
  <Param Name="Mode" Value="MatchActiveAvalonDockTheme" />
  <Param Name="Capture" Value="true" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-shell-native-headers" Action="set-shell-options">
  <Param Name="StyleOuterShell" Value="true" />
  <Param Name="UseCustomHeader" Value="false" />
  <Param Name="AutoSyncOs" Value="false" />
  <Param Name="Capture" Value="true" />
</AgentCommand>
```

### Multi-Window & Tab Overflow Stress Commands
```xml
<AgentCommand Id="req-overflow-open" Action="spawn-overflow-tabs">
  <Param Name="Capture" Value="true" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-float-open" Action="spawn-floating">
  <Param Name="Capture" Value="true" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-detach-open" Action="spawn-detached">
  <Param Name="Capture" Value="true" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-dock-all" Action="dock-all">
  <Param Name="Capture" Value="false" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-overflow-close" Action="close-overflow-tabs">
  <Param Name="Capture" Value="false" />
</AgentCommand>
```
```xml
<AgentCommand Id="req-reset-layout" Action="reset-layout">
  <Param Name="Capture" Value="true" />
</AgentCommand>
```

### Cold Constructor Benchmark & Full 6-Section Diagnostic Report
```xml
<AgentCommand Id="req-bench-ctors" Action="benchmark-cold-ctors" />
```
```xml
<AgentCommand Id="req-full-report" Action="export-full-report" />
```

### Live VS2026 Inline JSON Palette Override (`load-inline-json`)
```xml
<AgentCommand Id="req-inline-json-01" Action="load-inline-json">
  <Param Name="BasePalette" Value="VS2022Dark" />
  <Param Name="TemplateSet" Value="VS2022" />
  <Param Name="DisplayName" Value="Agent Teal Accent Test" />
  <Param Name="Capture" Value="true" />
  <Payload><![CDATA[
[
  { "Name": "EnvironmentBackground",  "Background": "FF161925" },
  { "Name": "EnvironmentTab",         "Background": "FF1F2437" },
  { "Name": "EnvironmentTabInactive", "Background": "FF191D2B" },
  { "Name": "EnvironmentHeader",      "Background": "FF0EA5E9" },
  { "Name": "FileTabSelectedBorder",  "Background": "FF14B8A6" },
  { "Name": "ToolWindowBorder",       "Background": "FF334155" }
]
  ]]></Payload>
</AgentCommand>
```

### Full 18-Theme Visual Screenshot Sweep (`sweep-all-18-screenshots`)
```xml
<AgentCommand Id="req-sweep-18" Action="sweep-all-18-screenshots" />
```

---

## 7. Guardrails & Hygiene

- **Git Hygiene:** `source/ThemeSuperTestApp/AgentBridge/` is ignored by `.gitignore` (`!/.agents/**` tracks this skill). Never force-add (`git add -f`) files inside `AgentBridge/`.
- **Rolling Retention:** `WindowScreenshotService` automatically prunes `AgentBridge/captures/` to the **40** newest capture prefixes, and `AgentBridgeService` prunes `AgentBridge/outbox/` to the **60** newest files while keeping `AgentBridge/latest/*` intact.
- **Zero `System.Text.Json`:** All `AgentBridge` messages are strictly XML (`XDocument`) to preserve `net48` assembly cleanliness.
