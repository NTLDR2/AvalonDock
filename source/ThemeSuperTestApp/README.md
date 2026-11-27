# ThemeSuperTestApp — AvalonDock v5 Theme Debugger & Best-Practices Auditor

`ThemeSuperTestApp` is a dedicated WPF visual inspection, performance-profiling, and best-practices auditing workbench for **AvalonDock v5** themes on **`.NET Framework 4.8` (`net48`)**.

> **Companion Technical Reports:**
> - [THEMES.md](../Components/THEMES.md) — Complete inventory of all 20 built-in themes across 8 assemblies.
> - [THEMES-ANALYSIS-AND-BEST-PRACTICES.md](../Components/THEMES-ANALYSIS-AND-BEST-PRACTICES.md) — Architectural analysis of `Theme` (`AvalonDock.Themes`) vs. `VsTheme : DictionaryTheme` (`AvalonDock.Themes.VS`) and `net48` vs. modern `.NET` WPF best practices.

---

## 1. Key Characteristics & Guardrails

- **Target Framework:** `.NET Framework 4.8` (`net48`) exclusively — zero `System.Text.Json`, `System.Memory`, or `Microsoft.Bcl.*` assemblies.
- **MVVM & IoC:** Hosted by **`Caliburn.Micro` `6.0.15-beta`** (`SimpleContainer`, action messages with leaf `FrameworkElement` `KeyTrigger`s, async `Screen` / `Conductor` lifecycles, and `IDockLayoutService` + `DockLayout`).
- **Root Docking Surface:** Classic **`DockingManager`** with `AllowFloatingWindows = true` and `AllowDetachedWindows = true`, using an implicit `<DataTemplate DataType="{x:Type boilerplate:DockableScreen}">` so views resolve identically in docked panes, tear-off `LayoutFloatingWindowControl`s, and standalone `DetachedAnchorableWindow`s.
- **Non-Invasive Auditing:** Inspects all 20 built-in themes as-is without mutating their `ResourceDictionary` or `Freezable` state.

---

## 2. Pinned Two-Row Theme & OS Diagnostics Header Bar

At the top of `ShellView`, an always-visible two-row command header provides instant architectural visibility:

1. **Row 1 — Active Theme & Base-Class Strip:**
   - **Theme Dropdown (`F8` / `Shift+F8`):** Lists all 20 built-in themes (plus any runtime-loaded `.vstheme` / `.json` themes) with polarity dots, color-coded base-class badges (`Theme (URI)` in teal, `DictionaryTheme` in amber, `VsTheme : DictionaryTheme` in purple), and CLR namespaces (`AvalonDock.Themes` vs. `AvalonDock.Themes.VS`).
   - **Inheritance & `[XmlnsDefinition]` Readout:** Displays the exact CLR inheritance chain (e.g., `VS2026DarkTheme -> VsTheme -> DictionaryTheme -> Theme`), `GetResourceUri()` tooltip, green/amber `[XmlnsDefinition]` mapping badge, `.ctor` / `Apply` latency pill, and a **`Custom Icon Tab Header`** toggle (`IconDocumentHeaderTemplate` vs. `<null>` native theme header template).
2. **Row 2 — Live Windows OS Dark/Light/High-Contrast Detector & Chrome Controls:**
   - Queries real OS build via `ntdll!RtlGetVersion`, explicit `HKCU\...\Personalize\AppsUseLightTheme` & `SystemUsesLightTheme` (`Explicit Dark`, `Explicit Light`, `Unset / Legacy OS`), `SystemParameters.HighContrast`, and `DWM\AccentColor` + `DWM\ColorPrevalence`, updating live via `WM_SETTINGCHANGE` / `WM_DWMCOLORIZATIONCOLORCHANGED`.
   - Provides opt-in simulation controls: **`DWM Title Bars`** (`Untouched net48 Default` | `Match Windows OS Mode` | `Match Active AvalonDock Theme` across both `ShellView` and standalone `DetachedAnchorableWindow`s), **`Style Outer Shell Chrome`**, and **`Auto-Sync to OS`**.

---

## 3. Built-In 5-Tool Diagnostic Suite

| Tool | Shortcut | Best-Practice Issue Diagnosed |
| :--- | :--- | :--- |
| **1. Theme Architecture & Profiler** (`ThemeProfilerViewModel`) | `Ctrl+Alt+P` | Measures cold `new XxxTheme()` constructor latency and managed `KB` allocation (`Theme (URI)` `~0.2 ms / 8 KB` vs. eager `DictionaryTheme` `~2–58 ms / 500–1,700 KB`), full 20-theme `OnThemeChanged` + WPF `Render` sweep latency, and missing `[XmlnsDefinition]` mappings. |
| **2. Resource & `Freezable` Auditor** (`ResourceAuditorViewModel`) | `Ctrl+Alt+R` | Walks root and nested `MergedDictionaries` in exact WPF lookup precedence order, renders color swatches and `#AARRGGBB` hex codes, flags **unfrozen `Freezable`s** (`!IsFrozen`), and computes cross-theme semantic `ResourceKeys` / `ImplicitStyle` parity diffs. |
| **3. Multi-Window Dict Sharing** (`WindowSharingTrackerViewModel`) | `Ctrl+Alt+W` | Uses `RuntimeHelpers.GetHashCode(dict)` across `DockingManager`, `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, `NavigatorWindow`, and `OverlayWindow` to contrast `DictionaryTheme`'s `O(1)` shared `Ref #1` instance against URI `Theme`s' per-window duplicated BAML parses. |
| **4. WPF XAML Binding & Resource Trace** (`WpfTraceListenerViewModel`) | `Ctrl+Alt+T` | Hooks `PresentationTraceSources` (`DataBindingSource`, `ResourceDictionarySource`, `MarkupSource`) in-process on `net48` to catch `System.Windows.Data Error: 40` and `{DynamicResource}` misses tagged with the active theme. |
| **5. Runtime `.vstheme` / JSON Loader** (`RuntimeThemeLoaderViewModel`) | `Ctrl+Alt+L` | Loads external `.vstheme`, `.vstheme.gz`, or VS2026 `.json` files with selectable `VS2015` / `VS2022` `Generic.xaml` templates and includes a live editable VS2026 JSON override sandbox exercising the zero-dependency pure-C# `VsJsonThemeParser`. |

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

## 5. Build, Run & Test Commands

```powershell
# Build entire solution (Debug & Release, zero warnings allowed)
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Debug --no-restore --warnaserror
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror

# Launch ThemeSuperTestApp interactively
.\source\ThemeSuperTestApp\bin\Debug\net48\ThemeSuperTestApp.exe

# Run automated STA diagnostic unit tests (always pass -m:1)
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net48 --filter "FullyQualifiedName~ThemeSuperTestAppDiagnosticsTests" -m:1
```
