# ThemeSuperTestApp — AvalonDock v5 Theme Debugger & Best-Practices Auditor

`ThemeSuperTestApp` is a dedicated WPF visual inspection, performance-profiling, best-practices auditing, and multimodal Antigravity bridge workbench for **AvalonDock v5** themes on **`.NET Framework 4.8` (`net48`)**.

> **Companion Technical Reports & Agent Skill:**
> - [THEMES.md](../Components/THEMES.md) — Complete inventory of all 18 built-in themes across 4 assemblies (`AvalonDock` + `AvalonDock.Themes.{Type1Legacy,Type2Hybrid,Type3New}`).
> - [THEMES-ANALYSIS-AND-BEST-PRACTICES.md](../Components/THEMES-ANALYSIS-AND-BEST-PRACTICES.md) — Architectural analysis of `Type1Legacy` (`Theme`), `Type2Hybrid` (`DictionaryTheme`), and `Type3New` (`VsTheme : DictionaryTheme`) and `net48` vs. modern `.NET` WPF best practices.
> - [.agents/skills/theme-super-test-bridge/SKILL.md](../../.agents/skills/theme-super-test-bridge/SKILL.md) — Workspace skill enabling Antigravity to inspect `F12` snapshots and remote-control a live `ThemeSuperTestApp.exe` session over XML.

---

## 1. Key Characteristics & Guardrails

- **Target Framework:** `.NET Framework 4.8` (`net48`) exclusively — zero `System.Text.Json`, `System.Memory`, or `Microsoft.Bcl.*` assemblies.
- **MVVM & IoC:** Hosted by **`Caliburn.Micro` `6.0.15-beta`** (`SimpleContainer`, action messages with leaf `FrameworkElement` `KeyTrigger`s, async `Screen` / `Conductor` lifecycles, and `IDockLayoutService` + `DockLayout`).
- **Root Docking Surface:** Classic **`DockingManager`** with `AllowFloatingWindows = true` and `AllowDetachedWindows = true`, using an implicit `<DataTemplate DataType="{x:Type boilerplate:DockableScreen}">` so views resolve identically in docked panes, tear-off `LayoutFloatingWindowControl`s, and standalone `DetachedAnchorableWindow`s.
- **Non-Invasive Auditing:** Inspects all 18 built-in themes as-is without mutating their `ResourceDictionary` or `Freezable` state.

---

## 2. Pinned Two-Row Theme & OS Diagnostics Header Bar

At the top of `ShellView`, an always-visible two-row command header provides instant architectural visibility:

1. **Row 1 — Active Theme & Base-Class Strip:**
   - **Theme Dropdown (`F8` / `Shift+F8`):** Lists all 18 built-in themes (plus any runtime-loaded `.vstheme` / `.json` themes) with polarity dots, color-coded base-class badges (`Theme (URI)` in teal, `DictionaryTheme` in amber, `VsTheme : DictionaryTheme` in purple), and CLR namespaces (`AvalonDock.Themes.Type1Legacy`, `AvalonDock.Themes.Type2Hybrid`, `AvalonDock.Themes.Type3New`).
   - **Inheritance, `[XmlnsDefinition]` & Bridge Readout:** Displays the exact CLR inheritance chain (e.g., `VS2026DarkTheme -> VsTheme -> DictionaryTheme -> Theme`), `GetResourceUri()` tooltip, green/amber `[XmlnsDefinition]` mapping badge, `.ctor` / `Apply` latency pill, **`Custom Icon Tab Header`** toggle (`IconDocumentHeaderTemplate` vs. `<null>` native theme header template), and the live **`Antigravity Bridge: Listening (N)`** status badge (`Paused (N)` in amber when paused).
2. **Row 2 — Live Windows OS Dark/Light/High-Contrast Detector, Chrome & Snapshot Controls:**
   - Queries real OS build via `ntdll!RtlGetVersion`, explicit `HKCU\...\Personalize\AppsUseLightTheme` & `SystemUsesLightTheme` (`Explicit Dark`, `Explicit Light`, `Unset / Legacy OS`), `SystemParameters.HighContrast`, and `DWM\AccentColor` + `DWM\ColorPrevalence`, updating live via `WM_SETTINGCHANGE` / `WM_DWMCOLORIZATIONCOLORCHANGED`.
   - Provides opt-in simulation controls (**`DWM Title Bars`**: `Untouched net48 Default` | `Match Windows OS Mode` | `Match Active AvalonDock Theme` across both `ShellView` and standalone `DetachedAnchorableWindow`s, **`Style Outer Shell Chrome`**, and **`Auto-Sync to OS`**), quick window/tab stress buttons, and the **`Send Snapshot to Antigravity (F12)`** action button.

---

## 3. Built-In 6-Tool Diagnostic Suite

| Tool | Shortcut | Best-Practice Issue Diagnosed |
| :--- | :--- | :--- |
| **1. Theme Architecture & Profiler** (`ThemeProfilerViewModel`) | `Ctrl+Alt+P` | Measures cold `new XxxTheme()` constructor latency and managed `KB` allocation (`Theme (URI)` `~0.2 ms / 8 KB` vs. eager `DictionaryTheme` `~2–58 ms / 500–1,700 KB`), full 18-theme `OnThemeChanged` + WPF `Render` sweep latency, and missing `[XmlnsDefinition]` mappings. |
| **2. Resource & `Freezable` Auditor** (`ResourceAuditorViewModel`) | `Ctrl+Alt+R` | Walks root and nested `MergedDictionaries` in exact WPF lookup precedence order, renders color swatches and `#AARRGGBB` hex codes, flags **unfrozen `Freezable`s** (`!IsFrozen`), and computes cross-theme semantic `ResourceKeys` / `ImplicitStyle` parity diffs. |
| **3. Multi-Window Dict Sharing** (`WindowSharingTrackerViewModel`) | `Ctrl+Alt+W` | Uses `RuntimeHelpers.GetHashCode(dict)` across `DockingManager`, `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, `NavigatorWindow`, and `OverlayWindow` to contrast `DictionaryTheme`'s `O(1)` shared `Ref #1` instance against URI `Theme`s' per-window duplicated BAML parses. |
| **4. WPF XAML Binding & Resource Trace** (`WpfTraceListenerViewModel`) | `Ctrl+Alt+T` | Hooks `PresentationTraceSources` (`DataBindingSource`, `ResourceDictionarySource`, `MarkupSource`) in-process on `net48` to catch `System.Windows.Data Error: 40` and `{DynamicResource}` misses tagged with the active theme. |
| **5. Runtime `.vstheme` / JSON Loader** (`RuntimeThemeLoaderViewModel`) | `Ctrl+Alt+L` | Loads external `.vstheme`, `.vstheme.gz`, or VS2026 `.json` files with selectable `VS2015` / `VS2022` `Generic.xaml` templates and includes a live editable VS2026 JSON override sandbox exercising the zero-dependency pure-C# `VsJsonThemeParser`. |
| **6. Antigravity Live Bridge** (`AgentBridgeToolboxViewModel`) | `Ctrl+Alt+A` | Bidirectional pure-XML file mailbox (`AgentBridge/inbox/` -> `outbox/`, `heartbeat.xml`), live listener toggle, operator snapshot note editor (`latest-context.md`), **`Send Snapshot (F12)`** pusher, rolling archive cleaner, and live command/snapshot activity `DataGrid`. |

---

## 4. Specimen Documents, Stress Harness & Report Export

- **`Theme Swatch Sheet` (`ThemeOverviewDocumentViewModel`):** Visual grid of every brush and color token in the active theme.
- **`Control & SystemColors Matrix` (`ControlGalleryDocumentViewModel`):** Side-by-side comparison of raw unthemed `net48` `Aero2` controls, live `SystemColors.*` dynamic brushes, and inherited `DockingManager` foreground/background surfaces.
- **Quick Stress Actions (`_Diagnostics` menu & Row 2 buttons):**
  - `+10 Overflow Tabs` — Spawns 10 icon + dirty-dot specimen tabs to exercise `MenuItemEx` overflow dropdowns and `Ctrl+Tab` (`NavigatorWindow`).
  - `Float Specimen` — Floats both a document and a toolbox into `LayoutFloatingWindowControl`s.
  - `Detach OS Window` — Detaches `Tear-Off Specimen Tool` into a standalone top-level OS `DetachedAnchorableWindow`.
  - `Export .md Report` — Exports a complete 6-section GitHub-Flavored Markdown diagnostic report to disk or clipboard.

---

## 5. Antigravity Multimodal Bridge & F12 Snapshot Workflow

`ThemeSuperTestApp` pairs with the workspace skill [`.agents/skills/theme-super-test-bridge/SKILL.md`](../../.agents/skills/theme-super-test-bridge/SKILL.md) through the git-ignored `source/ThemeSuperTestApp/AgentBridge/` folder (`AgentBridgePaths`, `WindowScreenshotService`, and `AgentBridgeService`):

1. **Dual-Mode Screenshot & Multi-Window Composite Engine (`WindowScreenshotService`):**
   - **WPF Client Capture (`latest-main-wpf.png`):** DPI-aware `RenderTargetBitmap` rendered through a `(0,0)`-aligned `VisualBrush` — succeeds even when `ShellView` is occluded behind another window.
   - **Win32 DWM Window Capture (`latest-main-dwm.png`):** Native `PrintWindow` (`PW_RENDERFULLCONTENT`) capture of `ShellView` and any standalone `DetachedAnchorableWindow` so Antigravity can visually inspect real Windows 10/11 Dark/Light title bars, caption buttons, and borders.
   - **Stitched Overview (`latest-composite.png`) & Context Bundle (`latest-context.md`):** Pressing **`F12`** (or clicking **Send Snapshot to Antigravity (F12)**) vertically stitches `ShellView` (WPF + DWM) and all visible `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, and `NavigatorWindow` windows onto a single labeled dark-slate overview PNG alongside `latest-context.md` (operator note + full 6-section diagnostic report).
   - **Rolling Retention:** Timestamped captures in `AgentBridge/captures/` automatically prune to the **40** most recent capture sets (`DefaultMaxRetainedCaptureSets = 40`), while `AgentBridge/latest/*` always holds the newest snapshot.
2. **Live `heartbeat.xml` & Pure-XML Command Mailbox (`AgentBridgeService`):**
   - Publishes `AgentBridge/heartbeat.xml` (`ProcessId`, `State="Listening|Paused|Stopped"`, `UpdatedAtUtc` every `2.5 s`, `ProcessedCommandCount`, `ActiveThemeId`, `ActiveBaseClass`, `WpfTraceErrorCount`) and transitions cleanly to `State="Stopped"` on exit.
   - Watches `AgentBridge/inbox/*.xml` (`FileSystemWatcher` + `350 ms` fallback poll, ignoring `*.tmp`), executes allow-listed commands FIFO on the WPF UI `Dispatcher` (awaiting `DispatcherPriority.Render` before capturing), and writes atomic `<AgentResponse Status="Ok|Error">` files to `AgentBridge/outbox/<requestId>-response.xml` (`60`-file rolling cap; zero `System.Text.Json`).

### Allow-Listed XML Command Verbs (16 Verbs)

```xml
<AgentCommand Id="req-demo" Action="apply-theme">
  <Param Name="ThemeId" Value="VS2026Dark" />
  <Param Name="ForceCold" Value="false" />
  <Param Name="Capture" Value="true" />
  <Payload>Optional note embedded in latest-context.md (or raw JSON for load-inline-json).</Payload>
</AgentCommand>
```

| Category | Verbs (`Action="..."`) | Key Parameters & Output |
| :--- | :--- | :--- |
| **Presence & Snapshot** | `ping`, `get-state`, `capture-snapshot` | `Note` / `<Payload>`, `IncludeDwm` (`true`) -> live `<ActiveTheme>`, `<OsState>`, `<Diagnostics>`, and `latest/*` PNG + `.md` bundle. |
| **Theme & Chrome Control** | `apply-theme`, `set-dwm-mode`, `set-shell-options` | `ThemeId` (any of 18 built-ins or runtime IDs), `ForceCold` (`false`), `Mode` (`DefaultUntouched` \| `MatchWindowsOsMode` \| `MatchActiveAvalonDockTheme`), `StyleOuterShell`, `UseCustomHeader`, `AutoSyncOs`, `Capture`. |
| **Layout & Window Stress** | `spawn-overflow-tabs`, `close-overflow-tabs`, `spawn-floating`, `spawn-detached`, `dock-all`, `reset-layout` | Optional `Capture` (`true` by default on `spawn-*`, `false` on close/dock/reset) -> multi-window `latest-composite.png`. |
| **Profiling, JSON & Sweep** | `benchmark-cold-ctors`, `export-full-report`, `load-inline-json`, `sweep-all-18-screenshots` | Writes `outbox/<Id>-benchmark.md`, `outbox/<Id>-full-report.md`, live `CustomRuntimeVsTheme` (`BasePalette`, `TemplateSet`, `DisplayName`, `<Payload>`), or all 18 theme PNGs + `captures/sweep-<Id>/sweep-index.md`. |

---

## 6. Build, Run & Test Commands

```powershell
# Build entire solution (Debug & Release, zero warnings allowed)
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Debug --no-restore --warnaserror
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# Launch ThemeSuperTestApp interactively
.\source\ThemeSuperTestApp\bin\Debug\net48\ThemeSuperTestApp.exe

# Run automated STA diagnostic & AgentBridge unit tests (always pass -m:1)
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net48 --filter "FullyQualifiedName~ThemeSuperTestAppDiagnosticsTests" -m:1
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net48 --filter "FullyQualifiedName~ThemeSuperTestAppAgentBridgeTests" -m:1
```
