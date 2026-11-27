---
title: Breaking Changes
layout: default
parent: Migration Guide
nav_order: 1
description: "Complete reference of breaking and behavioral changes between AvalonDock v4.x and v5."
---

# Breaking Changes (v4.x → v5.0.0 Upstream & NTLDR2 Fork v5.1.0)

This page catalogs every source, binary, package, target-framework, and behavioral breaking change between AvalonDock v4.x and **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0` fork)**, along with new non-breaking v5 capabilities.

{: .note }
**Explicit Scope Labels:** Every change on this page is labeled by its exact origin so readers comparing against upstream [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) release notes can distinguish upstream v5 changes from fork-specific policy:
- **`Upstream v5.0.0`** — Architectural breaking changes introduced in Dirkster99's `v5.0.0` release (which shipped 14 packages, including `Serializer.Json`, `Themes.VS`, and `Themes.VS2013`).
- **`Upstream v5.0.2` / `Upstream v5.1.0`** — Target framework expansions (`net8.0-windows` in `v5.0.2`; `net11.0-windows` in `v5.1.0`) and layout restore fixes (`#696`).
- **`NTLDR2 Fork v5.1.0`** — Fork-specific removal of the 3 `System.Text.Json`-dependent packages (14 → 11 libraries) to guarantee zero `System.Text.Json` DLL hell on `.NET Framework 4.8`.

---

## 1. Package & Namespace Changes

### XML Serializer Extracted to a Separate NuGet Package

**Origin / Scope:** `Upstream v5.0.0` *(retained in `NTLDR2 Fork v5.1.0`)*  
**Impact:** High — affects every project that saves or restores layouts.

`XmlLayoutSerializer` is no longer bundled inside `Dirkster.AvalonDock`. It now lives in a dedicated zero-dependency `netstandard2.0` package:

| Change | Details |
|:-------|:--------|
| **Removed from** | `Dirkster.AvalonDock` |
| **Moved to** | `Dirkster.AvalonDock.Serializer.Xml` |
| **Old namespace** | `AvalonDock.Layout.Serialization` |
| **New namespaces** | `AvalonDock.Serializer.Xml` + `AvalonDock.Core` (for `LayoutSerializationCallbackEventArgs` and `UnresolvedContentHandling`) |

```diff
- using AvalonDock.Layout.Serialization;
+ using AvalonDock.Core;
+ using AvalonDock.Serializer.Xml;
```

### Removal of `System.Text.Json`-Dependent Packages (`Serializer.Json`, `Themes.VS`, `Themes.VS2013`)

**Origin / Scope:** **`NTLDR2 Fork v5.1.0` (Fork-Specific — Shipped in `Upstream v5.0.0`)**  
**Impact:** High if referencing `Themes.VS2013` from v4 (or `Serializer.Json` / `Themes.VS` from upstream `v5.0.0`).

Whereas upstream `Dirkster99/AvalonDock v5.0.0` introduced `Dirkster.AvalonDock.Serializer.Json` and retained `Dirkster.AvalonDock.Themes.VS` and `Dirkster.AvalonDock.Themes.VS2013` (14 packages total), **`NTLDR2/AvalonDock` (`v5.1.0` fork) surgically removes those three packages** (shipping **11 libraries**) to guarantee clean, binding-redirect-free deployment on `.NET Framework 4.8` from Windows 7 SP1 64-bit through Windows 11 with zero out-of-band `System.Text.Json` assemblies:
- `Dirkster.AvalonDock.Serializer.Json` — use `Dirkster.AvalonDock.Serializer.Xml` (`XmlLayoutSerializer`), or implement a custom `LayoutSerializerBase` subclass in your own project if JSON persistence is required.
- `Dirkster.AvalonDock.Themes.VS` & `Dirkster.AvalonDock.Themes.VS2013` — migrate to `Dirkster.AvalonDock.Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`), `Dirkster.AvalonDock.Themes.Expression` (`ExpressionDarkTheme`, `ExpressionLightTheme`), or `Dirkster.AvalonDock.Themes.VS2010` (`VS2010Theme`). Base custom XAML themes on `/AvalonDock.Themes.Arc;component/DarkTheme.xaml` (or `LightTheme.xaml`) with `AvalonDock.Themes.Arc.Themes.ResourceKeys`.

### UI-Agnostic Core Package Extracted

**Origin / Scope:** `Upstream v5.0.0`  
**Impact:** Low — transitive for WPF apps.

`Dirkster.AvalonDock.Core` (`netstandard2.0`) holds UI-agnostic interfaces, enums, and serialization DTOs. `Dirkster.AvalonDock` references it transitively.

---

## 2. Serialization & Architecture Breaking Changes (`Upstream v5.0.0`)

### `IXmlSerializable` Removed in Favor of DTO Serialization (`#580`)

**Origin / Scope:** `Upstream v5.0.0`  
**Impact:** Medium — affects custom serializers or subclasses calling `ReadXml` / `WriteXml`.

All layout model classes (`LayoutRoot`, `LayoutPanel`, `LayoutGroup<T>`, `LayoutPositionableGroup<T>`, `LayoutContent`, `LayoutDocument`, `LayoutAnchorable`, `LayoutDocumentPane`, `LayoutAnchorablePane`, `LayoutAnchorSide`, `LayoutAnchorGroup`, `LayoutFloatingWindow`, etc.) **no longer implement `IXmlSerializable`**.

- **Why:** Coupling WPF layout elements directly to `IXmlSerializable` prevented custom formats, caused `XmlSerializer` assembly-probing issues on modern .NET, and tied serialization to UI types.
- **Backward Compatibility:** `XmlLayoutSerializer` **continues to read existing v4 XML layout files** (normalizing v4 `"True"`/`"False"` booleans and mismatched XML encoding declarations). However, **files saved by v5 are not guaranteed to load in v4**.
- **Fix for Custom Serializers:** Subclass `AvalonDock.Core.LayoutSerializerBase` and override `SerializeCore(Stream, LayoutRootDto)` and `DeserializeCore(Stream)` against the plain DTO types in `AvalonDock.Core.Serialization.Dto` (see [Layout Serialization]({{ site.baseurl }}{% link guides/serialization.md %})).

### Dependency Injection Builder Consolidation (`#584`)

**Impact:** Low — affects code written against early v5 preview DI APIs.

Top-level `IServiceCollection.AddToolbox<T>()` and `IServiceCollection.AddToggleDockOptions(...)` methods were replaced by a single `DockLayoutBuilder` callback on `AddDockLayoutService`:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.ConfigureToggleDock(opts => opts.ButtonSize = 28);
    dock.ConfigureDocking(opts => opts.AllowDetachedWindows = true);
    dock.AddToolbox<ExplorerToolbox>();
});
```

### Window Policy Properties on `IDockingManager` & `IRootDock` (`#637`)

**Impact:** Low — affects only custom implementations of `IDockingManager` or `IRootDock`.

Both `IDockingManager` and `IRootDock` added `bool AllowFloatingWindows { get; set; }` and `bool AllowDetachedWindows { get; set; }`. Built-in classes (`DockingManager`, `AvalonDock.Mvvm.RootDock`) implement them; custom implementations must add both properties (defaulting to `true`).

### Public API Cleanup (`#567`, `#581`, `#625`)

**Impact:** Low — binary breaking, mostly source-compatible.

- `DockingManager.SuspendDocumentsSourceBinding` and `DockingManager.SuspendAnchorablesSourceBinding` changed from public `bool` **fields** to public `bool` **properties** (`{ get; set; }`). Normal assignments compile unchanged, but assemblies compiled against v4 must be recompiled against v5.
- `LayoutSerializationCallbackEventArgs.Model` is typed as `ISerializableLayoutContent` (from `AvalonDock.Core.Serialization`) rather than `LayoutContent` so callbacks work identically across UI-agnostic serializers.

---

## 3. Target Framework Changes

**Impact:** High — if targeting legacy runtimes.

| Target Framework | Status in `v5.1.0` Fork | Origin / Scope |
|:-----------------|:------------------------|:---------------|
| `.NET Framework 4.0` / `4.5.2` | ❌ **Removed** | `Upstream v5.0.0` |
| `.NET Core 3.0` / `3.1` | ❌ **Removed** | `Upstream v5.0.0` |
| `.NET 5.0` | ❌ **Removed** | `Upstream v5.0.0` |
| `.NET 6.0` / `7.0` | ❌ **Not targeted** (out of Microsoft support) | `Upstream v5.0.0` |
| `.NET Framework 4.8` (`net48`) | ✅ **Supported (Exclusive Desktop Target)** | `Upstream v5.0.0` + **`NTLDR2 Fork v5.1.0`** (Win7 SP1–Win11) |
| `.NET 8.0` (`net8.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.2` (`#673`) |
| `.NET 9.0` (`net9.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.0` |
| `.NET 10.0` (`net10.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.0.0` |
| `.NET 11.0` (`net11.0-windows`) | Upstream only (`NTLDR2 Fork v5.1.0` targets `net48` strictly for WPF) | `Upstream v5.1.0` |
| `.NET Standard 2.0` (`netstandard2.0`) | ✅ **Supported** on `Core`, `Mvvm*`, `DependencyInjection`, and `Serializer.Xml` | `Upstream v5.0.0` & `NTLDR2 Fork v5.1.0` |

---

## 4. Behavioral Changes (`Upstream v5.0.0`–`v5.1.0`)

### Unresolved Layout Items Are Removed on Deserialization (`#625`)

**Impact:** Low — affects applications that inspect `LayoutRoot.Hidden` after loading a layout.

When a saved layout contains an item whose content is not supplied by `LayoutSerializationCallback` (for example, a retired tool window), v5 **removes that item from the layout** (`UnresolvedContentHandling.Remove`). In v4, unresolved documents were removed while unresolved anchorables stayed in `LayoutRoot.Hidden` forever across subsequent saves.

**Fix:** If your application loads plugins lazily after deserialization and expects parked anchorables in `LayoutRoot.Hidden`, opt into `UnresolvedContentHandling.Hide` globally or per item:

```csharp
var serializer = new XmlLayoutSerializer(dockManager);

serializer.LayoutSerializationCallback += (s, args) =>
{
    var content = FindContent(args.Model.ContentId);
    if (content != null)
    {
        args.Content = content;
        return;
    }

    // Keep known not-yet-loaded plugin tool windows parked in LayoutRoot.Hidden;
    // let unknown retired tool windows be removed cleanly.
    if (_knownPluginContentIds.Contains(args.Model.ContentId))
        args.UnresolvedContentHandling = UnresolvedContentHandling.Hide;
};
```

Once the plugin loads, attach its content and call `Show()`:

```csharp
var parked = dockManager.Layout.Hidden.FirstOrDefault(a => a.ContentId == contentId);
if (parked != null)
{
    parked.Content = pluginViewModel;
    parked.Show();
}
```

`Show()` restores the anchorable to its remembered pane and index, or docks it into an existing/new tool window pane if the remembered pane no longer exists. *(Note: `args.Cancel = true` always drops the item regardless of `UnresolvedContentHandling`.)*

### `ToolTip` Is No Longer Stored in Layout Files (`#625`)

**Impact:** Low — affects applications that only set `LayoutContent.ToolTip` once before saving.

Tooltips belong to the view/content (often UI elements or bindings that cannot be serialized as text) and are pushed from `LayoutItem` whenever a view attaches. v5 omits `ToolTip` when writing layouts and ignores legacy `ToolTip` attributes when reading v4 XML files. Set tooltips in your view template, `LayoutItemContainerStyle`, or view model instead.

### Floating Anchorable System Close Routes Through `HideWindowCommand` (`#368`, `#610`)

**Impact:** Low — improves consistency.

Clicking the OS title-bar close (`X`) button on a floating anchorable window now routes through `HideWindowCommand` (hiding hideable tool windows cleanly instead of bypassing hide semantics).

### Stable Pane Insertion Order & Restacking Fixes (`#556`)

**Impact:** Low.

Pane insertion order for `DocumentPaneDockRight` / `DocumentPaneDockBottom` and bottom panel restacking were corrected in v5. Verify hardcoded index assumptions if you manipulate `LayoutPanel.Children` directly by index.

---

## 5. New Non-Breaking Features (`Upstream v5.0.0`)

*(Note: `Upstream v5.0.0` also introduced `Dirkster.AvalonDock.Serializer.Json`, which is excluded in `NTLDR2 Fork v5.1.0` per the zero-`System.Text.Json` policy above.)*

| Feature | Package | Description |
|:--------|:--------|:------------|
| `ToggleDockingManager` | `Dirkster.AvalonDock` | VS Code / JetBrains Rider-style 6-zone sidebar toggle docking control (supported across all 5 theme packages and `GenericTheme`). |
| Standalone Windows | `Dirkster.AvalonDock` | `DetachAnchorableToWindow` detaches a tool window into a top-level OS window with its own taskbar entry (**View Mode → Window**), persisted via `LayoutAnchorable.IsDetached`. |
| Window Policy Switches | `Dirkster.AvalonDock` | `AllowFloatingWindows` and `AllowDetachedWindows` on `DockingManager`, `IRootDock`, and `DockingOptions` disable floating and/or standalone windows manager-wide. |
| Toolbox Shortcuts | `Dirkster.AvalonDock` | `IToolbox.Shortcut` registers a host-window `KeyBinding` to toggle a toolbox and appends the shortcut to its sidebar tooltip. |
| Tab KeyBinding Bypass | `Dirkster.AvalonDock` | `DockingManager.IgnoreTabControlKeyBindings` (`#508`) prevents `TabControlEx` from intercepting `Home`/`End` and other shortcuts needed by inner controls. |
| Arc Theme | `Dirkster.AvalonDock.Themes.Arc` | Modern compact dark and light themes (`ArcDarkTheme`, `ArcLightTheme`) with `ResourceKeys` brush customization. |
| MVVM Packages | `AvalonDock.Mvvm` & `.CommunityToolkit` | Zero-dependency (`DockableBase`, `ToolboxBase`, `DockLayoutService`, `SideToggleManager`) and `CommunityToolkit.Mvvm` (`ObservableDockableBase`, `ObservableToolboxBase`) view-model bases. |
| DI Builder | `AvalonDock.DependencyInjection` | `AddDockLayoutService(dock => ...)` builder and `AddAvalonDock<TFactory>()` service extensions. |

---

## Summary Matrix

| Category | Change | Origin / Scope | Impact | Required Action |
|:---------|:-------|:---------------|:-------|:----------------|
| **Packages** | XML serializer extracted to `Dirkster.AvalonDock.Serializer.Xml` | `Upstream v5.0.0` | High | Reference `Dirkster.AvalonDock.Serializer.Xml` |
| **Packages** | `Themes.VS2013`, `Themes.VS`, and `Serializer.Json` removed (no `System.Text.Json`) | **`NTLDR2 Fork v5.1.0`** *(shipped in `Upstream v5.0.0`)* | Medium–High | Switch to `Themes.Arc` (or `Expression` / `VS2010`) and `Serializer.Xml` |
| **Namespaces** | `AvalonDock.Layout.Serialization` → `AvalonDock.Serializer.Xml` | `Upstream v5.0.0` | High | Update `using` statements |
| **Serialization** | `IXmlSerializable` replaced by DTOs & `LayoutSerializerBase` | `Upstream v5.0.0` | Medium | Move custom `ReadXml`/`WriteXml` logic to DTOs |
| **Frameworks** | `.NET < 4.8` & `.NET Core 3.x / 5` dropped; `.NET 8` & `.NET 11` added upstream | `Upstream v5.0.0`–`v5.1.0` | High | Target `net48` in `NTLDR2 Fork v5.1.0` (`Upstream v5.0.2+` also ships `net8.0-windows`–`net11.0-windows`) |
| **DI** | `AddDockLayoutService(dock => ...)` builder pattern (`#584`) | `Upstream v5.0.0` | Low | Move `AddToolbox` / options inside builder |
| **Interfaces** | `AllowFloatingWindows` / `AllowDetachedWindows` on `IDockingManager` & `IRootDock` | `Upstream v5.0.0` | Low | Implement on custom interface implementations |
| **Binary API** | `Suspend*SourceBinding` fields became properties | `Upstream v5.0.0` | Low | Recompile assemblies against v5 |
| **Behavior** | Unresolved deserialized items default to `Remove` | `Upstream v5.0.0` | Low | Set `UnresolvedContentHandling.Hide` for lazy plugins |
| **Behavior** | `ToolTip` excluded from serialized layout files | `Upstream v5.0.0` | Low | Set tooltips in views, styles, or view models |
