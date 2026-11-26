---
title: Guides
layout: default
nav_order: 4
has_children: true
description: "Practical how-to guides for MVVM, ToggleDockingManager, Dependency Injection, Serialization, and Theming in AvalonDock v5."
permalink: /guides/
---

# Guides

Practical how-to guides covering AvalonDock v5's MVVM architecture, sidebar toggle docking, Dependency Injection, XML layout persistence, and runtime/custom theming.

---

| Guide | Description |
|:------|:------------|
| [MVVM Integration]({{ site.baseurl }}{% link guides/mvvm.md %}) | Build decoupled docking view models with `AvalonDock.Mvvm`, `AvalonDock.Mvvm.CommunityToolkit`, `IDockLayoutService`, or classic source collections. |
| [ToggleDockingManager]({{ site.baseurl }}{% link guides/toggle-docking-manager.md %}) | Configure VS Code / JetBrains Rider-style 6-zone sidebar toggle docking, shortcuts, and layout priority. |
| [Dependency Injection]({{ site.baseurl }}{% link guides/dependency-injection.md %}) | Register `IDockLayoutService`, toolboxes, `DockingOptions`, and custom services with `Microsoft.Extensions.DependencyInjection`. |
| [Layout Serialization]({{ site.baseurl }}{% link guides/serialization.md %}) | Save and restore complex layouts in XML using AvalonDock v5's DTO serialization pipeline. |
| [Theming]({{ site.baseurl }}{% link guides/theming.md %}) | Apply and switch between all 5 built-in theme packages (`Arc`, `VS2010`, `Expression`, `Metro`, `Aero`) and `GenericTheme` at runtime. |
| [Custom Themes]({{ site.baseurl }}{% link guides/custom-themes.md %}) | Customize theme brushes using `AvalonDock.Themes.Arc.Themes.ResourceKeys`, `DictionaryTheme` subclasses, or custom XAML dictionaries. |
| [Caliburn.Micro Integration]({{ site.baseurl }}{% link guides/caliburn-micro.md %}) | *(NTLDR2 Fork v5.1.0)* Host the classic `DockingManager` (`DockLayout` + `IDockLayoutService`) in a Caliburn.Micro 6 application: bootstrapper, `IDockable`/`IToolbox` boilerplate, conductor synchronization, async close guards, persistence. |
