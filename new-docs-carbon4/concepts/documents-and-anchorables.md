---
title: Documents & Anchorables
layout: default
parent: Concepts
nav_order: 3
description: "Understanding LayoutDocument and LayoutAnchorable lifecycle, placement strategies, auto-hide, and standalone detaching in AvalonDock v5."
---

# Documents & Anchorables
{: .no_toc }

AvalonDock distinguishes between two concrete `LayoutContent` types: **`LayoutDocument`** (workspace documents in the central editor well) and **`LayoutAnchorable`** (supporting tool windows docked around the workspace, auto-hidden on an edge, floated, or detached into standalone OS windows).
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Comparison at a Glance

| Capability | `LayoutDocument` | `LayoutAnchorable` |
|:-----------|:-----------------|:-------------------|
| **Typical role** | Code editors, designers, data views, reports | Solution Explorer, Properties, Output, Terminal |
| **Primary container** | `LayoutDocumentPane` | `LayoutAnchorablePane` or `LayoutAnchorGroup` |
| **`CanClose` default** | `true` (closing removes it from the layout) | `false` (clicking **×** hides it into `LayoutRoot.Hidden`) |
| **`CanHide`** | Always `false` (documents have no hidden state) | `true` by default (`Hide()` / `Show()`) |
| **`CanAutoHide`** | Not supported | `true` by default (`ToggleAutoHide()` / `ToggleSingleAutoHide()`) |
| **`CanFloat`** | `true` by default (`LayoutDocumentFloatingWindow`) | `true` by default (`LayoutAnchorableFloatingWindow`) |
| **Standalone OS window** | Not supported | Supported via `DockingManager.DetachAnchorableToWindow` (`IsDetached`) |
| **Dock in document well** | Native | Supported when `CanDockAsTabbedDocument = true` (default) |

---

## `LayoutDocument`

A `LayoutDocument` represents primary user content in the document well. Closing a document raises `DockingManager.DocumentClosing` (cancellable), removes the `LayoutDocument` from its parent pane, sets `Document.Content = null`, raises `DocumentClosed`, and activates the preceding sibling or selected document.

```xml
<avalonDock:LayoutDocumentPane>
    <avalonDock:LayoutDocument Title="Program.cs"
                               ContentId="src/Program.cs"
                               Description="C# Source File — C:\App\Program.cs"
                               CanClose="True"
                               CanFloat="True"
                               CanMove="True">
        <TextBox Text="// Document content" AcceptsReturn="True" />
    </avalonDock:LayoutDocument>
</avalonDock:LayoutDocumentPane>
```

### Key `LayoutDocument` Properties

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `Title` | `string` | `null` | Text displayed on the document tab header. |
| `ContentId` | `string` | `null` | Stable identifier used during layout serialization (falls back to `FrameworkElement.Name` if `Content` is a named `FrameworkElement`). |
| `Description` | `string` | `null` | Extended description (for example, a full file path shown in the `Ctrl+Tab` navigator window and persisted in `LayoutDocumentDto`). |
| `Content` | `object` | `null` | Hosted WPF `UIElement` or MVVM ViewModel (`IDockable`). |
| `CanClose` | `bool` | `true` | Whether the user can close the document tab. |
| `CanFloat` | `bool` | `true` | Whether the tab can be torn off into a floating window (subject to `DockingManager.AllowFloatingWindows`). |
| `CanMove` | `bool` | `true` | Whether the user can reorder or drag the document tab. |
| `CanHide` | `bool` | `false` | Read-only `false` — documents cannot be hidden. |
| `IsActive` | `bool` | `false` | Whether this document currently holds logical focus (`LayoutRoot.ActiveContent == this`). |
| `IsSelected` | `bool` | `false` | Whether this tab is the selected tab inside its `LayoutDocumentPane`. |
| `IsLastFocusedDocument` | `bool` | `false` | Read-only flag indicating the most recently focused item in the document well. |
| `IconSource` | `ImageSource` | `null` | Optional tab icon. |
| `ToolTip` | `object` | `null` | Optional tab tooltip (not serialized; bind from the view or `LayoutItemContainerStyle`). |

{: .note }
**Tab Icons Across Themes:** Classic themes (`GenericTheme`, `Aero`, `Metro`, `VS2010`, `Expression`) render `IconSource` in document and tool headers automatically. The modern flat `Arc` theme intentionally renders clean title-only document tabs by default; supply a custom `DockingManager.DocumentHeaderTemplate` if you want tab icons in `Arc`.

---

## `LayoutAnchorable`

A `LayoutAnchorable` represents a tool window or auxiliary panel. By default, clicking its header close button (`×`) **hides** the panel rather than destroying it—parking it in `LayoutRoot.Hidden` alongside its `PreviousContainer` and `PreviousContainerIndex` so `Show()` brings it back to the exact same pane and tab position.

```xml
<avalonDock:LayoutAnchorablePane Name="LeftPane" DockWidth="260">
    <avalonDock:LayoutAnchorable Title="Solution Explorer"
                                  ContentId="solutionExplorer"
                                  CanHide="True"
                                  CanClose="False"
                                  CanAutoHide="True"
                                  CanFloat="True"
                                  CanDockAsTabbedDocument="False">
        <TreeView />
    </avalonDock:LayoutAnchorable>
</avalonDock:LayoutAnchorablePane>
```

### Key `LayoutAnchorable` Properties

| Property | Type | Default | Description |
|:---------|:-----|:--------|:------------|
| `Title` | `string` | `null` | Header caption and tab title. |
| `ContentId` | `string` | `null` | Unique identifier persisted by layout serializers. |
| `Content` | `object` | `null` | Hosted `UIElement` or ViewModel (`IDockable` / `IToolbox`). |
| `CanHide` | `bool` | `true` | Whether the tool window can be hidden into `LayoutRoot.Hidden`. |
| `CanClose` | `bool` | `false` | Whether the tool window can be permanently closed (`Close()`). |
| `CanAutoHide` | `bool` | `true` | Whether the pin button and Auto-Hide command are enabled. |
| `CanFloat` | `bool` | `true` | Whether the panel can be torn off into a floating window. |
| `CanDockAsTabbedDocument` | `bool` | `true` | Whether dragging the panel allows dropping onto the central document well. |
| `CanMove` | `bool` | `true` | Whether the panel can be dragged/repositioned. |
| `AutoHideWidth` / `AutoHideHeight` | `double` | `0.0` | Flyout size when expanded from an auto-hide edge (`0` uses the pane's natural size). |
| `AutoHideMinWidth` / `AutoHideMinHeight` | `double` | `100.0` | Minimum flyout dimensions (must be non-negative). |
| `IsVisible` | `bool` | — | `true` when parented in the layout outside `LayoutRoot.Hidden`. Setting `IsVisible = true` calls `Show()`; setting `false` calls `Hide()`. |
| `IsHidden` | `bool` | — | Read-only; `true` when parked in `LayoutRoot.Hidden` (`Parent is LayoutRoot`). |
| `IsAutoHidden` | `bool` | — | Read-only; `true` when unpinned inside a `LayoutAnchorGroup` (`Parent is LayoutAnchorGroup`). |
| `IsFloating` | `bool` | — | Read-only (inherited from `LayoutContent`); `true` when inside a `LayoutFloatingWindow`. |
| `IsDetached` | `bool` | `false` | `true` while hosted in a standalone top-level `DetachedAnchorableWindow` via `DockingManager.DetachAnchorableToWindow`. |

{: .tip }
If you want a tool window's **×** button to close rather than hide on a case-by-case basis, either set `CanClose="True"` and `CanHide="False"` on the `LayoutAnchorable`, or set `args.CloseInsteadOfHide = true` in a `DockingManager.AnchorableHiding` handler.

---

## Adding vs Showing Anchorables (`AddToLayout` vs `Show`)

AvalonDock separates **inserting a brand-new unparented anchorable** from **restoring a hidden anchorable**:

| Method | When to Use | Precondition |
|:-------|:------------|:-------------|
| `anchorable.AddToLayout(dockManager, strategy)` | Adding a **newly created** `LayoutAnchorable` that is not yet part of any layout tree (`Parent == null`). | `!anchorable.IsVisible && !anchorable.IsHidden` *(throws `InvalidOperationException` if already visible or hidden)*. |
| `anchorable.Show()` | Restoring an anchorable that is currently **hidden** in `dockManager.Layout.Hidden` (`IsHidden == true`). | `anchorable.IsHidden` *(no-op if already `IsVisible`; throws `InvalidOperationException` if unparented)*. |

### Inserting a New Anchorable with `AnchorableShowStrategy`

`AnchorableShowStrategy` is a `[Flags]` enum (`Left`, `Right`, `Top`, `Bottom`, `Most`):

- Passing `AnchorableShowStrategy.Left`, `Right`, `Top`, or `Bottom` looks for an existing `LayoutAnchorablePane` on that side and adds the anchorable as a tab; if none exists, it creates a new pane at the outer edge.
- Combining `AnchorableShowStrategy.Most` (for example, `AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most`) always creates a new outer pane along that edge of `Layout.RootPanel` instead of joining an existing side pane.

```csharp
using AvalonDock.Layout;

var diagnostics = new LayoutAnchorable
{
    Title = "Diagnostics",
    ContentId = "diagnostics",
    Content = new DiagnosticsControl()
};

// Insert a new unparented anchorable on the right side
diagnostics.AddToLayout(dockManager, AnchorableShowStrategy.Right);
diagnostics.IsActive = true;
```

### Hiding, Restoring, and Removing Anchorables

```csharp
// 1. Hide an open anchorable (moves it into dockManager.Layout.Hidden)
diagnostics.Hide();

// 2. Restore a hidden anchorable to its remembered pane & index
//    (or to an existing/new docked pane if its previous pane no longer exists)
if (diagnostics.IsHidden)
{
    diagnostics.Show();
}

// 3. Detach completely from the layout (neither visible nor in LayoutRoot.Hidden,
//    without raising Closing events or disturbing auto-hidden siblings)
diagnostics.RemoveFromLayout();
```

---

## Auto-Hide (`ToggleAutoHide` vs `ToggleSingleAutoHide`)

In classic `DockingManager`, unpinning a tool window moves it from its `LayoutAnchorablePane` into a `LayoutAnchorGroup` on the nearest `LayoutAnchorSide` (`LeftSide`, `RightSide`, `TopSide`, or `BottomSide`):

```xml
<avalonDock:LayoutRoot.BottomSide>
    <avalonDock:LayoutAnchorSide>
        <avalonDock:LayoutAnchorGroup>
            <avalonDock:LayoutAnchorable Title="Output" ContentId="output" AutoHideHeight="180" />
            <avalonDock:LayoutAnchorable Title="Error List" ContentId="errorList" AutoHideHeight="180" />
        </avalonDock:LayoutAnchorGroup>
    </avalonDock:LayoutAnchorSide>
</avalonDock:LayoutRoot.BottomSide>
```

AvalonDock provides two methods on `LayoutAnchorable` depending on whether sibling tabs should move together:

| Method | Behavior |
|:-------|:---------|
| `anchorable.ToggleAutoHide()` | Moves **every tab** in the parent `LayoutAnchorablePane` (or `LayoutAnchorGroup`) together—matching classic Visual Studio pin-button behavior. Uses `DockingManager.LayoutEngine` if the original pane was garbage-collected. |
| `anchorable.ToggleSingleAutoHide()` | Moves **only this single `LayoutAnchorable`** between docked and auto-hidden state, leaving sibling tabs untouched in their current pane or `LayoutAnchorGroup`. |

{: .note }
`ToggleDockingManager` sets `SupportsAutoHideFlyout = false` and replaces slide-out hover flyouts with persistent click-to-toggle sidebar buttons (`ToggleDockButtonBar`) across 6 `DockZone` positions. See the [ToggleDockingManager Guide]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}).

---

## Lifecycle Events Summary

You can observe or cancel operations either centrally on `DockingManager` or per item on `LayoutDocument` / `LayoutAnchorable`:

| Operation | `DockingManager` Events | Model Events (`LayoutContent` / `LayoutAnchorable`) |
|:----------|:------------------------|:----------------------------------------------------|
| **Close document** | `DocumentClosing` *(cancellable)* → `DocumentClosed` | `Closing` (`CancelEventArgs`) → `Closed` |
| **Close anchorable** | `AnchorableClosing` *(cancellable)* → `AnchorableClosed` | `Closing` (`CancelEventArgs`) → `Closed` |
| **Hide anchorable** | `AnchorableHiding` *(cancellable + `CloseInsteadOfHide`)* → `AnchorableHidden` | `Hiding` (`CancelEventArgs`) → `IsVisibleChanged` |
| **Float / Re-dock** | `ContentFloating` *(cancellable)* → `ContentFloated`; `ContentDocking` *(cancellable)* → `ContentDocked` | `FloatingPropertiesUpdated` |
| **Focus / Tab selection** | `ActiveContentChanged` | `IsActiveChanged`, `IsSelectedChanged` |

See the [Events API Reference]({{ site.baseurl }}{% link api-reference/events.md %}) for complete `EventArgs` signatures and async save-on-close patterns.
