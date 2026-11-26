---
title: Contributing
layout: default
nav_order: 9
description: "How to build, test, and contribute to AvalonDock."
permalink: /contributing/
---

# Contributing

**[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock)** is an open-source `v5.1.0` fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock), and focused contributions are welcome! Here is everything you need to set up your environment, run tests, and open a pull request.

---

## Ways to Contribute

- **Star** the [NTLDR2/AvalonDock repository](https://github.com/NTLDR2/AvalonDock) (and upstream [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock))
- **Report fork bugs or roadmap feedback** via [NTLDR2/AvalonDock Issues](https://github.com/NTLDR2/AvalonDock/issues) with reproduction steps, OS version, and .NET target framework (`net48` or `.NET 8+`)
- **Submit pull requests** with focused bug fixes, tests, or documentation improvements
- **Improve documentation** — the Jekyll documentation site source lives in `documentation/` and deploys via `.github/workflows/docs.yml` (always label `Upstream v5.0.0`–`v5.1.0` vs. `NTLDR2 Fork v5.1.0` scope explicitly when editing version or migration claims)

---

## Development Setup

### Prerequisites

- **OS:** Windows (required — WPF projects only build and run on Windows)
- **.NET SDKs & Targeting Packs:** **.NET Framework 4.8 Developer Pack** plus the **.NET 8.0 SDK** (the only SDK needed) (see `source/Directory.Build.props`)
- **IDE:** Visual Studio 2022 / 2026, JetBrains Rider, or any editor with C# and .NET CLI support

### Restore & Build

All warnings are treated as errors in CI (`TreatWarningsAsErrors` is enabled in `source/Directory.Build.props`, and `AvalonDock.Core` plus all component packages enforce `GenerateDocumentationFile` so public members must carry XML documentation comments).

```powershell
git clone https://github.com/NTLDR2/AvalonDock.git
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
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net8.0-windows -m:1
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

By contributing to AvalonDock, you agree that your contributions will be licensed under the project's [Microsoft Public License (Ms-PL)](https://github.com/NTLDR2/AvalonDock/blob/master/LICENSE).
