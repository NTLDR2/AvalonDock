---
title: Events
layout: default
parent: API Reference
nav_order: 5
description: "Complete reference for DockingManager, IDockingManager, LayoutRoot, LayoutContent, and LayoutAnchorable events in AvalonDock v5."
---

# Events
{: .no_toc }

AvalonDock raises pre-operation cancellable events (`*Closing`, `*Hiding`, `ContentFloating`, `ContentDocking`) paired with post-operation notifications (`*Closed`, `*Hidden`, `ContentFloated`, `ContentDocked`), both on the WPF `DockingManager` and through the UI-agnostic `AvalonDock.Core.IDockingManager` interface.
{: .fs-6 .fw-300 }

## Table of Contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## `DockingManager` Events

### Document Lifecycle

| Event | EventArgs Type | Cancellable | Description |
|:------|:---------------|:------------|:------------|
| `DocumentClosing` | `DocumentClosingEventArgs` | Yes (`Cancel = true`) | Raised before a `LayoutDocument` closes. Setting `args.Cancel = true` aborts the close. |
| `DocumentClosed` | `DocumentClosedEventArgs` | No | Raised after a `LayoutDocument` is removed from its pane (before `document.Content` is cleared to `null`). |

### Anchorable Lifecycle

| Event | EventArgs Type | Cancellable | Description |
|:------|:---------------|:------------|:------------|
| `AnchorableClosing` | `AnchorableClosingEventArgs` | Yes (`Cancel = true`) | Raised before a `LayoutAnchorable` with `CanClose = true` is permanently closed. |
| `AnchorableClosed` | `AnchorableClosedEventArgs` | No | Raised after the `LayoutAnchorable` is closed and removed from the layout. |
| `AnchorableHiding` | `AnchorableHidingEventArgs` | Yes (`Cancel = true`) | Raised before a `LayoutAnchorable` is hidden into `LayoutRoot.Hidden`. Also exposes **`CloseInsteadOfHide`** (`bool`) to redirect the hide command into a full close (`AnchorableClosing` → `AnchorableClosed`). |
| `AnchorableHidden` | `AnchorableHiddenEventArgs` | No | Raised after the `LayoutAnchorable` moves into `LayoutRoot.Hidden`. |

### Floating, Re-Docking & Floating Window Controls

| Event | EventArgs Type | Cancellable | Description |
|:------|:---------------|:------------|:------------|
| `ContentFloating` | `ContentFloatingEventArgs` | Yes (`Cancel = true`) | Raised before a docked `LayoutContent` (or `LayoutAnchorablePane` header drag) is torn off into a floating window. |
| `ContentFloated` | `ContentFloatedEventArgs` | No | Raised once the floating window has been shown for the floated content. |
| `ContentDocking` | `ContentDockingEventArgs` | Yes (`Cancel = true`) | Raised **before** floating content is docked back into the layout—both during interactive **drag-and-drop onto drop targets** and programmatic/command `Dock` / `DockAsDocument` operations. Cancelling aborts the mutation atomically. |
| `ContentDocked` | `ContentDockedEventArgs` | No | Raised after floating content has settled into its docked destination pane (via drag-and-drop, `Dock()`, `DockAsDocument()`, or `DockAllFloatingWindows()`). |
| `LayoutFloatingWindowControlCreated` | `LayoutFloatingWindowControlCreatedEventArgs` | No | Raised when a new `LayoutFloatingWindowControl` WPF window is instantiated for torn-off content. |
| `LayoutFloatingWindowControlClosed` | `LayoutFloatingWindowControlClosedEventArgs` | No | Raised when a `LayoutFloatingWindowControl` is closed and removed from `DockingManager.FloatingWindows`. |

### Layout & Focus Changes

| Event | EventArgs Type | Description |
|:------|:---------------|:------------|
| `ActiveContentChanged` | `EventArgs` | Raised when `DockingManager.ActiveContent` changes. |
| `LayoutChanging` | `EventArgs` | Raised during coercion just before `DockingManager.Layout` is replaced with a new `LayoutRoot`. |
| `LayoutChanged` | `EventArgs` | Raised after `DockingManager.Layout` has been replaced and visual panels have been attached. |

---

## UI-Agnostic `IDockingManager` Events (`AvalonDock.Core`)

When consuming `IDockingManager` from a UI-agnostic service or ViewModel layer (`Dirkster.AvalonDock.Core`), `DockingManager` forwards every lifecycle and float/dock operation to the matching `AvalonDock.Core.Events` event right after the WPF event:

| `IDockingManager` Event | `AvalonDock.Core.Events` Type | Key Properties |
|:------------------------|:------------------------------|:---------------|
| `DocumentClosing` | `DocumentCancelEventArgs` | `Document` (`object`), `Cancel` (`bool`) |
| `DocumentClosed` | `DocumentEventArgs` | `Document` (`object`) |
| `AnchorableClosing` | `AnchorableCancelEventArgs` | `Anchorable` (`object`), `Cancel` (`bool`), `CloseInsteadOfHide` (`bool`) |
| `AnchorableClosed` | `AnchorableEventArgs` | `Anchorable` (`object`) |
| `AnchorableHiding` | `AnchorableCancelEventArgs` | `Anchorable` (`object`), `Cancel` (`bool`), `CloseInsteadOfHide` (`bool`) |
| `AnchorableHidden` | `AnchorableEventArgs` | `Anchorable` (`object`) |
| `ContentFloating` | `ContentCancelEventArgs` | `Content` (`object`), `Cancel` (`bool`) |
| `ContentFloated` | `ContentEventArgs` | `Content` (`object`) |
| `ContentDocking` | `ContentCancelEventArgs` | `Content` (`object`), `Cancel` (`bool`) |
| `ContentDocked` | `ContentEventArgs` | `Content` (`object`) |

---

## Layout Model Events (`AvalonDock.Layout`)

### `LayoutRoot` Events

| Event | EventArgs Type | Description |
|:------|:---------------|:------------|
| `ElementAdded` | `LayoutElementEventArgs` | Raised when a `LayoutElement` joins this `LayoutRoot` (`args.Element`). |
| `ElementRemoved` | `LayoutElementEventArgs` | Raised when a `LayoutElement` leaves this `LayoutRoot` (`args.Element`). |
| `Updated` | `EventArgs` | Raised whenever parent relationships change inside the layout tree. |
| `PropertyChanging` / `PropertyChanged` | `PropertyChangingEventArgs` / `PropertyChangedEventArgs` | Raised for `RootPanel`, `ActiveContent`, `LastFocusedDocument`, `Manager`, `Children`, `ChildrenCount`, and side properties. |

### `LayoutContent` & `LayoutAnchorable` Events

| Owner | Event | EventArgs Type | Description |
|:------|:------|:---------------|:------------|
| `LayoutContent` | `Closing` | `CancelEventArgs` | Per-item cancellable close notification. |
| `LayoutContent` | `Closed` | `EventArgs` | Per-item post-close notification. |
| `LayoutContent` | `IsActiveChanged` | `EventArgs` | Raised when `IsActive` changes. |
| `LayoutContent` | `IsSelectedChanged` | `EventArgs` | Raised when `IsSelected` changes within its parent pane. |
| `LayoutContent` | `FloatingPropertiesUpdated` | `EventArgs` | Raised when monitor bounds clamping adjusts `FloatingLeft`/`Top`/`Width`/`Height`. |
| `LayoutAnchorable` | `Hiding` | `CancelEventArgs` | Per-anchorable cancellable hide notification (raised when `HideAnchorable(cancelable: true)` runs). |
| `LayoutAnchorable` | `IsVisibleChanged` | `EventArgs` | Raised whenever `IsVisible` transitions between visible and hidden states. |

### `LayoutGroupBase` & Floating Window Model Events

| Owner | Event | EventArgs Type | Description |
|:------|:------|:---------------|:------------|
| `LayoutGroupBase` | `ChildrenCollectionChanged` | `EventArgs` | Raised when direct `Children` of the group are added, removed, or replaced. |
| `LayoutGroupBase` | `ChildrenTreeChanged` | `ChildrenTreeChangedEventArgs` | Carries `Change` (`ChildrenTreeChange.DirectChildrenChanged` or `ChildrenTreeChange.TreeChanged`). |
| `LayoutDocumentFloatingWindow` / `LayoutAnchorableFloatingWindow` | `IsVisibleChanged` | `EventArgs` | Raised when the floating window model's computed `IsVisible` state changes. |

---

## WPF `EventArgs` Class Reference (`AvalonDock`)

| Class | Base Class | Properties |
|:------|:-----------|:-----------|
| `DocumentClosingEventArgs` | `CancelEventArgs` | `LayoutDocument Document { get; }`, `bool Cancel { get; set; }` |
| `DocumentClosedEventArgs` | `EventArgs` | `LayoutDocument Document { get; }` |
| `AnchorableClosingEventArgs` | `CancelEventArgs` | `LayoutAnchorable Anchorable { get; }`, `bool Cancel { get; set; }` |
| `AnchorableClosedEventArgs` | `EventArgs` | `LayoutAnchorable Anchorable { get; }` |
| `AnchorableHidingEventArgs` | `CancelEventArgs` | `LayoutAnchorable Anchorable { get; }`, `bool Cancel { get; set; }`, `bool CloseInsteadOfHide { get; set; }` |
| `AnchorableHiddenEventArgs` | `EventArgs` | `LayoutAnchorable Anchorable { get; }` |
| `ContentFloatingEventArgs` | `CancelEventArgs` | `LayoutContent Content { get; }`, `bool Cancel { get; set; }` |
| `ContentFloatedEventArgs` | `EventArgs` | `LayoutContent Content { get; }` |
| `ContentDockingEventArgs` | `CancelEventArgs` | `LayoutContent Content { get; }`, `bool Cancel { get; set; }` |
| `ContentDockedEventArgs` | `EventArgs` | `LayoutContent Content { get; }` |
| `LayoutFloatingWindowControlCreatedEventArgs` | `EventArgs` *(sealed)* | `LayoutFloatingWindowControl LayoutFloatingWindowControl { get; }` |
| `LayoutFloatingWindowControlClosedEventArgs` | `EventArgs` *(sealed)* | `LayoutFloatingWindowControl LayoutFloatingWindowControl { get; }` |
| `LayoutElementEventArgs` (`AvalonDock.Layout`) | `EventArgs` | `LayoutElement Element { get; }` |
| `ChildrenTreeChangedEventArgs` (`AvalonDock.Layout`) | `EventArgs` | `ChildrenTreeChange Change { get; }` |

---

## Common Event Patterns

### Async Save-on-Close Prompt

Because WPF raises `DocumentClosing` synchronously, cancel the initial close before `await`-ing an async dialog, then call `args.Document.Close()` once the document's `IsDirty` flag is cleared:

```csharp
dockManager.DocumentClosing += async (_, args) =>
{
    if (args.Document.Content is not IEditableDocument doc || !doc.IsDirty)
    {
        return;
    }

    // Cancel synchronously before yielding to the async dialog
    args.Cancel = true;

    var decision = await PromptSaveChangesAsync(doc);
    if (decision == SaveDecision.Cancel)
    {
        return;
    }

    if (decision == SaveDecision.Save)
    {
        await doc.SaveAsync(); // Clears doc.IsDirty
    }
    else
    {
        doc.DiscardChanges();  // Clears doc.IsDirty
    }

    args.Document.Close();
};
```

### Close Transient Tool Windows Instead of Hiding Them

Set `args.CloseInsteadOfHide = true` in `AnchorableHiding` to permanently close transient search-result or build-run panels when the user clicks **×**, while keeping singleton panels (`Solution Explorer`, `Properties`) hideable:

```csharp
dockManager.AnchorableHiding += (_, args) =>
{
    if (args.Anchorable.ContentId?.StartsWith("searchResult:", StringComparison.Ordinal) == true)
    {
        args.CloseInsteadOfHide = true;
    }
};
```

### Customize Floating Window Chrome or Block Re-Docking

```csharp
// Customize newly created floating windows (e.g. decouple ownership from main window minimize)
dockManager.LayoutFloatingWindowControlCreated += (_, args) =>
{
    args.LayoutFloatingWindowControl.OwnedByDockingManagerWindow = false;
};

// Prevent a dedicated monitor tear-off panel from being re-docked
dockManager.ContentDocking += (_, args) =>
{
    if (args.Content.ContentId == "livePreviewMonitor")
    {
        args.Cancel = true;
    }
};
```
