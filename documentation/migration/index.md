---
title: Migration Guide
layout: default
nav_order: 8
has_children: true
description: "Step-by-step guide to migrating applications from AvalonDock v4.x to v5."
permalink: /migration/
---

# Migration Guide
{: .label .label-yellow }
v4 → v5.1.0 (Fork & Upstream Scope Labeled)

This guide walks through every step required to upgrade an existing WPF application from AvalonDock v4.x (such as `v4.74.1`) to **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0` fork)**, while explicitly distinguishing upstream `Dirkster99/AvalonDock` (`v5.0.0`–`v5.1.0`) changes from fork-specific changes.

---

## Scope & Version Baselines

Because this documentation covers the **`NTLDR2/AvalonDock` (`v5.1.0`) fork** rather than a strict mirror of upstream `v5.0.0` release notes, every migration item below is attributed to its exact baseline:

1. **`Upstream v5.0.0` ([Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock)):** Extracted `Serializer.Xml`, `Core`, `Mvvm`, `Mvvm.CommunityToolkit`, and `DependencyInjection` (`AddDockLayoutService` `#584`); replaced `IXmlSerializable` with plain DTOs (`#580`); added `ToggleDockingManager`, `DetachedAnchorableWindow`, `AllowFloatingWindows` / `AllowDetachedWindows` (`#637`), and `Themes.Arc`; dropped `< net48` and `.NET Core 3.x / .NET 5`. *(Note: upstream `v5.0.0` originally shipped **14 packages** on `nuget.org`, including `Serializer.Json`, `Themes.VS`, and `Themes.VS2013`.)*
2. **`Upstream v5.0.2` & `v5.1.0`:** Added `net8.0-windows` (`v5.0.2`, `#673`) and `net11.0-windows` (`v5.1.0`).
3. **`NTLDR2 Fork v5.1.0` ([NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock)):** Omits `Serializer.Json` and refactors `Themes.VS` (`VsJsonThemeParser`) + `Themes.VS2013` to pure zero-dependency C# — shipping **13 zero-`System.Text.Json` libraries** to eliminate `net48` binding-redirect DLL hell — targets `.NET Framework 4.8` (`net48`) strictly across all WPF packages/samples/tests (with `netstandard2.0` UI-agnostic libraries), and uses vendored `ProjectReference` / local `dotnet pack` distribution rather than publishing fork builds to `nuget.org`.

{: .important }
Review this guide alongside the complete [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) reference before starting your upgrade.

---

## Quick Upgrade Checklist

- [ ] Ensure your WPF project targets `.NET Framework 4.8` (`net48`)
- [ ] Update `Dirkster.AvalonDock` and theme `PackageReference` / `ProjectReference` entries to `5.1.0`
- [ ] Add `Dirkster.AvalonDock.Serializer.Xml` if you persist layouts
- [ ] Update serializer namespaces (`AvalonDock.Layout.Serialization` → `AvalonDock.Serializer.Xml` + `AvalonDock.Core`)
- [ ] Migrate any custom `IXmlSerializable` layout hooks to `LayoutSerializerBase` / DTOs
- [ ] Audit custom `Themes.VS2013` XAML dictionary merges (`DarkBrushs.xaml` / `DarkTheme.xaml` were replaced by embedded `.vstheme.gz` resources in v5 `#572`; base custom XAML themes on `Themes.Arc` or override `Vs2013*Theme.ThemeResourceDictionary` in C#)
- [ ] Check unresolved anchorable handling (`UnresolvedContentHandling.Hide`) if late-loaded plugins rely on `LayoutRoot.Hidden`
- [ ] Rebuild and test layout round-trips, floating/detached windows, and theme switching

---

## Step 1: Update Target Frameworks

Legacy out-of-support frameworks were dropped in `Upstream v5.0.0`, while `.NET 8` and `.NET 11` were added in subsequent `5.x` updates:

| v4.x Target | v5.1.0 Fork Status | Origin / Scope |
|:------------|:-------------------|:---------------|
| `.NET Framework 4.0` / `4.5.2` | ❌ Dropped — upgrade to `net48` | `Upstream v5.0.0` |
| `.NET Framework 4.8` (`net48`) | ✅ **Supported (Exclusive Desktop Target)** | `Upstream v5.0.0` + **`NTLDR2 Fork v5.1.0`** (Win7 SP1 64-bit – Win11) |
| `.NET Core 3.0` / `3.1`, `.NET 5` | ❌ Dropped | `Upstream v5.0.0` |
| `.NET 8` (`net8.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.2` (`#673`) |
| `.NET 9` (`net9.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.0` |
| `.NET 10` (`net10.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.0` |
| `.NET 11` (`net11.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.1.0` |

---

## Step 2: Update Project / Package References

In v4, XML serialization was bundled inside `Dirkster.AvalonDock`. Starting in `Upstream v5.0.0`, the XML serializer, MVVM helpers, and DI extensions ship in dedicated libraries. When targeting **`NTLDR2 Fork v5.1.0`**, reference the vendored `source/Components/*` projects directly or consume local `5.1.0` `.nupkg` artifacts packed from this repository *(note: `nuget.org` hosts upstream `Dirkster99` builds)*:

### Before (v4.x)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="4.74.1" />
<PackageReference Include="Dirkster.AvalonDock.Themes.VS2013" Version="4.74.1" />
```

### After (`NTLDR2 Fork v5.1.0` — Local NuGet Feed or Equivalent `ProjectReference`)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="5.1.0" />
<PackageReference Include="Dirkster.AvalonDock.Themes.Arc" Version="5.1.0" />

<!-- REQUIRED if you use XmlLayoutSerializer (extracted in Upstream v5.0.0) -->
<PackageReference Include="Dirkster.AvalonDock.Serializer.Xml" Version="5.1.0" />

<!-- OPTIONAL v5 MVVM & DI packages (introduced in Upstream v5.0.0) -->
<PackageReference Include="Dirkster.AvalonDock.Mvvm" Version="5.1.0" />
<PackageReference Include="Dirkster.AvalonDock.Mvvm.CommunityToolkit" Version="5.1.0" />
<PackageReference Include="Dirkster.AvalonDock.DependencyInjection" Version="5.1.0" />
```

### Package Architecture Mapping

| v4.x Package | `NTLDR2 Fork v5.1.0` Library | Origin / Scope | Action Required |
|:-------------|:-----------------------------|:---------------|:----------------|
| `Dirkster.AvalonDock` (included XML serializer) | `Dirkster.AvalonDock` (WPF controls & layout model) | `Upstream v5.0.0` | Keep; add `Dirkster.AvalonDock.Serializer.Xml` separately if saving layouts |
| — | `Dirkster.AvalonDock.Core` | `Upstream v5.0.0` | Transitive for WPF apps; reference directly in UI-agnostic `netstandard2.0` libraries |
| *(bundled in `AvalonDock`)* | `Dirkster.AvalonDock.Serializer.Xml` | `Upstream v5.0.0` | **Reference explicitly** if using `XmlLayoutSerializer` |
| — | `Dirkster.AvalonDock.Mvvm` | `Upstream v5.0.0` | Optional zero-dependency MVVM base classes & `DockLayoutService` |
| — | `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | `Upstream v5.0.0` | Optional `CommunityToolkit.Mvvm` (`ObservableObject`) base classes |
| — | `Dirkster.AvalonDock.DependencyInjection` | `Upstream v5.0.0` | Optional `IServiceCollection` & `DockLayoutBuilder` integration |
| `Dirkster.AvalonDock.Themes.VS2013` | `Dirkster.AvalonDock.Themes.VS2013` *(or `Themes.Arc`)* | `Upstream v5.0.0` (`#572`) + **`NTLDR2 Fork v5.1.0`** | `Vs2013*Theme` classes continue to work (zero `System.Text.Json` in this fork); loose v4 XAML files (`DarkBrushs.xaml` / `DarkTheme.xaml`) were replaced by embedded `.vstheme.gz` resources |
| — | `Dirkster.AvalonDock.Themes.VS` | `Upstream v5.0.0` + **`NTLDR2 Fork v5.1.0`** | Optional `VS2015*`, `VS2022*`, and `VS2026*` themes + `.vstheme` / pure-C# VS2026 JSON loader (`AvalonDock.Themes.VS` namespace; zero `System.Text.Json`) |
| *(early v5 `Serializer.Json`)* | `Dirkster.AvalonDock.Serializer.Xml` | **`NTLDR2 Fork v5.1.0`** *(introduced in upstream `v5.0.0`)* | Not shipped in this fork (zero `System.Text.Json` policy); use `XmlLayoutSerializer` or subclass `LayoutSerializerBase` |
| `Dirkster.AvalonDock.Themes.{VS2010,Expression,Metro,Aero}` | `Dirkster.AvalonDock.Themes.{VS2010,Expression,Metro,Aero}` | `Upstream v5.0.0` | Update version / project reference |

---

## Step 3: Update Serialization Code

### Standard `XmlLayoutSerializer` Usage

Update your `using` directive from `AvalonDock.Layout.Serialization` to `AvalonDock.Serializer.Xml` (and `AvalonDock.Core` if referencing `LayoutSerializationCallbackEventArgs` or `UnresolvedContentHandling` explicitly):

**Before (v4.x):**
```csharp
using AvalonDock.Layout.Serialization;

var serializer = new XmlLayoutSerializer(dockManager);
serializer.Serialize("layout.xml");
```

**After (v5):**
```csharp
using AvalonDock.Core;
using AvalonDock.Serializer.Xml;

var serializer = new XmlLayoutSerializer(dockManager);
serializer.Serialize("layout.xml");
```

{: .important }
**Key v5 Serialization Differences:**
1. **Legacy v4 XML files still load in v5:** `XmlLayoutSerializer` automatically normalizes legacy v4 capitalized booleans (`"True"`/`"False"`) and mismatched XML encoding prologs. However, **files written by v5 are not guaranteed to load in v4**.
2. **`IXmlSerializable` removed from layout classes:** `LayoutRoot`, `LayoutPanel`, `LayoutAnchorable`, `LayoutDocument`, etc. no longer implement `IXmlSerializable`. Custom serializers or hooks into `ReadXml`/`WriteXml` must move to `LayoutSerializerBase` and DTOs in `AvalonDock.Core.Serialization.Dto`.
3. **Unresolved anchorables are removed by default:** Set `serializer.UnresolvedContentHandling = UnresolvedContentHandling.Hide` if late-loaded plugins expect unresolved anchorables to remain in `LayoutRoot.Hidden`.
4. **`ToolTip` is no longer written to layout files:** Bind or assign tooltips on your view or view model instead.

---

## Step 4: Review Theme & Custom Dictionary Usage

- **`VS2010`, `Expression`, `Metro`, `Aero`, and `GenericTheme` instantiation is unchanged:** `new VS2010Theme()`, `new ExpressionDarkTheme()`, `new MetroTheme()`, and `new AeroTheme()` continue to work as in v4.
- **`VS2013` loose XAML dictionaries removed (`Upstream v5.0.0`, `#572`):** `new Vs2013DarkTheme()`, `new Vs2013LightTheme()`, and `new Vs2013BlueTheme()` continue to work in v5 (and run with zero `System.Text.Json` dependencies in `NTLDR2 Fork v5.1.0`). However, v4's loose XAML files (`DarkBrushs.xaml`, `LightBrushs.xaml`, `BlueBrushs.xaml`, `DarkTheme.xaml`) were replaced by embedded `.vstheme.gz` resources — base custom XAML merged-dictionary themes on `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` (or `LightTheme.xaml`) using `AvalonDock.Themes.Arc.Themes.ResourceKeys`, or override `Vs2013*Theme.ThemeResourceDictionary` in C#.
- Explore all 7 theme packages in [Themes]({{ site.baseurl }}{% link themes/index.md %}).

---

## Step 5: Layout Engine & DI Updates

- **`ILayoutEngine` (`Upstream v5.0.0`):** Layout tree insertion logic is formalized behind `ILayoutEngine` (`DefaultLayoutEngine` on `DockingManager`, `ToggleLayoutEngine` on `ToggleDockingManager`). Default behavior requires no changes.
- **Dependency Injection Builder (`Upstream v5.0.0`, `#584`):** If you adopted early v5 preview DI extensions (`services.AddToolbox<T>()` / `services.AddToggleDockOptions(...)`), consolidate them into `services.AddDockLayoutService(dock => { dock.ConfigureToggleDock(...); dock.AddToolbox<T>(); })`.

---

## Step 6: Build & Verify

1. **Compile** your solution and resolve any namespace or API cleanup errors.
2. **Test legacy layout loading:** Load an existing v4 `.xml` layout file from production and verify tool window and document restoration.
3. **Test docking & window modes:** Verify docking, auto-hide, floating windows, and standalone windows (**View Mode → Window**).
4. **Test themes:** Confirm all themes and custom brush overrides load cleanly at runtime.

---

## Detailed Reference & Support

- Read the complete [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) list for per-item origin tags (`Upstream v5.0.0`–`v5.1.0` vs. `NTLDR2 Fork v5.1.0`), impact, and code fixes.
- Report fork-specific migration issues on [NTLDR2/AvalonDock Issues](https://github.com/NTLDR2/AvalonDock/issues) (or consult upstream [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) for upstream `nuget.org` releases).
