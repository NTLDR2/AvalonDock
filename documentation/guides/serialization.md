---
title: Layout Serialization
layout: default
parent: Guides
nav_order: 4
description: "Save and restore AvalonDock v5 docking layouts using XML or custom DTO serializers."
---

# Layout Serialization
{: .no_toc }

AvalonDock v5 persists and restores complete docking layouts—including split orientations, proportional and pixel pane sizes, tab order, auto-hide groups, floating windows, standalone detached windows, and `ToggleDockingManager` sidebar zones—through a UI-agnostic DTO pipeline with zero third-party serializer dependencies.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Install the XML Serializer

`Dirkster.AvalonDock.Serializer.Xml` targets `netstandard2.0`, depends only on `Dirkster.AvalonDock.Core` (`System.Xml.Serialization` is inbox on both `.NET Framework 4.8` and modern `.NET`), and implements `AvalonDock.Core.ILayoutSerializer` via `LayoutSerializerBase`:

| Format | NuGet Package | Class & Namespace | Best For |
|:-------|:--------------|:------------------|:---------|
| **XML** | `Dirkster.AvalonDock.Serializer.Xml` | `AvalonDock.Serializer.Xml.XmlLayoutSerializer` | Human-readable configs, zero external dependencies, migrating legacy AvalonDock v4 XML layouts |
| **Custom** | `Dirkster.AvalonDock.Core` | Subclass `AvalonDock.Core.LayoutSerializerBase` | Custom binary, YAML, SQLite, or app-specific DTO persistence |

```bash
dotnet add package Dirkster.AvalonDock.Serializer.Xml
```

{: .note }
**Fork Scope (`NTLDR2 Fork v5.1.0` vs. Upstream `v5.0.0`):** Upstream `Dirkster99/AvalonDock v5.0.0` also introduced `Dirkster.AvalonDock.Serializer.Json`. In **`NTLDR2/AvalonDock` (`v5.1.0` fork)**, `Serializer.Json` is intentionally omitted to guarantee zero `System.Text.Json` assembly conflicts on `.NET Framework 4.8`. Applications requiring custom wire formats (JSON, binary, YAML, SQLite) can subclass `AvalonDock.Core.LayoutSerializerBase` directly against `LayoutRootDto`.

---

## Serialize and Deserialize Overloads

`ILayoutSerializer`, `LayoutSerializerBase`, and `LayoutSerializerExtensions` provide overloads for file paths, streams, and text readers/writers:

| Save Overload | Load Overload | Defined On | Typical Use Case |
|:--------------|:--------------|:-----------|:-----------------|
| `Serialize(string filepath)` | `Deserialize(string filepath)` | `ILayoutSerializer`, `LayoutSerializerBase` | Direct file save/load without boilerplate stream code |
| `Serialize(Stream stream)` | `Deserialize(Stream stream)` | `ILayoutSerializer`, `LayoutSerializerBase` | `MemoryStream` snapshots, embedded resources, database blobs |
| `Serialize(TextWriter writer)` | `Deserialize(TextReader reader)` | `LayoutSerializerBase`, `LayoutSerializerExtensions` | `StringWriter` / `StringReader` in-memory text inspection |
| `Serialize(StreamWriter writer)` | `Deserialize(StreamReader reader)` | `LayoutSerializerBase` | Custom `StreamWriter` / `StreamReader` encodings |

### Save a Layout

Because `Serialize(string filepath)` creates and disposes the file stream automatically, saving a layout takes a single statement once the target directory exists:

```csharp
using System.IO;
using AvalonDock.Serializer.Xml;

// Ensure parent directory exists when writing under %AppData%
if (Path.GetDirectoryName(layoutPath) is { Length: > 0 } dir)
{
    Directory.CreateDirectory(dir);
}

// XML — direct file path overload
new XmlLayoutSerializer(dockManager).Serialize("layout.xml");
```

To serialize into memory (for example, to store a layout in user settings or capture a reset snapshot), pass a `Stream` or `StringWriter`:

```csharp
using var buffer = new MemoryStream();
new XmlLayoutSerializer(dockManager).Serialize(buffer);
```

### Restore a Layout

Attach a `LayoutSerializationCallback` handler to supply UI controls or ViewModels for each stored `ContentId`, then call `Deserialize`:

```csharp
using System.IO;
using AvalonDock.Serializer.Xml;

if (File.Exists("layout.xml"))
{
    var serializer = new XmlLayoutSerializer(dockManager);

    serializer.LayoutSerializationCallback += (_, args) =>
    {
        args.Content = args.Model.ContentId switch
        {
            "explorer"   => new ExplorerControl(),
            "properties" => new PropertiesControl(),
            "output"     => new OutputControl(),
            _            => null // Unresolved items are dropped by default
        };
    };

    serializer.Deserialize("layout.xml");
}
```

---

## Legacy v4 XML Compatibility

`XmlLayoutSerializer` and `LayoutDtoMapper` include built-in compatibility handling so XML layout files saved by AvalonDock v4.x load cleanly in v5 without manual migration scripts:

1. **Encoding & BOM Mismatch Tolerance:** `XmlLayoutSerializer.DeserializeCore` decodes incoming streams via `StreamReader(..., Encoding.UTF8, detectEncodingFromByteOrderMarks: true)` before passing the decoded string to `XmlSerializer`. Legacy layouts saved with a mismatched XML prolog (for example, `encoding="utf-16"` written onto UTF-8 bytes) load cleanly instead of failing with `"There is no Unicode byte order mark"`.
2. **Capitalized Boolean Normalization:** AvalonDock v4 wrote capitalized `"True"` and `"False"` attribute values, which `XmlSerializer` rejects because `xsd:boolean` requires lowercase values. On load, `XmlLayoutSerializer` normalizes `"True"`/`"False"` to `"true"`/`"false"` across the 12 v4 boolean layout attributes (`IsSelected`, `IsLastFocusedDocument`, `IsMaximized`, `CanClose`, `CanFloat`, `CanShowOnHover`, `CanHide`, `CanAutoHide`, `CanDockAsTabbedDocument`, `CanDock`, `CanMove`, and `ShowHeader`) without modifying user string attributes like `Title` or `ContentId`.
3. **Fault-Tolerant Attribute Parsing:** `LayoutDtoMapper` falls back to `Orientation.Horizontal` for unrecognized orientation strings, ignores malformed `GridLength` values instead of aborting layout restoration, and parses `LastActivationTimeStamp` across both `InvariantCulture` and `CurrentCulture` (dropping unreadable timestamps rather than losing the user's layout).

{: .note }
When saving in v5, `XmlLayoutSerializer` emits clean, namespace-free XML (`<LayoutRoot>`) with standard lowercase `xsd:boolean` values and `ShouldSerialize*()` guards that omit default-valued attributes.

---

## `LayoutSerializationCallback` & Unresolved Content

During `Deserialize`, AvalonDock reconstructs the layout tree from DTOs and invokes `LayoutSerializationCallback` for each deserialized `LayoutAnchorable` and `LayoutDocument` whose `Content` is `null`.

### `LayoutSerializationCallbackEventArgs`

| Property | Type | Description |
|:---------|:-----|:------------|
| `Model` | `ISerializableLayoutContent` | The layout element being restored (`ISerializableLayoutAnchorable` or `ISerializableLayoutDocument`). Match items via `Model.ContentId`. |
| `Content` | `object` | Pre-populated with the matching item's `Content` from the layout prior to `Deserialize` (if one shared the same `ContentId`); set this to your ViewModel or UI control. |
| `Cancel` | `bool` | Set to `true` to discard this item immediately (`RemoveFromLayout()` for anchorables, `Close()` for documents). |
| `UnresolvedContentHandling` | `UnresolvedContentHandling` | Initialized from `serializer.UnresolvedContentHandling`; override per item when `Content` remains `null` and `Cancel` is `false`. |

### `UnresolvedContentHandling` (`Remove` vs `Hide`)

When an application update removes a tool window or a plugin has not loaded yet, a saved layout may reference a `ContentId` that cannot be resolved at startup. `LayoutSerializerBase.UnresolvedContentHandling` controls what happens when `args.Content` and `args.Model.Content` remain `null`:

| Mode | Behavior for Anchorables | Behavior for Documents |
|:-----|:-------------------------|:-----------------------|
| `UnresolvedContentHandling.Remove` *(default)* | Calls `RemoveFromLayout()`, cleanly detaching the item with no empty pane, no ghost entry in `LayoutRoot.Hidden`, and without disturbing sibling auto-hidden panels. | Calls `Close()`, removing the document tab. |
| `UnresolvedContentHandling.Hide` | Calls `HideAnchorable(false)`, parking the item in `LayoutRoot.Hidden` alongside its `PreviousContainerId` and `PreviousContainerIndex` (v4-compatible behavior). | Always removed (`Close()`); documents have no hidden state. |

You can keep `Remove` as the serializer-wide default and opt into `Hide` selectively for deferred plugin panels:

```csharp
serializer.UnresolvedContentHandling = UnresolvedContentHandling.Remove;

serializer.LayoutSerializationCallback += (_, args) =>
{
    if (TryResolveBuiltInViewModel(args.Model.ContentId, out var vm))
    {
        args.Content = vm;
    }
    else if (args.Model.ContentId?.StartsWith("plugin:", StringComparison.Ordinal) == true)
    {
        // Keep plugin tool windows parked until the plugin assembly loads
        args.UnresolvedContentHandling = UnresolvedContentHandling.Hide;
    }
};
```

Once the plugin loads later in the session, attach its content and call `Show()` on the parked `LayoutAnchorable`:

```csharp
var parked = dockManager.Layout.Hidden.FirstOrDefault(a => a.ContentId == "plugin:profiler");
if (parked != null)
{
    parked.Content = profilerPlugin.CreateToolViewModel();
    parked.Show();
}
```

---

## MVVM Serialization

{: .important }
**`Id` vs `ContentId`:** Layout elements (`LayoutContent` / `ISerializableLayoutContent`) persist their identifier in **`ContentId`**, whereas AvalonDock v5 MVVM contracts (`IDockable`, `DockableBase`, `ToolboxBase`, `ObservableDockableBase`, `ObservableToolboxBase`) expose **`Id`**. Always compare **`vm.Id == args.Model.ContentId`** in `LayoutSerializationCallback`.

### Modern v5 MVVM: `ToggleDockingManager` + `IDockLayoutService` (`DockLayout`)

When binding `DockLayout="{Binding LayoutService.Layout}"` on `ToggleDockingManager` (or `DockingManager`), `LayoutSyncBridge` automatically maps `IDockable.Id` to `LayoutContent.ContentId`.

To restore a saved layout on `ToggleDockingManager`, resolve registered `IToolbox` instances from `IDockLayoutService.Anchorables` by matching `t.Id == args.Model.ContentId`:

```csharp
using System.IO;
using System.Linq;
using AvalonDock.Core;
using AvalonDock.Serializer.Xml;

private void LoadLayout(string path, IDockLayoutService layoutService)
{
    if (!File.Exists(path))
    {
        return;
    }

    var serializer = new XmlLayoutSerializer(dockManager);
    serializer.LayoutSerializationCallback += (_, args) =>
    {
        var toolbox = layoutService.Anchorables
            .OfType<IToolbox>()
            .FirstOrDefault(t => t.Id == args.Model.ContentId);

        if (toolbox != null)
        {
            args.Content = toolbox;
            return;
        }

        var document = layoutService.Documents
            .FirstOrDefault(d => d.Id == args.Model.ContentId);

        if (document != null)
        {
            args.Content = document;
        }
    };

    serializer.Deserialize(path);
}
```

When `serializer.Deserialize(path)` assigns the restored `LayoutRoot` to `ToggleDockingManager.Layout`, `ToggleDockingManager.OnLayoutChanged` automatically:
1. Clears stale detached-zone bookkeeping and releases `INotifyPropertyChanged` and shortcut `KeyBinding` subscriptions from the previous layout tree.
2. Rebuilds all 6 `ToggleDockButtonBar` side rails from the restored layout—eliminating ghost buttons for any removed toolboxes.
3. Re-opens every `LayoutAnchorable` that was docked in the saved layout (deliberately honoring the user's saved open/closed state rather than re-applying `IToolbox.IsOpenByDefault`).

See the [ToggleDockingManager Guide]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) and [Tutorial: Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}) for complete end-to-end examples.

### Classic MVVM: `DocumentsSource` + `AnchorablesSource`

When binding `DocumentsSource` and `AnchorablesSource` (without `DockLayout`), `LayoutSerializerBase` automatically sets `SuspendDocumentsSourceBinding = true` and `SuspendAnchorablesSourceBinding = true` inside a `try`/`finally` block while deserializing.

Make sure your `LayoutItemContainerStyleSelector` binds `ContentId` to `Model.Id` when using `DockableBase` / `ObservableDockableBase`:

```xml
<Style x:Key="ToolItemStyle" TargetType="{x:Type avalonDockControls:LayoutAnchorableItem}">
    <Setter Property="Title" Value="{Binding Model.Title}" />
    <Setter Property="ContentId" Value="{Binding Model.Id}" />
    <Setter Property="CanClose" Value="{Binding Model.CanClose}" />
</Style>
```

Then return the matching ViewModels in `LayoutSerializationCallback`:

```csharp
serializer.LayoutSerializationCallback += (_, args) =>
{
    var vm = viewModels.FirstOrDefault(v => v.Id == args.Model.ContentId);
    if (vm != null)
    {
        args.Content = vm;
    }
    else
    {
        args.Cancel = true;
    }
};
```

See the [MVVM Guide]({{ site.baseurl }}{% link guides/mvvm.md %}) for full `PanesStyleSelector` and `DataTemplate` setup.

---

## What Gets Serialized

### Preserved Layout State

- **Panel hierarchy & splits:** `LayoutPanel` orientations (`Horizontal` / `Vertical`), `CanDock`, nested pane groups, and tab order within panes.
- **Dimensions:** `DockWidth`, `DockHeight`, `DockMinWidth`, `DockMinHeight`, `AutoHideWidth`, `AutoHideHeight`, `AutoHideMinWidth`, and `AutoHideMinHeight`.
- **Auto-hide state:** Side placement (`TopSide`, `RightSide`, `LeftSide`, `BottomSide`) and `LayoutAnchorGroup.PreviousContainerId` so unpinning/re-pinning remembers its target pane across restarts.
- **Floating windows:** `LayoutDocumentFloatingWindow` and `LayoutAnchorableFloatingWindow` trees, `FloatingLeft`, `FloatingTop`, `FloatingWidth`, `FloatingHeight`, and `IsMaximized`. If `DockingManager.AllowFloatingWindows` is `false` when loading, floating windows are automatically docked back into the main layout via `DockAllFloatingWindows()`.
- **Standalone detached windows:** `LayoutAnchorable.IsDetached` (`LayoutAnchorableDto.IsDetached`) along with saved window bounds and `PreviousContainerId` / `PreviousContainerIndex`. When `DockingManager.AllowDetachedWindows` is `true`, detached anchorables are automatically re-opened in standalone `DetachedAnchorableWindow` instances after load (centering onto an active screen if a previous monitor was disconnected); when `AllowDetachedWindows` is `false`, `IsDetached` is cleared and they remain in the layout.
- **Selection & metadata:** `ContentId`, `Title`, `Description` (documents), `IsSelected`, `IsLastFocusedDocument`, `LastActivationTimeStamp`, `CanClose`, `CanFloat`, `CanHide`, `CanAutoHide`, `CanDockAsTabbedDocument`, `CanMove`, `CanShowOnHover`, and `ShowHeader`.

### Not Preserved (Application State)

- **Live UI controls & ViewModels:** Reconnected via `LayoutSerializationCallback`.
- **Internal ViewModel state:** Unsaved editor text, scroll offsets, filter strings, or undo history must be persisted by your application state layer.
- **`IconSource` & `ToolTip`:** Supplied by your ViewModel bindings, `LayoutItemContainerStyleSelector`, or `DataTemplate` when the view attaches.
- **Runtime event handlers:** Subscriptions on custom controls must be wired when controls are created.

---

## Architecture: DTO-Based Serialization

AvalonDock v5 decouples the WPF `LayoutRoot` tree from wire formats through plain C# Data Transfer Objects in `AvalonDock.Core.Serialization.Dto`:

```text
Save:  LayoutRoot ──► LayoutDtoMapper.ToDto() ──► LayoutRootDto ──► Serializer.SerializeCore() ──► Stream / File
Load:  Stream / File ──► Serializer.DeserializeCore() ──► LayoutRootDto ──► LayoutDtoMapper.FromDto() ──► FixupLayout() ──► LayoutRoot
```

The mapping contract lives in `AvalonDock.Core.Serialization`:

```csharp
public interface ILayoutDtoMapper
{
    LayoutRootDto ToDto(ISerializableLayoutRoot layout);
    ISerializableLayoutRoot FromDto(LayoutRootDto dto);
}
```

### Complete DTO Class Hierarchy (`AvalonDock.Core.Serialization.Dto`)

All 17 DTO classes live in `AvalonDock.Core.Serialization.Dto` (`Dirkster.AvalonDock.Core`):

| DTO Class | Kind | Base Class | XML Element | Maps To / Purpose |
|:----------|:-----|:-----------|:------------|:------------------|
| `LayoutElementDto` | `abstract` | `object` | — | Root base class for all serialization DTOs |
| `LayoutGroupDto` | `abstract` | `LayoutElementDto` | — | Base class for layout container groups |
| `LayoutPositionableGroupDto` | `abstract` | `LayoutGroupDto` | *(polymorphic base)* | Base carrying `DockWidth`, `DockHeight`, `DockMin*`, `Floating*`, and `IsMaximized` |
| `LayoutContentDto` | `abstract` | `LayoutElementDto` | *(polymorphic base)* | Base carrying `ContentId`, `Title`, `IsSelected`, `Floating*`, `Can*`, and `PreviousContainer*` |
| `LayoutFloatingWindowDto` | `abstract` | `LayoutElementDto` | *(polymorphic base)* | Base class for document and anchorable floating window DTOs |
| `LayoutRootDto` | `class` | `LayoutElementDto` | `<LayoutRoot>` | Root container (`RootPanel`, `TopSide`, `RightSide`, `LeftSide`, `BottomSide`, `FloatingWindows`, `Hidden`) |
| `LayoutPanelDto` | `class` | `LayoutPositionableGroupDto` | `<LayoutPanel>` | Split container (`Orientation`, `CanDock`, `Children`) |
| `LayoutDocumentPaneGroupDto` | `class` | `LayoutPositionableGroupDto` | `<LayoutDocumentPaneGroup>` | Splitter group of document panes (`Orientation`, `Children`) |
| `LayoutDocumentPaneDto` | `class` | `LayoutPositionableGroupDto` | `<LayoutDocumentPane>` | Tabbed document pane (`Id`, `ShowHeader`, `Children` of `LayoutContentDto`) |
| `LayoutAnchorablePaneGroupDto` | `class` | `LayoutPositionableGroupDto` | `<LayoutAnchorablePaneGroup>` | Splitter group of anchorable panes (`Orientation`, `Children`) |
| `LayoutAnchorablePaneDto` | `class` | `LayoutPositionableGroupDto` | `<LayoutAnchorablePane>` | Tool window pane (`Id`, `Name`, `Children` of `LayoutAnchorableDto`) |
| `LayoutDocumentDto` | `class` | `LayoutContentDto` | `<LayoutDocument>` | Individual document (`Description`, `CanMove`; `CanClose` defaults to `true`) |
| `LayoutAnchorableDto` | `class` | `LayoutContentDto` | `<LayoutAnchorable>` | Individual tool window (`CanHide`, `CanAutoHide`, `AutoHide*`, `CanDockAsTabbedDocument`, `CanMove`, `IsDetached`; `CanClose` defaults to `false`) |
| `LayoutDocumentFloatingWindowDto` | `class` | `LayoutFloatingWindowDto` | `<LayoutDocumentFloatingWindow>` | Floating window hosting a `LayoutDocumentPaneGroupDto` (`RootPanel`) |
| `LayoutAnchorableFloatingWindowDto` | `class` | `LayoutFloatingWindowDto` | `<LayoutAnchorableFloatingWindow>` | Floating window hosting a `LayoutAnchorablePaneGroupDto` (`RootPanel`) |
| `LayoutAnchorSideDto` | `class` | `LayoutGroupDto` | `<TopSide>` / `<RightSide>` / `<LeftSide>` / `<BottomSide>` | Auto-hide border side containing `LayoutAnchorGroupDto` children |
| `LayoutAnchorGroupDto` | `class` | `LayoutGroupDto` | `<LayoutAnchorGroup>` | Auto-hide tab group (`Id`, `PreviousContainerId`, `Children` of `LayoutAnchorableDto`) |

### Implementing a Custom Serializer

To support another format (such as YAML, MessagePack, or Protobuf), subclass `LayoutSerializerBase` and override `SerializeCore` and `DeserializeCore`:

```csharp
using System.IO;
using AvalonDock.Core;
using AvalonDock.Core.Serialization.Dto;

public sealed class YamlLayoutSerializer : LayoutSerializerBase
{
    public YamlLayoutSerializer(IDockingManager manager)
        : base(manager)
    {
    }

    protected override void SerializeCore(Stream stream, LayoutRootDto dto)
    {
        // Write LayoutRootDto to stream in your target format
    }

    protected override LayoutRootDto DeserializeCore(Stream stream)
    {
        // Parse LayoutRootDto from stream
        return new LayoutRootDto();
    }
}
```

`LayoutSerializerBase` automatically handles suspending `DocumentsSource`/`AnchorablesSource` bindings, capturing previous content, running `LayoutDtoMapper`, relinking `PreviousContainer` references, invoking `LayoutSerializationCallback`, applying `UnresolvedContentHandling`, and calling `CollectGarbage()`.

---

## Auto-Save on Exit & Restore on Startup

Save the layout in `OnClosing` and restore it once the window is loaded (or in `OnContentRendered`) using defensive error handling:

```csharp
using System;
using System.ComponentModel;
using System.IO;
using AvalonDock.Serializer.Xml;

private static string LayoutFilePath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp",
    "layout.xml");

protected override void OnClosing(CancelEventArgs e)
{
    if (Path.GetDirectoryName(LayoutFilePath) is { Length: > 0 } dir)
    {
        Directory.CreateDirectory(dir);
    }

    new XmlLayoutSerializer(dockManager).Serialize(LayoutFilePath);
    base.OnClosing(e);
}

protected override void OnContentRendered(EventArgs e)
{
    base.OnContentRendered(e);

    if (!File.Exists(LayoutFilePath))
    {
        return;
    }

    try
    {
        var serializer = new XmlLayoutSerializer(dockManager);
        serializer.LayoutSerializationCallback += OnLayoutDeserialization;
        serializer.Deserialize(LayoutFilePath);
    }
    catch (Exception)
    {
        // Corrupt or incompatible layout file — fall back to default layout
        try { File.Delete(LayoutFilePath); } catch { /* ignore cleanup failure */ }
    }
}
```

---

## Related Documentation

- [Tutorial: Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}) — Step-by-step walkthrough covering code-behind, `ToggleDockingManager` + `IDockLayoutService`, classic MVVM, and reset workflows.
- [Core & MVVM API Reference]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}) — Full member signatures for `ILayoutSerializer`, `LayoutSerializerBase`, `LayoutSerializationCallbackEventArgs`, and `UnresolvedContentHandling`.
- [MVVM Guide]({{ site.baseurl }}{% link guides/mvvm.md %}) — `IDockable`, `DockLayout`, and `PanesStyleSelector` binding conventions.
- [Breaking Changes (v4 → v5)]({{ site.baseurl }}{% link migration/breaking-changes.md %}) — Namespace and package updates when migrating from AvalonDock v4 `XmlLayoutSerializer`.
