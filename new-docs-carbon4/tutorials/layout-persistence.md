---
title: "Tutorial: Layout Persistence"
layout: default
parent: Tutorials
nav_order: 4
description: "Save and restore AvalonDock v5 layouts with XmlLayoutSerializer across code-behind, ToggleDockingManager + IDockLayoutService, and classic MVVM."
---

# Tutorial: Layout Persistence
{: .no_toc }

In this tutorial you will add reliable XML layout persistence to an AvalonDock v5 application so panel sizes, splits, floating windows, standalone detached windows, and sidebar toggle states are saved on exit and restored on startup.
{: .fs-6 .fw-300 }

{: .tip }
The patterns in this tutorial are modeled directly on the `AvalonDockCodeApp` (`ToggleDockingManager` + `IDockLayoutService`), `MVVMTestApp` (`DocumentsSource` + `AnchorablesSource`), and `TestApp` sample projects in `source/`.

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## What You'll Build

- **Auto-save on exit** and **auto-restore on startup** using direct file-path and stream overloads
- **Part 1:** Basic code-behind persistence for XAML-defined `DockingManager` layouts
- **Part 2A:** Modern v5 MVVM persistence for `ToggleDockingManager` + `IDockLayoutService` (continuing directly from [Tutorial: Build an MVVM IDE]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}))
- **Part 2B:** Classic MVVM persistence for `DockingManager` with `DocumentsSource` and `AnchorablesSource`
- **Part 3–5:** Detached/floating window behavior, runtime **Reset Layout**, and defensive error recovery

---

## Prerequisites

Install `Dirkster.AvalonDock` plus `Dirkster.AvalonDock.Serializer.Xml`:

```bash
dotnet add package Dirkster.AvalonDock
dotnet add package Dirkster.AvalonDock.Serializer.Xml
```

---

## Part 1: Basic Layout Persistence (Code-Behind)

When your panels are declared directly in XAML without MVVM, `LayoutSerializationCallback` supplies the UI controls for each `ContentId`.

### Step 1: Define a Nullable-Safe Layout Path Helper

```csharp
using System;
using System.IO;

private static string LayoutFilePath => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "MyApp",
    "layout.xml");

private static void EnsureParentDirectory(string filePath)
{
    if (Path.GetDirectoryName(filePath) is { Length: > 0 } directory)
    {
        Directory.CreateDirectory(directory);
    }
}
```

### Step 2: Save on Window Close

`XmlLayoutSerializer` exposes `Serialize(string filepath)` directly via `ILayoutSerializer` / `LayoutSerializerBase`, as well as `Serialize(Stream)` when writing to memory or custom streams:

```csharp
using System.ComponentModel;
using AvalonDock.Serializer.Xml;

protected override void OnClosing(CancelEventArgs e)
{
    EnsureParentDirectory(LayoutFilePath);

    // Direct file-path overload (opens and disposes File.Create internally)
    new XmlLayoutSerializer(DockManager).Serialize(LayoutFilePath);

    base.OnClosing(e);
}
```

If you need explicit `FileStream` control (for example, `FileShare.None` or async staging via a `MemoryStream`), use the `Stream` overload:

```csharp
using var stream = File.Create(LayoutFilePath);
new XmlLayoutSerializer(DockManager).Serialize(stream);
```

### Step 3: Restore on `Loaded`

```csharp
using System.Windows;
using AvalonDock.Core;
using AvalonDock.Serializer.Xml;

private void DockManager_Loaded(object sender, RoutedEventArgs e)
{
    if (!File.Exists(LayoutFilePath))
    {
        return;
    }

    var serializer = new XmlLayoutSerializer(DockManager);

    serializer.LayoutSerializationCallback += (_, args) =>
    {
        args.Content = args.Model.ContentId switch
        {
            "explorer"   => FindOrCreateControl("explorer"),
            "properties" => FindOrCreateControl("properties"),
            "output"     => FindOrCreateControl("output"),
            _            => null // Unresolved items are dropped cleanly
        };
    };

    serializer.Deserialize(LayoutFilePath);
}
```

Wire `Loaded` on your `DockingManager` in XAML:

```xml
<avalonDock:DockingManager x:Name="DockManager" Loaded="DockManager_Loaded">
    <!-- Default initial layout -->
</avalonDock:DockingManager>
```

{: .important }
By default, `serializer.UnresolvedContentHandling` is `UnresolvedContentHandling.Remove`: any item whose `args.Content` remains `null` is removed from the layout without leaving empty ghost panes. If your app loads tool windows from optional plugins after startup, set `args.UnresolvedContentHandling = UnresolvedContentHandling.Hide` for plugin `ContentId`s so they remain parked in `DockManager.Layout.Hidden` with their saved pane and tab index until `Show()` is called.

---

## Part 2A: Modern v5 MVVM (`ToggleDockingManager` + `IDockLayoutService`)

If you built an application with `ToggleDockingManager`, `AvalonDock.Mvvm` (`ObservableToolboxBase` / `ObservableDockableBase`), and `AvalonDock.DependencyInjection` (`IDockLayoutService`) as shown in [Tutorial: Build an MVVM IDE]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}), layout persistence connects directly to `IDockLayoutService`.

### Step 1: Verify `Id` Mapping

`IDockable` and `IToolbox` ViewModels identify themselves via **`Id`** (for example, `Id = "SolutionExplorer"`), while serialized layout elements store that identifier in **`ContentId`**:

- When `DockLayout="{Binding LayoutService.Layout}"` is bound on `ToggleDockingManager`, `LayoutSyncBridge` automatically copies `IDockable.Id` to `LayoutContent.ContentId`.
- If you also supply a custom `PanesStyleSelector` (`LayoutItemContainerStyleSelector`), ensure it binds `ContentId` to `Model.Id` (not `Model.ContentId`):

```xml
<Style x:Key="ToolStyle" TargetType="{x:Type avalonDockControls:LayoutAnchorableItem}">
    <Setter Property="Title" Value="{Binding Model.Title}" />
    <Setter Property="ContentId" Value="{Binding Model.Id}" />
    <Setter Property="CanClose" Value="{Binding Model.CanClose}" />
    <Setter Property="CanFloat" Value="{Binding Model.CanFloat}" />
</Style>

<Style x:Key="DocumentStyle" TargetType="{x:Type avalonDockControls:LayoutDocumentItem}">
    <Setter Property="Title" Value="{Binding Model.Title}" />
    <Setter Property="ContentId" Value="{Binding Model.Id}" />
    <Setter Property="CanClose" Value="{Binding Model.CanClose}" />
</Style>
```

### Step 2: Save and Restore `ToggleDockingManager` in `MainWindow.xaml.cs`

Modeled on `AvalonDockCodeApp/MainWindow.xaml.cs`, `LoadLayout` matches each deserialized tool window against `LayoutService.Anchorables.OfType<IToolbox>()` using `t.Id == args.Model.ContentId`, and matches (or lazily reopens) documents by `d.Id == args.Model.ContentId`:

**File: `MainWindow.xaml.cs`**

```csharp
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using AvalonDock.Core;
using AvalonDock.DependencyInjection;
using AvalonDock.Serializer.Xml;
using MvvmIdeApp.ViewModels;

namespace MvvmIdeApp;

public partial class MainWindow : Window
{
    private MemoryStream? _defaultLayoutSnapshot;

    private static string LayoutFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MvvmIdeApp",
        "ToggleDockLayout.xml");

    public MainWindow(MainViewModel viewModel, ToggleDockOptions? dockOptions = null)
    {
        DataContext = viewModel;
        InitializeComponent();

        Loaded += OnMainWindowLoaded;
        Closing += OnMainWindowClosing;
    }

    private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Capture the fresh default layout in memory so "Reset Layout" works instantly at runtime
        _defaultLayoutSnapshot = new MemoryStream();
        new XmlLayoutSerializer(dockManager).Serialize(_defaultLayoutSnapshot);

        // Auto-restore the user's saved layout if present
        LoadLayoutFromFile(LayoutFilePath);
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        SaveLayoutToFile(LayoutFilePath);
        _defaultLayoutSnapshot?.Dispose();
    }

    private void OnSaveLayout(object sender, RoutedEventArgs e) =>
        SaveLayoutToFile(LayoutFilePath);

    private void OnLoadLayout(object sender, RoutedEventArgs e) =>
        LoadLayoutFromFile(LayoutFilePath);

    private void OnResetLayout(object sender, RoutedEventArgs e)
    {
        if (_defaultLayoutSnapshot == null || DataContext is not MainViewModel vm)
        {
            return;
        }

        _defaultLayoutSnapshot.Position = 0;
        var serializer = CreateConfiguredSerializer(vm);
        serializer.Deserialize(_defaultLayoutSnapshot);

        if (File.Exists(LayoutFilePath))
        {
            File.Delete(LayoutFilePath);
        }
    }

    private void SaveLayoutToFile(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
        }

        new XmlLayoutSerializer(dockManager).Serialize(path);
    }

    private void LoadLayoutFromFile(string path)
    {
        if (!File.Exists(path) || DataContext is not MainViewModel vm)
        {
            return;
        }

        try
        {
            var serializer = CreateConfiguredSerializer(vm);
            serializer.Deserialize(path);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to restore layout: {ex.Message}");
            try { File.Delete(path); } catch { /* ignore cleanup failure */ }
        }
    }

    private XmlLayoutSerializer CreateConfiguredSerializer(MainViewModel vm)
    {
        var serializer = new XmlLayoutSerializer(dockManager);

        serializer.LayoutSerializationCallback += (_, args) =>
        {
            var contentId = args.Model.ContentId;
            if (string.IsNullOrEmpty(contentId))
            {
                return;
            }

            // 1. Reconnect registered IToolbox panels by matching t.Id == args.Model.ContentId
            var toolbox = vm.LayoutService.Anchorables
                .OfType<IToolbox>()
                .FirstOrDefault(t => t.Id == contentId);

            if (toolbox != null)
            {
                args.Content = toolbox;
                return;
            }

            // 2. Reconnect already-open documents in IDockLayoutService
            var existingDoc = vm.LayoutService.Documents
                .FirstOrDefault(d => d.Id == contentId);

            if (existingDoc != null)
            {
                args.Content = existingDoc;
                return;
            }

            // 3. Optional: reopen file-backed documents whose ContentId is a file path
            if (File.Exists(contentId))
            {
                var reopened = new EditorDocumentViewModel(
                    id: contentId,
                    title: Path.GetFileName(contentId),
                    text: File.ReadAllText(contentId));

                vm.LayoutService.OpenDocument(reopened);
                args.Content = reopened;
            }
            // Any unknown ContentId leaves args.Content == null and is cleanly dropped.
        };

        return serializer;
    }
}
```

### What `ToggleDockingManager` Does Automatically on Restore

When `serializer.Deserialize(...)` replaces `dockManager.Layout`:
1. Any stored tool window whose `ContentId` is no longer registered in `LayoutService.Anchorables` is discarded by `LayoutSerializerBase` (`UnresolvedContentHandling.Remove`) before the layout attaches.
2. `ToggleDockingManager.OnLayoutChanged` rebuilds all 6 `ToggleDockButtonBar` side rails from the restored layout and refreshes shortcut `KeyBinding` registrations.
3. Every toolbox that was docked open when the user saved the layout is automatically re-opened in its saved zone, while minimized toolboxes stay collapsed on their side rail.

---

## Part 2B: Classic MVVM (`DocumentsSource` + `AnchorablesSource`)

If your application uses classic `DockingManager` collection bindings (`DocumentsSource` and `AnchorablesSource`) with `AvalonDock.Mvvm` base classes (`ObservableDockableBase` or `DockableBase`), keep the identifier property consistent across your ViewModels, XAML styles, and serialization callback.

### Step 1: Define ViewModels with Stable `Id` Values

**File: `ViewModels/ToolViewModel.cs` & `ViewModels/DocumentViewModel.cs`**

```csharp
using AvalonDock.Mvvm.CommunityToolkit;

namespace ClassicMvvmApp.ViewModels;

public partial class ToolViewModel : ObservableDockableBase
{
    public ToolViewModel(string id, string title)
    {
        Id = id;       // Stable tool identifier, e.g. "explorer", "output"
        Title = title;
        CanClose = false;
    }
}

public partial class DocumentViewModel : ObservableDockableBase
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private string _text = string.Empty;

    public DocumentViewModel(string id, string title, string text = "")
    {
        Id = id;       // Stable document identifier, e.g. file path or document key
        Title = title;
        _text = text;
    }
}
```

{: .note }
If you author custom ViewModels from scratch without `AvalonDock.Mvvm` (such as `PaneViewModel` in `source/MVVMTestApp`) and name your property `ContentId` instead of `Id`, bind `ContentId="{Binding Model.ContentId}"` in XAML and compare `t.ContentId == contentId` in `LayoutSerializationCallback`.

### Step 2: Add Layout Hooks to `WorkspaceViewModel`

**File: `ViewModels/WorkspaceViewModel.cs`**

```csharp
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ClassicMvvmApp.ViewModels;

public class WorkspaceViewModel
{
    public static string LayoutFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ClassicMvvmApp",
        "layout.xml");

    public ObservableCollection<ToolViewModel> Tools { get; } =
    [
        new ToolViewModel("explorer", "Solution Explorer"),
        new ToolViewModel("properties", "Properties"),
        new ToolViewModel("output", "Output")
    ];

    public ObservableCollection<DocumentViewModel> Documents { get; } =
    [
        new DocumentViewModel("welcome", "Welcome.md", "# Welcome")
    ];

    public Action<string>? SaveLayoutRequested { get; set; }
    public Action<string>? RestoreLayoutRequested { get; set; }

    public ICommand SaveLayoutCommand => new RelayCommand(() => SaveLayoutRequested?.Invoke(LayoutFilePath));
    public ICommand RestoreLayoutCommand => new RelayCommand(() => RestoreLayoutRequested?.Invoke(LayoutFilePath));
}
```

### Step 3: Bind `ContentId="{Binding Model.Id}"` and Restore in `MainWindow.xaml.cs`

Ensure your `LayoutItemContainerStyleSelector` (`PanesStyleSelector`) maps `Model.Id` onto `ContentId` for both `LayoutAnchorableItem` and `LayoutDocumentItem`, then implement save/restore in `MainWindow.xaml.cs`:

```csharp
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using AvalonDock.Serializer.Xml;
using ClassicMvvmApp.ViewModels;

namespace ClassicMvvmApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnWindowClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not WorkspaceViewModel vm)
        {
            return;
        }

        vm.SaveLayoutRequested = SaveLayout;
        vm.RestoreLayoutRequested = path => RestoreLayout(path, vm);

        RestoreLayout(WorkspaceViewModel.LayoutFilePath, vm);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        SaveLayout(WorkspaceViewModel.LayoutFilePath);
    }

    private void SaveLayout(string path)
    {
        if (Path.GetDirectoryName(path) is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
        }

        new XmlLayoutSerializer(DockManager).Serialize(path);
    }

    private void RestoreLayout(string path, WorkspaceViewModel vm)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var serializer = new XmlLayoutSerializer(DockManager);

        serializer.LayoutSerializationCallback += (_, args) =>
        {
            var contentId = args.Model.ContentId;

            // Match ToolViewModel.Id against serialized ContentId
            var tool = vm.Tools.FirstOrDefault(t => t.Id == contentId);
            if (tool != null)
            {
                args.Content = tool;
                return;
            }

            // Match DocumentViewModel.Id against serialized ContentId
            var doc = vm.Documents.FirstOrDefault(d => d.Id == contentId);
            if (doc != null)
            {
                args.Content = doc;
                return;
            }

            args.Cancel = true;
        };

        serializer.Deserialize(path);
    }
}
```

---

## Part 3: What Gets Serialized (Including Floating & Detached Windows)

### Serialized Layout State

| Layout Feature | Persisted Details & Restore Behavior |
|:---------------|:-------------------------------------|
| **Docked panels & splits** | `LayoutPanel` orientations (`Horizontal` / `Vertical`), pane order, `DockWidth`, `DockHeight`, `DockMinWidth`, `DockMinHeight` |
| **Auto-hide rails** | `TopSide`, `RightSide`, `LeftSide`, `BottomSide` groups, `AutoHideWidth`/`Height`, and `PreviousContainerId` so re-pinning returns to the original pane |
| **Floating windows** | `LayoutDocumentFloatingWindow` and `LayoutAnchorableFloatingWindow` hierarchies, `FloatingLeft`, `FloatingTop`, `FloatingWidth`, `FloatingHeight`, and `IsMaximized`. If `AllowFloatingWindows` is `false` on restore, floating items automatically dock back into the main window (`DockAllFloatingWindows()`). |
| **Standalone detached windows** | `LayoutAnchorable.IsDetached` (`IsDetached="true"`), window coordinates/size, and `PreviousContainerId`/`PreviousContainerIndex`. When `AllowDetachedWindows` is `true`, AvalonDock automatically reopens each detached anchorable in its own `DetachedAnchorableWindow` after load—and centers the window onto an active display if its saved monitor was disconnected. When `AllowDetachedWindows` is `false`, `IsDetached` is cleared and the panel remains inside the main window. |
| **Selection & flags** | `ContentId`, `Title`, `Description`, `IsSelected`, `IsLastFocusedDocument`, `LastActivationTimeStamp`, and `CanClose` / `CanFloat` / `CanHide` / `CanAutoHide` / `CanMove` |

### Not Serialized (Persist Separately)

| Application State | Recommended Persistence Strategy |
|:------------------|:---------------------------------|
| **Document contents & dirty state** | Save open file paths/contents in your application session file and reload in `LayoutSerializationCallback` |
| **ViewModel filter/scroll/tree state** | Persist ViewModel settings in XML/SQLite keyed by `IDockable.Id` |
| **`IconSource` & `ToolTip`** | Bind via `DataTemplate` or `LayoutItemContainerStyleSelector` |

---

## Part 4: Reset to Default Layout

Give users a **Window → Reset Layout** menu command using one of three patterns:

### Approach A: Restore from an In-Memory Startup Snapshot (Instant, No Restart)

Capture the initial layout into a `MemoryStream` in `Loaded` before deserializing the user's custom layout file (as shown in Part 2A). Rewinding `_defaultLayoutSnapshot.Position = 0` and calling `serializer.Deserialize(_defaultLayoutSnapshot)` resets the live UI immediately.

### Approach B: Restore from an Embedded Resource

Ship a curated `DefaultLayout.xml` as an `<EmbeddedResource>` in your `.csproj`:

```csharp
using System.Reflection;
using AvalonDock.Serializer.Xml;

public void ResetFromEmbeddedResource()
{
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream("MyApp.Resources.DefaultLayout.xml");
    if (stream == null)
    {
        return;
    }

    var serializer = new XmlLayoutSerializer(DockManager);
    serializer.LayoutSerializationCallback += OnLayoutCallback;
    serializer.Deserialize(stream);
}
```

### Approach C: Delete Saved File for Next Launch

```csharp
if (File.Exists(LayoutFilePath))
{
    File.Delete(LayoutFilePath);
}
```

---

## Part 5: Defensive Error Handling & The `ContentId` Contract

User-edited or truncated layout files should never crash application startup. Always guard `Deserialize` with a `try`/`catch` that deletes or renames unreadable layout files:

```csharp
private void RestoreLayoutSafe(string path)
{
    if (!File.Exists(path))
    {
        return;
    }

    try
    {
        var serializer = new XmlLayoutSerializer(DockManager);
        serializer.LayoutSerializationCallback += OnLayoutCallback;
        serializer.Deserialize(path);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Layout restore failed: {ex.Message}");
        try { File.Delete(path); } catch { /* ignore */ }
    }
}
```

### The `ContentId` / `IDockable.Id` Rules

1. **Every dockable item must have a unique identifier:** Duplicate `ContentId` values cause ambiguous matches during `FixupLayout`.
2. **Tool window IDs must be stable across versions:** Use constant strings like `"SolutionExplorer"`, `"Terminal"`, or `"Properties"`—never random per-session `Guid.NewGuid()` values for singleton tools.
3. **Document IDs should encode how to reopen them:** Use normalized file paths, URI strings, or database keys so `LayoutSerializationCallback` can reload closed documents on startup.
4. **Keep `Id` and `ContentId` aligned in MVVM:** `LayoutSyncBridge` maps `IDockable.Id` automatically when using `DockLayout`; bind `ContentId="{Binding Model.Id}"` in `PanesStyleSelector` when using `DocumentsSource` / `AnchorablesSource`.

---

## Next Steps

- Dive into the complete DTO hierarchy and custom `LayoutSerializerBase` implementations in the [Layout Serialization Guide]({{ site.baseurl }}{% link guides/serialization.md %}).
- Combine persistence with DI and `ToggleDockingManager` in [Tutorial: Build an MVVM IDE]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}) and [Tutorial: Dependency Injection App]({{ site.baseurl }}{% link tutorials/dependency-injection-app.md %}).
- Add runtime theme switching in [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}).
