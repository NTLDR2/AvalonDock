# AGENTS.md — AvalonDock v5 Guide for Humans & AI Coding Agents

Authoritative technical orientation for **AvalonDock v5** (`v5.1.0` on `master`, NuGet prefix `Dirkster.AvalonDock*`).

> [!IMPORTANT]
> **Documentation & Internals Sources of Truth:**
> - **v5 User & API Docs (`new-docs-carbon4/`):** Fresh 38-page v5 documentation site. **Ignore** legacy `docs/` and `AGENTS.md.old`.
> - **Engine Internals Skill (`.agents/skills/avalondock-internals/SKILL.md`):** Complete 9-part deep dive into AvalonDock's internal architecture and invariants—always read before editing `source/Components/*`.
> - **Fork Roadmap & Caliburn.Micro AI Reference (`ROADMAP.md`):** Vendoring roadmap for [NTLDR2/SomaFrameQD](https://github.com/NTLDR2/SomaFrameQD) and architectural spec for `AvalonDock.Mvvm.CaliburnMicro`, paired with the AI-ready Caliburn.Micro internals repository at [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro).

---

## Table of Contents

1. [What Is AvalonDock v5?](#1-what-is-avalondock-v5)
2. [v4 → v5 Differences & LLM Anti-Hallucination Guardrails](#2-v4--v5-differences--llm-anti-hallucination-guardrails)
3. [Package & Layer Architecture (11 NuGet Packages)](#3-package--layer-architecture-11-nuget-packages)
4. [How AvalonDock v5 Works — Core Invariants & Internals Skill](#4-how-avalondock-v5-works--core-invariants--internals-skill)
5. [Build, Test & Verification Commands](#5-build-test--verification-commands)
6. [Testing Architecture & Critical STA / FlaUI Rules](#6-testing-architecture--critical-sta--flaui-rules)
7. [Code Style, Static Analysis & Commit Conventions](#7-code-style-static-analysis--commit-conventions)
8. [Repository & Documentation Lookup Map](#8-repository--documentation-lookup-map)
9. [Vibecoding Pitfalls Checklist](#9-vibecoding-pitfalls-checklist)

---

## 1. What Is AvalonDock v5?

AvalonDock is a WPF document and tool-window docking layout system (`https://github.com/Dirkster99/AvalonDock` XAML namespace, Ms-PL license) built around six v5 pillars:

1. **Strict UI-Agnostic / WPF Split & Zero `System.Text.Json`:** `Core`, `Mvvm`, `Mvvm.CommunityToolkit`, `DependencyInjection`, and `Serializer.Xml` target **`netstandard2.0`** (zero WPF deps); WPF packages target `net8.0/9.0/10.0/11.0-windows` and `net48` (Windows 7 SP1 64-bit–Windows 11, **zero `System.Text.Json`**).
2. **Two Root Controls:** **`DockingManager`** (classic VS 4-edge auto-hide flyouts, tabbed documents, splits, `DocumentsSource`/`AnchorablesSource`, `DockLayout`) and **`ToggleDockingManager`** *(subclasses `DockingManager`)* (VS Code / Rider 6-zone click-to-toggle sidebars, `DockLayoutPriority`, `IToolbox.Shortcut`, 6-zone drag overlay).
3. **First-Class MVVM & DI:** Zero-dependency `AvalonDock.Mvvm`, source-generator-ready `AvalonDock.Mvvm.CommunityToolkit`, `IDockLayoutService` + `SideToggleManager`, and `services.AddDockLayoutService(dock => ...)` (`AvalonDock.DependencyInjection`).
4. **DTO XML Persistence:** Bidirectional mapping between `LayoutRoot` and 17 plain DTOs (`AvalonDock.Core.Serialization.Dto`), powering `XmlLayoutSerializer` (`Dirkster.AvalonDock.Serializer.Xml`, v4 XML backward-compatible) and custom `LayoutSerializerBase` subclasses.
5. **Dual Multi-Window Modes:** Owned borderless tear-off floating windows (`LayoutFloatingWindowControl`, gated by `AllowFloatingWindows`) **plus** unowned top-level OS tool windows with taskbar entries (`DetachedAnchorableWindow` / **"Window"** mode, gated by `AllowDetachedWindows`).
6. **Modernized Theme Engine:** 5 zero-dependency theme packages (`Themes.Arc`, `Themes.VS2010`, `Themes.Expression`, `Themes.Metro`, `Themes.Aero`) + built-in `GenericTheme` and `DictionaryTheme`, all styling both `DockingManager` and `ToggleDockingManager` with live runtime switching.

---

## 2. v4 → v5 Differences & LLM Anti-Hallucination Guardrails

AI models trained on older repositories frequently emit **AvalonDock v4** (or removed early-v5) code that fails to compile or introduces unwanted dependencies. Always apply the current **v5** patterns below:

| Area | Legacy / Removed Pattern (DO NOT USE) | Current v5 Pattern (USE THIS) |
|:-----|:--------------------------------------|:------------------------------|
| **XML Serializer Package & Namespace** | Bundled in `Dirkster.AvalonDock` under `using AvalonDock.Layout.Serialization;` | Extracted to **`Dirkster.AvalonDock.Serializer.Xml`** under `using AvalonDock.Serializer.Xml;` + `using AvalonDock.Core;` |
| **Removed `System.Text.Json` Modules** | Referencing `System.Text.Json`, `Dirkster.AvalonDock.Serializer.Json`, `Dirkster.AvalonDock.Themes.VS`, or `Dirkster.AvalonDock.Themes.VS2013` | **Surgically removed** to prevent `System.Text.Json` DLL hell on `net48` (Windows 7 SP1–Windows 11). Never re-introduce `System.Text.Json`. Use `Dirkster.AvalonDock.Serializer.Xml` and `Dirkster.AvalonDock.Themes.Arc` (or `VS2010`, `Expression`, `Metro`, `Aero`) |
| **Custom Serialization Hooks** | `IXmlSerializable` (`ReadXml` / `WriteXml` on `LayoutRoot`, `LayoutContent`, etc.) | **`IXmlSerializable` is removed** from all layout classes. Subclass `AvalonDock.Core.LayoutSerializerBase` (`SerializeCore` / `DeserializeCore` over `LayoutRootDto`) |
| **Serialization Callback Model Type** | `args.Model` typed as WPF `LayoutContent` | `args.Model` typed as UI-agnostic `ISerializableLayoutContent` (`AvalonDock.Core.Serialization`) |
| **Unresolved Deserialized Anchorables** | Left indefinitely in `LayoutRoot.Hidden` | **Removed by default** (`UnresolvedContentHandling.Remove` calls `RemoveFromLayout()`). Opt into `UnresolvedContentHandling.Hide` globally or per-item on `args` for lazy-loaded plugins |
| **Serialized `ToolTip`** | Written as an XML attribute | **Not serialized** in v5 (ignored when loading v4 XML). Bind `ToolTip` in XAML styles, templates, or ViewModels |
| **Custom XAML Dictionary Merges** | Merging `/AvalonDock.Themes.VS2013;component/DarkBrushs.xaml` or `DarkTheme.xaml` | **Removed** — base XAML custom themes on `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` (note spelling: `DarkBrushs.xaml` without `e`) with `AvalonDock.Themes.Arc.Themes.ResourceKeys`, or subclass `DictionaryTheme` |
| **Theme Namespaces** | Assuming separate `AvalonDock.Themes.VS` namespace | All built-in theme classes (`ArcDarkTheme`, `ArcLightTheme`, `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `GenericTheme`) live in **`AvalonDock.Themes`** |
| **MVVM Identifier Property Name** | Ad-hoc `ContentId` property on custom VMs | `IDockable` / `DockableBase` / `ToolboxBase` / `ObservableDockableBase` name their identifier **`Id`**, while WPF `LayoutItem` names its DP **`ContentId`**. In `LayoutItemContainerStyle`, always bind `<Setter Property="ContentId" Value="{Binding Model.Id}" />` |
| **DI Registration** | Early preview `services.AddToolbox<T>()` / `AddToggleDockOptions(...)` | Consolidated builder: `services.AddDockLayoutService(dock => { dock.ConfigureToggleDock(...); dock.ConfigureDocking(...); dock.AddToolbox<T>(); })` |
| **Document Body Template Property** | Inventing `DockingManager.DocumentTemplate` (does not exist) | Use `DockingManager.LayoutItemTemplate`, `LayoutItemTemplateSelector`, or implicit `<DataTemplate DataType="{x:Type ...}">` resources |
| **Adding vs Showing Anchorables** | Calling `Show()` on a brand-new unparented `LayoutAnchorable` | `anchorable.AddToLayout(dockManager, strategy)` inserts a **new unparented** anchorable (`Parent == null`); `anchorable.Show()` restores a **hidden** anchorable (`IsHidden == true` in `Layout.Hidden`) |
| **Target Frameworks** | `net40`, `net452`, `netcoreapp3.1`, `net5.0-windows` | `net8.0-windows`, `net9.0-windows`, `net10.0-windows`, `net11.0-windows`, `net48` (WPF packages); `netstandard2.0` (Core/MVVM/DI/Serializer.Xml) |
| **Source Binding Suspension** | `SuspendDocumentsSourceBinding` / `SuspendAnchorablesSourceBinding` as fields | Now public `bool` **properties** (`{ get; set; }`) on `DockingManager` and `IDockingManager` |

---

## 3. Package & Layer Architecture (11 NuGet Packages)

```text
Host App
  ├── AvalonDock.Mvvm.CommunityToolkit (optional, netstandard2.0) ──► AvalonDock.Core
  ├── AvalonDock.DependencyInjection (netstandard2.0) ──► AvalonDock.Mvvm + AvalonDock.Core
  ├── AvalonDock.Mvvm (netstandard2.0, zero external deps) ──► AvalonDock.Core
  ├── AvalonDock.Serializer.Xml (netstandard2.0, zero external deps) ──► AvalonDock.Core
  ├── AvalonDock.Themes.{Arc,VS2010,Expression,Metro,Aero} (WPF) ──► AvalonDock
  └── AvalonDock (WPF: DockingManager, ToggleDockingManager, Layout*, Controls*) ──► AvalonDock.Core
```

| Project (`source/Components/…`) | NuGet Package | Target Frameworks | Primary Namespace(s) |
|:--------------------------------|:--------------|:------------------|:---------------------|
| `AvalonDock.Core` | `Dirkster.AvalonDock.Core` | `netstandard2.0` | `AvalonDock.Core`, `AvalonDock.Core.Events`, `AvalonDock.Core.Serialization`, `AvalonDock.Core.Serialization.Dto`, `AvalonDock.Controls` *(pure geometry rules)* |
| `AvalonDock` | `Dirkster.AvalonDock` | `net8/9/10/11-windows;net48` | `AvalonDock`, `AvalonDock.Layout`, `AvalonDock.Controls`, `AvalonDock.Themes`, `AvalonDock.Converters`, `AvalonDock.Commands`, `AvalonDock.Serialization` |
| `AvalonDock.Mvvm` | `Dirkster.AvalonDock.Mvvm` | `netstandard2.0` | `AvalonDock.Mvvm` |
| `AvalonDock.Mvvm.CommunityToolkit` | `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `netstandard2.0` | `AvalonDock.Mvvm.CommunityToolkit` |
| `AvalonDock.DependencyInjection` | `Dirkster.AvalonDock.DependencyInjection` | `netstandard2.0` | `AvalonDock.DependencyInjection` |
| `AvalonDock.Serializer.Xml` | `Dirkster.AvalonDock.Serializer.Xml` | `netstandard2.0` | `AvalonDock.Serializer.Xml` |
| `AvalonDock.Themes.Arc` | `Dirkster.AvalonDock.Themes.Arc` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes`, `AvalonDock.Themes.Arc.Themes` |
| `AvalonDock.Themes.VS2010` | `Dirkster.AvalonDock.Themes.VS2010` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Expression` | `Dirkster.AvalonDock.Themes.Expression` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Metro` | `Dirkster.AvalonDock.Themes.Metro` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |
| `AvalonDock.Themes.Aero` | `Dirkster.AvalonDock.Themes.Aero` | `net8/9/10/11-windows;net48` | `AvalonDock.Themes` |

---

## 4. How AvalonDock v5 Works — Core Invariants & Internals Skill

> [!IMPORTANT]
> **Read `.agents/skills/avalondock-internals/SKILL.md` (`avalondock-internals` skill) before modifying `source/Components/*`, debugging layout/focus/MVVM/windowing/serialization issues, or writing complex STA docking tests.**

Key invariants covered in full in `.agents/skills/avalondock-internals/SKILL.md`:
1. **Model-First Parallel Trees:** MVVM `IRootDock` ↔ logical `LayoutRoot` (7 branches; `LayoutRoot` and `LayoutFloatingWindow` inherit `LayoutElement`, **not** `LayoutGroup<T>`) ↔ visual `ILayoutControl`s.
2. **`LayoutItem` & `LogicalChildren` Depth Order:** `LayoutItem` (`DataContext = this`, `Model = Content`, single `View` `ContentPresenter`, `SetCurrentValue` seeding); `DockingManager.LogicalChildren` sorts by **descending visual depth** (`InternalAddLogicalChild` / `InternalRemoveLogicalChild`) so inheritable DPs reach inner views.
3. **Grids & Tab Virtualization:** `LayoutGridControl<T>` + `LayoutGridResizerControl` splitters; `TabControlEx` (`PART_ItemsHolder` virtualization, `IgnoreTabControlKeyBindings`).
4. **MVVM Sync & Placement:** `LayoutSyncBridge` (`_contentToSide` recorded *before* `_anchorableModels.Add`; one-way `IRootDock` window policy sync), `ILayoutUpdateStrategy` (`DockAlignmentStrategy` / `ToggleLayoutStrategy`), and `ILayoutEngine` (`DefaultLayoutEngine` / `ToggleLayoutEngine`).
5. **`ToggleDockingManager` 6-Zone Sidebars:** Two vertical `DockPanel` rails hosting 6 `ToggleDockButtonBar`s (`ToggleDockBar_LeftTop`..`BottomRight`), exclusive-per-zone / simultaneous-across-zones toggling, saved-layout-safe `IToolbox.IsOpen` sync, and host-window `IToolbox.Shortcut` `KeyBinding`s.
6. **Dual Windowing & Airspace:** Tear-off `LayoutFloatingWindowControl` (`AllowFloatingWindows`) vs standalone unowned `DetachedAnchorableWindow` (`AllowDetachedWindows`, single-presenter `View` transfer + off-screen monitor clamping); single reused `OverlayWindow` per host (`ContentDocking` fired *before* mutation); `FocusElementManager` + `LayoutAutoHideWindowControl : HwndHost`.
7. **DTO XML Serialization & Themes:** `LayoutDtoMapper` (`LayoutRoot` ↔ 17 DTOs), `XmlLayoutSerializer` (v4 encoding & `"True"`/`"False"` normalization), `FixupLayout` (`PreviousContainerId` relinking on content & `LayoutAnchorGroup`, `UnresolvedContentHandling.Remove` → `RemoveFromLayout()`, `CollectGarbage()`), and live `Theme` / `DictionaryTheme` / `Arc` `ResourceKeys` switching.

---

## 5. Build, Test & Verification Commands

### Prerequisites
- **OS:** Windows (required — `UseWPF=true`)
- **.NET SDKs:** `.NET 8.0`, `.NET 9.0`, `.NET 10.0`, and `.NET 11.0` SDKs (plus `.NET Framework 4.8` targeting pack, included with Visual Studio / Windows SDK)
- **Solution:** `source/AvalonDock.sln`

### Restore & Build
`TreatWarningsAsErrors` is enabled globally in `source/Directory.Build.props`, and CI compiles with `--warnaserror`. Every compiler, analyzer, and XML doc warning fails the build.

```powershell
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

### Run Unit & Layout Regression Tests (Fast — Run First)
Always pass **`-m:1`** (single-threaded MSBuild execution). Test projects multi-target `net10.0-windows;net11.0-windows;net48` and run WPF STA dispatchers that cannot execute safely in parallel.

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1
```

To run a single NUnit test fixture or method quickly during iteration:

```powershell
dotnet test source/AutomationTest/AvalonDockTest/AvalonDockTest.csproj --configuration Release --no-build --framework net10.0-windows --filter "FullyQualifiedName~LayoutSyncBridgeTests" -m:1
```

### Run FlaUI Desktop UI Automation Tests
FlaUI tests launch real desktop WPF windows (`TestApp.exe` and `AvalonDockCodeApp.exe`) and drive mouse/keyboard input via Windows UI Automation (`UIA3`). Run them separately per desktop framework:

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net10.0-windows -m:1
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net11.0-windows -m:1
```

---

## 6. Testing Architecture & Critical STA / FlaUI Rules

All automated tests live in `source/AutomationTest/AvalonDockTest/` (**NUnit 4**):

1. **Model / Serializer / MVVM / DI Unit Tests (`Category!=FlaUI`):** Run directly or with `[Apartment(ApartmentState.STA)]` when instantiating `DependencyObject` layout elements (`LayoutRoot`, `LayoutAnchorable`, `DockingManager`).
2. **Hosted WPF Window STA Tests (`AutomationTestBase` in `TestHelpers/`):** Runs live WPF windows via `TestHost` + `ThreadExecutor.RunCodeAsSTA(...)` against a shared STA `TestApp` `Application`.
   - **CRITICAL — NEVER CALL `Application.Current.Shutdown()` IN TESTS:** `Application.Current` is a process-wide singleton and `IsShuttingDown` is an irreversible latch that breaks `pack://application:,,,` resource loading (`Themes/generic.xaml`) for **every** subsequent fixture! Always close individual `Window` instances in `[TearDown]` instead.
3. **FlaUI UI Automation Tests (`FlaUI/*.cs`, `[Category("FlaUI")]`):** `FlaUITestBase` launches `TestApp.exe` (build `source/AvalonDock.sln` before `--no-build` runs); `ToggleDockFlaUITestBase` (`[Category("ToggleDock")]`) launches `AvalonDockCodeApp.exe` and inspects sidebar bars via `AutomationId` `"ToggleDockBar_LeftTop"`..`"ToggleDockBar_BottomRight"`.

---

## 7. Code Style, Static Analysis & Commit Conventions

Configured via `source/Directory.Build.props` and `source/.editorconfig`:

- **Indentation & Formatting:** **Tabs** (`indent_style = tab`), UTF-8, LF line endings, Allman braces (`csharp_new_line_before_open_brace = all`), `using` directives outside `namespace`.
- **Language & Nullability:** `<LangVersion>13</LangVersion>` (or `latest` in `netstandard2.0` packages). `AvalonDock.Core`, `AvalonDock.Mvvm*`, and `AvalonDock.DependencyInjection` have `<Nullable>enable</Nullable>` (with `#nullable disable` in specific legacy-compatible files); classic `AvalonDock` files omit nullable annotations or use `#nullable disable`—match the surrounding file.
- **XML Documentation Required (`CS1591` / `SA1600`):** `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled on `AvalonDock` and component packages. Every `public` (and `protected`) type, member, parameter, and return value in non-test projects **must** have valid XML doc comments or the build will fail under `--warnaserror`.
- **Active Analyzers:**
  - `StyleCop.Analyzers` (enabled on all projects whose name does not contain `Test`)
  - `Microsoft.CodeAnalysis.NetAnalyzers`
  - `SonarAnalyzer.CSharp`
  - `Microsoft.CodeAnalysis.Metrics` (`source/CodeMetricsConfig.txt`)

### Commit Message Format
```text
[Fix|Feature] #<TicketID> - [<Topic>] <What was done>
```
Examples:
- `[Fix] #696 - [Serializer] Relink LayoutAnchorGroup.PreviousContainer on layout restore`
- `[Feature] #673 - [Build] Added net8.0-windows target to NuGet packages`

---

## 8. Repository & Documentation Lookup Map

| When Working On… | Key Source Files (`source/…`) | Authoritative v5 Docs (`new-docs-carbon4/…`) |
|:-----------------|:------------------------------|:-------------------------------------|
| **Root `DockingManager`, DPs, visual creation, logical children, events** | `Components/AvalonDock/DockingManager.cs`, `*EventArgs.cs` | `api-reference/docking-manager.md`, `api-reference/events.md`, `concepts/architecture.md` |
| **`ToggleDockingManager`, 6-zone sidebars, shortcuts, zone drag overlay** | `Components/AvalonDock/ToggleDockingManager.cs`, `ToggleLayoutEngine.cs`, `ToggleLayoutStrategy.cs`, `Controls/Toggle*.cs`, `AvalonDockCodeApp/` | `guides/toggle-docking-manager.md`, `api-reference/toggle-docking-manager.md` |
| **Layout model tree, `DockWidth`/`DockHeight`, `CollectGarbage()`, `AddToLayout` vs `Show`** | `Components/AvalonDock/Layout/*.cs`, `DefaultLayoutEngine.cs` | `concepts/layout-model.md`, `concepts/documents-and-anchorables.md`, `api-reference/layout-classes.md` |
| **WPF pane/tab controls, `LayoutItem`, splitters, `NavigatorWindow`, converters** | `Components/AvalonDock/Controls/*.cs`, `Converters/*.cs` | `api-reference/controls.md` |
| **Floating windows, standalone `DetachedAnchorableWindow`, `OverlayWindow`, `DragService`** | `Components/AvalonDock/Controls/LayoutFloatingWindowControl*.cs`, `DetachedAnchorableWindow.cs`, `DragService.cs`, `OverlayWindow.cs`, `*DropTarget.cs`, `Components/AvalonDock.Core/Controls/Overlay*.cs` | `concepts/floating-windows.md`, `api-reference/controls.md` |
| **UI-agnostic contracts, MVVM ViewModels, `IDockLayoutService`, `LayoutSyncBridge`** | `Components/AvalonDock.Core/Interfaces/*.cs`, `FactoryBase*.cs`, `Components/AvalonDock.Mvvm/*.cs`, `Components/AvalonDock.Mvvm.CommunityToolkit/*.cs`, `Components/AvalonDock/LayoutSyncBridge.cs`, `DockAlignmentStrategy.cs` | `guides/mvvm.md`, `api-reference/core-and-mvvm.md`, `tutorials/mvvm-ide.md` |
| **Dependency Injection (`AddDockLayoutService`, `DockingOptions`, `ToggleDockOptions`)** | `Components/AvalonDock.DependencyInjection/*.cs`, `Components/AvalonDock/ThemeManager.cs` | `guides/dependency-injection.md`, `tutorials/dependency-injection-app.md` |
| **XML Serialization & DTO mapping** | `Components/AvalonDock.Core/Serialization/*.cs`, `Dto/*.cs`, `Components/AvalonDock/Serialization/LayoutDtoMapper.cs`, `Components/AvalonDock.Serializer.Xml/XmlLayoutSerializer.cs` | `guides/serialization.md`, `tutorials/layout-persistence.md` |
| **Themes (`Arc`, `VS2010`, `Expression`, `Metro`, `Aero`, custom themes)** | `Components/AvalonDock/Themes/*.cs`, `Components/AvalonDock.Themes.Arc/`, `Components/AvalonDock.Themes.*` | `guides/theming.md`, `guides/custom-themes.md`, `themes/index.md`, `themes/arc.md` |
| **Migrating v4 apps & breaking changes** | — | `migration/index.md`, `migration/breaking-changes.md` |
| **Caliburn.Micro BCL (`AvalonDock.Mvvm.CaliburnMicro`) & fork roadmap** | `CaliburnDockTestApp/`, `Components/AvalonDock.Mvvm*/*.cs` | Root `ROADMAP.md` + AI-ready internals repo: `https://github.com/NTLDR2/Caliburn.Micro` |
| **Unit tests & FlaUI UI automation tests** | `AutomationTest/AvalonDockTest/*.cs`, `TestHelpers/*.cs`, `FlaUI/*.cs` | `getting-started/building-from-source.md`, `contributing.md` |

---

## 9. Vibecoding Pitfalls Checklist

Before finishing any code change or generating sample code for AvalonDock v5, verify:

- [ ] **No `System.Text.Json` anywhere:** Never add `System.Text.Json` package references or imports to any project—`net48` compatibility from Windows 7 SP1 through Windows 11 must remain free of `System.Text.Json` DLL conflicts.
- [ ] **No `netstandard2.0` boundary leaks:** `AvalonDock.Core`, `Mvvm*`, `DependencyInjection`, and `Serializer.Xml` must never reference WPF types (`System.Windows.*`).
- [ ] **`Id` ↔ `ContentId` mapped:** When binding ViewModels implementing `IDockable` / `IToolbox`, `LayoutItemContainerStyle` binds `ContentId` to `{Binding Model.Id}`, and `LayoutSerializationCallback` compares `vm.Id == args.Model.ContentId`.
- [ ] **Window policy configured in one place:** When `DockLayout` is bound, set `AllowFloatingWindows` / `AllowDetachedWindows` on `IRootDock` or `DockingOptions` (not only in XAML, which `LayoutSyncBridge` overwrites from `IRootDock`).
- [ ] **Adding vs restoring anchorables:** Use `AddToLayout` only when `Parent == null` (`!IsVisible && !IsHidden`); use `Show()` when `IsHidden == true`; use `RemoveFromLayout()` to detach without firing close events or disturbing auto-hide siblings.
- [ ] **DTO round-trip parity:** Any new persisted layout property is added to the appropriate DTO in `AvalonDock.Core.Serialization.Dto` (with `ShouldSerialize*()` / `[DefaultValue]` XML guards as appropriate), mapped in **both** `ToDto` and `FromDto` in `LayoutDtoMapper.cs`, and tested with `XmlLayoutSerializer`.
- [ ] **Tab icons in `Arc`:** Remember that `Arc` renders clean title-only document tabs by default; provide a custom `DocumentHeaderTemplate` if tab icons are required in `Arc`.
- [ ] **Zero warnings & `-m:1` tests:** `dotnet build source/AvalonDock.sln --configuration Release --warnaserror` passes with 0 warnings, and `dotnet test ... -m:1` passes without calling `Application.Current.Shutdown()`.
