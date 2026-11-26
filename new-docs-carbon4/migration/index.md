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
v4 → v5

This guide walks through every step required to upgrade an existing WPF application from AvalonDock v4.x (such as `v4.74.1`) to AvalonDock v5 (`v5.0.2+`).

---

## Overview

AvalonDock v5 modernizes the core architecture: layout persistence is decoupled into a plain DTO layer with separate XML and JSON serializer packages, Dependency Injection and MVVM are first-class citizens, floating and standalone detached windows were reworked from the ground up, and the theme stack gained Arc and modern Visual Studio (`.vstheme` and VS2026 JSON) support.

Most classic v4 applications upgrade with only a few `PackageReference` and `using` updates, while custom `IXmlSerializable` hooks or direct VS2013 XAML dictionary merges require targeted adjustments.

{: .important }
Review this guide alongside the complete [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) reference before starting your upgrade.

---

## Quick Upgrade Checklist

- [ ] Ensure your WPF project targets `.NET 8+` (`-windows`), or `.NET Framework 4.8`
- [ ] Update `Dirkster.AvalonDock` and theme `PackageReference` entries to `5.0.2` (or latest `5.x`)
- [ ] Add `Dirkster.AvalonDock.Serializer.Xml` (or `Serializer.Json`) if you persist layouts
- [ ] Update serializer namespaces (`AvalonDock.Layout.Serialization` → `AvalonDock.Serializer.Xml` + `AvalonDock.Core`)
- [ ] Migrate any custom `IXmlSerializable` layout hooks to `LayoutSerializerBase` / DTOs
- [ ] Replace any direct `/AvalonDock.Themes.VS2013;component/DarkBrushs.xaml` dictionary merges
- [ ] Check unresolved anchorable handling (`UnresolvedContentHandling.Hide`) if late-loaded plugins rely on `LayoutRoot.Hidden`
- [ ] Rebuild and test layout round-trips, floating/detached windows, and theme switching

---

## Step 1: Update Target Frameworks

AvalonDock v5 drops out-of-support legacy frameworks (`.NET Framework 4.0`, `4.5.2`, `.NET Core 3.x`, `.NET 5`):

| v4.x Target | v5 Status |
|:------------|:----------|
| `.NET Framework 4.0` / `4.5.2` | ❌ Dropped — upgrade to `net48` |
| `.NET Framework 4.8` (`net48`) | ✅ Supported |
| `.NET Core 3.0` / `3.1`, `.NET 5` | ❌ Dropped — upgrade to `net8.0-windows` or newer |
| `.NET 8` (`net8.0-windows`) | ✅ Supported since `v5.0.2` |
| `.NET 9` (`net9.0-windows`) | ✅ Supported since `v5.0.0` |
| `.NET 10` (`net10.0-windows`) | ✅ Supported since `v5.0.0` |
| `.NET 11` (`net11.0-windows`) | ✅ Supported in `v5.1.0+` |

---

## Step 2: Update NuGet Packages

In v4, XML serialization was bundled inside `Dirkster.AvalonDock`. In v5, serializers, MVVM helpers, and DI extensions ship in dedicated packages:

### Before (v4.x)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="4.74.1" />
<PackageReference Include="Dirkster.AvalonDock.Themes.VS2013" Version="4.74.1" />
```

### After (v5)

```xml
<PackageReference Include="Dirkster.AvalonDock" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.Themes.VS2013" Version="5.0.2" />

<!-- REQUIRED if you use XmlLayoutSerializer -->
<PackageReference Include="Dirkster.AvalonDock.Serializer.Xml" Version="5.0.2" />

<!-- OPTIONAL new v5 packages -->
<PackageReference Include="Dirkster.AvalonDock.Serializer.Json" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.Mvvm" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.Mvvm.CommunityToolkit" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.DependencyInjection" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.Themes.Arc" Version="5.0.2" />
<PackageReference Include="Dirkster.AvalonDock.Themes.VS" Version="5.0.2" />
```

### Package Architecture Mapping

| v4.x Package | v5 Package | Action Required |
|:-------------|:-----------|:----------------|
| `Dirkster.AvalonDock` (included XML serializer) | `Dirkster.AvalonDock` (WPF controls & layout model) | Keep; add serializer package separately if saving layouts |
| — | `Dirkster.AvalonDock.Core` | Automatically referenced by `AvalonDock` (install directly only in UI-agnostic `netstandard2.0` libraries) |
| *(bundled in `AvalonDock`)* | `Dirkster.AvalonDock.Serializer.Xml` | **Install explicitly** if using `XmlLayoutSerializer` |
| — | `Dirkster.AvalonDock.Serializer.Json` | Optional new `System.Text.Json` layout serializer |
| — | `Dirkster.AvalonDock.Mvvm` | Optional zero-dependency MVVM base classes & `DockLayoutService` |
| — | `Dirkster.AvalonDock.Mvvm.CommunityToolkit` | Optional `CommunityToolkit.Mvvm` (`ObservableObject`) base classes |
| — | `Dirkster.AvalonDock.DependencyInjection` | Optional `IServiceCollection` & `DockLayoutBuilder` integration |
| `Dirkster.AvalonDock.Themes.*` | `Dirkster.AvalonDock.Themes.*` (+ new `Arc` & `VS`) | Update version; consider adopting `Themes.Arc` or `Themes.VS` |

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

### Optional: Switch to JSON Serialization

```csharp
using AvalonDock.Serializer.Json;

var serializer = new JsonLayoutSerializer(dockManager);
serializer.Serialize("layout.json");
```

---

## Step 4: Review Theme & Custom Dictionary Usage

- **Standard theme instantiation is unchanged:** `new Vs2013DarkTheme()`, `new VS2010Theme()`, `new ExpressionDarkTheme()`, `new MetroTheme()`, and `new AeroTheme()` continue to work as in v4.
- **Custom themes merging `VS2013` XAML dictionaries:** In v5, `Dirkster.AvalonDock.Themes.VS2013` was migrated onto compressed `.vstheme.gz` resources (`#572`). If your application merged `/AvalonDock.Themes.VS2013;component/DarkBrushs.xaml` or `DarkTheme.xaml` directly in XAML, switch to merging `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` or build a palette using `VsTheme` / `VsThemeColorPalette` from `Dirkster.AvalonDock.Themes.VS`.
- **New v5 Themes:** Explore `Dirkster.AvalonDock.Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`) and `Dirkster.AvalonDock.Themes.VS` (`VS2015*`, `VS2022*`, `VS2026*`, plus custom `.vstheme` and VS2026 JSON override files) in [Themes]({{ site.baseurl }}{% link themes/index.md %}).

---

## Step 5: Layout Engine & DI Updates

- **`ILayoutEngine`:** Layout tree insertion logic is formalized behind `ILayoutEngine` (`DefaultLayoutEngine` on `DockingManager`, `ToggleLayoutEngine` on `ToggleDockingManager`). Default behavior requires no changes.
- **Dependency Injection Builder (`#584`):** If you adopted early v5 preview DI extensions (`services.AddToolbox<T>()` / `services.AddToggleDockOptions(...)`), consolidate them into `services.AddDockLayoutService(dock => { dock.ConfigureToggleDock(...); dock.AddToolbox<T>(); })`.

---

## Step 6: Build & Verify

1. **Compile** your solution and resolve any namespace or API cleanup errors.
2. **Test legacy layout loading:** Load an existing v4 `.xml` layout file from production and verify tool window and document restoration.
3. **Test docking & window modes:** Verify docking, auto-hide, floating windows, and standalone windows (**View Mode → Window**).
4. **Test themes:** Confirm all themes and custom brush overrides load cleanly at runtime.

---

## Detailed Reference & Support

- Read the complete [Breaking Changes]({{ site.baseurl }}{% link migration/breaking-changes.md %}) list for per-item impact and code fixes.
- Report migration issues on [GitHub Issues](https://github.com/Dirkster99/AvalonDock/issues) or ask questions in [GitHub Discussions](https://github.com/Dirkster99/AvalonDock/discussions).
