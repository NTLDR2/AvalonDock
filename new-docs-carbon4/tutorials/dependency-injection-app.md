---
title: "Tutorial: Dependency Injection App"
layout: default
parent: Tutorials
nav_order: 2
description: "Deep-dive tutorial on building scalable, testable AvalonDock v5 applications with AddDockLayoutService and Microsoft.Extensions.DependencyInjection."
---

# Tutorial: Dependency Injection Deep Dive

This tutorial builds on the [MVVM IDE Application tutorial]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}) and walks through every Dependency Injection registration pattern, options object, lifecycle hook, and unit-testing technique available in `Dirkster.AvalonDock.DependencyInjection`.

{: .tip }
The patterns in this tutorial mirror `source/AvalonDockCodeApp` in the AvalonDock repository.

---

## What You'll Learn

- How `AddDockLayoutService(dock => ...)` wires up `IDockLayoutService`, `SideToggleManager`, `DockingOptions`, `ToggleDockOptions`, and `IToolbox` singletons in one call
- Configuring manager-wide window policies (`ConfigureDocking`) and sidebar behavior (`ConfigureToggleDock`)
- Registering parameterized toolboxes with `AddToolbox<T>(sp => ...)`
- Disposing unmanaged toolbox resources cleanly on application shutdown
- Unit-testing `MainViewModel` and `DockLayoutService` in isolation with NUnit (zero WPF window required)

---

## Prerequisites

```bash
dotnet add package Dirkster.AvalonDock
dotnet add package Dirkster.AvalonDock.Mvvm
dotnet add package Dirkster.AvalonDock.DependencyInjection
dotnet add package Dirkster.AvalonDock.Themes.Arc
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package CommunityToolkit.Mvvm
```

---

## Complete DI Surface Overview

### `IServiceCollection` Extension Methods

| Method | Purpose |
|:-------|:--------|
| `AddDockLayoutService(Action<DockLayoutBuilder>)` | Primary v5 entry point: configures toolboxes + options and registers `IDockLayoutService`, `SideToggleManager`, `ToggleDockOptions`, and `DockingOptions` |
| `AddDockLayoutService()` | Registers `IDockLayoutService` and `SideToggleManager` without a builder callback |
| `AddDockLayoutService<TService>()` | Registers a custom `IDockLayoutService` implementation |
| `AddAvalonDock<TFactory>(ServiceLifetime)` | Registers a custom `IFactory` implementation (`Singleton` by default) |
| `AddAvalonDockSerializer<TSerializer>()` | Registers an `ILayoutSerializer` (such as `XmlLayoutSerializer` or a custom `LayoutSerializerBase` implementation) |
| `AddAvalonDockState<TState>()` | Registers an `IDockState` layout state service |
| `AddAvalonDockThemeManager<TThemeManager>()` | Registers an `IThemeManager` service |
| `AddDockingManager(factory)` | Registers an `IDockingManager` instance factory |
| `AddAutoHideManager<TAutoHide>()` | Registers an `IAutoHideManager` service |
| `AddFloatingWindowService<TFloating>()` | Registers an `IFloatingWindowService` service |
| `AddDragDropHandler<TDragDrop>()` | Registers a custom `IDragDropHandler` rule service |

### `DockLayoutBuilder` Methods

| Method | Purpose |
|:-------|:--------|
| `ConfigureDocking(Action<DockingOptions>?)` | Sets manager-wide `AllowFloatingWindows` and `AllowDetachedWindows` policies |
| `ConfigureToggleDock(Action<ToggleDockOptions>?)` | Sets sidebar `ButtonSize`, `DefaultDockWidth`, `DefaultDockHeight`, `LayoutPriority`, header buttons, and inherited `DockingOptions` |
| `AddToolbox<T>()` | Registers `T` as singleton for both `T` and `IToolbox` |
| `AddToolbox<T>(Func<IServiceProvider, T>)` | Registers `T` via factory delegate as singleton for both `T` and `IToolbox` |

---

## Step 1: Registration Patterns

### Parameterless Toolboxes

Toolboxes with public constructors (including constructors that inject other DI services) use `AddToolbox<T>()`:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.AddToolbox<SearchToolbox>();
    dock.AddToolbox<ProblemsToolbox>();
});
```

Because `AddToolbox<T>()` registers `T` first and maps `IToolbox => sp.GetRequiredService<T>()`, resolving either `IEnumerable<IToolbox>` or `SearchToolbox` returns the **exact same singleton instance**.

### Factory-Constructed Toolboxes

When a toolbox needs explicit constructor arguments or delegates not registered in DI, pass a factory lambda:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.AddToolbox<FolderExplorerToolbox>(sp =>
        new FolderExplorerToolbox(filePath => { /* initial callback */ }));
});
```

### Options Inheritance (`DockingOptions` & `ToggleDockOptions`)

`ToggleDockOptions` inherits from `DockingOptions`, and `DockLayoutBuilder` shares a single underlying options object across both `ConfigureDocking` and `ConfigureToggleDock`:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.ConfigureToggleDock(opts =>
    {
        // Inherited from DockingOptions (auto-applied to IDockLayoutService.Layout):
        opts.AllowFloatingWindows = true;
        opts.AllowDetachedWindows = true;

        // ToggleDockingManager visual settings (injected into MainWindow):
        opts.ButtonSize = 28;
        opts.DefaultDockWidth = 280;
        opts.DefaultDockHeight = 220;
        opts.LayoutPriority = nameof(DockLayoutPriority.BottomFullWidth);
        opts.ShowHeaderMinimizeButton = true;
        opts.ShowHeaderOptionsButton = true;
    });
});
```

| `LayoutPriority` Value | Behavior | IDE Equivalent |
|:-----------------------|:---------|:---------------|
| `BottomFullWidth` | Bottom panel spans the full window width; left/right sidebars sit above it | JetBrains Rider |
| `SidesFullHeight` | Left and right sidebars span the full window height; bottom panel sits between them | VS Code |
| `Default` | No automatic panel restructuring | Classic AvalonDock |

---

## Step 2: Build the Composition Root (`App.xaml.cs`)

Remove `StartupUri` from `App.xaml` and configure the `ServiceCollection` in `App.xaml.cs`:

```csharp
using System;
using System.Windows;
using AvalonDock;
using AvalonDock.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MyApp.ViewModels;

namespace MyApp;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDockLayoutService(dock =>
        {
            dock.ConfigureToggleDock(opts =>
            {
                opts.ButtonSize = 28;
                opts.DefaultDockWidth = 280;
                opts.DefaultDockHeight = 220;
                opts.LayoutPriority = nameof(DockLayoutPriority.BottomFullWidth);
            });

            // Registration order controls sidebar button order within each DockZone
            dock.AddToolbox<FolderExplorerToolbox>(sp =>
                new FolderExplorerToolbox(_ => { }));
            dock.AddToolbox<SearchToolbox>();
            dock.AddToolbox<SourceControlToolbox>();
            dock.AddToolbox<ProblemsToolbox>();
            dock.AddToolbox<TerminalToolbox>();
        });

        // MainViewModel receives IDockLayoutService and SideToggleManager
        services.AddSingleton<MainViewModel>();

        // MainWindow receives MainViewModel and ToggleDockOptions
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
```

---

## Step 3: Inject `IDockLayoutService` & `SideToggleManager` into `MainViewModel`

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDockLayoutService _dockService;
    private readonly SideToggleManager _sideToggle;

    public IRootDock DockLayout => _dockService.Layout;

    public FolderExplorerToolbox? Explorer => _dockService.GetAnchorable<FolderExplorerToolbox>();

    public bool IsLeftSidebarOpen => _dockService.IsSideOpen(ToolboxSide.Left);

    public MainViewModel(IDockLayoutService dockService, SideToggleManager sideToggle)
    {
        _dockService = dockService;
        _sideToggle = sideToggle;

        Explorer?.SetOpenFileCallback(OpenFile);
        _dockService.AnchorableStateChanged += (_, _) => OnPropertyChanged(nameof(IsLeftSidebarOpen));
    }

    public void OpenFile(string filePath)
    {
        _dockService.OpenOrActivateDocument(
            existing => existing.FilePath == filePath,
            () =>
            {
                var tab = new EditorTabViewModel();
                tab.LoadFile(filePath);
                return tab;
            });
    }

    public void CloseFile(IDockable tab) => _dockService.CloseDocument(tab);

    [RelayCommand]
    private void ToggleLeftSidebar() => _sideToggle.Toggle(ToolboxSide.Left);
}
```

---

## Step 4: Apply `ToggleDockOptions` in `MainWindow.xaml.cs`

```csharp
using System;
using System.Windows;
using AvalonDock;
using AvalonDock.DependencyInjection;
using MyApp.ViewModels;

namespace MyApp;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel, ToggleDockOptions? dockOptions = null)
    {
        DataContext = viewModel;
        InitializeComponent();

        if (dockOptions != null)
        {
            dockManager.ButtonSize = dockOptions.ButtonSize;
            dockManager.DefaultDockWidth = dockOptions.DefaultDockWidth;
            dockManager.DefaultDockHeight = dockOptions.DefaultDockHeight;
            dockManager.ShowHeaderMinimizeButton = dockOptions.ShowHeaderMinimizeButton;
            dockManager.ShowHeaderOptionsButton = dockOptions.ShowHeaderOptionsButton;

            if (Enum.TryParse<DockLayoutPriority>(dockOptions.LayoutPriority, out var priority))
            {
                dockManager.LayoutPriority = priority;
            }
        }
    }
}
```

In `MainWindow.xaml`, bind `DockLayout` and map `Model.Id` → `LayoutItem.ContentId` via `PanesStyleSelector`:

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

---

## Step 5: Deterministic Cleanup with `IDisposable`

Any singleton toolbox implementing `IDisposable` is automatically disposed when `(_serviceProvider as IDisposable)?.Dispose()` runs in `App.OnExit`:

```csharp
using System;
using System.Diagnostics;
using AvalonDock.Mvvm;

public class TerminalToolbox : ToolboxBase, IDisposable
{
    private Process? _shellProcess;

    public void Dispose()
    {
        if (_shellProcess is { HasExited: false })
        {
            _shellProcess.Kill();
        }

        _shellProcess?.Dispose();
    }
}
```

---

## Step 6: Unit Testing Without WPF

Because `AvalonDock.Core`, `AvalonDock.Mvvm`, and `AvalonDock.DependencyInjection` target `netstandard2.0` with zero WPF coupling, you can test your entire docking ViewModel and DI setup in fast headless NUnit tests (no `[Apartment(ApartmentState.STA)]` or `Window` needed):

```csharp
using AvalonDock.Core;
using AvalonDock.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MyApp.ViewModels;
using NUnit.Framework;

[TestFixture]
public class MainViewModelDockTests
{
    [Test]
    public void OpenFile_RegistersAndActivatesDocument_WithoutDuplicates()
    {
        var services = new ServiceCollection();
        services.AddDockLayoutService(dock =>
        {
            dock.ConfigureDocking(o => o.AllowDetachedWindows = false);
            dock.AddToolbox<SearchToolbox>();
        });
        services.AddSingleton<MainViewModel>();

        using var provider = services.BuildServiceProvider();
        var dockService = provider.GetRequiredService<IDockLayoutService>();
        var vm = provider.GetRequiredService<MainViewModel>();

        // Window policy configured in DI is applied directly to IRootDock
        Assert.That(dockService.Layout.AllowDetachedWindows, Is.False);

        // Act: open the same path twice
        vm.OpenFile("Program.cs");
        vm.OpenFile("Program.cs");

        // Assert: only one tab created, and it is active
        var doc = dockService.FindDocument<EditorTabViewModel>(d => d.FilePath == "Program.cs");
        Assert.That(doc, Is.Not.Null);
        Assert.That(dockService.ActiveDockable, Is.SameAs(doc));
    }
}
```

---

## Next Steps

- Build a complete multi-panel application from scratch in [Tutorial: MVVM IDE Application]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}).
- Persist and restore your layout with `XmlLayoutSerializer` in [Tutorial: Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}).
- Customize themes and branding in [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}).
- Browse all interfaces, base classes, and builder types in [Core, MVVM & DI Reference]({{ site.baseurl }}{% link api-reference/core-and-mvvm.md %}).
