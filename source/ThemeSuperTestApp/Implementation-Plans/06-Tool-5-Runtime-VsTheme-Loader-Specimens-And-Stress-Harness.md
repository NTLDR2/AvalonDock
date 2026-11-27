# Plan 06 (of 07) — Tool 5: Runtime `.vstheme` / `.json` Loader, Specimen Documents & Stress Harness

> **Execution Order:** **Step 6 of 7** (Requires [Plan 05](05-Tools-3-And-4-MultiWindow-Sharing-Tracker-And-WPF-Trace-Listener.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Complete `ThemeSuperTestApp`'s interactive testing capabilities by adding:
1. **Tool 5 — Runtime `.vstheme` / `.vstheme.gz` / VS2026 `.json` Loader (`RuntimeThemeLoaderViewModel`):** Exercises `AvalonDock.Themes.VS`'s dynamic `DictionaryTheme` pipeline (`VsTheme`, `VsThemeParser`, `VsJsonThemeParser`, `VsTheme.FromJson`) on user files or pasted JSON snippets — including testing invalid JSON syntax error diagnostics (`FormatException` line/column) live in the UI.
2. **Visual Specimen Documents (`ThemeOverviewDocumentViewModel` & `ControlGalleryDocumentViewModel`):** Rich visual inspection sheets hosted inside `DockingManager` document tabs (and detachable/floatable!) to test contrast, typography, icons, borders, scrollbars, menus, and `Aero2` vs. `SystemColors.*` vs. AvalonDock `ResourceKeys` behavior.
3. **One-Click Visual Stress Harness & Markdown Diagnostic Report Exporter:** Instant toolbar/menu actions to spawn floating windows, detached OS windows, 12+ overflow tabs (`MenuItemEx` dropdown + `Ctrl+Tab` `NavigatorWindow`), and export a complete snapshot report.

---

## 2. Step-by-Step Implementation Tasks

### Step 6.1 — Build Tool 5: `ViewModels/Tools/RuntimeThemeLoaderViewModel.cs` & `Views/Tools/RuntimeThemeLoaderView.xaml`
Implement `RuntimeThemeLoaderViewModel : ToolboxScreen` (`PreferredSide = DockSide.Left`, `IsOpenByDefault = true`, `Shortcut = "Ctrl+Alt+L"`):
1. **External File Loader Section:**
   - `BrowseThemeFile` action (`Microsoft.Win32.OpenFileDialog` filtering `*.vstheme;*.vstheme.gz;*.json`).
   - Template Set selector: `VS2015 Classic Templates (Themes/Generic.xaml)` vs. `VS2022/VS2026 Rounded Templates (Themes/VS2022/Generic.xaml)`.
   - Base Palette selector (for VS2026 override-only `.json` files): `None (Standalone Defaults)` | `VS2022 Dark Base` | `VS2022 Light Base` | `VS2022 Blue Base` | `VS2015 Dark Base`.
   - `LoadAndApplyFile` action: parses via `VsTheme` / `VsThemeParser.ParseGZip` / `VsTheme.FromJson`, registers the resulting `VsTheme` as a live `ThemeDescriptor` (`IsCustomRuntimeTheme = true`, `BaseClassKind = ThemeBaseClassKind.VsTheme`) in `ThemeCatalogService`, selects it in the main Theme Dropdown, and triggers Tools 1–4 to audit it!
2. **Inline Live VS2026 JSON Sandbox Editor:**
   - Pre-populated editable `TextBox` with a sample 5-token VS2026 JSON array (`EnvironmentBackground`, `EnvironmentTab`, `EnvironmentHeader`, `FileTabSelectedBorder`, `ToolWindowBorder` — including `//` comments so the zero-dependency `VsJsonThemeParser` is exercised live!).
   - `ApplyInlineJsonOverride` button: calls `VsJsonThemeParser.Parse(JsonText)` + `VsThemeResourceBuilder.Build(...)` in ~1 ms and applies the resulting `VsTheme` live! If the user types invalid JSON, catches `FormatException` and displays the exact `(Line X, Column Y)` diagnostic message in a status box.

### Step 6.2 — Build Specimen Documents (`ViewModels/Documents/*` & `Views/Documents/*`)
Create three purpose-built `DocumentScreen` types opened at startup (and spawnable on demand):
1. **`ThemeOverviewDocumentViewModel` / `ThemeOverviewDocumentView.xaml` ("Active Theme Swatch & Chrome Sheet"):**
   - Visual grid rendering the active theme's key AvalonDock elements side-by-side: Document Tab states (Active/Selected-Inactive/Hovered/Unselected), Tool Window Caption states (Active/Inactive + Grip + Close/Pin glyphs), Auto-Hide Tab preview, `NavigatorWindow` colors, `OverlayWindow` drop-target star/preview-box brushes, and Context Menu brushes (resolved live from the active theme's audited `ResourceEntryRecord`s so it works across **all 20 themes** regardless of which assembly's `ResourceKeys` class defines them!).
2. **`ControlGalleryDocumentViewModel` / `ControlGalleryDocumentView.xaml` ("WPF Control & SystemColors Contrast Matrix"):**
   - Three-column side-by-side comparison inside a single scrollable document:
     - **Column A — Default Unthemed WPF Controls (`net48` `Aero2`):** `Button`, `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `Slider`, `ProgressBar`, `ListBox`, `TreeView`, `Menu`, `ContextMenu` (exposes white-on-white or black-on-dark readability issues when an app hosts standard WPF controls inside a Dark AvalonDock pane!).
     - **Column B — `SystemColors.*` Bound Surface:** Swatches for `SystemColors.WindowBrush`, `ControlBrush`, `ControlTextBrush`, `HighlightBrush`, `GrayTextBrush` (visually proving how `net48` `SystemColors` behave under normal Windows Dark Mode vs. Windows High Contrast!).
     - **Column C — AvalonDock `DockManager.Background` / `Foreground` Inherited Surface:** Shows what inner views actually inherit via `DockingManager.LogicalChildren`!
3. **`SpecimenNoteDocumentViewModel` / `SpecimenNoteDocumentView.xaml`:**
   - Lightweight multi-instance document with vector `DrawingImage` `IconSource`, toggleable `IsModified` (`*` suffix in `Title`), `CanClose` toggle, and customizable long title for overflow testing.

### Step 6.3 — Add One-Click Stress Harness Actions & Diagnostic Report Export to `ShellViewModel`
Add dedicated stress-test actions to the `_Diagnostics` menu and a compact quick-action strip in `ShellView`:
1. **`SpawnOverflowTabs`:** Opens 10 `SpecimenNoteDocumentViewModel` tabs with vector icons, mixed `CanClose` / dirty states, and long titles so the user can immediately click the document pane's **`MenuItemEx` overflow dropdown arrow** and press **`Ctrl+Tab` (`NavigatorWindow`)** while watching Tool 4 (`WpfTraceListenerViewModel`) for zero binding errors!
2. **`SpawnFloatingToolAndDocument`:** Programmatically floats a specimen document (`LayoutDocument.Float()`) and a toolbox (`LayoutAnchorable.Float()`) to spawn both `LayoutDocumentFloatingWindowControl` and `LayoutAnchorableFloatingWindowControl`.
3. **`SpawnDetachedOsWindow`:** Programmatically detaches a specimen toolbox into a standalone top-level OS `DetachedAnchorableWindow` (testing both Tool 3's dictionary sharing tracker and Plan 02's DWM dark title-bar helper!).
4. **`DockAllWindows` / `ResetLayout`:** Restores all floating/detached windows and resets the XML layout cleanly.
5. **`ExportDiagnosticReport` (`SaveFileDialog` -> `.md`):** Generates a comprehensive Markdown diagnostic report capturing: OS Theme Snapshot, Active Theme Metadata, full 20-Theme Cold `.ctor` & Apply Benchmark Table, Active Theme Unfrozen `Freezable` Audit, Multi-Window Dictionary Sharing Snapshot, and Captured WPF Binding/Resource Trace Errors.

---

## 3. Verification & Exit Gate to Plan 07

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Debug --no-restore --warnaserror
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git diff --name-only -- source/Components/
```

**Expected Result:**
- Both `Debug` and `Release` compile with **0 warnings, 0 errors**.
- All 5 diagnostic tools, 3 specimen document types, runtime `.vstheme`/`.json` loader, multi-window stress spawners, and `.md` report export function cleanly on `net48`.
- Zero edits to `source/Components/`.
