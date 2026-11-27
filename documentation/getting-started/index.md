---
title: Getting Started
layout: default
nav_order: 2
has_children: true
description: "Learn how to install, configure, and build AvalonDock v5 in your WPF application."
permalink: /getting-started/
---

# Getting Started

Get up and running with **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock) (`v5.1.0` fork)** in minutes. This section covers vendored/package consumption of the 13 zero-`System.Text.Json` libraries, building your first docking layout, and compiling the solution from source.

---

## Choose Your Path

| Guide | Description |
|:------|:------------|
| [Installation]({{ site.baseurl }}{% link getting-started/installation.md %}) | Consume `AvalonDock` via vendored `ProjectReference` or local `dotnet pack` (plus upstream `Dirkster.AvalonDock*` NuGet context). |
| [Quick Start]({{ site.baseurl }}{% link getting-started/quick-start.md %}) | Build a working Visual Studio-style docking window in under 5 minutes. |
| [Building from Source]({{ site.baseurl }}{% link getting-started/building-from-source.md %}) | Clone `NTLDR2/AvalonDock`, compile with `--warnaserror`, and run the unit and FlaUI UI test suites locally. |

---

## Prerequisites

- **Windows** — AvalonDock's UI controls use WPF and run on Windows.
- **Supported Application Runtime:** **`.NET Framework 4.8`** (`net48`, exclusive desktop WPF target in `NTLDR2 Fork v5.1.0` — Windows 7 SP1 64-bit to Windows 11). *(Upstream `v5.0.2+` additionally ships `net8.0-windows`–`net11.0-windows` builds.)*
- **UI-Agnostic Class Libraries:** View-model and service projects can target **`.NET Standard 2.0`** (`netstandard2.0`) when referencing `AvalonDock.Core`, `AvalonDock.Mvvm*`, `AvalonDock.DependencyInjection`, or `AvalonDock.Serializer.Xml`.
- **IDE / Tooling:** Visual Studio 2022 / 2026 (recommended), JetBrains Rider, or VS Code with the .NET SDK CLI.
