---
title: MVVM Integration
layout: default
parent: Guides
nav_order: 1
description: "How to build decoupled MVVM docking applications with AvalonDock.Mvvm, CommunityToolkit.Mvvm, and IDockLayoutService."
---

# MVVM Integration

AvalonDock v5 provides first-class MVVM support through three `netstandard2.0` packages—`Dirkster.AvalonDock.Core`, `Dirkster.AvalonDock.Mvvm`, and `Dirkster.AvalonDock.Mvvm.CommunityToolkit`—so your view-model project never needs a reference to WPF.

This guide covers both:
1. **Recommended v5 Architecture:** `IDockLayoutService` + `DockLayout` binding (works with both `ToggleDockingManager` and classic `DockingManager`).
2. **Classic v4/v5 Architecture:** `DocumentsSource` + `AnchorablesSource` collection bindings.

{: .tip }
For a complete step-by-step walkthrough, follow [Tutorial: Build an MVVM IDE Application]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}). The `source/AvalonDockCodeApp` sample in the repository is the live reference implementation.

---

## Install

For zero external dependencies (hand-written properties using `SetProperty`):

```bash
dotnet add package Dirkster.AvalonDock.Mvvm
dotnet add package Dirkster.AvalonDock.DependencyInjection
```

For [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) source-generator support (`[ObservableProperty]` and `[RelayCommand]`):

```bash
dotnet add package Dirkster.AvalonDock.Mvvm.CommunityToolkit
dotnet add package Dirkster.AvalonDock.DependencyInjection
```

### Choosing Between `AvalonDock.Mvvm` and `AvalonDock.Mvvm.CommunityToolkit`

Both packages implement the exact same `AvalonDock.Core` interfaces (`IDockable`, `IToolbox`, `IDock`) and can even be mixed within the same application:

| Role | `AvalonDock.Mvvm` *(zero external deps)* | `AvalonDock.Mvvm.CommunityToolkit` *(uses `ObservableObject`)* | Core Interface |
|:-----|:-----------------------------------------|:---------------------------------------------------------------|:---------------|
| Abstract dockable base | `DockableBase` | `ObservableDockableBase` | `IDockable` |
| Abstract toolbox base | `ToolboxBase` | `ObservableToolboxBase` | `IToolbox` |
| Abstract dock container | `DockBase` | `ObservableDockBase` | `IDock` |
| Concrete leaf document | `Document` | `ObservableDocument` | `IDockable` |
| Concrete leaf tool | `Tool` | `ObservableTool` | `IDockable` |
| Layout container VMs | `RootDock`, `DocumentDock`, `ToolDock` | *(provided by `AvalonDock.Mvvm` via `DockLayoutService`)* | `IRootDock`, `IDocumentDock`, `IToolDock` |

---

## Recommended v5 Pattern: `IDockLayoutService` + `DockLayout`

In the v5 service pattern, `DockLayoutService` automatically constructs and maintains an `IRootDock` tree from all registered `IToolbox` instances. Your `MainViewModel` injects `IDockLayoutService` to open/close documents and query toolboxes without touching UI controls.

### 1. Define a Toolbox View Model

Inherit from `ToolboxBase` (or `ObservableToolboxBase`) and configure its `Id`, `Title`, `Zone`, and optional keyboard `Shortcut`:

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;

public class ExplorerToolbox : ToolboxBase
{
    public ExplorerToolbox()
    {
        Id = "Explorer";
        Title = "Explorer";
        Zone = DockZone.LeftTop;
        Shortcut = "Ctrl+Shift+E";   // Registers KeyBinding & appends "(Ctrl+Shift+E)" to tooltip
        ToolTipText = "Solution Explorer";
        IsOpenByDefault = true;
    }
}
```

### 2. Define a Document View Model

Using `AvalonDock.Mvvm.CommunityToolkit` (with `[ObservableProperty]` source generators):

```csharp
using System.IO;
using AvalonDock.Mvvm.CommunityToolkit;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class EditorTabViewModel : ObservableDocument
{
    [ObservableProperty] private string _content = string.Empty;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _toolTip = string.Empty;

    public void LoadFile(string path)
    {
        FilePath = path;
        Id = path;
        Title = Path.GetFileName(path);
        ToolTip = path;
        Content = File.ReadAllText(path);
        IsModified = false;
    }
}
```

Or using `AvalonDock.Mvvm` (`Document` / `DockableBase`, zero external dependencies):

```csharp
using System.IO;
using AvalonDock.Mvvm;

public class EditorTabViewModel : Document
{
    private string _content = string.Empty;
    private string _filePath = string.Empty;

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public void LoadFile(string path)
    {
        FilePath = path;
        Id = path;
        Title = Path.GetFileName(path);
        Content = File.ReadAllText(path);
        IsModified = false;
    }
}
```

### 3. Drive the Layout from `MainViewModel`

`AddDockLayoutService` registers both `IDockLayoutService` and `SideToggleManager` as singletons in DI, so both can be injected directly:

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    private readonly IDockLayoutService _dockService;
    private readonly SideToggleManager _sideToggle;

    // Bind this property to ToggleDockingManager.DockLayout or DockingManager.DockLayout
    public IRootDock DockLayout => _dockService.Layout;

    // Typed lookup of any registered toolbox
    public ExplorerToolbox? Explorer => _dockService.GetAnchorable<ExplorerToolbox>();

    public MainViewModel(IDockLayoutService dockService, SideToggleManager sideToggle)
    {
        _dockService = dockService;
        _sideToggle = sideToggle;
    }

    public void OpenFile(string path)
    {
        _dockService.OpenOrActivateDocument(
            existing => existing.FilePath == path,
            () =>
            {
                var tab = new EditorTabViewModel();
                tab.LoadFile(path);
                return tab;
            });
    }

    [RelayCommand]
    private void TogglePrimarySideBar() => _sideToggle.Toggle(ToolboxSide.Left);

    [RelayCommand]
    private void ToggleBottomPanel() => _sideToggle.Toggle(ToolboxSide.Bottom);

    [RelayCommand]
    private void ToggleSecondarySideBar() => _sideToggle.Toggle(ToolboxSide.Right);
}
```

*(If you are not using Dependency Injection, instantiate `new DockLayoutService(toolboxes)` and `new SideToggleManager(dockService)` directly.)*

### 4. Bind `DockLayout` in XAML

For the VS Code / Rider-style **`ToggleDockingManager`**:

```xml
<avalonDock:ToggleDockingManager x:Name="dockManager"
    DockLayout="{Binding DockLayout}"
    LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">
    <avalonDock:LayoutRoot>
        <avalonDock:LayoutPanel Orientation="Horizontal">
            <avalonDock:LayoutDocumentPaneGroup>
                <avalonDock:LayoutDocumentPane />
            </avalonDock:LayoutDocumentPaneGroup>
        </avalonDock:LayoutPanel>
    </avalonDock:LayoutRoot>
</avalonDock:ToggleDockingManager>
```

For the classic Visual Studio-style **`DockingManager`**, bind the **same** `DockLayout` property (not `Layout`, which holds the WPF `LayoutRoot`):

```xml
<avalonDock:DockingManager x:Name="dockManager"
    DockLayout="{Binding DockLayout}"
    LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">
    <avalonDock:LayoutRoot>
        <avalonDock:LayoutPanel Orientation="Horizontal">
            <avalonDock:LayoutAnchorablePane DockWidth="220" />
            <avalonDock:LayoutDocumentPane />
        </avalonDock:LayoutPanel>
    </avalonDock:LayoutRoot>
</avalonDock:DockingManager>
```

When `DockLayout` is bound on the classic `DockingManager`, `LayoutSyncBridge` and `DockAlignmentStrategy` map each `IToolDock.Alignment` (`Left`, `Right`, `Top`, `Bottom`) to any visible `LayoutAnchorablePane` docked on that side, or auto-hide it onto that side's `LayoutAnchorGroup` if no pane is docked there yet.

### 5. Controlling Window Policy from View Models

{: .new }
> New in v5

`IRootDock` exposes manager-wide window policy properties so view models can disable tear-off floating windows or standalone top-level tool windows ("Window" view mode) without referencing the view:

```csharp
dockService.Layout.AllowFloatingWindows = false;   // Disables tear-off floating windows
dockService.Layout.AllowDetachedWindows = false;   // Disables standalone "Window" view mode
```

You can also configure these once at DI registration via `dock.ConfigureDocking(...)` — see [Dependency Injection]({{ site.baseurl }}{% link guides/dependency-injection.md %}) and [Floating Windows]({{ site.baseurl }}{% link concepts/floating-windows.md %}).

---

## Bridging ViewModel Properties with `LayoutItemContainerStyleSelector`

WPF `LayoutAnchorableItem` and `LayoutDocumentItem` containers wrap your view models (`LayoutItem.Model`). Use a `StyleSelector` to bind layout metadata (`Title`, `ContentId`, `CanClose`, `IconSource`) from your view model to the WPF container:

```csharp
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Core;

public class PanesStyleSelector : StyleSelector
{
    public Style? ToolboxStyle { get; set; }
    public Style? DocumentStyle { get; set; }

    public override Style? SelectStyle(object item, DependencyObject container)
    {
        if (item is IToolbox) return ToolboxStyle;
        if (item is IDockable) return DocumentStyle;
        return base.SelectStyle(item, container);
    }
}
```

```xml
<local:PanesStyleSelector x:Key="PanesStyleSelector">
    <local:PanesStyleSelector.ToolboxStyle>
        <Style TargetType="{x:Type avalonDockControls:LayoutAnchorableItem}">
            <Setter Property="Title" Value="{Binding Model.Title}" />
            <Setter Property="ContentId" Value="{Binding Model.Id}" />
            <Setter Property="CanClose" Value="{Binding Model.CanClose}" />
            <Setter Property="CanFloat" Value="{Binding Model.CanFloat}" />
        </Style>
    </local:PanesStyleSelector.ToolboxStyle>
    <local:PanesStyleSelector.DocumentStyle>
        <Style TargetType="{x:Type avalonDockControls:LayoutDocumentItem}">
            <Setter Property="Title" Value="{Binding Model.Title}" />
            <Setter Property="ContentId" Value="{Binding Model.Id}" />
            <Setter Property="ToolTip" Value="{Binding Model.ToolTip}" />
            <Setter Property="CanClose" Value="{Binding Model.CanClose}" />
            <Setter Property="CanFloat" Value="{Binding Model.CanFloat}" />
        </Style>
    </local:PanesStyleSelector.DocumentStyle>
</local:PanesStyleSelector>
```

{: .important }
**Bind `ContentId` to `Model.Id`:** `IDockable`, `DockableBase`, `ToolboxBase`, `ObservableDockableBase`, and `ObservableToolboxBase` name their unique identifier property **`Id`**, whereas WPF `LayoutItem` names its dependency property **`ContentId`**. Always write `<Setter Property="ContentId" Value="{Binding Model.Id}" />` (unless you define a custom `ContentId` property on your own view model) so layout serialization can match items accurately.

---

## Classic Approach: `DocumentsSource` + `AnchorablesSource`

Applications that manage raw `ObservableCollection<T>` properties without `IDockLayoutService` can continue binding `DocumentsSource` and `AnchorablesSource` directly on `DockingManager`:

```csharp
using AvalonDock.Mvvm;

public class DocumentViewModel : Document
{
    private string _text = string.Empty;

    public DocumentViewModel(string id, string title)
    {
        Id = id;
        Title = title;
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}

public class ToolViewModel : Tool
{
    public ToolViewModel(string id, string title)
    {
        Id = id;
        Title = title;
        CanClose = false;
    }
}
```

Render view models using implicit `DataTemplate` resources (matched automatically by `DataType`), `LayoutItemTemplate`, or `LayoutItemTemplateSelector`:

```xml
<avalonDock:DockingManager DocumentsSource="{Binding Documents}"
                           AnchorablesSource="{Binding Tools}"
                           ActiveContent="{Binding ActiveDocument, Mode=TwoWay}"
                           LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">

    <avalonDock:DockingManager.Resources>
        <DataTemplate DataType="{x:Type local:DocumentViewModel}">
            <TextBox Text="{Binding Text, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" />
        </DataTemplate>
        <DataTemplate DataType="{x:Type local:ToolViewModel}">
            <TextBlock Text="{Binding Title}" Margin="8" />
        </DataTemplate>
    </avalonDock:DockingManager.Resources>

    <avalonDock:LayoutRoot>
        <avalonDock:LayoutPanel Orientation="Horizontal">
            <avalonDock:LayoutAnchorablePane DockWidth="200" />
            <avalonDock:LayoutDocumentPane />
        </avalonDock:LayoutPanel>
    </avalonDock:LayoutRoot>
</avalonDock:DockingManager>
```

---

## Selecting Content Templates (`LayoutItemTemplateSelector`)

If you prefer explicit template selection over implicit `DataType` templates, assign `DockingManager.LayoutItemTemplateSelector`:

```csharp
using System.Windows;
using System.Windows.Controls;

public class DockTemplateSelector : DataTemplateSelector
{
    public DataTemplate? DocumentTemplate { get; set; }
    public DataTemplate? ExplorerTemplate { get; set; }
    public DataTemplate? PropertiesTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            DocumentViewModel => DocumentTemplate,
            ExplorerToolbox => ExplorerTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
```

---

## `IDockLayoutService` & Extension Methods Summary

Core members on `IDockLayoutService` and extension methods in `AvalonDock.Core.DockLayoutServiceExtensions`:

| Member | Kind | Description |
|:-------|:-----|:------------|
| `Layout` | Property (`IRootDock`) | Root MVVM layout tree — bind to `DockLayout` on `DockingManager` or `ToggleDockingManager`. |
| `ActiveDockable` | Property (`IDockable?`) | Gets or sets the currently active dockable. |
| `Documents` | Property (`IEnumerable<IDockable>`) | Currently open document view models. |
| `Anchorables` | Property (`IEnumerable<IDockable>`) | Registered toolbox / anchorable view models. |
| `AnchorableStateChanged` | Event (`EventHandler?`) | Raised whenever any registered toolbox's `IsOpen` property changes. |
| `OpenDocument(IDockable)` | Method | Adds a document (if not already open) and makes it active. |
| `CloseDocument(IDockable)` | Method | Removes a document and activates the last remaining document. |
| `FindDocument<T>(Func<T, bool>)` | Extension | Returns the first open document of type `T` matching the predicate, or `null`. |
| `OpenOrActivateDocument<T>(predicate, factory)` | Extension | Activates an existing matching document of type `T`, or creates and opens a new one via `factory`. |
| `GetAnchorable<T>()` | Extension | Returns the registered `IToolbox` instance of type `T`, or `null`. |
| `ShowAnchorable(IDockable)` | Extension | Docks/shows a toolbox by setting `IToolbox.IsOpen = true`. |
| `HideAnchorable(IDockable)` | Extension | Collapses/auto-hides a toolbox by setting `IToolbox.IsOpen = false`. |
| `IsAnchorableOpen(IDockable)` | Extension | Returns `true` when the dockable is an `IToolbox` with `IsOpen == true`. |
| `IsSideOpen(ToolboxSide)` | Extension | Returns `true` when any `IToolbox` on `Left`, `Right`, or `Bottom` is currently open. |

See [Core, MVVM & DI Reference]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}) for complete member tables of all interfaces and classes.
