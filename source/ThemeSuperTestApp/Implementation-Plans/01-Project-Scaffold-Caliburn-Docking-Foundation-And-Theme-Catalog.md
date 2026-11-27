# Plan 01 (of 07) — Project Scaffold, Caliburn.Micro Docking Foundation & Lazy Theme Catalog

> **Execution Order:** **Step 1 of 7** (Foundation stage — must compile cleanly with `--warnaserror` before starting [Plan 02](02-Windows-OS-Theme-Detection-DWM-Chrome-And-Pinned-Shell-Header.md)).
> **Hard Constraints Across All 7 Plans:**
> 1. **DO NOT edit any existing theme code or XAML** (`source/Components/AvalonDock.Themes.*` and `source/Components/AvalonDock/Themes/*` must remain 100% untouched).
> 2. **Target `.NET Framework 4.8` (`net48`) exclusively** with **zero** `System.Text.Json`, `System.Memory`, or `Microsoft.Bcl.*` transitive dependencies.
> 3. **Use `Caliburn.Micro` `6.0.15-beta`** (`SimpleContainer`, action messages, `DockLayout` + `IDockLayoutService`) and **classic `DockingManager` only** (no `ToggleDockingManager`).

---

## Master 7-Plan Sequence Overview

| Plan | File | Scope |
| :--- | :--- | :--- |
| **01** | `01-Project-Scaffold-Caliburn-Docking-Foundation-And-Theme-Catalog.md` | `ThemeSuperTestApp.csproj` (`net48`), Caliburn.Micro `6.0.15-beta` + `DockLayout` boilerplate, and lazy 20-theme `ThemeCatalogService` with base-class / `[XmlnsDefinition]` reflection metadata. |
| **02** | [02-Windows-OS-Theme-Detection-DWM-Chrome-And-Pinned-Shell-Header.md](02-Windows-OS-Theme-Detection-DWM-Chrome-And-Pinned-Shell-Header.md) | Win32/Registry Windows 10/11 Dark/Light/High-Contrast live detector, opt-in DWM dark title-bar helper, and `ShellView` pinned Theme/Base-Class/OS header bar. |
| **03** | [03-Tool-1-Theme-Architecture-Inspector-And-Constructor-Profiler.md](03-Tool-1-Theme-Architecture-Inspector-And-Constructor-Profiler.md) | **Tool 1:** Theme Architecture & Timing Profiler toolbox (20-theme matrix, cold `.ctor` benchmark vs. `OnThemeChanged` apply profiler, `[XmlnsDefinition]` XAML audit). |
| **04** | [04-Tool-2-ResourceDictionary-Freezable-Auditor-And-Cross-Theme-Diff.md](04-Tool-2-ResourceDictionary-Freezable-Auditor-And-Cross-Theme-Diff.md) | **Tool 2:** Live `ResourceDictionary` & `Freezable.IsFrozen` Auditor + cross-theme `ResourceKeys` parity diff toolbox. |
| **05** | [05-Tools-3-And-4-MultiWindow-Sharing-Tracker-And-WPF-Trace-Listener.md](05-Tools-3-And-4-MultiWindow-Sharing-Tracker-And-WPF-Trace-Listener.md) | **Tools 3 & 4:** Multi-window `ResourceDictionary` object-identity sharing tracker + live `PresentationTraceSources` XAML binding/resource error listener. |
| **06** | [06-Tool-5-Runtime-VsTheme-Loader-Specimens-And-Stress-Harness.md](06-Tool-5-Runtime-VsTheme-Loader-Specimens-And-Stress-Harness.md) | **Tool 5 & Specimens:** Runtime `.vstheme`/`.json` custom theme loader, WPF control contrast specimen documents, overflow/window stress actions, and Markdown report export. |
| **07** | [07-Solution-Integration-STA-Diagnostic-Unit-Tests-And-Documentation.md](07-Solution-Integration-STA-Diagnostic-Unit-Tests-And-Documentation.md) | Wire `ThemeSuperTestApp` into `source/AvalonDock.sln`, add STA diagnostic unit tests in `AvalonDockTest`, write `README.md`, and run final end-to-end verification gates. |
| **08** | [08-Dual-Mode-Screenshot-Engine-Composite-Stitcher-And-F12-Snapshot.md](08-Dual-Mode-Screenshot-Engine-Composite-Stitcher-And-F12-Snapshot.md) | **Antigravity Bridge (Part 1):** `.gitignore` hygiene, dual-mode screenshot engine (`RenderTargetBitmap` + Win32 `PrintWindow` DWM title bars + multi-window composite PNG + 40-capture retention), and `F12` **"Send Snapshot to Antigravity"**. |
| **09** | [09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md](09-Live-AgentBridge-XML-Mailbox-Service-Sweep-Engine-And-Tool-6-UI.md) | **Antigravity Bridge (Part 2):** Auto-listening `AgentBridgeService` (`heartbeat.xml`, `XDocument` XML `inbox/`->`outbox/` remote commands, 20-theme screenshot sweep) and **Tool 6: Antigravity Live Bridge** (`Ctrl+Alt+A`). |
| **10** | [10-Antigravity-Workspace-Skill-STA-Bridge-Tests-And-Docs-Sync.md](10-Antigravity-Workspace-Skill-STA-Bridge-Tests-And-Docs-Sync.md) | **Antigravity Bridge (Part 3):** Workspace skill (`.agents/skills/theme-super-test-bridge/SKILL.md`), isolated temp-dir STA unit tests in `AvalonDockTest`, and `README.md` sync. |

---

## 1. Objective & Scope of Plan 01

Stand up a clean, warning-free `net48` WPF application in `source/ThemeSuperTestApp/` hosted by `Caliburn.Micro` `6.0.15-beta` (`SimpleContainer`), establish the reusable v5 `DockLayout` + `IDockLayoutService` docking plumbing for classic `DockingManager`, and implement the **`ThemeCatalogService`** that catalogs all **20 built-in AvalonDock themes** using **lazy `Func<Theme>` factories** (so cold constructor costs are preserved for profiling rather than paid upfront in `Bootstrapper.Configure()`).

---

## 2. Step-by-Step Implementation Tasks

### Step 1.1 — Create `source/ThemeSuperTestApp/ThemeSuperTestApp.csproj`
Create an SDK-style WPF project modeled on `source/ExperimentalTestApp/ExperimentalTestApp.csproj`:
- `<OutputType>WinExe</OutputType>`
- `<TargetFrameworks>net48</TargetFrameworks>` (**exclusively `net48`**)
- `<UseWPF>true</UseWPF>`
- `<RootNamespace>ThemeSuperTestApp</RootNamespace>` and `<AssemblyName>ThemeSuperTestApp</AssemblyName>` (kept identical so Caliburn.Micro `ViewLocator` resolves pack URIs cleanly)
- `<Nullable>enable</Nullable>`, `<ImplicitUsings>disable</ImplicitUsings>`, `<IsPackable>false</IsPackable>`
- Single NuGet `<PackageReference Include="Caliburn.Micro" Version="6.0.15-beta" />`
- `<ProjectReference>` items to:
  - `..\Components\AvalonDock\AvalonDock.csproj`
  - `..\Components\AvalonDock.Core\AvalonDock.Core.csproj`
  - `..\Components\AvalonDock.Mvvm\AvalonDock.Mvvm.csproj`
  - `..\Components\AvalonDock.Serializer.Xml\AvalonDock.Serializer.Xml.csproj`
  - All 7 theme packages: `AvalonDock.Themes.Arc`, `AvalonDock.Themes.VS`, `AvalonDock.Themes.VS2013`, `AvalonDock.Themes.VS2010`, `AvalonDock.Themes.Expression`, `AvalonDock.Themes.Metro`, `AvalonDock.Themes.Aero`.
- Add `<InternalsVisibleTo Include="AvalonDockTest" />` (via `<ItemGroup>` or `Properties/AssemblyInfo.cs`) so Plan 07 can unit-test internal diagnostic services directly.

### Step 1.2 — Add Caliburn.Micro v6 + AvalonDock v5 Docking Plumbing
Following the proven `AGENTS.md` guardrails from `source/ExperimentalTestApp/`, create:
1. **`Infrastructure/TraceLog.cs` & `Infrastructure/LogSink.cs`:** Lightweight `Caliburn.Micro.ILog` implementation assigned before `Initialize()`.
2. **`Boilerplate/DockableScreen.cs`, `DocumentScreen.cs`, `ToolboxScreen.cs`, `DockLayoutConductor.cs`:**
   - Base `Screen` / `Conductor<T>.Collection.OneActive` bridges implementing `IDockable`, `IDocument`, and `IToolbox` (`Id`, `Title`, `CanClose`, `CanFloat`, `IsOpen`, `PreferredSide`, `Shortcut`, `IconSource`).
   - Use modern Caliburn.Micro `6.0.15-beta` async lifecycle hooks (`OnInitializedAsync`, `OnActivatedAsync`, `OnDeactivateAsync` — zero `[Obsolete]` v4 shims, zero `ICommand` boilerplate).
3. **`Docking/DockingResources.xaml`, `PanesStyleSelector.cs`, `ZonePlacementStrategy.cs`, `DocumentCloseBehavior.cs`, `ToolboxVisibilityBehavior.cs`, `ToolboxShortcutBehavior.cs`:**
   - **Critical View-Hosting Rule:** Define an implicit `<DataTemplate DataType="{x:Type boilerplate:DockableScreen}">` using `cal:View.Model="{Binding}"` inside `DockingResources.xaml` and leave `DockingManager.LayoutItemTemplate` **unset** so views render identically in docked panes, `LayoutFloatingWindowControl`, and standalone `DetachedAnchorableWindow`.
   - Bind `<Setter Property="ContentId" Value="{Binding Model.Id}" />` in `LayoutItemContainerStyle` (`PanesStyleSelector`).
   - Include a custom `DocumentHeaderTemplate` rendering `IconSource` + `Title` + dirty indicator so tab icons can be tested across **all** themes (including `Arc`, which omits icons in its default header template, and `VS`/`VS2013`) — plus a toggleable option in later plans to switch between custom `DocumentHeaderTemplate` vs. theme-native header template!

### Step 1.3 — Implement `Models/ThemeDescriptor.cs` & `Services/ThemeCatalogService.cs`
Create rich metadata models that inspect every theme **without instantiating it eagerly** until requested:
1. **`ThemeBaseClassKind` enum (`Models/ThemeBaseClassKind.cs`):**
   - `UriTheme` — Direct subclass of `AvalonDock.Themes.Theme` overriding `GetResourceUri()` (`Arc*`, `VS2010`, `Expression*`, `Metro`, `Aero`, `GenericTheme`).
   - `DictionaryTheme` — Direct subclass of `AvalonDock.Themes.DictionaryTheme` (`Vs2013{Blue,Dark,Light}Theme`).
   - `VsTheme` — Subclass of `AvalonDock.Themes.VS.VsTheme : DictionaryTheme` (`VS2015*`, `VS2022*`, `VS2026*`, plus runtime-loaded `.vstheme`/`.json` instances).
2. **`ThemePolarity` enum (`Models/ThemePolarity.cs`):**
   - `Light`, `Dark`, `Blue`, `ClassicSystem` (`Aero`, `VS2010`, `Metro`, `Generic`).
3. **`ThemeDescriptor` (`Models/ThemeDescriptor.cs`, inherits `PropertyChangedBase`):**
   - Static reflection metadata (computed at registration with **zero** `new XxxTheme()` allocation):
     - `Id` (`string`), `DisplayName` (`string`), `ThemeType` (`Type`), `Func<Theme> Factory`
     - `BaseClassKind` (`ThemeBaseClassKind`), `BaseClassBadgeText` (e.g., `"Theme (URI)"`, `"DictionaryTheme"`, `"VsTheme : DictionaryTheme"`)
     - `InheritanceChain` (`string`, e.g., `"VS2026DarkTheme -> VsTheme -> DictionaryTheme -> Theme"`)
     - `AssemblyName` (`string`), `ClrNamespace` (`string`)
     - `IsMappedInXmlnsDefinition` (`bool` — inspects `[XmlnsDefinitionAttribute]` on `ThemeType.Assembly` for `ThemeType.Namespace`, automatically flagging `AvalonDock.Themes.VS`!)
     - `Polarity` (`ThemePolarity`), `IsCustomRuntimeTheme` (`bool`)
   - Live runtime telemetry properties (updated when instantiated or applied):
     - `IsInstantiated` (`bool`), `CachedInstance` (`Theme?`), `ResourceUriText` (`string` — resolved `GetResourceUri()?.ToString() ?? "<null — DictionaryTheme>"`)
     - `LastColdConstructorMs` (`double?`), `LastColdAllocatedBytes` (`long?`), `LastApplyMs` (`double?`)
4. **`ThemeCatalogService` (`Services/ThemeCatalogService.cs`):**
   - Registers all **20 built-in themes** in logical family order (`Arc` Light/Dark, `VS2026` Light/Blue/Dark, `VS2022` Light/Blue/Dark, `VS2015` Light/Blue/Dark, `VS2013` Light/Blue/Dark, `VS2010`, `Expression` Light/Dark, `Metro`, `Aero`, `Generic`).
   - `Attach(DockingManager dockingManager)`: attaches to the live classic `DockingManager`.
   - `ApplyTheme(ThemeDescriptor descriptor, bool forceFreshInstance = false)`:
     - Measures cold constructor cost (`Stopwatch.GetTimestamp()` + `GC.GetTotalMemory(false)` delta) when `CachedInstance == null || forceFreshInstance`.
     - Measures `DockingManager.Theme = instance` (`OnThemeChanged`) duration in milliseconds.
     - Fires `ActiveThemeChanged` event for all diagnostic tools.
   - `RegisterRuntimeTheme(...)`: allows Tool 5 (Plan 06) to append external `.vstheme`/`.json` themes into the live `ObservableCollection<ThemeDescriptor>`.

### Step 1.4 — Minimal Compilable `App.xaml`, `Bootstrapper.cs`, `ShellViewModel.cs` & `ShellView.xaml`
- Wire `App.xaml` (hosting `<local:Bootstrapper x:Key="Bootstrapper" />`), `Bootstrapper.cs` (`SimpleContainer`, registering `ShellView` as `.Singleton<ShellView>()` so `GetAllInstances` never creates an orphan `Window`, `IDockLayoutService` with `AllowFloatingWindows = true` and `AllowDetachedWindows = true`, and `ThemeCatalogService`), and a minimal `ShellViewModel` + `ShellView` hosting `<ad:DockingManager x:Name="DockManager" DockLayout="{Binding DockLayout}">` with initial `Zone_Left`, `Zone_Right`, `Zone_Bottom` anchorable panes.

---

## 3. Verification & Exit Gate to Plan 02

```powershell
dotnet restore source/ThemeSuperTestApp/ThemeSuperTestApp.csproj
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
Get-ChildItem "source/ThemeSuperTestApp/bin/Release/net48/*.dll" | Select-Object Name
git diff --name-only -- source/Components/
```

**Expected Result:**
- `ThemeSuperTestApp` builds with **0 warnings, 0 errors** on `net48`.
- `bin/Release/net48/` contains only the 15 clean AvalonDock + Caliburn.Micro + `Microsoft.Xaml.Behaviors` DLLs (zero `System.Text.Json`, `System.Memory`, or `Microsoft.Bcl.*`).
- `git diff --name-only -- source/Components/` shows **zero** edits to any existing theme or component file.
