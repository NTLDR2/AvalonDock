# Contributing to AvalonDock (NTLDR2 Fork)

Thank you for contributing to **[NTLDR2/AvalonDock](https://github.com/NTLDR2/AvalonDock)** (an opinionated `v5.1.0` fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) prioritizing `.NET Framework 4.8` + WPF and zero `System.Text.Json` dependencies).

## Prerequisites

- **OS:** Windows (`UseWPF=true` requires Windows)
- **.NET SDKs:** the **.NET 8.0 SDK** (the only SDK needed), plus the **.NET Framework 4.8 Developer Pack**
- **IDE:** Visual Studio 2022 / 2026, JetBrains Rider, or VS Code with .NET CLI support

## Building (`--warnaserror`)

`TreatWarningsAsErrors` is enabled globally in `source/Directory.Build.props`, and public APIs require XML documentation comments:

```powershell
dotnet restore source/AvalonDock.sln
dotnet build source/AvalonDock.sln --configuration Release --no-restore --warnaserror
```

## Running Tests (`-m:1`)

Always execute tests single-threaded (`-m:1`) because WPF STA dispatchers on `net48` cannot run safely in parallel:

```powershell
# Fast unit, XML DTO serialization, MVVM, DI, and STA layout regression tests
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category!=FlaUI" -m:1

# Desktop UI automation tests (FlaUI)
dotnet test source/AvalonDock.sln --configuration Release --no-restore --filter "Category=FlaUI" --framework net48 -m:1
```

## Fork Guardrails & Pull Request Expectations

- **Zero `System.Text.Json`:** Never add `System.Text.Json` package references or imports to any library, sample, or test project.
- **`.NET Framework 4.8` & `netstandard2.0` Boundaries:** Verify changes compile and pass tests on `net48`, and never reference `System.Windows.*` inside `netstandard2.0` projects (`Core`, `Mvvm*`, `DependencyInjection`, `Serializer.Xml`).
- **Code Style:** Follow `source/.editorconfig` (tabs for indentation, UTF-8, LF line endings, Allman braces, `using` directives outside `namespace`).
- **Documentation (`documentation/`):** The 41-page Jekyll documentation site lives in `documentation/` (deployed automatically via `.github/workflows/docs.yml`). When updating migration or version notes, explicitly distinguish **Upstream `v5.0.0`–`v5.1.0`** changes from **`NTLDR2 Fork v5.1.0`** changes. Also keep [AGENTS.md](AGENTS.md), [.agents/skills/avalondock-internals/SKILL.md](.agents/skills/avalondock-internals/SKILL.md), and [ROADMAP.md](ROADMAP.md) aligned when modifying architectural behavior.

## Reporting Issues

- Report fork-specific issues or roadmap proposals on [NTLDR2/AvalonDock Issues](https://github.com/NTLDR2/AvalonDock/issues).
- For upstream `Dirkster.AvalonDock` discussions and upstream NuGet releases, see [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock).

## License

By contributing, you agree that your contributions will be licensed under the [Microsoft Public License (Ms-PL)](LICENSE) that covers the project.
