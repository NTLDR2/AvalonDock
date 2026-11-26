---
title: Getting Started
layout: default
nav_order: 2
has_children: true
description: "Learn how to install, configure, and build AvalonDock v5 in your WPF application."
permalink: /getting-started/
---

# Getting Started

Get up and running with AvalonDock v5 in minutes. This section covers NuGet package selection, building your first docking layout, and compiling the solution from source.

---

## Choose Your Path

| Guide | Description |
|:------|:------------|
| [Installation]({{ site.baseurl }}{% link getting-started/installation.md %}) | Install `Dirkster.AvalonDock` and optional theme, serializer, MVVM, and DI packages. |
| [Quick Start]({{ site.baseurl }}{% link getting-started/quick-start.md %}) | Build a working Visual Studio-style docking window in under 5 minutes. |
| [Building from Source]({{ site.baseurl }}{% link getting-started/building-from-source.md %}) | Clone, compile, and run the unit and FlaUI UI test suites locally. |

---

## Prerequisites

- **Windows** — AvalonDock's UI controls use WPF and run on Windows.
- **Supported Application Runtimes:** **.NET 8** (`net8.0-windows`), **.NET 9** (`net9.0-windows`), **.NET 10** (`net10.0-windows`), **.NET 11** (`net11.0-windows`, `v5.1.0+`), or **.NET Framework 4.8** (`net48`).
- **UI-Agnostic Class Libraries:** View-model and service projects can target **`.NET Standard 2.0`** (`netstandard2.0`) when referencing `AvalonDock.Core`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, or `AvalonDock.Serializer.*`.
- **IDE / Tooling:** Visual Studio 2022 or later (recommended), JetBrains Rider, or VS Code with the .NET SDK CLI.
