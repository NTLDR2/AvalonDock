---
name: avalondock-internals
description: >-
  Deep-dive reference for AvalonDock v5 internal architecture and engine invariants:
  the three parallel trees (IRootDock, LayoutRoot, visual Controls), LayoutItem wrappers
  and LogicalChildren depth ordering, LayoutGridControl splitters and TabControlEx
  virtualization, LayoutSyncBridge / ILayoutUpdateStrategy / ILayoutEngine pipelines,
  ToggleDockingManager 6-zone sidebars and shortcuts, LayoutFloatingWindowControl vs
  DetachedAnchorableWindow windowing, DragService / OverlayWindow reuse / HwndHost
  airspace, DTO XML serialization fixup, and Theme / DictionaryTheme / Arc ResourceKeys.
  Use this skill whenever modifying or debugging code under source/Components/,
  investigating layout, focus, MVVM sync, windowing, drag-and-drop, serialization, or
  theme behavior, or writing non-trivial AvalonDock tests.
---

# AvalonDock v5 Internals — How AvalonDock Works from the Inside Out

Understanding these internal mechanisms prevents subtle regressions in layout mutation, focus tracking, logical/visual tree inheritance, MVVM bindings, window ownership, drag-and-drop overlays, and XML serialization.

---

## 1. The Three Parallel Trees: MVVM `IRootDock` ↔ Logical `LayoutRoot` ↔ Visual `Control`s

AvalonDock is **model-first**. Visual WPF controls never own layout structure; instead, they observe and project the `LayoutRoot` model tree:

1. **Optional UI-Agnostic MVVM Tree (`AvalonDock.Core` / `AvalonDock.Mvvm`):**
   - `IRootDock` (`RootDock`) holds `IDocumentDock` (`DocumentDock`) and `IToolDock` (`ToolDock`, aligned `Left`, `Right`, `Top`, `Bottom`), whose `VisibleDockables` contain `IDockable` / `IToolbox` ViewModels.
   - Managed by `DockLayoutService` (`IDockLayoutService`) and bridged to WPF by `LayoutSyncBridge` when `DockingManager.DockLayout` is bound.
2. **WPF Layout Model Tree (`AvalonDock.Layout`):**
   - Every node inherits from `LayoutElement : DependencyObject` (`Parent`, `Root`, `INotifyPropertyChanged`, `INotifyPropertyChanging`).
   - **Important Inheritance Detail:** `LayoutRoot` and `LayoutFloatingWindow` inherit directly from `LayoutElement` (implementing `ILayoutContainer`), **not** from `LayoutGroup<T>`.
   - `LayoutRoot` (`DockingManager.Layout`) owns **7 child branches** enumerated by `LayoutRoot.Children` (so `layout.Descendents()` visits all of them!):
     - `RootPanel` (`LayoutPanel`) — docked split hierarchy (`LayoutPanel`, `LayoutDocumentPaneGroup`, `LayoutDocumentPane`, `LayoutAnchorablePaneGroup`, `LayoutAnchorablePane`, `LayoutDocument`, `LayoutAnchorable`)
     - `TopSide`, `BottomSide`, `LeftSide`, `RightSide` (`LayoutAnchorSide` → `LayoutAnchorGroup` → auto-hidden `LayoutAnchorable`s)
     - `FloatingWindows` (`ObservableCollection<LayoutFloatingWindow>` — `LayoutDocumentFloatingWindow` holding a `LayoutDocumentPaneGroup` `RootPanel`, or `LayoutAnchorableFloatingWindow` holding a `LayoutAnchorablePaneGroup` `RootPanel`)
     - `Hidden` (`ObservableCollection<LayoutAnchorable>` — hidden tool windows where `IsHidden == true`, i.e., `Parent is LayoutRoot`)
   - **Automatic Reparenting & Change Bubbling:** Adding an element to `LayoutGroup<T>.Children` automatically detaches it from its old `Parent`, assigns `element.Parent = this`, raises `ChildrenCollectionChanged`, bubbles `ChildrenTreeChanged` (`DirectChildrenChanged` on the immediate group, `TreeChanged` on ancestors), and fires `LayoutRoot.ElementAdded` / `Updated`.
   - **Garbage Collection (`LayoutRoot.CollectGarbage()`):** Prunes empty `LayoutAnchorablePane`, `LayoutAnchorablePaneGroup`, `LayoutPanel`, `LayoutFloatingWindow`, and `LayoutAnchorGroup` nodes **only when** no surviving element's `ILayoutPreviousContainer.PreviousContainer` points to them, collapses redundant single-child nested pane groups, and **always preserves at least one main-window `LayoutDocumentPane`** so new documents always have a drop target. Built-in close/hide/float/dock/deserialize paths call `CollectGarbage()` automatically; call it manually only after direct `Children` mutations.
3. **WPF Visual Control Tree (`AvalonDock.Controls`):**
   - `DockingManager.CreateUIElementForModel(ILayoutElement)` is the factory mapping model nodes to controls (`ILayoutControl`):
     - `LayoutPanel` → `LayoutPanelControl`
     - `LayoutDocumentPaneGroup` → `LayoutDocumentPaneGroupControl`
     - `LayoutAnchorablePaneGroup` → `LayoutAnchorablePaneGroupControl`
     - `LayoutDocumentPane` → `LayoutDocumentPaneControl`
     - `LayoutAnchorablePane` → `LayoutAnchorablePaneControl`
     - `LayoutAnchorSide` / `LayoutAnchorGroup` → `LayoutAnchorSideControl` / `LayoutAnchorGroupControl` → `LayoutAnchorControl`
     - `LayoutDocumentFloatingWindow` / `LayoutAnchorableFloatingWindow` → `LayoutDocumentFloatingWindowControl` / `LayoutAnchorableFloatingWindowControl`

---

## 2. `LayoutItem` Wrappers, Single-Presenter Rule & Logical-Tree Depth Ordering

Every `LayoutDocument` and `LayoutAnchorable` in `Layout` gets a companion wrapper in `DockingManager._layoutItems` (retrieved via `dockManager.GetLayoutItemFromModel(layoutContent)`):

- **`LayoutDocumentItem : LayoutItem`** (for `LayoutDocument`) and **`LayoutAnchorableItem : LayoutItem`** (for `LayoutAnchorable`).
- **Key Properties on `LayoutItem`:**
  - `DataContext` of `LayoutItem` is **`this`** (the `LayoutItem` itself!).
  - `LayoutElement` → the `LayoutDocument` or `LayoutAnchorable`.
  - `Model` → `LayoutElement.Content` (the user's ViewModel or `UIElement`). This is why `LayoutItemContainerStyle` setters always bind `{Binding Model.Title}`, `{Binding Model.Id}`, etc.!
  - `View` → a **lazily created single `ContentPresenter`** bound to `LayoutElement.Content`, `DockingManager.LayoutItemTemplate`, and `DockingManager.LayoutItemTemplateSelector`.
- **Style Precedence Trick (`SetCurrentValue`):** When `LayoutItem.Attach(LayoutContent)` seeds `CanClose`, `CanHide`, `Title`, etc. from the model, it uses `SetCurrentValue(...)` rather than setting a local DP value. Consequently, a `<Setter Property="CanClose" Value="{Binding Model.CanClose}" />` in `LayoutItemContainerStyle` / `LayoutItemContainerStyleSelector` cleanly overrides the model default instead of losing to WPF local-value precedence.
  - **Exceptions (verified):** `SetDefaultBindings()` runs *after* the container style is applied and assigns `IsSelected`, `IsActive` and (on `LayoutAnchorableItem`) `Visibility` as **local values**, which shadow style bindings on those properties. A two-way `Visibility ← Model.IsVisible` style binding therefore does not keep a ViewModel in sync. Sync it in code instead, via `LayoutAnchorable.IsVisibleChanged` and `Show()`/`Hide()` (see `ExperimentalTestApp/Docking/ToolboxVisibilityBehavior.cs`).
- **Where `LayoutItemTemplate` is applied (verified):** The pane/document control templates (generic and all theme packages) host `LayoutItem.View` in an *outer* `ContentPresenter` with `Content = LayoutItem.View` **and** `ContentTemplate = LayoutItem.View.ContentTemplate`. WPF applies a non-null `ContentTemplate` even to `UIElement` content, so in docked and floating panes a `LayoutItemTemplate` is instantiated with `DataContext` = **the inner `View` presenter** (its `.Content` is the ViewModel), and the inner presenter is never shown. `DetachedAnchorableWindow` shows `LayoutItem.View` itself, where the template's `DataContext` is **the ViewModel**. Templates that bind to their `DataContext` (for example Caliburn.Micro `cal:View.Model`) should use implicit `DataTemplate`s keyed by ViewModel type, with `LayoutItemTemplate` unset. Then the outer presenter shows `View` directly and `View` resolves the implicit template with `DataContext` = ViewModel in every host.
- **Why `DockingManager.LogicalChildren` Sorts by Descending Visual Tree Depth (`GetOrderedLogicalChildren`):**
  - `LayoutItem.View` and raw `UIElement` contents are added as logical children of `DockingManager` (`InternalAddLogicalChild`), while visually residing deep inside `LayoutDocumentPaneControl` or `LayoutAnchorablePaneControl` (which are *also* reachable from `DockingManager`).
  - WPF's `DescendentsWalker` visits each node **at most once**, walking logical children before visual children. If a visual ancestor were enumerated first, `TreeWalkHelper.OnInheritablePropertyChanged` would hit the inner `View` via the visual tree first, skip it (because its visual parent != its logical parent), and then ignore it on the second logical visit—breaking `DataContext`, `FontSize`, and `FontFamily` inheritance!
  - Sorting `_logicalChildren` by **descending visual tree depth** guarantees deepest nodes are visited via their logical link first. Never bypass `InternalAddLogicalChild` / `InternalRemoveLogicalChild`.

---

## 3. Dynamic Grid & Splitter Mechanics (`LayoutGridControl<T>`) & Tab Virtualization (`TabControlEx`)

- **`LayoutGridControl<T>` (`LayoutPanelControl`, `LayoutDocumentPaneGroupControl`, `LayoutAnchorablePaneGroupControl`):**
  - Subscribes to `_model.ChildrenTreeChanged` (acting only on `ChildrenTreeChange.DirectChildrenChanged`, coalesced via `_asyncRefreshCalled` at `DispatcherPriority.Normal`).
  - `UpdateChildren()` reuses existing `ILayoutControl` visuals whose `.Model` still matches, creates visuals for new models via `CreateUIElementForModel`, interleaves `LayoutGridResizerControl` (`Thumb`) splitters between visible children, and builds `RowDefinitions` or `ColumnDefinitions` from `DockWidth` / `DockHeight` (`1*` default) and `CalculatedDockMinWidth()` / `CalculatedDockMinHeight()` (`25.0` px default minimum).
  - Dragging a `LayoutGridResizerControl` displays a lightweight ghost border (`_resizerGhost`) during drag and updates both neighbors' `DockWidth` / `DockHeight` (`Star` weights scaled proportionally, or `Pixel` / `Auto` converted to `Pixel`) on `DragCompleted`.
- **`TabControlEx` (`LayoutDocumentPaneControl` & `LayoutAnchorablePaneControl`):**
  - Standard WPF `TabControl` destroys and recreates the visual tree of tab content on every tab switch. `TabControlEx` (`IsVirtualizingDocument = true`, `IsVirtualizingAnchorable = true` by default) keeps materialized `ContentPresenter`s inside `PART_ItemsHolder` and toggles `Visibility.Visible` / `Visibility.Collapsed` so editors, scroll positions, and stateful controls survive tab switching.
  - When `DockingManager.IgnoreTabControlKeyBindings = true`, `TabControlEx.OnKeyDown` skips `base.OnKeyDown(e)` so keys like `Home`, `End`, `PageUp`, `PageDown`, and `Ctrl+Tab` are not stolen from hosted text editors or grids.

---

## 4. MVVM Synchronization & Placement Pipeline (`LayoutSyncBridge`, `ILayoutUpdateStrategy`, `ILayoutEngine`)

AvalonDock v5 supports two MVVM binding modes on `DockingManager` (and `ToggleDockingManager`):
- **Mode A (Recommended v5):** Bind `DockLayout="{Binding DockLayout}"` (`IRootDock`, typically from `IDockLayoutService.Layout`).
- **Mode B (Classic v4/v5):** Bind `DocumentsSource` and `AnchorablesSource` directly.

**What happens internally when `DockLayout` is set (`OnDockLayoutChanged`):**
1. `LayoutSyncBridge.Attach()` creates internal `_documentModels` and `_anchorableModels` `ObservableCollection<object>` instances and walks the `IRootDock` tree:
   - For every `IDocumentDock`, copies `VisibleDockables` into `_documentModels` and subscribes to `CollectionChanged`.
   - For every `IToolDock`, maps `IToolDock.Alignment` → `AnchorSide` into `_contentToSide[child] = side` **before** adding `child` to `_anchorableModels`, and subscribes to `CollectionChanged`.
   - **Critical Ordering Invariant:** When a new tool is added to `IToolDock.VisibleDockables` at runtime, `OnAnchorableCollectionChanged` **must** write `_contentToSide[item] = addSide.Value` *before* `_anchorableModels.Add(item)`, because adding to `AnchorablesSource` synchronously triggers `LayoutUpdateStrategy.BeforeInsertAnchorable`, which immediately looks up `_contentToSide`!
2. `LayoutSyncBridge` assigns `_manager.DocumentsSource = _documentModels` and `_manager.AnchorablesSource = _anchorableModels`.
3. `SyncWindowPolicyToWpf()` pushes `_rootDock.AllowFloatingWindows` and `_rootDock.AllowDetachedWindows` **one-way** onto `DockingManager`. *(Note: if you bind `DockLayout`, configure window policy on `IRootDock` or DI `DockingOptions`, because `IRootDock`'s default `true` overwrites a XAML `AllowFloatingWindows="False"` attribute!)*
4. Two-way focus & close sync (guarded by `_isSyncing`): `ActiveContentChanged` ↔ `IRootDock.ActiveDockable`, and `DocumentClosed` removes the closed ViewModel from `IDocumentDock.VisibleDockables`.

**`ILayoutUpdateStrategy` vs `ILayoutEngine` — Know the Difference:**
- **`ILayoutUpdateStrategy`** (`DockingManager.LayoutUpdateStrategy` DP): **Item-routing hook** called by `AttachAnchorablesSource` / `AttachDocumentsSource` (`BeforeInsertAnchorable`, `AfterInsertAnchorable`, `BeforeInsertDocument`, `AfterInsertDocument`).
  - Classic `DockingManager` auto-installs **`DockAlignmentStrategy`** when `DockLayout` is set and `LayoutUpdateStrategy == null`: if a visible `LayoutAnchorablePane` already exists on the target `AnchorSide` in `RootPanel`, it adds the tool as a tab there; otherwise it places the tool into that side's auto-hide `LayoutAnchorGroup`.
  - `ToggleDockingManager` installs **`ToggleLayoutStrategy`** in its constructor: reads `IToolbox`, copies `Title`, `Id` → `ContentId`, `Icon` → `ToggleDock.Icon`, `ToolTipText` → `ToggleDock.ToolTip`, and places the `LayoutAnchorable` into a `LayoutAnchorGroup` on `LeftSide`, `RightSide`, or `BottomSide` based on `IToolbox.Zone`.
- **`ILayoutEngine`** (`DockingManager.LayoutEngine` virtual property): **Structural tree engine** operating purely on `LayoutRoot` (no visual-tree dependencies):
  - `DefaultLayoutEngine` (on `DockingManager`): `InsertPane`, `FindOrCreateContentPanel`, `FixSplitOrientation`, `EnsureBottomFullWidth`, `EnsureSidesFullHeight`.
  - `ToggleLayoutEngine` (on `ToggleDockingManager`): adds 6-zone awareness (`InsertPaneForZone`, `FixSplitOrientationForZone`) so `LeftTop` precedes `LeftBottom`, `RightTop` precedes `RightBottom`, and `BottomLeft` precedes `BottomRight`.

### What `LayoutSyncBridge` does *not* do (verified)

- It never calls `IDockable.OnClose()` or `IDockable.OnSelected()`, and never writes `IDockable.IsActive`, `DockState` or `CanClose`. Its only ViewModel writes are `IRootDock.ActiveDockable` (from `ActiveContentChanged`) and removing a closed document from the first `IDocumentDock.VisibleDockables`.
- `ActiveContent` changing to a non-`IDockable` (or `null`) sets `IRootDock.ActiveDockable = null`; consumers must ignore `null`.
- Removing a ViewModel from `IDocumentDock.VisibleDockables` removes its `LayoutDocument` through `DocumentsSourceElementsChanged` **without** raising `DocumentClosing`/`DocumentClosed`. MVVM frameworks with async close guards, such as Caliburn.Micro `CanCloseAsync`, can therefore cancel `DocumentClosing` and close through their own pipeline without re-entrancy. The reference implementation is `ExperimentalTestApp/Docking/DocumentCloseBehavior.cs` plus `Boilerplate/DockLayoutConductor.cs`.
- `DockingManager.OnDockLayoutChanged` installs `DockAlignmentStrategy` only when `LayoutUpdateStrategy == null`. That strategy knows only the side and falls back to auto-hide groups when no docked pane is visible on that side.

---

## 5. `ToggleDockingManager` Internals (6-Zone Sidebars, Exclusive Bars, `IToolbox` Sync & Shortcuts)

`ToggleDockingManager` (`source/Components/AvalonDock/ToggleDockingManager.cs`) subclasses `DockingManager` with `SupportsAutoHideFlyout = false` (preventing `AutoHideWindowManager` from opening hover/slide flyouts).

1. **6 Zones Mapped onto 2 Vertical Sidebars (`SetupToggleDockButtonBars`):**
   - Collapses classic `LeftSidePanel`, `RightSidePanel`, `TopSidePanel`, and `BottomSidePanel`.
   - Calls `AutoHideAllDockedAnchorables()` (`ToggleSingleAutoHide()`) to gather all non-floating anchorables onto `Layout.LeftSide`, `RightSide`, and `BottomSide`.
   - Creates 6 `ToggleDockButtonBar` controls with stable UI Automation IDs (`ToggleDockBar_LeftTop`, `ToggleDockBar_LeftBottom`, `ToggleDockBar_RightTop`, `ToggleDockBar_RightBottom`, `ToggleDockBar_BottomLeft`, `ToggleDockBar_BottomRight`).
   - Injects two `DockPanel` rails (`_injectedLeftDockPanel` at column `0` and `_injectedRightDockPanel` at column `2`, `RowSpan = 3`) directly into the `DockingManager` template root `Grid`:
     - **Left Rail:** `[LeftTop]` → separator → `[LeftBottom]` → `Hidden Panels (⋯)` button → `[BottomLeft]` (docked bottom).
     - **Right Rail:** `[RightTop]` → separator → `[RightBottom]` → `[BottomRight]` (docked bottom).
   - **Stale Icon Binding Guard (`ClearBarIconContent`):** Before removing bars on layout/theme rebuilds, `RemoveToggleDockButtonBars()` explicitly clears `btn.IconContent = null` and `btn.IconSource = null`. Because vector `Viewbox` icons use `RelativeSource.FindAncestor` bindings to `ToggleDockButton.Foreground`, clearing `IconContent` first forces WPF to re-evaluate foreground bindings when re-parented instead of leaving icons stuck in their previous focused/white brush!
   - **Thread-Safe Style Caching:** `_staticToggleStyle` (`ToggleAnchorablePaneControlStyle` loaded via absolute `pack://application:,,,` URI) is `[ThreadStatic]` because unsealed WPF `Style` objects have `Dispatcher` thread affinity—essential when multi-threaded STA tests instantiate managers on separate threads.
2. **Exclusive-Per-Zone, Simultaneous-Across-Zones Toggling (`ToggleAnchorable`):**
   - Opening an anchorable in `zone` calls `HideDockedInBar(GetBarForZone(zone))` — collapsing **only** any sibling currently open in that *same* zone bar (and writing `SetToolboxIsOpen` back to that collapsed sibling so its `IToolbox.IsOpen` never goes stale!).
   - Thus `LeftTop` and `LeftBottom` can be open simultaneously in a vertical split, and `BottomLeft` + `BottomRight` can be open side-by-side in a horizontal split (`EnsureBottomZoneOrder` guarantees `BottomLeft` panes always stay left of `BottomRight` panes).
   - After docking (`DockFromAutoHide` → `FixSplitOrientation`), `LayoutPriority` restructures corners via `ToggleLayoutEngine`:
     - `DockLayoutPriority.BottomFullWidth` *(default, JetBrains Rider style)*: lifts bottom panes to the outer vertical `LayoutPanel` so bottom spans full width.
     - `DockLayoutPriority.SidesFullHeight` *(VS Code style)*: lifts left/right panes to the outer horizontal `LayoutPanel` so sidebars span full height.
3. **Two-Way `IToolbox.IsOpen` & Deserialization State Reconciliation:**
   - `RegisterToolbox` subscribes to `IToolbox.PropertyChanged` for `nameof(IToolbox.IsOpen)` and syncs mutations in both directions using `_syncDepth` to prevent re-entrancy loops.
   - **Fresh Load vs Saved Layout Reload:** On initial load (`ApplyInitialToolboxState`), `ToggleDockingManager` opens toolboxes where `toolbox.IsOpen || toolbox.IsOpenByDefault` (so setting `IsOpen = true` in DI before the window loads works!). Conversely, in `OnLayoutChanged` (e.g., `XmlLayoutSerializer.Deserialize`), it captures `CollectDockedAnchorables(newLayout)` before rebuilding bars, re-opens **only** what was docked in the saved layout, and calls `SyncToolboxStateToLayout()`—deliberately **not** re-applying `IsOpenByDefault` over the user's saved layout!
4. **Host-Window Shortcuts (`RefreshShortcuts`):** Parses `IToolbox.Shortcut` via `KeyGestureConverter` (ignoring invalid strings without throwing) and attaches `KeyBinding`s (`ToggleToolboxCommand`) to `Window.GetWindow(this).InputBindings`. `ToggleDockButton` automatically appends `" (<Shortcut>)"` to the tooltip.

---

## 6. Windowing Internals: Floating Windows vs Standalone `DetachedAnchorableWindow`

AvalonDock v5 has **two** distinct multi-window mechanisms—do not confuse them:

| Mechanism | Control Class | Chrome & Ownership | Content Hosted | Drag-Docking | Global Switch |
|:----------|:--------------|:-------------------|:---------------|:-------------|:--------------|
| **Tear-Off Floating Window** | `LayoutDocumentFloatingWindowControl` / `LayoutAnchorableFloatingWindowControl` | Custom themed borderless `WindowChrome`; owned by main window by default (`OwnedByDockingManagerWindow = true`, `ShowInTaskbar = false`) | Full `Layout*PaneGroup` split/tab tree (via `FloatingWindowContentHost : HwndHost`) | Yes (`IOverlayWindowHost` + `DragService`) | `AllowFloatingWindows` |
| **Standalone Window ("Window" Mode)** | `DetachedAnchorableWindow` | Standard OS `SingleBorderWindow`; **unowned** (`Owner = null`, `ShowInTaskbar = true`), independent minimize & z-order | Single `LayoutAnchorable`'s `LayoutItem.View` (`ContentPresenter`) + header | No (OS window manager; closing reattaches) | `AllowDetachedWindows` |

**Key Windowing Invariants in v5:**
- **Pre-HWND Safe Floating:** Calling `Float()` before the host `Window` has created its Win32 `HWND` defers `Owner` assignment to `Window.SourceInitialized` (`UpdateOwnership`) and defers `Show()` via `ShowWhenHostWindowIsShown()`.
- **Inheritable Property Mirroring:** Because top-level WPF `Window`s cannot join `DockingManager`'s logical tree, `DockingManager.OnPropertyChanged` explicitly mirrors inheritable properties (`FontSize`, `FontFamily`, `FlowDirection`, `TextOptions.*`) onto open floating windows (`SyncInheritedProperty`) without clobbering local/style values on the floating window.
- **`Alt+F4` / System Close Consistency:** Closing a `LayoutAnchorableFloatingWindowControl` via `Alt+F4` or `WM_CLOSE` routes through `HideWindowCommand` (respecting `AnchorableHiding`), and closing a `LayoutDocumentFloatingWindowControl` routes through `CloseWindowCommand` (respecting `DocumentClosing` cancellation).
- **Single-Presenter Transfer in `DetachAnchorableToWindow`:** A WPF `Visual` can have only one parent. `DockingManager.DetachAnchorableToWindow` calls `DetachFromLayout(anchorable)`, severs `layoutItem.View` from both its old template parent (`DisconnectFromVisualParent`, clearing `ContentProperty` bindings first!) and `DockingManager`'s logical children (`InternalRemoveLogicalChild`), and hands `view` to `new DetachedAnchorableWindow(anchorable, view, header)`.
  - On classic `DockingManager`, `DetachFromLayout` hides the model into `LayoutRoot.Hidden` (preserving `PreviousContainer` and `PreviousContainerIndex`).
  - On `ToggleDockingManager`, `DetachFromLayout` overrides this to collapse the panel onto its sidebar stripe (`_detachedZones[anchorable] = zone`) so its `ToggleDockButton` stays visible, `IToolbox.IsOpen` remains `true`, and clicking the sidebar button brings the standalone window to the foreground (`ActivateDetachedWindow`)!
- **Off-Screen Monitor Protection & Shutdown Cleanup:** `DetachedAnchorableWindow` checks that at least an `80 × 80` DIP intersection overlaps `SystemParameters.VirtualScreen*` before restoring saved coordinates (falling back to `WindowStartupLocation.CenterScreen` if a secondary monitor was unplugged). `HookHostWindow()` closes all ownerless `DetachedAnchorableWindow`s when the main window closes so the process never hangs under `ShutdownMode.OnLastWindowClose`.
- **Temporary Manager Unload Survival:** When `DockingManager` unloads (e.g., inside an outer `TabControl`), floating windows are hidden into `_fwHiddenList` (`KeepContentVisibleOnClose = true`) and detached windows close with `keepDetachedFlag: true` so `DockingManager_Loaded` / `RestoreDetachedAnchorables` seamlessly restores them when re-loaded.

---

## 7. Interactive Drag-and-Drop, `OverlayWindow` Reuse & Focus / `HwndHost` Airspace

- **`DragService` & Single-`OverlayWindow`-Per-Host:**
  - During a floating-window drag (`WM_MOVING`), `DragService.UpdateMouseLocation` queries `DockingManager.GetOverlayWindowHostsByZOrder` (walking Win32 `GW_HWNDFIRST` → `GW_HWNDNEXT` handles so floating windows above the main window get hit-tested before `DockingManager`!).
  - Each `IOverlayWindowHost` (`DockingManager` and `LayoutDocumentFloatingWindowControl`) allocates **at most one `OverlayWindow`** and calls `HideOverlay()` rather than closing/recreating it per drag.
  - Because Windows can drop a modal move loop without firing `WM_EXITSIZEMOVE` when crossing monitors of different DPIs, `DragService` calls `_manager.HideAllOverlayWindows()` at **both** drag start and drag end/abort (`#587`), guaranteeing transparent overlay windows never leak.
  - Dropping onto a `DropTarget<T>` calls `DockingManager.RaiseContentDocking(content)` **before** mutating the layout tree (allowing atomic cancellation via `ContentDocking`), performs the tree move, calls `RaiseContentDocked(content)`, and runs `root.CollectGarbage()`.
- **`ToggleDockingManager` 6-Zone Drag Overlay (`ToggleDockDragOverlay`):**
  - Dragging a `ToggleDockButton` or tool pane header in `ToggleDockingManager` intercepts `StartDraggingFloatingWindowForPane` / `StartDraggingFloatingWindowForContent` and shows a 6-zone visual overlay (`LeftTop`, `LeftBottom`, `RightTop`, `RightBottom`, `BottomLeft`, `BottomRight`) instead of tearing off a classic floating pane, relocating via `MoveAnchorableToZone`. Document tab drags still use the standard `OverlayWindow`.
- **Focus & `HwndHost` Airspace (`FocusElementManager` & `AutoHideWindowManager`):**
  - `FocusElementManager` tracks the last focused WPF `IInputElement` (`FullWeakDictionary<ILayoutElement, IInputElement>`) **and** Win32 child `HWND` (`WindowHookHandler` hook for `WindowsFormsHost` / WebView2 / native hosts) per `LayoutContent`, restoring exact inner focus on tab activation (`SetFocusOnLastElement`).
  - Classic auto-hide flyouts use `LayoutAutoHideWindowControl : HwndHost` so slide-out tool panels render cleanly *above* Win32 airspace controls (`WindowsFormsHost`, DirectX/OpenGL viewports) and auto-collapse after `DockingManager.AutoHideDelay` (`500` ms default) once focus leaves.

---

## 8. DTO Serialization & Deserialization Fixup Pipeline

```text
Save:  LayoutRoot ──► LayoutDtoMapper.ToDto() ──► LayoutRootDto ──► SerializeCore(Stream, dto)
Load:  DeserializeCore(Stream) ──► LayoutRootDto ──► LayoutDtoMapper.FromDto() ──► FixupLayout() ──► Manager.Layout = layout
```

1. **17 Plain DTOs (`AvalonDock.Core.Serialization.Dto`):** Zero WPF or external library dependencies. `LayoutDtoMapper` (`source/Components/AvalonDock/Serialization/LayoutDtoMapper.cs`) maps `GridLength`, `Orientation`, `PreviousContainerId` (`ILayoutPaneSerializable.Id`), `Floating*` bounds, `IsMaximized`, and `LayoutAnchorable.IsDetached`.
2. **`XmlLayoutSerializer` (`AvalonDock.Serializer.Xml`):**
   - Writes namespace-free `<LayoutRoot>` XML using `System.Xml.Serialization.XmlSerializer` over `LayoutRootDto`.
   - **Legacy v4 XML Compatibility on Load:** Decodes streams into a `string` via `StreamReader(..., Encoding.UTF8, detectEncodingFromByteOrderMarks: true)` (so legacy files with mismatched `encoding="utf-16"` prologs on UTF-8 bytes do not throw) and runs `LegacyBooleanRegex` to lowercase `"True"`/`"False"` on the 12 v4 boolean attributes (`IsSelected`, `IsLastFocusedDocument`, `IsMaximized`, `CanClose`, `CanFloat`, `CanShowOnHover`, `CanHide`, `CanAutoHide`, `CanDockAsTabbedDocument`, `CanDock`, `CanMove`, `ShowHeader`) without touching user strings like `Title` or `ContentId`.
3. **`LayoutSerializerBase.Deserialize` Lifecycle (`source/Components/AvalonDock.Core/Serialization/LayoutSerializerBase.cs`):**
   - `StartDeserialization()` sets `SuspendDocumentsSourceBinding = true` and `SuspendAnchorablesSourceBinding = true` inside `try`/`finally`.
   - `CaptureCurrentState()` snapshots existing `ISerializableLayoutAnchorable[]` and `ISerializableLayoutDocument[]` so pre-existing content/titles/icons can be matched by `ContentId`.
   - `FixupLayout(layout)`:
     1. Calls `FixCachedRootOnDeserialize()` on every descendant.
     2. Relinks `PreviousContainerId` → `PreviousContainer` (`ISerializableLayoutPane.Id`) across **both** `LayoutContent` items and `LayoutAnchorGroup` auto-hide groups (`#696`).
     3. Invokes `LayoutSerializationCallback` for each anchorable and document.
     4. Cancelling an anchorable (`args.Cancel = true`) or leaving it unresolved under default `UnresolvedContentHandling.Remove` calls **`RemoveFromLayout()`** (never `Close()`, because closing an auto-hidden anchorable would restore its entire `LayoutAnchorGroup` including siblings!). Under `UnresolvedContentHandling.Hide`, unresolved anchorables call `HideAnchorable(false)` into `LayoutRoot.Hidden`. Unresolved documents always call `Close()`.
     5. Calls `layout.CollectGarbage()`.
   - Assigning `Manager.Layout = layout` triggers `OnLayoutChanged`, which enforces `AllowFloatingWindows` (`DockAllFloatingWindows()` before visuals are created if `false`) and schedules `RestoreDetachedAnchorables` at `DispatcherPriority.Loaded` (or clears `IsDetached` if `AllowDetachedWindows == false`).

---

## 9. Theme Engine Internals (`Theme` vs `DictionaryTheme` & `Arc` `ResourceKeys`)

1. **Two Base Classes in `AvalonDock.Themes`:**
   - **`Theme`** (`abstract`): Pack-URI-based themes (`ArcDarkTheme`, `ArcLightTheme`, `VS2010Theme`, `ExpressionDarkTheme`, `ExpressionLightTheme`, `MetroTheme`, `AeroTheme`, `GenericTheme`) override `GetResourceUri()`.
   - **`DictionaryTheme : Theme`** (`abstract`): Runtime-constructed custom themes pass a built `ResourceDictionary` to `base(themeResourceDictionary)` and return `null` from `GetResourceUri()`.
   - Changing `DockingManager.Theme` swaps the merged dictionary on `DockingManager`, every open `LayoutFloatingWindowControl`, and every open `DetachedAnchorableWindow` (`UpdateThemeResources`), and triggers `ToggleDockingManager.ReapplyThemeStyles()` (which preserves currently open toolboxes across theme switches).
2. **Assembly-Scoped `ComponentResourceKey`s (`AvalonDock.Themes.Arc`):** `AvalonDock.Themes.Arc.Themes.ResourceKeys` scopes brush and color keys to `typeof(ResourceKeys)` so custom `DictionaryTheme` subclasses or merged XAML dictionaries can re-accent `DarkTheme.xaml` / `LightTheme.xaml` cleanly.
