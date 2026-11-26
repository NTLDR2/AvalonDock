---
title: Contributing
layout: default
nav_order: 9
description: "How to build, test, and contribute to AvalonDock."
permalink: /contributing/
---

# Contributing

AvalonDock is an open source project and contributions are welcome! Here is everything you need to set up your environment, run tests, and open a pull request.

---

## Ways to Contribute

- **Star** the [repository](https://github.com/Dirkster99/AvalonDock) to show your support
- **Report bugs** via [GitHub Issues](https://github.com/Dirkster99/AvalonDock/issues) with reproduction steps, OS version, and .NET target framework
- **Request features** or discuss design ideas via [GitHub Issues](https://github.com/Dirkster99/AvalonDock/issues) and [Discussions](https://github.com/Dirkster99/AvalonDock/discussions)
- **Submit pull requests** with focused bug fixes, tests, or improvements
- **Improve documentation** — the documentation site source lives in `docs/`

---

## Development Setup

### Prerequisites

- **OS:** Windows (required — WPF projects only build and run on Windows)
- **.NET SDKs:** **.NET 9.0**, **.NET 10.0**, and **.NET 11.0** SDKs (plus **.NET 8.0** SDK or targeting pack when building all `ComponentTargetFrameworks`; see `source/Directory.Build.props`)
- **IDE:** Visual Studio 2022 / 2026 or any editor with C# and .NET CLI support

### Restore & Build

All warnings are treated as errors in CI (`TreatWarningsAsErrors` is enabled in `source/Directory.Build.props`, and `AvalonDock.Core` plus all component packages enforce `GenerateDocumentationFile` so public members must carry XML documentation comments).

```powershell
git clone https://github.com/Dirkster99/AvalonDock.git
cd AvalonDock

dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

### Running Tests

{: .important }
Always pass `-m:1` (single-threaded MSBuild execution). Multiple test targets use WPF STA threads and `.NET Framework 4.8`, which cannot execute in parallel safely.

#### 1. Fast Unit & Layout Regression Tests

Run the non-UI unit test suite first:

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1
```

#### 2. FlaUI Desktop UI Automation Tests

FlaUI tests launch real WPF windows (`AvalonDockCodeApp` and test hosts) and drive mouse/keyboard input. Run them separately, once per modern .NET desktop target:

```powershell
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net10.0-windows -m:1
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net11.0-windows -m:1
```

---

## Pull Request Guidelines

1. **Fork** the repository and create a branch from `master`.
2. Keep each PR **focused** — one bug fix or feature per pull request.
3. Follow the existing **code style** enforced by `.editorconfig` and `StyleCop.Analyzers` (tabs for indentation, UTF-8, `#nullable disable` in legacy files where present, complete XML doc comments on public APIs).
4. Add or update **unit or FlaUI regression tests** in `source/AutomationTest/AvalonDockTest/` for behavioral changes.
5. Verify that `dotnet build ... --warnaserror` succeeds with **zero warnings** and tests pass before opening the PR.

### Commit Message Format

```text
[Fix|Feature] #<IssueNumber> - [<Topic>] <What was done>
```

Examples:
- `[Fix] #696 - [Serializer] Relink LayoutAnchorGroup.PreviousContainer on layout restore`
- `[Feature] #673 - [Build] Added net8.0-windows target to NuGet packages`
- `[Fix] #683 - [Themes] VS2010 theme supports ToggleDockingManager without Arc resources`

---

## Code Quality Standards

Every non-test project in `source/` runs static analysis during compilation:

- **StyleCop.Analyzers** — formatting, ordering, and XML documentation rules
- **Microsoft.CodeAnalysis.NetAnalyzers** — .NET API and performance guidelines
- **SonarAnalyzer.CSharp** — reliability and maintainability rules
- **Microsoft.CodeAnalysis.Metrics** — code complexity metrics (`source/CodeMetricsConfig.txt`)
- **`TreatWarningsAsErrors`** — globally enabled in `source/Directory.Build.props`

---

## License

By contributing to AvalonDock, you agree that your contributions will be licensed under the project's [Microsoft Public License (Ms-PL)](https://github.com/Dirkster99/AvalonDock/blob/master/LICENSE).
