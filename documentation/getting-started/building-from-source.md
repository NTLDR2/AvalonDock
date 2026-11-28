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
- **.NET SDKs & Targeting Packs:** **.NET Framework 4.8 Developer Pack** (primary prioritized desktop target) plus the **.NET 8.0 SDK** (the only SDK required; `LangVersion` is 12 and the tests pin NUnit 4.3.x)
- **IDE:** Visual Studio 2022 / 2026, JetBrains Rider, or VS Code with .NET CLI support

---

## Clone & Build (`NTLDR2 Fork v5.1.0`)

```powershell
git clone https://github.com/NTLDR2/AvalonDock.git
cd AvalonDock

dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

{: .note }
`TreatWarningsAsErrors` is enabled globally in `source/Directory.Build.props`, and CI builds with `--warnaserror`. All compiler, analyzer, and XML documentation warnings must be resolved for the build to succeed.

---

## Run Tests

{: .important }
Tests **must** run single-threaded (`-m:1`) because WPF requires STA threads on `.NET Framework 4.8`, which cannot run in parallel without cross-window interference.

### Test Suites

| Suite | Command | Description |
|:------|:--------|:------------|
| **Unit Tests** | `dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1` | Fast unit, XML serialization, DI, MVVM, and STA layout regression tests on `net48` |
| **FlaUI UI Tests (.NET Framework 4.8)** | `dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net48 -m:1` | Launches live WPF windows (`TestApp.exe` & `AvalonDockCodeApp.exe`) to automate drag-and-drop, floating, and toggle-dock gestures on `net48` |

---

## Solution Structure

```text
source/
├── AvalonDock.sln                          # Main Visual Studio / dotnet CLI solution
├── Directory.Build.props                   # Shared TFMs, version, license, and analyzer settings
├── Components/                             # 9 NuGet package projects
│   ├── AvalonDock/                         # Main WPF docking library (DockingManager, ToggleDockingManager)
│   ├── AvalonDock.Core/                    # UI-agnostic interfaces, enums, and serialization DTOs (netstandard2.0)
│   ├── AvalonDock.Mvvm/                    # Zero-dependency MVVM base classes & DockLayoutService (netstandard2.0)
│   ├── AvalonDock.Mvvm.CommunityToolkit/   # CommunityToolkit.Mvvm ObservableObject base classes (netstandard2.0)
│   ├── AvalonDock.DependencyInjection/     # IServiceCollection & DockLayoutBuilder extensions (netstandard2.0)
│   ├── AvalonDock.Serializer.Xml/          # XML layout serializer (netstandard2.0)
│   ├── AvalonDock.Themes.Type1Legacy/      # Classic URI themes (Aero, Expression Dark/Light, Metro, VS2010)
│   ├── AvalonDock.Themes.Type2Hybrid/      # DictionaryTheme Visual Studio 2013 Dark, Light & Blue themes
│   └── AvalonDock.Themes.Type3New/         # VsTheme Visual Studio 2015, 2022 & 2026 themes + .vstheme / JSON loader
├── AutomationTest/                         # NUnit unit tests & FlaUI UI automation tests
├── AvalonDockCodeApp/                      # v5 reference app (ToggleDockingManager + MVVM + DI)
├── MVVMTestApp/                            # Classic MVVM sample (DocumentsSource / AnchorablesSource + XML)
├── TestApp/                                # Classic code-behind showcase (themes, serialization, interop)
├── MLibTest/                               # MLib application theming integration demo
├── CaliburnDockTestApp/                    # Upstream Caliburn.Micro 4 integration sample (standalone)
├── ExperimentalTestApp/                    # Caliburn.Micro 6 + classic DockingManager (DockLayout) reference sample
├── ThemeSuperTestApp/                      # 18-theme visual debugger, 6-tool profiler/auditor & Antigravity XML AgentBridge
├── WinFormsTestApp/                        # Windows Forms ElementHost interop demo
└── AvalonDocPanelMemoryLeaks/              # Memory-leak regression harness (standalone)
```

---

## Code Quality Analyzers

Non-test projects automatically run:
- **StyleCop.Analyzers** — consistent formatting and XML documentation rules
- **Microsoft.CodeAnalysis.NetAnalyzers** — .NET design and performance rules
- **SonarAnalyzer.CSharp** — code reliability and maintainability checks
- **Microsoft.CodeAnalysis.Metrics** — maintainability and coupling checks

See [Contributing]({{ site.baseurl }}{% link contributing.md %}) for pull request and commit message conventions.
