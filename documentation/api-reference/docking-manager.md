---
title: DockingManager
layout: default
parent: API Reference
nav_order: 1
description: "Complete API reference for AvalonDock.DockingManager properties, DependencyProperties, methods, and events."
---

# DockingManager
{: .no_toc }

`AvalonDock.DockingManager` (`Dirkster.AvalonDock`) is the root WPF control that renders a `LayoutRoot` tree, synchronizes MVVM bindings (`DockLayout` or `DocumentsSource`/`AnchorablesSource`), manages tear-off floating windows and standalone detached windows, coordinates drag-and-drop overlays, and applies themes.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Declaration & Inheritance

```text
System.Windows.Controls.Control
  └── AvalonDock.DockingManager
        └── AvalonDock.ToggleDockingManager
```

- **Namespace:** `AvalonDock`
- **Assembly / Package:** `AvalonDock.dll` (`Dirkster.AvalonDock`)
- **Attributes:** `[ContentProperty(nameof(Layout))]`, `[TemplatePart(Name = "PART_AutoHideArea")]`
- **Implements:** `IOverlayWindowHost`, `AvalonDock.Core.Serialization.ISerializableDockingManager`, `AvalonDock.Core.IDockingManager`

---

## Properties

### Layout, Theme & Strategy

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `Layout` | `LayoutRoot` | DP (coerced non-null) | Root of the WPF layout model tree (`[ContentProperty]`). Setting `null` coerces to a new `LayoutRoot` containing a `LayoutDocumentPaneGroup(new LayoutDocumentPane())`. |
| `DockLayout` | `IRootDock` | DP (`null`) | UI-agnostic v5 MVVM layout tree (`AvalonDock.Core.IRootDock`). When non-null, attaches `LayoutSyncBridge` and installs `DockAlignmentStrategy` if `LayoutUpdateStrategy` is unset. |
| `Theme` | `Theme` | DP (`null`) | Active theme (`ArcDarkTheme`, `ArcLightTheme`, `VS2010Theme`, etc.). Changing at runtime updates `DockingManager`, all `FloatingWindows`, all `DetachedAnchorables` windows, `NavigatorWindow`, and `OverlayWindow`. |
| `ActiveContent` | `object` | DP (`null`) | Currently active content (supports two-way binding to your active ViewModel or `LayoutContent`). |
| `LayoutUpdateStrategy` | `ILayoutUpdateStrategy` | DP (`null`) | Custom hook invoked before and after inserting documents or anchorables. |
| `LayoutEngine` | `ILayoutEngine` | `virtual` get (`DefaultLayoutEngine`) | Strategy performing structural tree insertions and orientation fixes. Overridden by `ToggleDockingManager` (`ToggleLayoutEngine`). |
| `SuspendDocumentsSourceBinding` | `bool` | CLR (`false`) | Suspends `DocumentsSource` `CollectionChanged` processing while `true` (managed automatically during deserialization). |
| `SuspendAnchorablesSourceBinding` | `bool` | CLR (`false`) | Suspends `AnchorablesSource` `CollectionChanged` processing while `true` (managed automatically during deserialization). |
| `LogicalChildrenPublic` | `IEnumerator` | CLR (read-only) | Enumerates logical children ordered by descending visual tree depth so inheritable WPF properties propagate reliably. |

### MVVM Data Binding & Item Containers

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `DocumentsSource` | `IEnumerable` | DP (`null`) | Collection of document ViewModels or controls for classic collection-bound MVVM. |
| `AnchorablesSource` | `IEnumerable` | DP (`null`) | Collection of tool window ViewModels or controls for classic collection-bound MVVM. |
| `LayoutItemTemplate` | `DataTemplate` | DP (`null`) | Default `DataTemplate` used to render the body content of both documents and anchorables. |
| `LayoutItemTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selects a content `DataTemplate` per item. |
| `LayoutItemContainerStyle` | `Style` | DP (`null`) | `Style` applied to `LayoutItem` wrappers (`LayoutDocumentItem` / `LayoutAnchorableItem`) to bind `Title`, `ContentId`, `CloseCommand`, etc. |
| `LayoutItemContainerStyleSelector` | `StyleSelector` | DP (`null`) | Selects distinct `Style`s for `LayoutDocumentItem` vs `LayoutAnchorableItem`. |

{: .warning }
There is no `DocumentTemplate` property on `DockingManager`. Use `LayoutItemTemplate`, `LayoutItemTemplateSelector`, or implicit `<DataTemplate DataType="{x:Type ...}">` resources in `Window.Resources` / `DockingManager.Resources`.

### Header, Title, Icon & Context Menu Customization

Setting a `*TemplateSelector` clears the corresponding `*Template` property (and vice versa) so only one is active at a time:

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `DocumentHeaderTemplate` | `DataTemplate` | DP (`null`) | Template for document tab headers. |
| `DocumentHeaderTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for document tab headers (also populates `DocumentPaneMenuItemHeaderTemplateSelector` if unset). |
| `DocumentTitleTemplate` | `DataTemplate` | DP (`null`) | Template for document titles (for example, in floating window title bars). |
| `DocumentTitleTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for document titles. |
| `DocumentPaneMenuItemHeaderTemplate` | `DataTemplate` | DP (coerces to `DocumentHeaderTemplate`) | Template for items in the document pane overflow dropdown menu. |
| `DocumentPaneMenuItemHeaderTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for items in the document pane overflow dropdown menu. |
| `AnchorableHeaderTemplate` | `DataTemplate` | DP (`null`) | Template for docked anchorable tab headers. |
| `AnchorableHeaderTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for docked anchorable tab headers. |
| `AnchorableTitleTemplate` | `DataTemplate` | DP (`null`) | Template for anchorable pane caption bars. |
| `AnchorableTitleTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for anchorable pane caption bars. |
| `IconContentTemplate` | `DataTemplate` | DP (`null`) | Template for rendering content icons. |
| `IconContentTemplateSelector` | `DataTemplateSelector` | DP (`null`) | Selector for content icons. |
| `DocumentContextMenu` | `ContextMenu` | DP (`null`) | Custom right-click `ContextMenu` for document tabs (automatically inherits `DockingManager.Resources`). |
| `AnchorableContextMenu` | `ContextMenu` | DP (`null`) | Custom right-click `ContextMenu` for anchorable headers/tabs (automatically inherits `DockingManager.Resources`). |

### Pane Control Templates, Styles & Grid Splitters

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `DocumentPaneTemplate` | `ControlTemplate` | DP (`null`) | `ControlTemplate` for `LayoutDocumentPaneControl`. |
| `AnchorablePaneTemplate` | `ControlTemplate` | DP (`null`) | `ControlTemplate` for `LayoutAnchorablePaneControl`. |
| `AnchorSideTemplate` | `ControlTemplate` | DP (`null`) | `ControlTemplate` for `LayoutAnchorSideControl`. |
| `AnchorGroupTemplate` | `ControlTemplate` | DP (`null`) | `ControlTemplate` for `LayoutAnchorGroupControl`. |
| `AnchorTemplate` | `ControlTemplate` | DP (`null`) | `ControlTemplate` for `LayoutAnchorControl` (individual auto-hide edge tabs). |
| `DocumentPaneControlStyle` | `Style` | DP (`null`) | `Style` applied to `LayoutDocumentPaneControl`. |
| `AnchorablePaneControlStyle` | `Style` | DP (`null`) | `Style` applied to `LayoutAnchorablePaneControl`. |
| `GridSplitterWidth` | `double` | DP (`6.0`) | Width in pixels of vertical `LayoutGridResizerControl` splitters. |
| `GridSplitterHeight` | `double` | DP (`6.0`) | Height in pixels of horizontal `LayoutGridResizerControl` splitters. |
| `GridSplitterVerticalStyle` | `Style` | DP (`null`) | Custom `Style` for vertical `LayoutGridResizerControl` splitters. |
| `GridSplitterHorizontalStyle` | `Style` | DP (`null`) | Custom `Style` for horizontal `LayoutGridResizerControl` splitters. |

### Behavior, Keyboard, Virtualization & Windowing

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `IgnoreTabControlKeyBindings` | `bool` | DP (`false`) | When `true`, prevents the internal document `TabControlEx` from handling navigation keys (`Home`, `End`, `PageUp`, `PageDown`, arrows) so hosted editors and data grids receive them unimpeded. |
| `IsVirtualizingDocument` | `bool` | CLR (`true`) | Virtualizes inactive tab visuals inside `LayoutDocumentPaneControl` so switching tabs does not tear down/rebuild active presenters unnecessarily. |
| `IsVirtualizingAnchorable` | `bool` | CLR (`true`) | Virtualizes inactive tab visuals inside `LayoutAnchorablePaneControl`. |
| `ShowNavigator` | `bool` | DP (`true`) | Enables the `Ctrl+Tab` `NavigatorWindow` popup for keyboard switching between documents and tool windows. |
| `AllowMixedOrientation` | `bool` | DP (`false`) | Allows splitting document panes perpendicular to their parent group's current orientation. |
| `AllowFloatingWindows` | `bool` | DP (`true`) | Global switch for tear-off floating windows. Setting to `false` docks all open floating windows immediately (`DockAllFloatingWindows()`), disables Float commands, and docks floating windows in subsequently loaded layouts. |
| `AllowDetachedWindows` | `bool` | DP (`true`) | Global switch for standalone OS tool windows (`DetachedAnchorableWindow`). Setting to `false` returns all detached panels (`ReattachAllDetachedAnchorables()`) and keeps detached panels docked on layout load. |
| `AutoWindowSizeWhenOpened` | `bool` | DP (`false`) | Sizes newly dragged floating windows from their content's minimum size and margins. |
| `AllowMovingFloatingWindowWithKeyboard` | `bool` | DP (`false`) | Allows moving a focused floating window with keyboard arrow keys. |
| `ShowSystemMenu` | `bool` | DP (`true`) | Shows the standard window system menu on floating windows when a custom context menu is not set. |
| `AutoHideDelay` | `int` | DP (`500`) | Delay in milliseconds before an expanded auto-hide flyout slides closed after losing focus (recommended range: `0`–`1500`). |
| `SupportsAutoHideFlyout` | `bool` | CLR (`true`, `protected set`) | Whether auto-hidden anchorables open in the classic slide-out flyout (`false` on `ToggleDockingManager`). |
| `AllowAnchorDoubleClickDock` | `bool` | DP (`false`) | When `true`, double-clicking an auto-hide edge tab toggles it back to its docked (pinned) pane. |
| `AllowAnchorRightClickContextMenu` | `bool` | DP (`false`) | When `true`, right-clicking an auto-hide edge tab opens `AnchorableContextMenu`. |

### Visual Tree & Active Window Collections

| Property | Type | Kind | Description |
|:---------|:-----|:-----|:------------|
| `FloatingWindows` | `IEnumerable<LayoutFloatingWindowControl>` | CLR (read-only) | Active `LayoutFloatingWindowControl` windows currently managed by this `DockingManager`. |
| `DetachedAnchorables` | `IEnumerable<LayoutAnchorable>` | CLR (read-only) | `LayoutAnchorable` models currently hosted in standalone `DetachedAnchorableWindow` OS windows. |
| `AutoHideWindow` | `LayoutAutoHideWindowControl` | Read-only DP | Currently active auto-hide flyout control. |
| `LayoutRootPanel` | `LayoutPanelControl` | DP | Root `LayoutPanelControl` visual generated for `Layout.RootPanel`. |
| `LeftSidePanel` / `RightSidePanel` / `TopSidePanel` / `BottomSidePanel` | `LayoutAnchorSideControl` | DP | Visual side strip controls generated for the four `LayoutAnchorSide` properties of `LayoutRoot`. |

---

## Public Methods

| Method | Return Type | Description |
|:-------|:------------|:------------|
| `GetLayoutItemFromModel(LayoutContent content)` | `LayoutItem` | Returns the `LayoutDocumentItem` or `LayoutAnchorableItem` wrapper associated with `content` (exposing commands such as `CloseCommand`, `FloatCommand`, `HideCommand`, and `DetachToWindowCommand`). |
| `CreateFloatingWindow(LayoutContent contentModel, bool isContentImmutable)` | `LayoutFloatingWindowControl` | Moves `contentModel` into a new `LayoutFloatingWindowControl` (or returns `null` when `AllowFloatingWindows` is `false` or `!contentModel.CanFloat`). |
| `DockAllFloatingWindows()` | `void` | Docks the content of every floating window back into the main layout (using each item's `PreviousContainer` or creating a main-window pane) and closes all emptied floating windows. |
| `DetachAnchorableToWindow(LayoutAnchorable anchorable)` | `void` | Moves the content presenter of `anchorable` into a standalone top-level `DetachedAnchorableWindow` with its own taskbar entry and independent minimize/z-order state. Does nothing when `AllowDetachedWindows` is `false`. |
| `ReattachAnchorable(LayoutAnchorable anchorable)` | `void` | Closes the standalone `DetachedAnchorableWindow` for `anchorable` and returns its content to its remembered layout position. |
| `ReattachAllDetachedAnchorables()` | `void` | Closes all standalone detached windows and returns every detached anchorable to the layout. |
| `IsDetached(LayoutAnchorable anchorable)` | `bool` | Returns `true` if `anchorable` is currently hosted in a standalone `DetachedAnchorableWindow`. |

---

## Events Overview

| Category | Events |
|:---------|:-------|
| **Documents** | `DocumentClosing` (`DocumentClosingEventArgs`, cancellable), `DocumentClosed` (`DocumentClosedEventArgs`) |
| **Anchorables** | `AnchorableClosing` (`AnchorableClosingEventArgs`, cancellable), `AnchorableClosed` (`AnchorableClosedEventArgs`), `AnchorableHiding` (`AnchorableHidingEventArgs`, cancellable + `CloseInsteadOfHide`), `AnchorableHidden` (`AnchorableHiddenEventArgs`) |
| **Floating & Docking** | `ContentFloating` (`ContentFloatingEventArgs`, cancellable), `ContentFloated` (`ContentFloatedEventArgs`), `ContentDocking` (`ContentDockingEventArgs`, cancellable), `ContentDocked` (`ContentDockedEventArgs`), `LayoutFloatingWindowControlCreated`, `LayoutFloatingWindowControlClosed` |
| **Layout & Focus** | `ActiveContentChanged` (`EventArgs`), `LayoutChanging` (`EventArgs`), `LayoutChanged` (`EventArgs`) |

`DockingManager` also implements the UI-agnostic events on `AvalonDock.Core.IDockingManager`. See the [Events API Reference]({{ site.baseurl }}{% link api-reference/events.md %}) for full signatures and examples.

---

## XAML Usage Example

```xml
<avalonDock:DockingManager
    x:Name="dockManager"
    DocumentsSource="{Binding Documents}"
    AnchorablesSource="{Binding Tools}"
    ActiveContent="{Binding ActiveDocument, Mode=TwoWay}"
    LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}"
    IgnoreTabControlKeyBindings="True"
    AllowMixedOrientation="True"
    AllowAnchorDoubleClickDock="True"
    DocumentClosing="OnDocumentClosing"
    AnchorableHiding="OnAnchorableHiding">

    <avalonDock:DockingManager.Theme>
        <themes:ArcDarkTheme />
    </avalonDock:DockingManager.Theme>

    <avalonDock:DockingManager.LayoutItemTemplate>
        <DataTemplate>
            <ContentPresenter Content="{Binding}" />
        </DataTemplate>
    </avalonDock:DockingManager.LayoutItemTemplate>

    <avalonDock:LayoutRoot>
        <avalonDock:LayoutPanel Orientation="Horizontal">
            <avalonDock:LayoutAnchorablePane Name="LeftTools" DockWidth="250" />
            <avalonDock:LayoutDocumentPane />
        </avalonDock:LayoutPanel>
    </avalonDock:LayoutRoot>
</avalonDock:DockingManager>
```

---

## C# Usage Example

```csharp
using System.Windows;
using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Themes;

var dockManager = new DockingManager
{
    Theme = new ArcDarkTheme(),
    IgnoreTabControlKeyBindings = true,
    ShowNavigator = true,
    Layout = new LayoutRoot
    {
        RootPanel = new LayoutPanel(new LayoutDocumentPaneGroup(new LayoutDocumentPane()))
    }
};

dockManager.DocumentClosing += (_, args) =>
{
    if (args.Document.Content is IEditableDocument doc && doc.IsDirty)
    {
        var choice = MessageBox.Show(
            $"Save changes to '{args.Document.Title}'?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel);

        if (choice == MessageBoxResult.Cancel)
        {
            args.Cancel = true;
        }
    }
};
```
