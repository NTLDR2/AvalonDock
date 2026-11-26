---
title: Controls
layout: default
parent: API Reference
nav_order: 4
description: "Complete API reference for public WPF controls in AvalonDock.Controls and XAML value converters in AvalonDock.Converters."
---

# Controls & Converters
{: .no_toc }

`AvalonDock.Controls` contains the WPF visual elements, item container wrappers, floating and standalone windows, sidebar toggle bars, and drag-drop overlay hosts that render a `LayoutRoot` tree, paired with XAML `MarkupExtension` converters in `AvalonDock.Converters`.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Item Container Wrappers (`LayoutItem`)

When `DockingManager` attaches a `LayoutDocument` or `LayoutAnchorable`, it creates a corresponding `LayoutDocumentItem` or `LayoutAnchorableItem` (`DataContext = this`, exposing your ViewModel or control on `Model`). Style these wrappers via `DockingManager.LayoutItemContainerStyle` or `LayoutItemContainerStyleSelector`—or retrieve them in code with `dockManager.GetLayoutItemFromModel(layoutContent)`.

### `LayoutItem` (`public abstract class : FrameworkElement`)

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `LayoutElement` | `LayoutContent` | CLR (read-only) | Underlying `LayoutDocument` or `LayoutAnchorable` model. |
| `Model` | `object` | CLR (read-only) | User content (`LayoutElement.Content`) — typically your MVVM ViewModel (`{Binding Model.*}` in container styles). |
| `View` | `ContentPresenter` | CLR (read-only) | Single `ContentPresenter` hosting `LayoutElement.Content` with `LayoutItemTemplate` / `LayoutItemTemplateSelector`. |
| `Title` | `string` | DP (`null`) | Display title synced with `LayoutElement.Title`. |
| `ContentId` | `string` | DP (`null`) | Serialization identifier synced with `LayoutElement.ContentId`. |
| `IconSource` | `ImageSource` | DP (`null`) | Tab/header icon synced with `LayoutElement.IconSource`. |
| `ToolTip` | `object` | DP (`null`) | Inherited `FrameworkElement.ToolTip` with metadata override forwarding changes to `LayoutElement.ToolTip`. |
| `Visibility` | `Visibility` | DP (`Visible`) | Synced with model visibility (`Collapsed` closes a `LayoutDocument`; `Hidden`/`Collapsed` hides a `LayoutAnchorable`). |
| `IsSelected` | `bool` | DP (`false`) | Two-way synced with `LayoutElement.IsSelected`. |
| `IsActive` | `bool` | DP (`false`) | Two-way synced with `LayoutElement.IsActive`. |
| `CanClose` | `bool` | DP (`true` on `LayoutItem`, `false` on `LayoutAnchorableItem`) | Whether closing is permitted. Seeded via `SetCurrentValue` so a `<Setter Property="CanClose" ... />` in `LayoutItemContainerStyle` always takes precedence over the model default. |
| `CanFloat` | `bool` | DP (`true`) | Whether tearing off into a floating window is permitted. |

#### Commands on `LayoutItem`

All commands are DependencyProperties populated with built-in defaults that can be overridden via style bindings:

| Command Property | Default Behavior |
|:-----------------|:-----------------|
| `CloseCommand` | Closes the item when `LayoutElement.CanClose` is `true` (raises `DocumentClosing` / `AnchorableClosing`). |
| `FloatCommand` | Tears the item off into a `LayoutFloatingWindowControl` when `AllowFloatingWindows && CanFloat` and not already floating. |
| `DockAsDocumentCommand` | Docks the item into a main-window `LayoutDocumentPane`. |
| `CloseAllButThisCommand` | Closes all other open documents in document panes and floating document windows. |
| `CloseAllCommand` | Closes all open documents across document panes and floating document windows. |
| `ActivateCommand` | Sets `LayoutElement.IsActive = true`. |
| `NewVerticalTabGroupCommand` | Splits the current `LayoutDocumentPane` side-by-side (`Orientation.Horizontal`) and moves this document into the new pane. |
| `NewHorizontalTabGroupCommand` | Splits the current `LayoutDocumentPane` top-to-bottom (`Orientation.Vertical`) and moves this document into the new pane. |
| `MoveToNextTabGroupCommand` | Moves this document to the next sibling `LayoutDocumentPane` in its `LayoutDocumentPaneGroup`. |
| `MoveToPreviousTabGroupCommand` | Moves this document to the previous sibling `LayoutDocumentPane` in its `LayoutDocumentPaneGroup`. |

### `LayoutAnchorableItem` (`public class : LayoutItem`)

Overrides `CanCloseProperty` default metadata to `false` and adds tool-window properties and commands:

| Member | Type | Kind / Default | Description |
|:-------|:-----|:---------------|:------------|
| `CanHide` | `bool` | DP (`true`) | Synced with `LayoutAnchorable.CanHide`. |
| `CanMove` | `bool` | DP (`true`) | Synced with `LayoutAnchorable.CanMove`. |
| `HideCommand` | `ICommand` | DP | Hides the anchorable into `LayoutRoot.Hidden` (`AnchorableHiding` → `AnchorableHidden`). |
| `AutoHideCommand` | `ICommand` | DP | Toggles auto-hide when docked/auto-hidden and `CanAutoHide` is `true`. |
| `DockCommand` | `ICommand` | DP | Docks a floating anchorable back into the main layout (`ExecuteDockCommand`). |
| `DetachToWindowCommand` | `ICommand` | DP | **Toggles standalone OS window mode ("Window" view mode):** calls `DetachAnchorableToWindow` when docked, or `ReattachAnchorable` when already detached. Enabled when `AllowDetachedWindows && CanFloat`. |

### `LayoutDocumentItem` (`public class : LayoutItem`)

| Member | Type | Kind / Default | Description |
|:-------|:-----|:---------------|:------------|
| `Description` | `string` | DP (`null`) | Synced with `LayoutDocument.Description` (displayed in `NavigatorWindow` details). |

---

## Floating & Standalone Window Controls

### `LayoutFloatingWindowControl` (`public abstract class : Window, ILayoutControl`)

Base class for tear-off floating windows (`ShowInTaskbar = false` by default).

| Member | Type | Kind / Default | Description |
|:-------|:-----|:---------------|:------------|
| `Model` | `ILayoutElement` | `abstract` get | Underlying `LayoutFloatingWindow` model. |
| `OwnedByDockingManagerWindow` | `bool` | DP (`true`) | When `true`, assigns the host `Window` as `Owner`; set to `false` to decouple minimize/z-order from the main window. |
| `AllowMinimize` | `bool` | DP (`false`) | Allows minimizing the floating window when `true`. |
| `ResizeBorderThickness` | `Thickness` | DP (`default`) | Overrides `WindowChrome.ResizeBorderThickness` when non-default. |
| `IsMaximized` | `bool` | DP (`false`, `private set`) | Reflects and persists maximized window state. |
| `IsDragging` | `bool` | Read-only DP (`false`) | `true` while the floating window is being dragged by the user. |
| `IsContentImmutable` | `bool` | DP (`false`, `private set`) | Prevents replacing the `FloatingWindowContentHost` once loaded. |
| `TotalMargin` | `Thickness` | Read-only DP | Computed chrome/border/title margin used when `AutoWindowSizeWhenOpened` is enabled. |
| `ContentMinWidth` / `ContentMinHeight` | `double` | Read-only DPs (`0.0`) | Aggregated minimum dimensions of hosted `FrameworkElement` content. |
| `EnableBindings()` / `DisableBindings()` | `virtual void` | methods | Attaches or suspends model visibility/update bindings when `DockingManager` loads/unloads. |
| `FloatingWindowContentHost` | `HwndHost` | `protected internal` nested class | Child `HwndHost` presenting `RootVisual`, `Content` (`UIElement` DP), and `SizeToContent` (`SizeToContent` DP). |

### `LayoutAnchorableFloatingWindowControl` & `LayoutDocumentFloatingWindowControl`

| Control | Base Class | Public Members |
|:--------|:-----------|:---------------|
| `LayoutAnchorableFloatingWindowControl` | `LayoutFloatingWindowControl` | `Model` (`ILayoutElement`), `SingleContentLayoutItem` (`LayoutItem` DP — selected item when `IsSinglePane`), `HideWindowCommand` (`ICommand`), `CloseWindowCommand` (`ICommand`). |
| `LayoutDocumentFloatingWindowControl` | `LayoutFloatingWindowControl` | `Model` (`ILayoutElement`), `SingleContentLayoutItem` (`LayoutItem` DP), `HideWindowCommand` (`ICommand`), `CloseWindowCommand` (`ICommand`), `HideOverlayWindow()` (`void`), `GetDropAreas(LayoutFloatingWindowControl)` (`IEnumerable<IDropArea>`). |

### `DetachedAnchorableWindow` (`public class : Window`)

Standalone top-level OS window (`Owner = null`, `ShowInTaskbar = true`, `WindowStyle = SingleBorderWindow`, `ResizeMode = CanResize`) used by `DockingManager.DetachAnchorableToWindow`:

| Member | Signature | Description |
|:-------|:----------|:------------|
| Constructor | `DetachedAnchorableWindow(LayoutAnchorable model, ContentPresenter hostedView, FrameworkElement header = null)` | Hosts a disconnected `LayoutItem.View` presenter beneath an optional caption header, restoring saved bounds (or centering if off-screen). |
| `Model` | `LayoutAnchorable` | The anchorable whose content is hosted in this window. |
| `HasView` | `bool` | `true` while `hostedView` has not yet been released. |
| `IsClosed` | `bool` | `true` once `OnClosed` has fired. |
| `ReleaseView()` | `ContentPresenter` | Removes and returns `hostedView` so `DockingManager` can reinsert it into the docked layout. |
| `UpdateThemeResources(Theme oldTheme, Theme newTheme)` | `void` | Swaps merged theme `ResourceDictionary` entries on this standalone window when `DockingManager.Theme` changes. |

---

## Container, Pane & Splitter Controls

| Control | Base Class | Key Public Members & Purpose |
|:--------|:-----------|:-----------------------------|
| `LayoutDocumentControl` | `Control` | Hosts a single document's view (`Model` `LayoutContent` DP, `LayoutItem` read-only DP). |
| `LayoutAnchorableControl` | `Control` | Hosts a single anchorable's view (`Model` `LayoutAnchorable` DP, `LayoutItem` read-only DP). |
| `TabControlEx` | `TabControl` | Virtualizing tab control (`PART_ItemsHolder` `Panel`). Exposes `IsVirtualiting` (`bool`) and `IgnoreTabControlKeyBindings` (`bool` — bypasses `TabControl.OnKeyDown` when `DockingManager.IgnoreTabControlKeyBindings` is `true`). |
| `LayoutDocumentPaneControl` | `TabControlEx`, `ILayoutControl` | Visual tab control for a `LayoutDocumentPane` (`Model` `ILayoutElement`). Styled via `DockingManager.DocumentPaneControlStyle` / `DocumentPaneTemplate`. |
| `LayoutAnchorablePaneControl` | `TabControlEx`, `ILayoutControl` | Visual tab control for a `LayoutAnchorablePane` (`Model` `ILayoutElement`). Styled via `DockingManager.AnchorablePaneControlStyle` / `AnchorablePaneTemplate`. |
| `LayoutGridControl<T>` | `Grid`, `ILayoutControl`, `IAdjustableSizeLayout` | Abstract dynamic `Grid` interleaving child pane visuals with `LayoutGridResizerControl` splitters (`Model`, `Orientation`, `AdjustFixedChildrenPanelSizes(Size?)`). |
| `LayoutPanelControl` | `LayoutGridControl<ILayoutPanelElement>` | Visual grid container for `LayoutPanel`. |
| `LayoutDocumentPaneGroupControl` | `LayoutGridControl<ILayoutDocumentPane>` | Visual splitter container for `LayoutDocumentPaneGroup`. |
| `LayoutAnchorablePaneGroupControl` | `LayoutGridControl<ILayoutAnchorablePane>` | Visual splitter container for `LayoutAnchorablePaneGroup`. |
| `LayoutGridResizerControl` | `Thumb` | Interactive splitter thumb between docked panes/panels. Exposes `BackgroundWhileDragging` (`Brush` DP, default `Brushes.Black`) and `OpacityWhileDragging` (`double` DP, default `0.5`). |

---

## Auto-Hide, Header & Tab Strip Controls

### Auto-Hide & Header Controls

| Control | Base Class | Key Public Members & Purpose |
|:--------|:-----------|:-----------------------------|
| `LayoutAnchorSideControl` | `Control`, `ILayoutControl` | Edge strip hosting `Children` (`ObservableCollection<LayoutAnchorGroupControl>`), `Model`, and read-only DPs `IsLeftSide`, `IsTopSide`, `IsRightSide`, `IsBottomSide`. |
| `LayoutAnchorGroupControl` | `Control`, `ILayoutControl` | Visual group on an anchor side (`Model`, `Children` `ObservableCollection<LayoutAnchorControl>`). |
| `LayoutAnchorControl` | `Control`, `ILayoutControl` | Individual auto-hide edge tab (`Model`, `Side` `AnchorSide` read-only DP). Supports hover expand (`CanShowOnHover`), `AllowAnchorDoubleClickDock`, and `AllowAnchorRightClickContextMenu`. |
| `LayoutAutoHideWindowControl` | `HwndHost`, `ILayoutControl` | Slide-out Win32-hosted flyout window for auto-hidden panels (`Model`, `Background` `Brush` DP, `AnchorableStyle` `Style` DP for the inner `LayoutAnchorableControl`, and `AnchorableGridStyle` `Style` DP for styling the enclosing `Grid`). |
| `AnchorablePaneTitle` | `Control` | Draggable title bar of a docked, floating, or detached anchorable (`Model` `LayoutAnchorable` DP, `LayoutItem` read-only DP). |

### Tab Items & Tab Strip Panels

| Control | Base Class | Key Public Members & Purpose |
|:--------|:-----------|:-----------------------------|
| `LayoutDocumentTabItem` | `ContentControl` | Document tab header (`Model` `LayoutContent` DP, `LayoutItem` read-only DP). Supports middle-click close (`CloseCommand`), drag reordering, and drag-out floating. |
| `LayoutAnchorableTabItem` | `Control` | Tool-window tab header (`Model` `LayoutContent` DP, `LayoutItem` read-only DP). Supports drag reordering and drag-out floating. |
| `DocumentPaneTabPanel` | `Panel` | Horizontal tab strip for document panes; hides overflowing tabs and promotes a selected hidden tab to index `0`. |
| `AnchorablePaneTabPanel` | `Panel` | Horizontal tab strip for anchorable panes; shrinks tab widths evenly when total desired width exceeds available width. |

---

## Toggle Docking Controls (`ToggleDockingManager`)

Used by `AvalonDock.ToggleDockingManager` for Rider / VS Code-style click-to-toggle sidebars:

| Type | Kind | Summary |
|:-----|:-----|:--------|
| `ToggleDock` | `static class` | Attached properties for XAML-declared `LayoutAnchorable` elements (`Zone`, `Order`, `Icon`, `ToolTip`, `Shortcut`, `IsOpenByDefault`). |
| `ToggleDockButtonBar` | `ItemsControl` | Zone button strip (`Zone`, `ButtonSize`, `Buttons`, `ButtonToggled`, `ToolboxDraggedToZone`). |
| `ToggleDockButton` | `ToggleButton` | Individual sidebar toggle button (`Toolbox`, `Anchorable`, `Icon`, `Title`, `ToolTipText`, `Zone`, `ButtonSize`, `ShowTitle`); appends `" (<Shortcut>)"` to tooltips automatically when `IToolbox.Shortcut` is set. |
| `ToggleAnchorablePaneTitle` | `AnchorablePaneTitle` | Compact header with minimize (`—`) and options (`⋮`) menu (`ShowMinimizeButton`, `ShowOptionsButton`). |
| `NullToFalseConverter` | `IValueConverter` | Sealed converter returning `false` when `null`, otherwise `System.Convert.ToBoolean(value)`. |

See the [ToggleDockingManager API Reference]({{ site.baseurl }}{% link api-reference/toggle-docking-manager.md %}) for full member signatures.

---

## Navigator, Overlay & Menu Utility Controls

### `NavigatorWindow` (`public class : Window`)

Popup window displayed when pressing `Ctrl+Tab` (while `DockingManager.ShowNavigator` is `true`). Supports `Tab` / `Up` / `Down` to cycle items and `Left` / `Right` to jump between the Documents and Tool Windows lists.

- **Template Parts:** `PART_AnchorableListBox` (`ListBox`), `PART_DocumentListBox` (`ListBox`)

| Property | Type | Kind / Default | Description |
|:---------|:-----|:---------------|:------------|
| `DocumentsLabel` | `string` | DP (localized `"Active Files"`) | Customizable header text displayed above the documents list. |
| `AnchorablesLabel` | `string` | DP (localized `"Active Tool Windows"`) | Customizable header text displayed above the tool windows list. |
| `Documents` | `LayoutDocumentItem[]` | Read-only DP | Open documents ordered by `LastActivationTimeStamp` descending. |
| `Anchorables` | `IEnumerable<LayoutAnchorableItem>` | Read-only DP | Visible anchorables ordered by `LastActivationTimeStamp` descending. |
| `SelectedDocument` | `LayoutDocumentItem` | DP (`null`) | Currently highlighted document item. |
| `SelectedAnchorable` | `LayoutAnchorableItem` | DP (`null`) | Currently highlighted anchorable item. |

```xml
<!-- Customize NavigatorWindow labels via an implicit Style in DockingManager.Resources -->
<Style TargetType="{x:Type avalonDockControls:NavigatorWindow}"
       BasedOn="{StaticResource {x:Type avalonDockControls:NavigatorWindow}}">
    <Setter Property="DocumentsLabel" Value="Open Editors" />
    <Setter Property="AnchorablesLabel" Value="Panels &amp; Views" />
</Style>
```

### `OverlayWindow` & Public Drop-Area Types

| Type | Kind | Public Members & Template Parts |
|:-----|:-----|:--------------------------------|
| `OverlayWindow` | `class : Window` | Transparent drag-drop indicator window (`IsHostedInFloatingWindow` `bool`). Styled in themes via template parts: `PART_DropTargetsContainer` (`Canvas`), `PART_DockingManagerDropTargets` (`Grid` + 4 `PART_DockingManagerDropTarget*` elements), `PART_AnchorablePaneDropTargets` (`Grid` + 5 `PART_AnchorablePaneDropTarget*` elements), `PART_DocumentPaneDropTargets` (`Grid` + 5 `PART_DocumentPaneDropTarget*` elements), `PART_DocumentPaneFullDropTargets` (`Grid` + 5 `PART_DocumentPaneFullDropTarget*` + 4 `PART_DocumentPaneDropTarget*AsAnchorablePane` elements), and `PART_PreviewBox` (`Path`). |
| `DropAreaType` | `enum` | `DockingManager`, `DocumentPane`, `DocumentPaneGroup`, `AnchorablePane`. |
| `IDropArea` | `interface` | `Rect DetectionRect { get; }`, `DropAreaType Type { get; }`, `Point TransformToDeviceDPI(Point dragPosition)`. |
| `DropArea<T>` | `class : IDropArea` | Typed drop region wrapper exposing `T AreaElement { get; }` (`where T : FrameworkElement`). |

{: .note }
Hit-test target implementations (`DockingManagerDropTarget`, `DocumentPaneDropTarget`, `DocumentPaneDropAsAnchorableTarget`, `DocumentPaneGroupDropTarget`, `AnchorablePaneDropTarget`) are `internal` to `AvalonDock` and driven automatically by the `PART_*` elements in `OverlayWindow`'s `ControlTemplate`.

### Menu, Dropdown & Tree Helper Types

| Type | Base Class | Key Public Members |
|:-----|:-----------|:-------------------|
| `DropDownButton` | `ToggleButton` | Opens `DropDownContextMenu` (`ContextMenu` DP) bound to `DropDownContextMenuDataContext` (`object` DP) on left-click (used for pane overflow/options buttons). |
| `DropDownControlArea` | `ContentControl` | Opens `DropDownContextMenu` (`ContextMenu` DP) with `DropDownContextMenuDataContext` (`object` DP) on right-click (`PreviewMouseRightButtonUp`) with multi-DPI placement handling. |
| `MenuItemEx` | `MenuItem` | Adds `IconTemplate` (`DataTemplate` DP) and `IconTemplateSelector` (`DataTemplateSelector` DP) to WPF `MenuItem`. |
| `ContextMenuEx` | `ContextMenu` | Generates `MenuItemEx` item containers and refreshes `ItemsSource` bindings when opened. |
| `Extensions` (`AvalonDock.Controls`) | `static class` | Visual/logical tree helpers on `DependencyObject`: `FindVisualChildren<T>()`, `FindLogicalChildren<T>()`, `FindVisualAncestor<T>()`, `FindLogicalAncestor<T>()`, and `FindVisualTreeRoot()`. |

---

## Value Converters (`AvalonDock.Converters`)

All 12 public converters in `AvalonDock.Converters` derive from `System.Windows.Markup.MarkupExtension`, allowing inline XAML usage (`Converter="{avalonDockConverters:BoolToVisibilityConverter}"`) without declaring static resources:

| Converter Class | Implements | Conversion Behavior |
|:----------------|:-----------|:--------------------|
| `BoolToVisibilityConverter` | `IValueConverter` | `true` → `Visibility.Visible`; `false` or `null` → `ConverterParameter` (if `Visibility`) else `Visibility.Collapsed`. Supports `ConvertBack`. |
| `InverseBoolToVisibilityConverter` | `IValueConverter` | `false` → `Visibility.Visible`; `true` → `ConverterParameter` (if `Visibility`) else `Visibility.Collapsed`. Supports `ConvertBack`. |
| `NullToDoNothingConverter` | `IValueConverter` | Returns `Binding.DoNothing` when `value == null`, otherwise passes `value` through unchanged. |
| `AnchorSideToOrientationConverter` | `IValueConverter` | Maps `AnchorSide.Left` / `Right` → `Orientation.Vertical` and `Top` / `Bottom` → `Orientation.Horizontal`. |
| `AnchorSideToAngleConverter` | `IValueConverter` | Rotates side anchor headers (`90.0` degrees for `AnchorSide.Left` / `Right` when `ConverterParameter` requests rotation). |
| `UriSourceToBitmapImageConverter` | `IValueConverter` | Converts a `Uri` (or URI string) into an `Image` / `BitmapImage` for menu and tab icons. |
| `OverlayWindowToVisibilityConverter` | `IValueConverter` | Controls drop-target visibility inside `OverlayWindow` templates. |
| `AnchorableContextMenuHideVisibilityConverter` | `IMultiValueConverter` | Computes **Hide** vs **Close** menu item visibility on anchorable context menus. |
| `LayoutItemFromLayoutModelConverter` | `IValueConverter` | Resolves the `LayoutItem` wrapper for a given `LayoutContent` via `DockingManager.GetLayoutItemFromModel`. |
| `ActivateCommandLayoutItemFromLayoutModelConverter` | `IValueConverter` | Resolves `LayoutItem.ActivateCommand` from a `LayoutContent` model. |
| `AutoHideCommandLayoutItemFromLayoutModelConverter` | `IValueConverter` | Resolves `LayoutAnchorableItem.AutoHideCommand` from a `LayoutAnchorable` model. |
| `HideCommandLayoutItemFromLayoutModelConverter` | `IValueConverter` | Resolves `LayoutAnchorableItem.HideCommand` from a `LayoutAnchorable` model. |
