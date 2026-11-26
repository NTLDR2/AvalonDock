---
title: Dependency Injection
layout: default
parent: Guides
nav_order: 3
description: "Register AvalonDock v5 services, toolboxes, and docking options with Microsoft.Extensions.DependencyInjection."
---

# Dependency Injection

AvalonDock v5 provides first-class integration with `Microsoft.Extensions.DependencyInjection` via the `Dirkster.AvalonDock.DependencyInjection` (`netstandard2.0`) package.

{: .tip }
For a complete application walkthrough, see [Tutorial: Dependency Injection App]({{ site.baseurl }}{% link tutorials/dependency-injection-app.md %}). The `source/AvalonDockCodeApp` sample project demonstrates these patterns in action.

---

## Install

```bash
dotnet add package Dirkster.AvalonDock.DependencyInjection
```

{: .note }
`Dirkster.AvalonDock.DependencyInjection` transitively references `Dirkster.AvalonDock.Core`, `Dirkster.AvalonDock.Mvvm`, and `Microsoft.Extensions.DependencyInjection.Abstractions`. In your WPF startup project, also install `Microsoft.Extensions.DependencyInjection` (to build the `ServiceProvider`) and `Dirkster.AvalonDock`.

---

## Extension Methods Reference

All registration extensions live on `IServiceCollection` in the `AvalonDock.DependencyInjection` namespace (`ServiceCollectionExtensions`):

### Layout Service Registration

| Extension Method | Registered Services & Behavior |
|:-----------------|:-------------------------------|
| `AddDockLayoutService(Action<DockLayoutBuilder> configure)` | Configures toolboxes and options through `DockLayoutBuilder`, and registers singletons for `IDockLayoutService` (`DockLayoutService`), `SideToggleManager`, `ToggleDockOptions`, and `DockingOptions`. |
| `AddDockLayoutService()` | Registers `IDockLayoutService` (`DockLayoutService`) and `SideToggleManager` as singletons using any `IToolbox` and `DockingOptions` services already registered in the container. |
| `AddDockLayoutService<TService>()` | Registers a custom `TService : class, IDockLayoutService` implementation as the `IDockLayoutService` singleton. |

#### `DockLayoutBuilder` Methods

Used inside `services.AddDockLayoutService(dock => { ... })`:

| Builder Method | Purpose |
|:---------------|:--------|
| `ConfigureDocking(Action<DockingOptions>? configure = null)` | Configures manager-wide window policy (`AllowFloatingWindows`, `AllowDetachedWindows`). Applied automatically to `IDockLayoutService.Layout`. |
| `ConfigureToggleDock(Action<ToggleDockOptions>? configure = null)` | Configures `ToggleDockingManager` sidebar button size, default dock dimensions, header buttons, and `LayoutPriority` (plus inherited `DockingOptions` switches). |
| `AddToolbox<T>()` | Registers `T : class, IToolbox` as a singleton under both `T` and `IToolbox`. |
| `AddToolbox<T>(Func<IServiceProvider, T> factory)` | Registers `T : class, IToolbox` via a factory delegate as a singleton under both `T` and `IToolbox`. |

### Additional Pluggable Service Extensions

For advanced architectures using custom factories or UI-agnostic service abstractions from `AvalonDock.Core`:

| Extension Method | Constraint | Registers |
|:-----------------|:-----------|:----------|
| `AddAvalonDock<TFactory>(ServiceLifetime lifetime = Singleton)` | `TFactory : class, IFactory` | `TFactory` and `IFactory` (resolved from `TFactory`) with the specified lifetime |
| `AddAvalonDockSerializer<TSerializer>()` | `TSerializer : class, ILayoutSerializer` | `ILayoutSerializer` singleton |
| `AddAvalonDockState<TState>()` | `TState : class, IDockState` | `IDockState` singleton |
| `AddAvalonDockThemeManager<TThemeManager>()` | `TThemeManager : class, IThemeManager` | `TThemeManager` and `IThemeManager` singletons |
| `AddDockingManager(Func<IServiceProvider, IDockingManager> factory)` | — | `IDockingManager` singleton via factory |
| `AddAutoHideManager<TAutoHide>()` | `TAutoHide : class, IAutoHideManager` | `IAutoHideManager` singleton |
| `AddFloatingWindowService<TFloating>()` | `TFloating : class, IFloatingWindowService` | `IFloatingWindowService` singleton |
| `AddDragDropHandler<TDragDrop>()` | `TDragDrop : class, IDragDropHandler` | `IDragDropHandler` singleton |

---

## Recommended Setup (v5 Builder Pattern)

```csharp
using AvalonDock;
using AvalonDock.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddDockLayoutService(dock =>
{
    dock.ConfigureToggleDock(opts =>
    {
        opts.ButtonSize = 28;
        opts.DefaultDockWidth = 280;
        opts.DefaultDockHeight = 220;
        opts.LayoutPriority = nameof(DockLayoutPriority.BottomFullWidth);
    });

    // Registration order determines initial sidebar button order per DockZone
    dock.AddToolbox<ExplorerToolbox>();
    dock.AddToolbox<SearchToolbox>();
    dock.AddToolbox<OutputToolbox>();
});

services.AddSingleton<MainViewModel>();
services.AddSingleton<MainWindow>();

var serviceProvider = services.BuildServiceProvider();
```

---

## Configuring `DockingOptions` & `ToggleDockOptions`

{: .new }
> New in v5

`DockLayoutBuilder` maintains a **single shared `ToggleDockOptions` instance** (which derives from `DockingOptions`) and registers that instance under **both** `ToggleDockOptions` and `DockingOptions` — even if you only call `AddToolbox<T>()` without calling `ConfigureDocking` or `ConfigureToggleDock`.

### Manager-Wide Window Policy (`DockingOptions`)

`DockingOptions` applies to **both** `DockingManager` and `ToggleDockingManager`. When `IDockLayoutService` is resolved, `AddDockLayoutService` automatically copies these flags onto `IDockLayoutService.Layout` (`IRootDock`), and any manager bound via `DockLayout` follows them automatically:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.ConfigureDocking(opts =>
    {
        opts.AllowFloatingWindows = false;   // Prevent tearing tabs/panes into floating windows
        opts.AllowDetachedWindows = false;   // Disable standalone top-level "Window" view mode
    });

    dock.AddToolbox<ExplorerToolbox>();
});
```

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `AllowFloatingWindows` | `bool` | `true` | Whether documents and anchorables can be floated into owned floating windows. |
| `AllowDetachedWindows` | `bool` | `true` | Whether anchorables can be detached into standalone top-level OS windows. |

### Toggle Sidebar Settings (`ToggleDockOptions`)

`ToggleDockOptions` inherits `AllowFloatingWindows` and `AllowDetachedWindows` from `DockingOptions` and adds sidebar settings for `ToggleDockingManager`:

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `ButtonSize` | `double` | `25` | Sidebar toggle button size in device-independent pixels. |
| `DefaultDockWidth` | `double` | `250` | Default width for left/right docked tool panes. |
| `DefaultDockHeight` | `double` | `200` | Default height for bottom docked tool panes. |
| `LayoutPriority` | `string` | `"BottomFullWidth"` | Layout restructuring mode (`"BottomFullWidth"`, `"SidesFullHeight"`, or `"Default"`). |
| `ShowHeaderMinimizeButton` | `bool` | `true` | Whether the minimize (`—`) button is shown in tool pane headers. |
| `ShowHeaderOptionsButton` | `bool` | `true` | Whether the options (`⋯`) menu button is shown in tool pane headers. |

---

## Factory-Based Toolbox Registration

When a toolbox requires constructor parameters or runtime callbacks from other services, use the `Func<IServiceProvider, T>` overload:

```csharp
services.AddDockLayoutService(dock =>
{
    dock.AddToolbox<FolderExplorerToolbox>(sp =>
        new FolderExplorerToolbox(filePath => { /* handle file open */ }));
});
```

---

## WPF Application Lifecycle Integration

Remove `StartupUri="MainWindow.xaml"` from `App.xaml`, resolve `MainWindow` in `OnStartup`, and dispose the `ServiceProvider` in `OnExit` so any `IDisposable` toolboxes (such as terminal processes or file watchers) clean up deterministically:

```csharp
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

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

    protected override void OnExit(ExitEventArgs e)
    {
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
```

---

## Applying `ToggleDockOptions` in `MainWindow`

`AllowFloatingWindows` and `AllowDetachedWindows` flow automatically through `DockLayout`. To apply the visual `ToggleDockOptions` properties to a `ToggleDockingManager`, inject `ToggleDockOptions` into `MainWindow`:

```csharp
using System;
using System.Windows;
using AvalonDock;
using AvalonDock.DependencyInjection;

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

---

## Injecting Services into View Models

Because `AddDockLayoutService` registers both `IDockLayoutService` and `SideToggleManager`, view models can request either or both via constructor injection:

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;

public class MainViewModel
{
    private readonly IDockLayoutService _dockService;
    private readonly SideToggleManager _sideToggle;

    public MainViewModel(IDockLayoutService dockService, SideToggleManager sideToggle)
    {
        _dockService = dockService;
        _sideToggle = sideToggle;
    }

    public IRootDock DockLayout => _dockService.Layout;

    public ExplorerToolbox? Explorer => _dockService.GetAnchorable<ExplorerToolbox>();

    public void ToggleLeftSidebar() => _sideToggle.Toggle(ToolboxSide.Left);
}
```
