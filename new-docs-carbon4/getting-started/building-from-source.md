---
title: Building from Source
layout: default
parent: Getting Started
nav_order: 3
description: "Clone, compile, and run tests for AvalonDock from source code."
---

# Building from Source

This guide covers cloning the repository, compiling all libraries and sample applications, and running both the unit test and FlaUI desktop UI test suites locally.

---

## Prerequisites

- **OS:** Windows (WPF projects only build and execute on Windows)
- **.NET SDKs:** **.NET 9.0 SDK**, **.NET 10.0 SDK**, and **.NET 11.0 SDK** (required to build `net10.0-windows` and `net11.0-windows` targets; CI also installs **.NET 8.0 SDK** for `net8.0-windows` component targets)
- **IDE:** Visual Studio 2022 / 2026 or any editor with .NET CLI support

---

## Clone & Build

```powershell
git clone https://github.com/Dirkster99/AvalonDock.git
cd AvalonDock

dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

{: .note }
`TreatWarningsAsErrors` is enabled globally in `source/Directory.Build.props`, and CI builds with `--warnaserror`. All compiler, analyzer, and XML documentation warnings must be resolved for the build to succeed.

---

## Run Tests

{: .important }
Tests **must** run single-threaded (`-m:1`) because WPF requires STA threads and several test projects target `.NET Framework 4.8`, which cannot run in parallel without cross-window interference.

### Test Suites

| Suite | Command | Description |
|:------|:--------|:------------|
| **Unit Tests** | `dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1` | Fast unit, serialization, DI, MVVM, and STA layout regression tests |
| **FlaUI UI Tests (.NET 10)** | `dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net10.0-windows -m:1` | Launches live WPF windows to automate drag-and-drop, floating, and toggle-dock gestures on .NET 10 |
| **FlaUI UI Tests (.NET 11)** | `dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net11.0-windows -m:1` | Runs the FlaUI desktop UI automation suite on .NET 11 |

---

## Solution Structure

```text
source/
├── AvalonDock.sln                          # Main Visual Studio / dotnet CLI solution
├── Directory.Build.props                   # Shared TFMs, version, license, and analyzer settings
├── Components/                             # 14 NuGet package projects
│   ├── AvalonDock/                         # Main WPF docking library (DockingManager, ToggleDockingManager)
│   ├── AvalonDock.Core/                    # UI-agnostic interfaces, enums, and serialization DTOs (netstandard2.0)
│   ├── AvalonDock.Mvvm/                    # Zero-dependency MVVM base classes & DockLayoutService (netstandard2.0)
│   ├── AvalonDock.Mvvm.CommunityToolkit/   # CommunityToolkit.Mvvm ObservableObject base classes (netstandard2.0)
│   ├── AvalonDock.DependencyInjection/     # IServiceCollection & DockLayoutBuilder extensions (netstandard2.0)
│   ├── AvalonDock.Serializer.Xml/          # XML layout serializer (netstandard2.0)
│   ├── AvalonDock.Serializer.Json/         # System.Text.Json layout serializer (netstandard2.0)
│   ├── AvalonDock.Themes.Arc/              # Modern Arc Dark & Light themes
│   ├── AvalonDock.Themes.VS/               # VS2015, VS2022, VS2026 & custom .vstheme / JSON engine
│   ├── AvalonDock.Themes.VS2013/           # Visual Studio 2013 Dark, Light & Blue (.vstheme.gz)
│   ├── AvalonDock.Themes.VS2010/           # Visual Studio 2010 theme
│   ├── AvalonDock.Themes.Expression/       # Expression Dark & Light themes
│   ├── AvalonDock.Themes.Metro/            # Metro flat theme
│   └── AvalonDock.Themes.Aero/             # Classic Windows Aero theme
├── AutomationTest/                         # NUnit unit tests & FlaUI UI automation tests
├── AvalonDockCodeApp/                      # v5 reference app (ToggleDockingManager + MVVM + DI)
├── MVVMTestApp/                            # Classic MVVM sample (DocumentsSource / AnchorablesSource + XML)
├── TestApp/                                # Classic code-behind showcase (themes, serialization, interop)
├── VS2013Test/                             # VS2013 themed IDE demo
├── MLibTest/                               # MLib application theming integration demo
├── CaliburnDockTestApp/                    # Caliburn.Micro MVVM integration demo
└── WinFormsTestApp/                        # Windows Forms ElementHost interop demo
```

---

## Code Quality Analyzers

Non-test projects automatically run:
- **StyleCop.Analyzers** — consistent formatting and XML documentation rules
- **Microsoft.CodeAnalysis.NetAnalyzers** — .NET design and performance rules
- **SonarAnalyzer.CSharp** — code reliability and maintainability checks
- **Microsoft.CodeAnalysis.Metrics** — maintainability and coupling checks

See [Contributing]({{ site.baseurl }}{% link contributing.md %}) for pull request and commit message conventions.
