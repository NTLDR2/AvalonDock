---
title: Caliburn.Micro Integration
layout: default
parent: Guides
nav_order: 7
description: "How to host AvalonDock v5 (classic DockingManager + DockLayout/IDockLayoutService) in a Caliburn.Micro application: bootstrapper, IDockable boilerplate, conductor synchronization, close guards, persistence."
---

# Caliburn.Micro Integration

{: .note }
**Scope: NTLDR2 Fork v5.1.0.** This guide and its live reference implementation, `source/ExperimentalTestApp`, belong to this fork; upstream AvalonDock ships no Caliburn.Micro guidance. The sample was rewritten for **Caliburn.Micro 6.0.15-beta**, the NuGet build of upstream commit `83e93b34`. That commit is part of the [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro) fork, which adds AI-friendly documentation only. When this guide and the Caliburn.Micro fork documentation disagree, prefer the Caliburn.Micro documentation.

This guide shows how to combine:

- the **classic `DockingManager`**, driven through the v5 MVVM surface (`DockLayout` ← `IDockLayoutService.Layout`, ViewModels implementing `IDockable` / `IToolbox`);
- **Caliburn.Micro**: `BootstrapperBase` + `SimpleContainer`, `Conductor<T>.Collection.OneActive`, the async screen lifecycle, actions, and `IEventAggregator`.

No Caliburn.Micro ↔ AvalonDock base class library ships today. The integration therefore needs a small amount of **boilerplate**: about four classes and four WPF behaviors/strategies. The sample keeps it in clearly labelled `Boilerplate/` and `Docking/` folders. A dedicated, optional library is future work (see `ROADMAP.md`, Phase 3).

---

## Packages

```xml
<PackageReference Include="Caliburn.Micro" Version="6.0.15-beta" />
<!-- AvalonDock: AvalonDock, AvalonDock.Core, AvalonDock.Mvvm (DockLayoutService), AvalonDock.Serializer.Xml, a theme package -->
```

`Caliburn.Micro.Core` targets `netstandard2.0` with **no dependencies**. On `net462` (used by `net48`), `Caliburn.Micro` adds only `Microsoft.Xaml.Behaviors.Wpf`, which has no dependencies either. There is no `System.Text.Json` anywhere in the graph.

---

## 1. Bootstrapper

```csharp
public sealed class Bootstrapper : BootstrapperBase
{
	private readonly SimpleContainer _container = new SimpleContainer();

	public Bootstrapper()
	{
		LogManager.GetLog = type => new TraceLog(type); // before Initialize(): loggers are cached in static fields
		Initialize();
	}

	protected override void Configure()
	{
		_container.Instance(_container);
		_container.Singleton<IWindowManager, WindowManager>()
		          .Singleton<IEventAggregator, EventAggregator>()
		          .Singleton<ShellViewModel>()
		          .Singleton<ShellView>(); // views can be container-built (ViewLocator asks IoC first) - see the warning below

		// Same singleton under its concrete type and under IToolbox.
		_container.Singleton<ExplorerViewModel>();
		_container.RegisterHandler(typeof(IToolbox), null, c => c.GetInstance(typeof(ExplorerViewModel), null));

		var layoutService = new DockLayoutService(_container.GetAllInstances(typeof(IToolbox)).Cast<IToolbox>());
		layoutService.Layout.AllowFloatingWindows = true;   // window policy: configure it ONCE, on the IRootDock
		layoutService.Layout.AllowDetachedWindows = true;
		_container.Instance<IDockLayoutService>(layoutService);
	}

	protected override object GetInstance(Type service, string key) => _container.GetInstance(service, key);
	protected override IEnumerable<object> GetAllInstances(Type service) => _container.GetAllInstances(service);
	protected override void BuildUp(object instance) => _container.BuildUp(instance);

	protected override async void OnStartup(object sender, StartupEventArgs e)
		=> await DisplayRootViewForAsync<ShellViewModel>();
}
```

{: .warning }
Do not use the legacy patterns: synchronous `DisplayRootViewFor<T>()`, `OnInitialize()`/`OnActivate()`, `CanClose(Action<bool>)`, `#if CALIBURN_ASYNC` shims, or a hand-written `RelayCommand`. `OnInitializeAsync`/`OnActivateAsync` are `[Obsolete]`; override `OnInitializedAsync`/`OnActivatedAsync`.

{: .warning }
**Register container-built `Window` views as singletons.** `SimpleContainer.GetAllInstances` enumerates its lazy result twice, so a `PerRequest` view that `ViewLocator` resolves through `IoC.GetAllInstances(viewType)` is constructed **twice**. The orphan `Window` stays in `Application.Windows` and keeps the process alive after the main window closes.

---

## 2. The IDockable / IToolbox boilerplate

| Class | Purpose |
|:------|:--------|
| `DockableScreen : Screen, IDockable` | `Title` ↔ `DisplayName` share one backing field. `IDockable.IsActive` is implemented **explicitly** and surfaces as `IsDockActive`. |
| `DocumentScreen : DockableScreen` | Base class for documents. |
| `ToolboxScreen : DockableScreen, IToolbox` | Tool windows: `Zone`, `Shortcut`, `IsOpen`, `IsOpenByDefault`. `CanClose = false` (tools hide). |
| `DockLayoutConductor<T> : Conductor<T>.Collection.OneActive` | Synchronizes the conductor with `IDockLayoutService` (see below). |

Two flags are both called *IsActive* and must never be confused:

- `Screen.IsActive` is Caliburn.Micro's lifecycle state. It has a private setter and is driven by conductors.
- `IDockable.IsActive` means "has AvalonDock focus". In the sample it surfaces as `IsDockActive`.

{: .warning }
**Never bind `LayoutItem.IsActive` to `Model.IsActive`** on a Caliburn.Micro screen: that path resolves to the lifecycle flag.

{: .important }
**`DockingManager`/`LayoutSyncBridge` never call `IDockable.OnClose()` or `OnSelected()`, and never set `IDockable.IsActive`.** The bridge only writes `IRootDock.ActiveDockable` and removes closed documents from the `IDocumentDock`. Focus tracking and close guards are therefore the integration's job.

---

## 3. Synchronizing the conductor with the layout

The conductor's `Items` collection is the **single source of truth** for open documents.

| Concern | Rule |
|:--------|:-----|
| Open documents | `Items` add/remove/reset is mirrored into the layout's `IDocumentDock.VisibleDockables`. Nothing else mutates that collection. The bridge turns it into its internal `DocumentsSource`. When a document leaves `DocumentsSource`, AvalonDock drops its tab **silently**, without raising `DocumentClosing`. |
| Active document | `ActiveItem` → `IDockLayoutService.ActiveDockable` → the bridge sets `ActiveContent`. In the other direction, AvalonDock focus → `IRootDock.ActiveDockable` → `ActivateItemAsync(doc)`, **only** for documents already in `Items`. Toolboxes and `null` never enter `Items`. A re-entrancy flag prevents ping-pong. |
| Focus flag | When `ActiveDockable` changes, the previous dockable gets `IsActive = false` and the new one gets `IsActive = true` followed by `OnSelected()`. This applies to documents and toolboxes alike. |

{: .warning }
**With `DockLayout` bound, do not bind `DocumentsSource`, `AnchorablesSource` or `ActiveContent` in XAML.** The bridge owns all three. In particular, a two-way `ActiveContent` ↔ `ActiveItem` binding would call `ActivateItemAsync` with tool ViewModels, and Caliburn.Micro's `EnsureItem` would add them to `Items`.

---

## 4. Routing AvalonDock closes through Caliburn.Micro

AvalonDock asks synchronously, through the cancelable `DocumentClosing` event. Caliburn.Micro answers asynchronously, through `CanCloseAsync`, which may show a dialog. The bridge between them is a small behavior:

```csharp
private void OnDocumentClosing(object sender, DocumentClosingEventArgs e)
{
	if (!(e.Document?.Content is IChild child) || !(child.Parent is IConductor conductor))
		return;                                   // not a conducted screen: let AvalonDock close it

	e.Cancel = true;                              // always cancel the AvalonDock close...
	_pending = CloseAfterAsync(_pending, conductor, child); // ...and queue the Caliburn.Micro close
}
// CloseAfterAsync awaits the previous request, then: await conductor.CloseItemAsync(child);
```

- This covers the tab ✕, a middle click on the tab, and the document context menu ("Close", "Close All", "Close All But This"). Ctrl+W, *Close All Documents* and application exit already run through the conductor.
- The queue makes "Close All" over several modified documents ask **one question at a time**.
- Approved closes remove the document from `Items`, then from the `IDocumentDock`, and finally the tab disappears, without re-raising `DocumentClosing`.
- A guard that asks Save / Don't Save / Cancel:

```csharp
public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
{
	if (!IsModified)
		return true;

	var prompt = new SaveChangesViewModel(Title);
	await _windowManager.ShowDialogAsync(prompt, null, DialogSettings.CreateFixedDialog());
	switch (prompt.Choice)
	{
		case SaveChangesChoice.Save: Save(); return true;
		case SaveChangesChoice.Discard: return true;
		default: return false;
	}
}
```

---

## 5. XAML

```xml
<!-- Implicit template keyed by the ViewModel base type. Do NOT use DockingManager.LayoutItemTemplate (see the warning below). -->
<DataTemplate DataType="{x:Type boilerplate:DockableScreen}">
	<ContentControl cal:View.Model="{Binding}" IsTabStop="False" />
</DataTemplate>

<Style x:Key="DocumentItemStyle" TargetType="{x:Type ad:LayoutItem}">
	<Setter Property="Title" Value="{Binding Model.Title}" />      <!-- live: AvalonDock never copies IDockable.Title -->
	<Setter Property="ContentId" Value="{Binding Model.Id}" />     <!-- required to restore layouts -->
	<Setter Property="CanClose" Value="{Binding Model.CanClose}" />
</Style>

<ad:DockingManager DockLayout="{Binding DockLayout}"
                   LayoutItemContainerStyleSelector="{StaticResource PanesStyleSelector}">
	<i:Interaction.Behaviors>
		<docking:DocumentCloseBehavior />
		<docking:ToolboxVisibilityBehavior />
	</i:Interaction.Behaviors>
	<ad:DockingManager.LayoutUpdateStrategy>
		<docking:ZonePlacementStrategy />   <!-- places IToolbox content into the Zone_* panes -->
	</ad:DockingManager.LayoutUpdateStrategy>
	<ad:LayoutRoot> <!-- Zone_LeftTop ... Zone_BottomRight anchorable panes around a LayoutDocumentPane --> </ad:LayoutRoot>
</ad:DockingManager>
```

{: .warning }
**Why not `LayoutItemTemplate`?** The theme templates (generic and all six theme packages) display `LayoutItem.View`, a `ContentPresenter` whose `Content` is the ViewModel, inside an *outer* `ContentPresenter` that also receives `LayoutItem.View.ContentTemplate`. A `LayoutItemTemplate` is therefore applied with `DataContext` = **the inner `ContentPresenter`** in docked and floating panes, but with `DataContext` = **the ViewModel** in a `DetachedAnchorableWindow`. `cal:View.Model="{Binding}"` then binds Caliburn.Micro to the presenter (the result is a "Cannot find view for ContentPresenter" placeholder plus a flood of binder log messages). `{Binding Content}`, which the legacy sample used, breaks in detached windows. With `LayoutItemTemplate` unset, the outer presenter shows `LayoutItem.View` directly, and that presenter resolves the implicit `DataTemplate` with `DataContext` = the ViewModel in every host. This also keeps a single view instance per ViewModel across docking, floating and detaching (verified in `ExperimentalTestApp`).

### How `IToolbox` is honoured on the classic `DockingManager`

The classic manager consumes only part of `IToolbox`. `DockLayoutService` groups toolboxes by side, and the bridge's default `DockAlignmentStrategy` docks them onto that side, or into an auto-hide group when no docked pane is visible there yet.

| Member | Sample handling |
|:-------|:----------------|
| `Zone` | `ZonePlacementStrategy` places the tool into the anchorable pane named `Zone_<zone>`. Assigning any strategy also stops the manager from installing `DockAlignmentStrategy`. |
| `IsOpen` | `ToolboxVisibilityBehavior` syncs it with the anchorable's visibility in both directions. After a layout load, the restored layout decides. |
| `IsOpenByDefault` | The shell applies it at start-up and on *Reset Layout*. |
| `Shortcut` | `ToolboxShortcutBehavior` on the main window toggles `IsOpen`. |
| `ToolTipText` | Container style `ToolTip` binding. |

{: .warning }
A two-way `Visibility` binding in the `LayoutAnchorableItem` container style (the `MVVMTestApp` pattern) does not keep `IsOpen` in sync. `LayoutAnchorableItem` assigns `Visibility` as a local value **after** the container style is applied, which shadows the style binding. Sync `IsOpen` in code instead.

---

## 6. Actions instead of commands

Caliburn.Micro ships no `ICommand`, and a docking shell does not need one:

- Menus use `cal:Message.Attach="Save"`; a `CanSave` property becomes the guard.
- Buttons and lists use `x:Name` conventions: `<Button x:Name="OpenSelected"/>`, `<ListBox x:Name="Files"/>` with `SelectedFile`, `<ComboBox x:Name="Themes"/>` with `SelectedTheme`.
- Application shortcuts use `Microsoft.Xaml.Behaviors` key triggers hosted on a zero-sized leaf `FrameworkElement` inside the visual tree:

```xml
<FrameworkElement Width="0" Height="0" DockPanel.Dock="Top" Focusable="False" IsHitTestVisible="False">
	<i:Interaction.Triggers>
		<i:KeyTrigger Key="S" Modifiers="Control">
			<cal:ActionMessage MethodName="Save" />
		</i:KeyTrigger>
	</i:Interaction.Triggers>
</FrameworkElement>
```

{: .warning }
**Never attach a guarded `<cal:ActionMessage>` directly to `<Window>` or a layout panel.** `ActionMessage.ApplyAvailabilityEffect` sets `source.IsEnabled = Can<Method>()`. On `<Window>`, any `false` guard (for example `CanSave == false` on a clean document) sets `Window.IsEnabled = false` and disables all mouse and keyboard input across the entire window. With `KeyTrigger` (`ActiveOnFocus=False`) hosted on a leaf `FrameworkElement`, `KeyTrigger` still walks up the visual tree and listens on the root `Window`, while `ActionMessage` only toggles that leaf element's `IsEnabled`.

`ActionMessage.EnforceGuardsDuringInvocation` is `false` by default, so a key trigger on that leaf element invokes the action even when its guard is `false`. Make the action methods check their own preconditions.

---

## 7. Layout persistence

```csharp
var serializer = new XmlLayoutSerializer(dockingManager);   // IDockingManager (AvalonDock.Core)
serializer.LayoutSerializationCallback += (_, args) =>
{
	// args.Content already holds whatever is open under this ContentId; resolve only the rest.
	if (args.Content == null)
		args.Content = ResolveLayoutContent(args.Model.ContentId); // toolbox by Id, or re-create + Adopt a document
};
serializer.Deserialize(reader);
```

- When a document is re-created during deserialization, *adopt* it: add it to `Items` without activating it. The tab is matched by `Content` when the bridge re-attaches its sources, so no duplicate appears.
- Documents that are open but missing from the XML are re-imported afterwards.
- Unresolved elements are removed (`UnresolvedContentHandling.Remove`).
- Auto-save in the shell's `OnDeactivateAsync(close: true)` **before** calling the base implementation. Caliburn.Micro's conductor closes, and therefore removes, every document while it deactivates.

---

## 8. Caliburn.Micro behaviour inside a docking manager

| Topic | Behaviour |
|:------|:----------|
| View caching | `cal:View.Model` reuses the view cached by `IViewAware.GetView()`. The view moves with the single `LayoutItem.View` presenter into floating and detached windows, and WPF does not detach Caliburn.Micro action triggers on `Unloaded`. `OnViewLoaded` runs once per view instance. |
| Dialog owner | `WindowManager` makes the *active* window the owner of a dialog, which may be a floating window. |
| Convention lists | Name-bound `ItemsControl`s need an explicit `ItemTemplate`; otherwise Caliburn.Micro applies its default `cal:View.Model` item template. |
| Event aggregator | Subscribe in `OnActivatedAsync` and unsubscribe in `OnDeactivateAsync`, so hidden tool windows stop reacting. Republish the current state when a tool window is shown again. |
| Logging | Caliburn.Micro's `DebugLog` always calls `string.Format`; use a logger that formats only when arguments are present. |

---

## See also

- `source/ExperimentalTestApp/README.md`: a file-by-file guide to the sample.
- [MVVM Integration]({{ site.baseurl }}{% link guides/mvvm.md %}): the `IDockLayoutService` + `DockLayout` architecture.
- [Layout Serialization]({{ site.baseurl }}{% link guides/serialization.md %}) and [Theming]({{ site.baseurl }}{% link guides/theming.md %}).
- [NTLDR2/Caliburn.Micro](https://github.com/NTLDR2/Caliburn.Micro): `AGENTS.md` plus `docs/01-core` (screens, conductors, event aggregator) and `docs/02-platform` (bootstrapper, actions, view location).
