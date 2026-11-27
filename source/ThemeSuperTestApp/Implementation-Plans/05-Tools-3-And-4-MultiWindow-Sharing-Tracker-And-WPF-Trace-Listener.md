# Plan 05 (of 07) — Tools 3 & 4: Multi-Window Dictionary Sharing Tracker & Live WPF Trace Listener

> **Execution Order:** **Step 5 of 7** (Requires [Plan 04](04-Tool-2-ResourceDictionary-Freezable-Auditor-And-Cross-Theme-Diff.md) completed).
> **Hard Constraints:** `net48` exclusively; `Caliburn.Micro` `6.0.15-beta`; classic `DockingManager` only; **zero** edits to existing AvalonDock theme libraries.

---

## 1. Objective & Scope

Build two specialized runtime diagnostic tools inside `ThemeSuperTestApp` that uncover invisible runtime bugs and memory/CPU waste across multi-window docking workflows:
1. **Tool 3 — Multi-Window `ResourceDictionary` Instance Sharing Tracker (`WindowSharingTrackerViewModel`):** Inspects `DockingManager` and every open AvalonDock window (`LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, `NavigatorWindow`, `OverlayWindow`) via `RuntimeHelpers.GetHashCode(dict)` to visually demonstrate how `DictionaryTheme` shares **1 single `ResourceDictionary` object instance** across all windows (`O(1)`), whereas URI-based `Theme` (`Arc`, `VS2010`, `Aero`, etc.) allocates **N separate BAML-parsed `ResourceDictionary` instances** (one per window).
2. **Tool 4 — Live WPF Binding & Resource Error Trace Listener (`WpfTraceListenerViewModel`):** Hooks `PresentationTraceSources.DataBindingSource` and `PresentationTraceSources.ResourceDictionarySource` in-process so elusive XAML errors (such as `#684` `MenuItemEx` binding errors on overflow menus, `System.Windows.Data Error: 40`, or broken `{DynamicResource}` lookups in floating/detached windows) are caught, timestamped, and tagged with the active theme automatically!

---

## 2. Step-by-Step Implementation Tasks

### Step 5.1 — Implement Tool 3: `Services/WindowDictionaryTrackerService.cs` & `WindowSharingTrackerViewModel`
1. **`WindowDictionarySnapshot` model (`Models/WindowDictionarySnapshot.cs`):**
   - `HostKind` (`"Main DockingManager"`, `"LayoutAnchorableFloatingWindowControl"`, `"LayoutDocumentFloatingWindowControl"`, `"DetachedAnchorableWindow"`, `"NavigatorWindow"`, `"OverlayWindow"`).
   - `WindowTitle` (`string`), `IsVisible` (`bool`).
   - `ThemeDictionaryInstanceId` (`string` — formatted as `Ref #1 (0x1A2B3C4D)` using `RuntimeHelpers.GetHashCode(themeDict)` so identical object references share the exact same `Ref #1` label!).
   - `SourceUri` (`string` — `themeDict.Source?.ToString() ?? "<In-Memory DictionaryTheme>"`).
   - `NestedMergedCount` (`int`) and `SharesRootInstanceWithDockingManager` (`bool` — `ReferenceEquals(windowThemeDict, dockManagerThemeDict)`).
2. **`WindowDictionaryTrackerService`:**
   - Inspects `DockingManager.Resources.MergedDictionaries` and iterates `Application.Current.Windows` (filtering to AvalonDock window types: `LayoutFloatingWindowControl`, `DetachedAnchorableWindow`, `NavigatorWindow`, `OverlayWindow`).
   - Locates each host's active theme `ResourceDictionary` (matching either `ReferenceEquals(d, dictionaryTheme.ThemeResourceDictionary)` or `d.Source == theme.GetResourceUri()`).
   - Computes summary metrics: `TotalThemedHosts`, `DistinctDictionaryInstances`, and `RedundantBamlParsesCount` (`DistinctDictionaryInstances - 1`).
3. **`ViewModels/Tools/WindowSharingTrackerViewModel.cs` & `Views/Tools/WindowSharingTrackerView.xaml` (`Shortcut = "Ctrl+Alt+W"`):**
   - Auto-refreshes when `ActiveThemeChanged` fires, when `DockingManager` layout/window events occur, or on a lightweight opt-in `1s` `DispatcherTimer` while open (so transient `OverlayWindow` during drag or `NavigatorWindow` on `Ctrl+Tab` can also be captured in a sticky "Observed Transient Windows" log!).
   - Displays a clear verdict banner:
     - Green when `DistinctDictionaryInstances == 1` across `>= 2` hosts: `"SHARED (O(1)): All N windows reference the exact same ResourceDictionary instance."`
     - Amber when `DistinctDictionaryInstances > 1`: `"DUPLICATED (N instances): Each window parsed its own separate ResourceDictionary from BAML."`

### Step 5.2 — Implement Tool 4: `Services/WpfTraceCaptureService.cs` & `WpfTraceListenerViewModel`
1. **`WpfTraceEventRecord` (`Models/WpfTraceEventRecord.cs`):**
   - `SequenceNumber` (`int`), `Timestamp` (`DateTime`), `Category` (`"DataBinding"` | `"ResourceDictionary"` | `"Markup"`), `Severity` (`TraceEventType`), `EventId` (`int`, e.g., `40`), `ThemeName` (`string`), `BaseClassBadge` (`string`), `Message` (`string`).
2. **`WpfTraceCaptureService` (`TraceListener` subclass, `IDisposable`):**
   - Attaches to:
     - `PresentationTraceSources.DataBindingSource` (`SourceLevels.Warning | SourceLevels.Error`)
     - `PresentationTraceSources.ResourceDictionarySource` (`SourceLevels.Warning | SourceLevels.Error`)
     - `PresentationTraceSources.MarkupSource` (`SourceLevels.Warning | SourceLevels.Error`)
   - Calls `PresentationTraceSources.Refresh()` once before attaching so WPF enables internal trace switches on `net48` even when launched outside a debugger!
   - Marshals captured events safely onto the UI `Dispatcher` (`BeginInvoke` at `DispatcherPriority.Background` to avoid re-entrant layout/binding deadlocks while WPF is in the middle of evaluating a binding!), capping the ring buffer at e.g. `1,000` entries.
3. **`ViewModels/Tools/WpfTraceListenerViewModel.cs` & `Views/Tools/WpfTraceListenerView.xaml` (`PreferredSide = DockSide.Bottom`, `IsOpenByDefault = true`, `Shortcut = "Ctrl+Alt+T"`):**
   - Toolbar: `IsCaptureEnabled` toggle (on by default), `FilterCurrentThemeOnly` checkbox, `SearchFilter` textbox, `ClearEvents` button, `CopySelectedOrAll` button, and live `ErrorCount` / `WarningCount` badge in the pane header/toolbar.
   - Split master-detail view: virtualized `DataGrid` of trace records + read-only monospace `TextBox` below showing the full multi-line WPF binding path / visual ancestor dump of the selected trace error.

### Step 5.3 — Register Tools 3 & 4 in `Bootstrapper.cs`
Register `WindowDictionaryTrackerService`, `WpfTraceCaptureService`, `RegisterToolbox<WindowSharingTrackerViewModel>()`, and `RegisterToolbox<WpfTraceListenerViewModel>()` in `Bootstrapper.Configure()`, disposing `WpfTraceCaptureService` cleanly on exit.

---

## 3. Verification & Exit Gate to Plan 06

```powershell
dotnet build source/ThemeSuperTestApp/ThemeSuperTestApp.csproj --configuration Release --no-restore --warnaserror
git diff --name-only -- source/Components/
```

**Expected Result:**
- Builds with **0 warnings, 0 errors**.
- Tearing off a floating or detached window under `ArcDark` shows `2+` distinct `ResourceDictionary` object IDs in Tool 3, whereas switching to `VS2022Dark` or `Vs2013Dark` shows `1` shared `Ref #1` across all windows.
- Tool 4 captures any XAML `System.Windows.Data Error` or resource lookup warning live without requiring Visual Studio's Output window.
- Zero edits to `source/Components/`.
