---
title: "Tutorial: MVVM IDE Application"
layout: default
parent: Tutorials
nav_order: 1
description: "Build a VS Code / Rider-style multi-document IDE using AvalonDock v5 MVVM, CommunityToolkit.Mvvm, Dependency Injection, and ToggleDockingManager."
---

# Tutorial: Build an MVVM IDE Application (v5)

In this tutorial you will build a Visual Studio Code / JetBrains Rider-style IDE from scratch using AvalonDock v5's `ToggleDockingManager`, `AvalonDock.Mvvm.CommunityToolkit`, and `AvalonDock.DependencyInjection`. Every document and tool window is driven by view models injected through `IDockLayoutService`.

![MVVM IDE Application Reference Preview]({{ site.baseurl }}/images/AvalonDockCodeApp_Dark_Welcome.png)

{: .tip }
This tutorial mirrors the architecture of `source/AvalonDockCodeApp` in the AvalonDock repository.

---

## What You'll Build

- **6-zone sidebar toggle bars** with keyboard shortcuts (`Ctrl+Shift+E`, `Ctrl+Shift+F`, `Ctrl+Shift+U`)
- **Explorer panel** (`LeftTop`) — double-click a file to open or activate its editor tab
- **Search panel** (`LeftTop`) — shares the upper-left zone with Explorer
- **Output panel** (`BottomLeft`, open on startup) — logs workspace activity
- **Workspace sidebar toggles** — top-right buttons using `SideToggleManager` to collapse/restore entire sides at once
- **XML layout save & restore** — preserves panel zones, sizes, and open state across sessions

---

## Prerequisites

- Windows with the **.NET 8.0 SDK** and the **.NET Framework 4.8 Developer Pack**

```bash
dotnet new wpf -n MvvmIdeApp
cd MvvmIdeApp
# In MvvmIdeApp.csproj, set <TargetFramework>net48</TargetFramework> and <LangVersion>12</LangVersion>

dotnet add package Dirkster.AvalonDock
dotnet add package Dirkster.AvalonDock.Mvvm
dotnet add package Dirkster.AvalonDock.Mvvm.CommunityToolkit
dotnet add package Dirkster.AvalonDock.DependencyInjection
dotnet add package Dirkster.AvalonDock.Serializer.Xml
dotnet add package Dirkster.AvalonDock.Themes.Type3New
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package CommunityToolkit.Mvvm
```

---

## How the Pieces Fit Together

```text
App.xaml.cs (Composition Root)
├── AddDockLayoutService(dock => {
│       dock.ConfigureToggleDock(...)       ← sidebar button size, default dock widths/heights, priority
│       dock.AddToolbox<ExplorerToolbox>()  ← singleton IToolbox registrations
│       dock.AddToolbox<SearchToolbox>()
│       dock.AddToolbox<OutputToolbox>()
│   })                                      ← also registers IDockLayoutService & SideToggleManager
└── AddSingleton<MainViewModel>()           ← injects IDockLayoutService & SideToggleManager

MainViewModel
├── DockLayout (IRootDock)                  ← bound to ToggleDockingManager.DockLayout
├── OpenFile()                              ← _dockService.OpenOrActivateDocument(...)
└── ToggleLeft / ToggleBottom               ← _sideToggle.Toggle(ToolboxSide.*)

MainWindow.xaml (ToggleDockingManager)
├── DockLayout="{Binding DockLayout}"       ← synchronizes ViewModel tree ↔ WPF layout
├── DataTemplates per ViewModel type        ← renders Explorer, Search, Output & Editor views
└── PanesStyleSelector                      ← binds LayoutItem.ContentId ← Model.Id & Title ← Model.Title
```

---

## Step 1: Create Toolbox View Models

Inherit each tool panel from `ObservableToolboxBase` (`AvalonDock.Mvvm.CommunityToolkit`) so you can use `[ObservableProperty]` and `[RelayCommand]` source generators. *(If you prefer zero third-party dependencies, inherit from `ToolboxBase` in `AvalonDock.Mvvm` instead.)*

{: .note }
Set `ToolTipText` to the base label (e.g. `"Explorer"`) and set `Shortcut` separately (e.g. `"Ctrl+Shift+E"`). `ToggleDockButton` automatically appends `" (Ctrl+Shift+E)"` to the tooltip and registers the `KeyBinding` on the host window!

**File: `ViewModels/ExplorerToolbox.cs`**

```csharp
using System;
using System.Collections.ObjectModel;
using AvalonDock.Core;
using AvalonDock.Mvvm.CommunityToolkit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MvvmIdeApp.ViewModels;

public partial class ExplorerToolbox : ObservableToolboxBase
{
    private Action<string> _openFileCallback = _ => { };

    [ObservableProperty]
    private ObservableCollection<string> _files = new()
    {
        "Program.cs",
        "MainWindow.xaml",
        "App.xaml",
        "README.md"
    };

    public ExplorerToolbox()
    {
        Id = "Explorer";
        Title = "Explorer";
        ToolTipText = "Solution Explorer";
        Shortcut = "Ctrl+Shift+E";
        Zone = DockZone.LeftTop;
        IsOpenByDefault = true;
    }

    public void SetOpenFileCallback(Action<string> callback)
        => _openFileCallback = callback;

    [RelayCommand]
    private void OpenSelectedFile(string? fileName)
    {
        if (!string.IsNullOrEmpty(fileName))
        {
            _openFileCallback(fileName);
        }
    }
}
```

**File: `ViewModels/SearchToolbox.cs`**

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm.CommunityToolkit;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MvvmIdeApp.ViewModels;

public partial class SearchToolbox : ObservableToolboxBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    public SearchToolbox()
    {
        Id = "Search";
        Title = "Search";
        ToolTipText = "Search in Files";
        Shortcut = "Ctrl+Shift+F";
        Zone = DockZone.LeftTop;
    }
}
```

**File: `ViewModels/OutputToolbox.cs`**

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm.CommunityToolkit;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MvvmIdeApp.ViewModels;

public partial class OutputToolbox : ObservableToolboxBase
{
    [ObservableProperty]
    private string _outputText = "Ready.\n";

    public OutputToolbox()
    {
        Id = "Output";
        Title = "Output";
        ToolTipText = "Output Log";
        Shortcut = "Ctrl+Shift+U";
        Zone = DockZone.BottomLeft;
        IsOpenByDefault = true;
    }

    public void AppendLine(string text) => OutputText += text + "\n";
}
```

---

## Step 2: Create the Document View Model

Documents inherit from `ObservableDocument` (or `Document` in `AvalonDock.Mvvm`). Setting `Id = path` gives every open file a stable identifier for duplicate detection and layout serialization.

**File: `ViewModels/EditorTabViewModel.cs`**

```csharp
using System;
using System.IO;
using AvalonDock.Mvvm.CommunityToolkit;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MvvmIdeApp.ViewModels;

public partial class EditorTabViewModel : ObservableDocument
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private string _toolTip = string.Empty;

    public void LoadFile(string path)
    {
        FilePath = path;
        Id = path;
        Title = Path.GetFileName(path);
        ToolTip = path;

        if (File.Exists(path))
        {
            Content = File.ReadAllText(path);
        }
        else
        {
            Content = $"// Sample content for {Title}\nnamespace MvvmIdeApp;\n";
        }

        IsModified = false;
    }
}
```

---

## Step 3: Create `MainViewModel`

`MainViewModel` receives `IDockLayoutService` and `SideToggleManager` through constructor injection:

**File: `ViewModels/MainViewModel.cs`**

```csharp
using AvalonDock.Core;
using AvalonDock.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MvvmIdeApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDockLayoutService _dockService;
    private readonly SideToggleManager _sideToggle;

    /// <summary>Bind to ToggleDockingManager.DockLayout in XAML.</summary>
    public IRootDock DockLayout => _dockService.Layout;

    /// <summary>Exposed so MainWindow can reconnect toolboxes on layout deserialization.</summary>
    public IDockLayoutService LayoutService => _dockService;

    public ExplorerToolbox? Explorer => _dockService.GetAnchorable<ExplorerToolbox>();

    public OutputToolbox? Output => _dockService.GetAnchorable<OutputToolbox>();

    public MainViewModel(IDockLayoutService dockService, SideToggleManager sideToggle)
    {
        _dockService = dockService;
        _sideToggle = sideToggle;

        Explorer?.SetOpenFileCallback(OpenFile);
        Output?.AppendLine("IDE initialized via Dependency Injection.");
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

        Output?.AppendLine($"Opened: {filePath}");
    }

    [RelayCommand]
    private void TogglePrimarySideBar() => _sideToggle.Toggle(ToolboxSide.Left);

    [RelayCommand]
    private void ToggleBottomPanel() => _sideToggle.Toggle(ToolboxSide.Bottom);
}
```

---

## Step 4: Configure Dependency Injection (`App.xaml` & `App.xaml.cs`)

{: .important }
Remove `StartupUri="MainWindow.xaml"` from `App.xaml` so WPF does not instantiate a second parameterless `MainWindow` outside DI.

**File: `App.xaml`**

```xml
<Application x:Class="MvvmIdeApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnMainWindowClose">
    <Application.Resources />
</Application>
```

**File: `App.xaml.cs`**

```csharp
using System;
using System.Windows;
using AvalonDock;
using AvalonDock.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using MvvmIdeApp.ViewModels;

namespace MvvmIdeApp;

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
                opts.DefaultDockWidth = 260;
                opts.DefaultDockHeight = 200;
                opts.LayoutPriority = nameof(DockLayoutPriority.BottomFullWidth);
            });

            // Registration order sets initial button order within each DockZone
            dock.AddToolbox<ExplorerToolbox>();
            dock.AddToolbox<SearchToolbox>();
            dock.AddToolbox<OutputToolbox>();
        });

        services.AddSingleton<MainViewModel>();
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

## Step 5: Create `PanesStyleSelector`

`PanesStyleSelector` maps ViewModel properties (`Model.Title`, `Model.Id`) onto the generated `LayoutAnchorableItem` and `LayoutDocumentItem` containers:

**File: `PanesStyleSelector.cs`**

```csharp
using System.Windows;
using System.Windows.Controls;
using AvalonDock.Core;
using MvvmIdeApp.ViewModels;

namespace MvvmIdeApp;

public class PanesStyleSelector : StyleSelector
{
    public Style? ToolboxStyle { get; set; }
    public Style? DocumentStyle { get; set; }

    public override Style? SelectStyle(object item, DependencyObject container)
    {
        if (item is IToolbox)
            return ToolboxStyle;

        if (item is EditorTabViewModel)
            return DocumentStyle;

        return base.SelectStyle(item, container);
    }
}
```

---

## Step 6: Build `MainWindow.xaml`

Notice that both `ToolboxStyle` and `DocumentStyle` bind `ContentId` to **`{Binding Model.Id}`** (since `IDockable` / `ObservableDockableBase` exposes `Id`):

**File: `MainWindow.xaml`**

```xml
<Window x:Class="MvvmIdeApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/NTLDR2/AvalonDock"
        xmlns:avalonDockControls="clr-namespace:AvalonDock.Controls;assembly=AvalonDock"
        xmlns:local="clr-namespace:MvvmIdeApp"
        xmlns:vm="clr-namespace:MvvmIdeApp.ViewModels"
        Title="MVVM IDE — AvalonDock v5"
        Width="1050" Height="700"
        Background="#252729">

    <Window.Resources>
        <!-- Explorer content template -->
        <DataTemplate DataType="{x:Type vm:ExplorerToolbox}">
            <DockPanel Background="#252526" Margin="4">
                <Button DockPanel.Dock="Bottom"
                        Content="Open Selected File"
                        Command="{Binding OpenSelectedFileCommand}"
                        CommandParameter="{Binding SelectedItem, ElementName=FilesList}"
                        Margin="4" Padding="6,4" />
                <ListBox x:Name="FilesList"
                         ItemsSource="{Binding Files}"
                         SelectedIndex="0"
                         Background="#252526"
                         Foreground="#CCCCCC"
                         BorderThickness="0" />
            </DockPanel>
        </DataTemplate>

        <!-- Search content template -->
        <DataTemplate DataType="{x:Type vm:SearchToolbox}">
            <Border Background="#252526" Padding="12">
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         Background="#3C3C3C" Foreground="#CCCCCC"
                         BorderThickness="0" Padding="6,4"
                         VerticalAlignment="Top" />
            </Border>
        </DataTemplate>

        <!-- Output content template -->
        <DataTemplate DataType="{x:Type vm:OutputToolbox}">
            <TextBox Text="{Binding OutputText, Mode=OneWay}"
                     IsReadOnly="True" AcceptsReturn="True"
                     FontFamily="Consolas" FontSize="12"
                     VerticalScrollBarVisibility="Auto"
                     Background="#1E1E1E" Foreground="#DCDCDC"
                     BorderThickness="0" />
        </DataTemplate>

        <!-- Editor document template -->
        <DataTemplate DataType="{x:Type vm:EditorTabViewModel}">
            <TextBox Text="{Binding Content, UpdateSourceTrigger=PropertyChanged}"
                     AcceptsReturn="True" AcceptsTab="True"
                     FontFamily="Consolas" FontSize="13"
                     VerticalScrollBarVisibility="Auto"
                     Background="#1E1E1E" Foreground="#DCDCDC"
                     BorderThickness="0" />
        </DataTemplate>

        <!-- Container style selector bridging Model.Id -> ContentId -->
        <local:PanesStyleSelector x:Key="PanesStyleSelector">
            <local:PanesStyleSelector.ToolboxStyle>
                <Style TargetType="{x:Type avalonDockControls:LayoutAnchorableItem}">
                    <Setter Property="Title" Value="{Binding Model.Title}" />
                    <Setter Property="ContentId" Value="{Binding Model.Id}" />
                </Style>
            </local:PanesStyleSelector.ToolboxStyle>
            <local:PanesStyleSelector.DocumentStyle>
                <Style TargetType="{x:Type avalonDockControls:LayoutDocumentItem}">
                    <Setter Property="Title" Value="{Binding Model.Title}" />
                    <Setter Property="ToolTip" Value="{Binding Model.ToolTip}" />
                    <Setter Property="ContentId" Value="{Binding Model.Id}" />
                </Style>
            </local:PanesStyleSelector.DocumentStyle>
        </local:PanesStyleSelector>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Top Menu Bar -->
        <Menu Grid.Row="0" Background="#252729" Foreground="#CCCCCC" Padding="4">
            <MenuItem Header="_File" Foreground="#CCCCCC">
                <MenuItem Header="E_xit" Click="OnExit" Foreground="Black" />
            </MenuItem>
            <MenuItem Header="_View" Foreground="#CCCCCC">
                <MenuItem Header="Toggle Left Sidebar"
                          Command="{Binding TogglePrimarySideBarCommand}"
                          Foreground="Black" />
                <MenuItem Header="Toggle Bottom Panel"
                          Command="{Binding ToggleBottomPanelCommand}"
                          Foreground="Black" />
            </MenuItem>
            <MenuItem Header="_Layout" Foreground="#CCCCCC">
                <MenuItem Header="Save Layout" Click="OnSaveLayout" Foreground="Black" />
                <MenuItem Header="Load Layout" Click="OnLoadLayout" Foreground="Black" />
            </MenuItem>
        </Menu>

        <!-- ToggleDockingManager -->
        <avalonDock:ToggleDockingManager x:Name="dockManager" Grid.Row="1"
            Background="#252729"
            DockLayout="{Binding DockLayout}"
            LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">

            <avalonDock:ToggleDockingManager.Theme>
                <avalonDock:VS2026DarkTheme />
            </avalonDock:ToggleDockingManager.Theme>

            <avalonDock:LayoutRoot>
                <avalonDock:LayoutPanel Orientation="Horizontal">
                    <avalonDock:LayoutDocumentPaneGroup>
                        <avalonDock:LayoutDocumentPane>
                            <avalonDock:LayoutDocument Title="Welcome" ContentId="welcome">
                                <Border Background="#1E1E1E" Padding="40">
                                    <StackPanel VerticalAlignment="Center"
                                                HorizontalAlignment="Center">
                                        <TextBlock Text="MVVM IDE" FontSize="28"
                                                   FontWeight="Light" Foreground="#CCCCCC"
                                                   HorizontalAlignment="Center" />
                                        <TextBlock Text="Powered by AvalonDock v5" FontSize="14"
                                                   Foreground="#808080"
                                                   HorizontalAlignment="Center" Margin="0,8,0,24" />
                                        <TextBlock Foreground="#808080" FontSize="13"
                                                   TextAlignment="Center" LineHeight="24">
                                            <Run Text="Press Ctrl+Shift+E, Ctrl+Shift+F, or Ctrl+Shift+U to toggle panels" />
                                            <LineBreak />
                                            <Run Text="Drag sidebar buttons to relocate tools across all 6 DockZones" />
                                        </TextBlock>
                                    </StackPanel>
                                </Border>
                            </avalonDock:LayoutDocument>
                        </avalonDock:LayoutDocumentPane>
                    </avalonDock:LayoutDocumentPaneGroup>
                </avalonDock:LayoutPanel>
            </avalonDock:LayoutRoot>
        </avalonDock:ToggleDockingManager>
    </Grid>
</Window>
```

---

## Step 7: Wire Options & Layout Save/Load in `MainWindow.xaml.cs`

`MainWindow` applies `ToggleDockOptions` and reconnects registered `IToolbox` view models by `t.Id == args.Model.ContentId` when loading a saved layout:

**File: `MainWindow.xaml.cs`**

```csharp
using System;
using System.IO;
using System.Linq;
using System.Windows;
using AvalonDock;
using AvalonDock.Core;
using AvalonDock.DependencyInjection;
using AvalonDock.Serializer.Xml;
using MvvmIdeApp.ViewModels;

namespace MvvmIdeApp;

public partial class MainWindow : Window
{
    private static string LayoutFilePath =>
        Path.Combine(AppContext.BaseDirectory, "MvvmIdeLayout.xml");

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

    private void OnSaveLayout(object sender, RoutedEventArgs e)
    {
        new XmlLayoutSerializer(dockManager).Serialize(LayoutFilePath);
    }

    private void OnLoadLayout(object sender, RoutedEventArgs e)
    {
        if (!File.Exists(LayoutFilePath) || DataContext is not MainViewModel vm)
        {
            return;
        }

        var serializer = new XmlLayoutSerializer(dockManager);
        serializer.LayoutSerializationCallback += (_, args) =>
        {
            var toolbox = vm.LayoutService.Anchorables
                .OfType<IToolbox>()
                .FirstOrDefault(t => t.Id == args.Model.ContentId);

            if (toolbox != null)
            {
                args.Content = toolbox;
            }
        };

        serializer.Deserialize(LayoutFilePath);
    }

    private void OnExit(object sender, RoutedEventArgs e) => Close();
}
```

Run your IDE:

```bash
dotnet run
```

---

## Adding Another Tool Panel in 3 Lines

1. **Define the ViewModel:**
   ```csharp
   public partial class ProblemsToolbox : ObservableToolboxBase
   {
       public ProblemsToolbox()
       {
           Id = "Problems";
           Title = "Problems";
           Shortcut = "Ctrl+Shift+M";
           Zone = DockZone.BottomRight;
       }
   }
   ```
2. **Register in `AddDockLayoutService`:**
   ```csharp
   dock.AddToolbox<ProblemsToolbox>();
   ```
3. **Add a `DataTemplate` in `MainWindow.xaml`:**
   ```xml
   <DataTemplate DataType="{x:Type vm:ProblemsToolbox}">
       <TextBlock Text="No problems detected" Foreground="#808080" Margin="12" />
   </DataTemplate>
   ```

---

## Next Steps

- Explore advanced DI options and NUnit ViewModel testing in [Tutorial: Dependency Injection App]({{ site.baseurl }}{% link tutorials/dependency-injection-app.md %}).
- Persist open documents, XML layouts, and lazy plugin panels in [Tutorial: Layout Persistence]({{ site.baseurl }}{% link tutorials/layout-persistence.md %}).
- Add runtime theme switching across `VS` and classic themes in [Tutorial: Styling & Theming]({{ site.baseurl }}{% link tutorials/styling-and-theming.md %}).
