# Plan 08 (of 10) — Dual-Mode Screenshot Engine (`RenderTargetBitmap` + Win32 `PrintWindow`), Multi-Window Stitcher & `F12` Snapshot

> **Execution Order:** **Step 8 of 10** (Requires [Plan 07](07-Solution-Integration-STA-Diagnostic-Unit-Tests-And-Documentation.md) completed; precedes [Plan 09](09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md)).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; zero `System.Text.Json` / `System.Memory` / `Microsoft.Bcl.*`; **zero** edits to existing AvalonDock theme libraries.

---

## Antigravity Bridge Extension Overview (Plans 08–10)

| Plan | File | Scope |
| :--- | :--- | :--- |
| **08** | `08-Dual-Mode-Screenshot-Engine-Composite-Stitcher-And-F12-Snapshot.md` | `.gitignore` hygiene, `AgentBridgePaths`, dual-mode screenshot engine (WPF `RenderTargetBitmap` + Win32 `PrintWindow` DWM title-bar capture + multi-window composite PNG stitcher + 40-snapshot rolling retention), and `F12` **"Send Snapshot to Antigravity"** push workflow. |
| **09** | [09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md](09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md) | Auto-listening `AgentBridgeService` (`heartbeat.xml` + `XDocument` XML `inbox/` -> `outbox/` command processor + 20-theme automated screenshot sweep) and dockable **Tool 6: Antigravity Live Bridge** (`Ctrl+Alt+A`). |
| **10** | [10-Antigravity-Workspace-Skill-STA-Bridge-Tests-And-Docs-Sync.md](10-Antigravity-Workspace-Skill-STA-Bridge-Tests-And-Docs-Sync.md) | Workspace skill (`.agents/skills/theme-super-test-bridge/SKILL.md`), isolated temp-dir STA unit tests in `AvalonDockTest`, `README.md` update, and full solution verification gates. |

---

## 1. Objective & Scope of Plan 08

Enable `ThemeSuperTestApp` to capture pixel-accurate `.png` screenshots of itself — **both** occluded-safe WPF visual-tree captures (`RenderTargetBitmap`) **and** native Win32/DWM window captures including OS dark/light title bars (`PrintWindow` with `PW_RENDERFULLCONTENT = 2`), plus a stitched side-by-side multi-window composite image whenever floating or detached windows are open — and write a ready-to-read multimodal snapshot bundle (`PNG`s + Markdown telemetry + optional user note) into `source/ThemeSuperTestApp/AgentBridge/latest/` with one click or `F12`.

---

## 2. Step-by-Step Implementation Tasks

### Step 8.1 — Add `/source/ThemeSuperTestApp/AgentBridge/` to Root `.gitignore`
Edit root `.gitignore` to ignore `/source/ThemeSuperTestApp/AgentBridge/` so runtime screenshots (`*.png`), `heartbeat.xml`, and `inbox/`/`outbox/` XML files never clutter `git status` (note that Antigravity's `read_file` and `write_file` tools still access gitignored workspace directories directly, while `!/.agents/**` remains tracked for Plan 10's skill).

### Step 8.2 — Implement `Services/AgentBridgePaths.cs`
Create a deterministic workspace bridge path resolver:
1. Default constructor walks upward from `AppDomain.CurrentDomain.BaseDirectory` to locate `source/ThemeSuperTestApp/` (checking for `ThemeSuperTestApp.csproj`), falling back to `Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AgentBridge")` if run from an external copy.
2. Overload `public AgentBridgePaths(string bridgeRootDirectory)` allows isolated temporary directories in Plan 10's unit tests.
3. Exposes and ensures directories on demand:
   - `BridgeRootDirectory` (`.../source/ThemeSuperTestApp/AgentBridge`)
   - `LatestDirectory` (`.../AgentBridge/latest`)
   - `CapturesDirectory` (`.../AgentBridge/captures`)
   - `InboxDirectory` (`.../AgentBridge/inbox`)
   - `OutboxDirectory` (`.../AgentBridge/outbox`)
   - `HeartbeatFilePath` (`.../AgentBridge/heartbeat.xml`)

### Step 8.3 — Implement Dual-Mode Screenshot & Composite Engine (`Services/WindowScreenshotService.cs` & `Models/ScreenshotCaptureBundle.cs`)
Using **100% inbox `.NET Framework 4.8` WPF + Win32 GDI/User32 APIs** (zero `System.Drawing` or NuGet packages needed):
1. **Extend `Services/NativeMethods.cs`** (keeping `[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]` on every P/Invoke for `CA1060`/`CA5392` compliance):
   - `user32.dll`: `GetWindowRect`, `IsIconic`, `PrintWindow` (`PW_RENDERFULLCONTENT = 0x00000002`), `GetWindowDC`, `ReleaseDC`.
   - `gdi32.dll`: `CreateCompatibleDC`, `CreateCompatibleBitmap`, `SelectObject`, `DeleteObject`, `DeleteDC`.
2. **Mode A — Occluded-Safe WPF Visual Capture (`CaptureWpfVisualBitmap(FrameworkElement element)`):**
   - Reads `VisualTreeHelper.GetDpi(element)` (inbox `net48`), computes pixel dimensions from `ActualWidth` / `ActualHeight` (calling `Measure`/`Arrange` if needed on off-screen test elements), renders via `RenderTargetBitmap`, freezes the bitmap, and returns `BitmapSource`.
   - Captures `ShellView` (`main-wpf.png`) and each open `LayoutFloatingWindowControl` / `DetachedAnchorableWindow` even if `ThemeSuperTestApp` is behind Antigravity's IDE window.
3. **Mode B — Win32 + DWM Non-Client Window Capture (`TryCaptureWin32DwmWindowBitmap(Window window)`):**
   - Obtains `HWND` via `WindowInteropHelper(window).Handle`. If `hwnd == IntPtr.Zero || IsIconic(hwnd)` or rect is empty, returns `null` gracefully.
   - Allocates compatible DC + `HBITMAP` matching `GetWindowRect`, invokes `PrintWindow(hwnd, memDc, PW_RENDERFULLCONTENT)` (falling back to flags `0` if needed), converts to WPF `BitmapSource` via `Imaging.CreateBitmapSourceFromHBitmap(...)`, calls `bitmapSource.Freeze()`, and **guarantees `DeleteObject(hBitmap)` + `DeleteDC` + `ReleaseDC` in `finally`** so zero GDI handles ever leak!
4. **Mode C — Multi-Window Side-by-Side Composite Stitcher (`BuildCompositeOverviewBitmap(...)`):**
   - Renders a dark-slate (`#0F172A`) overview canvas via `DrawingVisual` + `RenderTargetBitmap` containing header labels (`[1] Main ShellView (WPF)`, `[2] Main ShellView (Win32 DWM Title Bar)`, plus `[3..N]` every open `LayoutFloatingWindowControl` and `DetachedAnchorableWindow` — both WPF and DWM captures when available!) so Antigravity can inspect the entire multi-window state in a **single** `.png` read!
5. **Atomic Save & Rolling Retention (`MaxRetainedCaptureSets = 40`):**
   - Writes every `.png` and `.md` to a `.tmp` sibling path first and moves/replaces atomically so Antigravity never reads a partially written file.
   - Copies the newest capture set to `AgentBridge/latest/` (`latest-main-wpf.png`, `latest-main-dwm.png`, `latest-composite.png`, `latest-context.md`) AND archives `<timestampOrRequestId>-*` in `AgentBridge/captures/`, pruning oldest archived files once more than `40` capture prefixes exist.

### Step 8.4 — Wire One-Click `"Send Snapshot to Antigravity (F12)"` into `ShellViewModel` & `ShellView`
1. Add `string UserSnapshotNote` property and `async Task SendSnapshotToAntigravity()` action to `ShellViewModel`:
   - Awaits `Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render)` on `ShellView` so any pending layout/theme updates finish painting.
   - Captures the full screenshot bundle + writes `latest-context.md` (prepending `UserSnapshotNote` when provided, screenshot file paths, and `DiagnosticReportExporterService.BuildFullMarkdownReport()`).
   - Updates `StatusText` with the saved `AgentBridge/latest/` timestamp and file list.
2. Add `F12` `KeyTrigger` (inside `ShellView.xaml`'s zero-size leaf `FrameworkElement`) and a prominent **`Send Snapshot to Antigravity (F12)`** button in `ShellView.xaml` + `_Diagnostics` menu item.

---

## 3. Verification & Exit Gate to Plan 09

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Debug --no-restore --warnaserror
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git status --short -- source/Components/
```

**Expected Result:**
- Both `Debug` and `Release` compile with **0 warnings, 0 errors**.
- STA smoke test verifies `WindowScreenshotService` produces valid PNG headers (`89 50 4E 47`), composite multi-image stitching, and 40-capture rolling retention without leaking GDI handles or polluting `git status`.
- Zero edits to `source/Components/`.
