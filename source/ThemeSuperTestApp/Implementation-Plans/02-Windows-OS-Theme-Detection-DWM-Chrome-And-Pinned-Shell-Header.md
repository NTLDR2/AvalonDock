# Plan 02 (of 07) — Windows OS Theme Detection, DWM Chrome Helper & Pinned Shell Header Bar

> **Execution Order:** **Step 2 of 7** (Requires [Plan 01](01-Project-Scaffold-Caliburn-Docking-Foundation-And-Theme-Catalog.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Implement live Windows 10/11 Dark Mode, Light Mode, High Contrast, and OS Accent Color detection on `.NET Framework 4.8`, pair it with opt-in DWM non-client title-bar / outer-shell chrome controls, and build the permanent **Pinned Top Theme & OS Diagnostics Header Bar** in `ShellView.xaml` so the user can always see at a glance:
1. **Which theme is currently selected** (via a rich custom-templated dropdown),
2. **Exact base class & namespace** (`Theme` vs. `DictionaryTheme` vs. `VsTheme : DictionaryTheme`, full inheritance chain, and `[XmlnsDefinition]` warning badge), and
3. **Live Windows OS Light/Dark/High-Contrast state** and how it interacts with `net48` WPF chrome.

---

## 2. Step-by-Step Implementation Tasks

### Step 2.1 — Implement `Services/WindowsOsThemeService.cs` & `Models/WindowsOsThemeSnapshot.cs`
Because `.NET Framework 4.8` predates `.NET 9`'s `Application.ThemeMode`, build a rock-solid, safe Win32 + Registry detector targeting Windows 7 SP1 through Windows 11:
1. **`WindowsOsThemeSnapshot` immutable record/class:**
   - `OsVersionCaption` (via P/Invoke `ntdll!RtlGetVersion` so `Environment.OSVersion` manifest shims never lie about Windows 10 vs. Windows 11 build `>= 22000`, e.g., `"Windows 11 (Build 26100)"`).
   - `AppsThemeMode` (`ExplicitLight`, `ExplicitDark`, or `NotConfiguredOrLegacyOs` — distinguishes whether `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme` is explicitly present as `1` or `0` vs. absent on older Windows!).
   - `SystemThemeMode` (`ExplicitLight`, `ExplicitDark`, or `NotConfiguredOrLegacyOs` — from `SystemUsesLightTheme`).
   - `IsTitleBarAccentColorEnabled` (`bool` — from `Personalize\ColorPrevalence`).
   - `IsHighContrastEnabled` (`bool` — `SystemParameters.HighContrast`) and `HighContrastSchemeName` (`Registry` `HighContrast\Pre-High Contrast Scheme` / active scheme).
   - `OsAccentColor` (`System.Windows.Media.Color` — decoded from `HKCU\Software\Microsoft\Windows\DWM\AccentColor` ABGR `DWORD` or fallback `SystemParameters.WindowGlassColor`) + frozen `OsAccentBrush`.
   - `SupportsDwmImmersiveDarkMode` (`bool` — Windows 10 build `>= 17763`).
2. **Live OS Change Monitoring (Zero Polling):**
   - Attach an `HwndSourceHook` to `ShellView`'s `WindowInteropHelper.Handle` listening for `WM_SETTINGCHANGE` (`0x001A`, checking `lParam` `"ImmersiveColorSet"` / `"WindowMetrics"`), `WM_DWMCOLORIZATIONCOLORCHANGED` (`0x0320`), and `WM_SYSCOLORCHANGE` (`0x0015`), plus `SystemEvents.UserPreferenceChanged` (unsubscribed cleanly on window close!).
   - Whenever the user toggles Dark/Light mode or Accent Color in Windows Settings while `ThemeSuperTestApp` is running, recompute `CurrentSnapshot` and raise `OsThemeChanged` on the WPF `Dispatcher`.

### Step 2.2 — Implement Opt-In `Services/DwmWindowChromeHelper.cs`
On `net48`, WPF leaves native Win32 title bars white even when Windows OS Dark Mode *or* a Dark AvalonDock theme is active — affecting both the main `ShellView` window and unowned `DetachedAnchorableWindow` ("Window" mode) instances:
- P/Invoke `dwmapi.dll!DwmSetWindowAttribute`:
  - Use `DWMWA_USE_IMMERSIVE_DARK_MODE = 20` on Windows 10 Build `18985+` / Windows 11, falling back to `19` on Windows 10 `17763–18984` (no-op safely on Windows 7/8).
- Provide **three explicit modes** selectable in the header bar (defaulting to **`DefaultUntouched`** so baseline `net48` behavior is observed unaltered first!):
  1. `DefaultUntouched` *(Default)* — Leaves Win32/WPF `net48` title bars completely untouched so the user sees raw out-of-the-box `net48` behavior.
  2. `MatchWindowsOsMode` — Sets `DWMWA_USE_IMMERSIVE_DARK_MODE` to match `AppsThemeMode == ExplicitDark` on `ShellView` and any open `DetachedAnchorableWindow`.
  3. `MatchActiveAvalonDockTheme` — Sets `DWMWA_USE_IMMERSIVE_DARK_MODE = 1` whenever the active AvalonDock theme's `Polarity == Dark`, and `0` otherwise, across `ShellView` and all open `DetachedAnchorableWindow`s.

### Step 2.3 — Build the Pinned Theme & OS Diagnostics Header Bar in `Views/ShellView.xaml` & `ViewModels/ShellViewModel.cs`
Upgrade `ShellView` and `ShellViewModel` with a structured, high-clarity two-row diagnostic command bar docked at the top above `<ad:DockingManager x:Name="DockManager">`:
1. **Row 1 — Master Menu + Rich Theme Selector + Base-Class Architecture Strip:**
   - **Menu (`_File`, `_View` toolboxes, `_Theme` quick actions, `_Layout` XML save/load/reset, `_Diagnostics`):** Pure Caliburn.Micro `cal:Message.Attach` actions (and leaf `FrameworkElement` `KeyTrigger`s so `Window.IsEnabled` is never disabled by action availability guards).
   - **Rich Theme `ComboBox` (`ItemsSource="{Binding Themes}"`, `SelectedItem="{Binding SelectedTheme}"`):**
     - Each dropdown row renders: `[Polarity Color Dot]` + **Bold `DisplayName`** + **Color-Coded Base-Class Pill** (`Theme (URI)` in teal, `DictionaryTheme` in amber, `VsTheme : DictionaryTheme` in purple) + muted `ClrNamespace` (`AvalonDock.Themes` vs. `AvalonDock.Themes.VS`).
   - **Selected Theme Live Callout Strip (immediately right of the `ComboBox`):**
     - Displays **Base Class:** `{SelectedTheme.BaseClassBadgeText}`
     - Displays **Inheritance:** `{SelectedTheme.InheritanceChain}`
     - Displays **XAML `[XmlnsDefinition]` Status Badge:** Green `Xmlns Mapped` vs. Amber `Missing XmlnsDefinition (clr-namespace required)`
     - Displays **Timing Pill:** `.ctor: X.XX ms | Apply: Y.YY ms`
     - Checkbox: **Use Custom Tab Icon Header Template** (lets you toggle `DockingManager.DocumentHeaderTemplate` live between the app's icon-enabled template vs. `null` so you can inspect each theme's **native** tab header template!).
2. **Row 2 — Live Windows OS Theme Banner & Chrome Controls:**
   - Displays live OS pills: `OS: {OsVersionCaption}`, `Apps Mode: Explicit Dark / Explicit Light / Unset`, `System Mode: Dark / Light`, `High Contrast: Off / ON`, `[OS Accent Swatch + #RRGGBB]`, and a **Theme vs. OS Contrast Notice** (e.g., `"Active AvalonDock theme is Dark while net48 Aero2 / OS Apps Mode is Light"`).
   - Interactive diagnostic controls:
     - `DWM Title Bars:` ComboBox (`Untouched net48 Default` | `Match Windows OS` | `Match Active Theme`)
     - Checkbox: `Style Outer Shell Chrome from Active Theme` (toggles `ShellView` Menu/Header/StatusBar between raw unthemed `net48` `Aero2` system brushes vs. `DockManager.Background` / active theme brushes so theme bleed/contrast issues are obvious!)
     - Checkbox / Button: `Sync Theme to OS Mode` (switches between `VS2022Dark` and `VS2022Light` — or user-chosen pair — when OS `AppsUseLightTheme` flips).

---

## 3. Verification & Exit Gate to Plan 03

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Debug --no-restore --warnaserror
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git diff --name-only -- source/Components/
```

**Expected Result:**
- Both `Debug` and `Release` compile with **0 warnings, 0 errors**.
- Launching `ThemeSuperTestApp.exe` shows all 20 themes in the dropdown with clear base-class badges (`Theme`, `DictionaryTheme`, `VsTheme : DictionaryTheme`), inheritance chains, live Windows 10/11 Dark/Light/High-Contrast readout, and working DWM/chrome toggles.
- Zero changes to `source/Components/`.
