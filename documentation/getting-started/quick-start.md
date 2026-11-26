---
title: Quick Start
layout: default
parent: Getting Started
nav_order: 2
description: "Build your first AvalonDock v5 docking layout in 5 minutes."
---

# Quick Start

This guide walks you through creating a complete WPF docking layout with tool windows, tabbed documents, an auto-hidden bottom panel, and the modern Arc Dark theme in under 5 minutes.

---

## Step 1: Create a WPF Project

Create a new WPF application targeting `.NET 9` or `.NET 10` (or `.NET 8` / `.NET 11`):

```bash
dotnet new wpf -n MyDockingApp --framework net10.0-windows
cd MyDockingApp
```

## Step 2: Install AvalonDock and a Theme

```bash
dotnet add package Dirkster.AvalonDock
dotnet add package Dirkster.AvalonDock.Themes.Arc
```

## Step 3: Define the Docking Layout in XAML

Replace the contents of `MainWindow.xaml` with:

```xml
<Window x:Class="MyDockingApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:avalonDock="https://github.com/Dirkster99/AvalonDock"
        xmlns:themes="clr-namespace:AvalonDock.Themes;assembly=AvalonDock.Themes.Arc"
        Title="My Docking App" Height="600" Width="900">

    <avalonDock:DockingManager x:Name="dockManager">
        <!-- Apply the Arc Dark theme -->
        <avalonDock:DockingManager.Theme>
            <themes:ArcDarkTheme />
        </avalonDock:DockingManager.Theme>

        <avalonDock:LayoutRoot>
            <avalonDock:LayoutPanel Orientation="Horizontal">

                <!-- Left: Tool Window -->
                <avalonDock:LayoutAnchorablePane DockWidth="220">
                    <avalonDock:LayoutAnchorable Title="Explorer"
                                                  ContentId="explorer"
                                                  CanClose="False">
                        <TreeView>
                            <TreeViewItem Header="Solution" IsExpanded="True">
                                <TreeViewItem Header="MyDockingApp" IsExpanded="True">
                                    <TreeViewItem Header="MainWindow.xaml" />
                                    <TreeViewItem Header="App.xaml" />
                                </TreeViewItem>
                            </TreeViewItem>
                        </TreeView>
                    </avalonDock:LayoutAnchorable>
                </avalonDock:LayoutAnchorablePane>

                <!-- Center: Tabbed Documents -->
                <avalonDock:LayoutDocumentPane>
                    <avalonDock:LayoutDocument Title="Welcome"
                                               ContentId="welcome">
                        <TextBlock Text="Welcome to AvalonDock v5!"
                                   FontSize="24"
                                   HorizontalAlignment="Center"
                                   VerticalAlignment="Center" />
                    </avalonDock:LayoutDocument>
                    <avalonDock:LayoutDocument Title="Document1.txt"
                                               ContentId="doc1">
                        <TextBox AcceptsReturn="True"
                                 Text="Type your content here..."
                                 FontFamily="Consolas" />
                    </avalonDock:LayoutDocument>
                </avalonDock:LayoutDocumentPane>

                <!-- Right: Properties Tool Window -->
                <avalonDock:LayoutAnchorablePane DockWidth="200">
                    <avalonDock:LayoutAnchorable Title="Properties"
                                                  ContentId="properties"
                                                  CanClose="False">
                        <StackPanel Margin="8">
                            <TextBlock Text="Properties Panel"
                                       FontWeight="Bold" Margin="0,0,0,8" />
                            <TextBlock Text="Name:" />
                            <TextBox Margin="0,2,0,8" />
                            <TextBlock Text="Value:" />
                            <TextBox Margin="0,2,0,8" />
                        </StackPanel>
                    </avalonDock:LayoutAnchorable>
                </avalonDock:LayoutAnchorablePane>

            </avalonDock:LayoutPanel>

            <!-- Bottom: Output Panel (auto-hidden on startup) -->
            <avalonDock:LayoutRoot.BottomSide>
                <avalonDock:LayoutAnchorSide>
                    <avalonDock:LayoutAnchorGroup>
                        <avalonDock:LayoutAnchorable Title="Output"
                                                      ContentId="output">
                            <TextBox AcceptsReturn="True"
                                     IsReadOnly="True"
                                     Text="Build started...&#x0a;Build succeeded."
                                     FontFamily="Consolas" />
                        </avalonDock:LayoutAnchorable>
                    </avalonDock:LayoutAnchorGroup>
                </avalonDock:LayoutAnchorSide>
            </avalonDock:LayoutRoot.BottomSide>
        </avalonDock:LayoutRoot>
    </avalonDock:DockingManager>
</Window>
```

## Step 4: Run the Application

```bash
dotnet run
```

You now have a working Visual Studio-style layout featuring:
- An **Explorer** tool window docked on the left
- **Tabbed documents** in the center workspace
- A **Properties** tool window docked on the right
- An **auto-hidden Output** tool window pinned to the bottom edge

Try dragging tabs to split panes, tearing panels off into floating windows, or detaching a tool window into its own top-level OS window via **View Mode → Window** in the tool header options menu.

{: .tip }
Prefer a modern **VS Code / JetBrains Rider-style** interface with compact sidebar toggle buttons and Dependency Injection instead of classic auto-hide flyouts? Check out the [ToggleDockingManager Guide]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) and [Tutorial: Build an MVVM IDE Application]({{ site.baseurl }}{% link tutorials/mvvm-ide.md %}).

---

## What's Next?

- Understand how `LayoutRoot`, `LayoutPanel`, and panes fit together in [Layout Model]({{ site.baseurl }}{% link concepts/layout-model.md %}).
- Bind documents and toolboxes cleanly using view models and DI in [MVVM Integration]({{ site.baseurl }}{% link guides/mvvm.md %}) and [Dependency Injection]({{ site.baseurl }}{% link guides/dependency-injection.md %}).
- Persist user layouts across sessions with XML in [Layout Serialization]({{ site.baseurl }}{% link guides/serialization.md %}).
- Explore all 5 theme packages in [Themes]({{ site.baseurl }}{% link themes/index.md %}).
