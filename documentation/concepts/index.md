---
title: Concepts
layout: default
nav_order: 3
has_children: true
description: "Core architecture, layout tree model, dockable content types, and windowing modes in AvalonDock v5."
permalink: /concepts/
---

# Concepts
{: .no_toc }

Understand how AvalonDock v5 separates its UI-agnostic core from the WPF layout tree, how panels and panes nest to form complex workspaces, and how documents, tool windows, floating windows, and standalone detached windows behave.
{: .fs-6 .fw-300 }

---

## Core Topics

| Page | Summary |
|:-----|:--------|
| [Architecture]({{ site.baseurl }}{% link concepts/architecture.md %}) | The 13-package v5 architecture: `AvalonDock.Core`, `AvalonDock.Mvvm`, `LayoutSyncBridge`, `ILayoutEngine`, DTO serialization, and theme layers. |
| [Layout Model]({{ site.baseurl }}{% link concepts/layout-model.md %}) | How `LayoutRoot`, `LayoutPanel`, pane groups, panes, auto-hide sides, `Hidden`, and `FloatingWindows` form a live layout tree—and how `ILayoutEngine` and `ILayoutUpdateStrategy` manipulate it. |
| [Documents & Anchorables]({{ site.baseurl }}{% link concepts/documents-and-anchorables.md %}) | Differences between `LayoutDocument` and `LayoutAnchorable`, programmatic placement (`AddToLayout` vs `Show`), auto-hide (`ToggleAutoHide` vs `ToggleSingleAutoHide`), and lifecycle events. |
| [Floating & Detached Windows]({{ site.baseurl }}{% link concepts/floating-windows.md %}) | Comparing AvalonDock tear-off `LayoutFloatingWindowControl` instances with standalone OS-level `DetachedAnchorableWindow` windows, multi-monitor clamping, and global switches. |

---

## Choosing an Authoring Model

AvalonDock v5 supports three ways to build and drive a docking workspace—all backed by the same `LayoutRoot` model at runtime:

| Approach | Entry Point | Best For |
|:---------|:------------|:---------|
| **Modern v5 MVVM + DI** | `DockLayout` (`IRootDock` / `IDockLayoutService`) on `ToggleDockingManager` or `DockingManager` | Testable ViewModels (`IDockable`, `IToolbox`), Rider/VS Code-style sidebars, and `Microsoft.Extensions.DependencyInjection` apps — see the [MVVM Guide]({{ site.baseurl }}{% link guides/mvvm.md %}) and [Dependency Injection Guide]({{ site.baseurl }}{% link guides/dependency-injection.md %}). |
| **Classic Collection MVVM** | `DocumentsSource` + `AnchorablesSource` + `LayoutItemContainerStyleSelector` | Existing AvalonDock v4 MVVM apps binding `ObservableCollection<T>` directly to `DockingManager`. |
| **Declarative XAML / Code-Behind** | `<avalonDock:LayoutRoot>` in XAML or `new LayoutRoot()` in C# | Static tool layouts, rapid prototypes, or custom dynamic layout builders. |
