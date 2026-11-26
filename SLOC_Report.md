# AvalonDock Source Lines of Code (SLOC) Report

**Generated:** 2026-11-25  
**Repository Path:** `C:\SOFTDEV\AvalonDock-V5-NEW`  
**Scope:** All Git-tracked text files, **excluding** `.txt` and `.md` files (as well as `LICENSE`, binary assets such as `.png`/`.ico`/`.snk`/`.gz`/`.cur`/`.design`, and untracked `bin/`/`obj/` build outputs).

---

## 1. Executive Summary

| Scope | Files | SLOC (Code) | Comments | Blank | Total Physical Lines |
|:---|---:|---:|---:|---:|---:|
| **Entire Repository** (all non-`.txt`/`.md` text files) | 659 | **87,537** | 25,587 | 13,698 | 126,822 |
| **`source/` Directory Only** | 652 | **87,149** | 25,564 | 13,633 | 126,346 |
| **C# + XAML Only** (entire repository) | 578 | **77,566** | 24,175 | 13,039 | 114,780 |
| **Shipped Packages Only** (`source/Components/*`) | 362 | **57,623** | 19,789 | 8,883 | 86,295 |
| **Shipped C# Code Only** (`source/Components/**/*.cs`) | 295 | **30,206** | 17,058 | 7,424 | 54,688 |

> **Methodology:**
> - **SLOC** counts non-blank, non-comment lines containing executable/declarative source or configuration content.
> - C# (`.cs`) comment stripping is string-literal-aware and excludes single-line (`//`, `///`) and multi-line (`/* ... */`) comment-only lines.
> - XML/XAML-family files (`.xaml`, `.csproj`, `.props`, `.resx`, `.vstheme`, `.xml`, `.config`, `.manifest`) exclude `<!-- ... -->` comment-only lines.
> - Config files (`.yml`, `.editorconfig`, `.gitignore`, `Gemfile`) exclude `#` comment-only lines.

---

## 2. Breakdown by Repository Area

| Area | Files | SLOC | Comments | Blank | Total Lines |
|:---|---:|---:|---:|---:|---:|
| **1. Libraries & Themes** (`source/Components/*`) | 362 | **57,623** | 19,789 | 8,883 | 86,295 |
| **2. Sample Apps & Solution Config** (`source/*` excl. Components & Tests) | 202 | **16,687** | 3,243 | 2,218 | 22,148 |
| **3. Automated Tests** (`source/AutomationTest/*`) | 88 | **12,839** | 2,532 | 2,532 | 17,903 |
| **4. Repo / CI / Docs Config** (`.github/workflows/*`, `docs/_config.yml`, `Gemfile`, `.gitignore`) | 7 | **388** | 23 | 65 | 476 |
| **Total** | **659** | **87,537** | **25,587** | **13,698** | **126,822** |

*(Note: `source/.editorconfig` alone accounts for **4,044** SLOC inside Area 2; excluding `.editorconfig` reduces the full repository total to **83,493 SLOC**.)*

---

## 3. Breakdown by File Extension

| Extension | Files | SLOC | Comments | Blank | Total Lines |
|:---|---:|---:|---:|---:|---:|
| `.cs` | 505 | **51,932** | 22,125 | 11,556 | 85,613 |
| `.xaml` | 73 | **25,634** | 2,050 | 1,483 | 29,167 |
| `.editorconfig` | 1 | **4,044** | 257 | 167 | 4,468 |
| `.resx` | 21 | **2,522** | 960 | 220 | 3,702 |
| `.vstheme` | 6 | **1,508** | 60 | 54 | 1,622 |
| `.csproj` | 26 | **817** | 25 | 90 | 932 |
| `.yml` | 5 | **352** | 22 | 61 | 435 |
| `.sln` | 1 | **318** | 0 | 1 | 319 |
| `.bat` | 1 | **100** | 0 | 19 | 119 |
| `.runsettings` | 1 | **56** | 0 | 13 | 69 |
| `.props` | 1 | **55** | 6 | 3 | 64 |
| `.xml` | 3 | **52** | 0 | 0 | 52 |
| `.config` | 5 | **41** | 0 | 0 | 41 |
| `.gitignore` | 2 | **38** | 4 | 2 | 44 |
| `.manifest` | 2 | **34** | 78 | 27 | 139 |
| `.json` | 3 | **21** | 0 | 0 | 21 |
| `.settings` | 1 | **7** | 0 | 0 | 7 |
| `Gemfile` | 1 | **5** | 0 | 2 | 7 |
| `.name` | 1 | **1** | 0 | 0 | 1 |
| **Total** | **659** | **87,537** | **25,587** | **13,698** | **126,822** |

---

## 4. Breakdown of Shipped Component Projects (`source/Components/*`)

These 14 projects produce the published `Dirkster.AvalonDock*` NuGet packages:

| Component Project | Files | Total SLOC | C# SLOC | XAML SLOC | Other SLOC | Total Lines |
|:---|---:|---:|---:|---:|---:|---:|
| `AvalonDock` | 192 | **30,169** | 26,162 | 1,631 | 2,376 | 51,126 |
| `AvalonDock.Themes.VS` | 39 | **8,868** | 918 | 6,397 | 1,553 | 11,043 |
| `AvalonDock.Themes.Arc` | 16 | **4,375** | 150 | 4,208 | 17 | 5,494 |
| `AvalonDock.Themes.VS2013` | 18 | **3,790** | 337 | 3,430 | 23 | 5,067 |
| `AvalonDock.Themes.VS2010` | 6 | **2,043** | 33 | 1,993 | 17 | 2,259 |
| `AvalonDock.Themes.Aero` | 9 | **2,035** | 141 | 1,877 | 17 | 2,335 |
| `AvalonDock.Themes.Expression` | 12 | **2,002** | 111 | 1,874 | 17 | 2,347 |
| `AvalonDock.Themes.Metro` | 6 | **1,903** | 31 | 1,855 | 17 | 2,067 |
| `AvalonDock.Core` | 42 | **1,331** | 1,316 | 0 | 15 | 2,683 |
| `AvalonDock.Mvvm` | 8 | **510** | 492 | 0 | 18 | 819 |
| `AvalonDock.Mvvm.CommunityToolkit` | 5 | **231** | 210 | 0 | 21 | 368 |
| `AvalonDock.DependencyInjection` | 5 | **208** | 186 | 0 | 22 | 443 |
| `AvalonDock.Serializer.Json` | 2 | **102** | 81 | 0 | 21 | 152 |
| `AvalonDock.Serializer.Xml` | 2 | **56** | 38 | 0 | 18 | 92 |
| **Components Subtotal** | **362** | **57,623** | **30,206** | **23,265** | **4,152** | **86,295** |

---

## 5. Subfolder Breakdown of `source/Components/AvalonDock`

Because `source/Components/AvalonDock` (`30,169` SLOC / ~1.73 MB) is the largest single project, breaking it down by subfolder is essential for context-window planning:

| Subfolder | Files | Total SLOC | C# SLOC | XAML SLOC | `.resx` SLOC | Total Lines | Raw Size (KB) |
|:---|---:|---:|---:|---:|---:|---:|---:|
| `Controls/` | 75 | **15,078** | 15,078 | 0 | 0 | 28,638 | 931.9 KB |
| `(Project Root)` (`DockingManager.cs`, `ToggleDockingManager.cs`, `LayoutSyncBridge.cs`, engines, events) | 26 | **5,998** | 5,962 | 0 | 0 | 9,428 | 340.9 KB |
| `Layout/` | 43 | **3,318** | 3,318 | 0 | 0 | 5,305 | 158.0 KB |
| `Properties/` (localized `Resources.*.resx` + `AssemblyInfo`) | 20 | **2,514** | 174 | 0 | 2,340 | 3,721 | 159.9 KB |
| `Themes/` (`generic.xaml`, `Theme.cs`, `DictionaryTheme.cs`, `GenericTheme.cs`) | 5 | **1,683** | 52 | 1,631 | 0 | 1,855 | 85.4 KB |
| `Commands/` | 8 | **649** | 649 | 0 | 0 | 979 | 19.6 KB |
| `Serialization/` (`LayoutDtoMapper.cs`) | 1 | **553** | 553 | 0 | 0 | 661 | 19.3 KB |
| `Converters/` | 14 | **376** | 376 | 0 | 0 | 539 | 16.3 KB |
| **`AvalonDock` Subtotal** | **192** | **30,169** | **26,162** | **1,631** | **2,340** | **51,126** | **1,731.3 KB** |

---

## 6. Breakdown of Sample Projects & Root `source/` Files

| Folder | Files | Total SLOC | C# SLOC | XAML SLOC | Total Lines | Raw Size (KB) |
|:---|---:|---:|---:|---:|---:|---:|
| `MLibTest/` | 78 | **4,975** | 4,303 | 424 | 8,337 | 262.9 KB |
| `(source/ Root)` (`.editorconfig`, `AvalonDock.sln`, `Directory.Build.props`, etc.) | 5 | **4,525** | 0 | 0 | 4,978 | 302.8 KB |
| `AvalonDockCodeApp/` *(v5 MVVM + DI + ToggleDockingManager reference app)* | 26 | **2,347** | 1,612 | 683 | 2,802 | 98.5 KB |
| `VS2013Test/` | 29 | **1,883** | 1,316 | 544 | 2,188 | 64.0 KB |
| `MVVMTestApp/` *(Classic MVVM + XmlLayoutSerializer sample)* | 17 | **932** | 757 | 129 | 1,159 | 28.9 KB |
| `TestApp/` | 11 | **731** | 389 | 237 | 980 | 39.6 KB |
| `CaliburnDockTestApp/` | 14 | **557** | 357 | 181 | 650 | 21.8 KB |
| `WinFormsTestApp/` | 6 | **370** | 245 | 0 | 529 | 24.0 KB |
| `AvalonDocPanelMemoryLeaks/` | 12 | **344** | 126 | 40 | 499 | 20.6 KB |
| `.idea/` | 4 | **23** | 0 | 0 | 26 | 0.6 KB |
| **Samples & `source/` Config Subtotal** | **202** | **16,687** | **9,105** | **2,238** | **22,148** | **863.7 KB** |

---

## 7. Context-Window Chunking Guide for AI Documentation Regeneration

Although the full repository contains **87,537 SLOC**, **more than 60% of those lines never need to be loaded into an AI context window** when regenerating user/API documentation:

- **Skip completely (~54,000+ SLOC):**
  - `source/AutomationTest/*` (**12,839 SLOC**) — unit & FlaUI tests (only inspect selectively if checking edge-case behavior).
  - Legacy sample apps (`MLibTest`, `VS2013Test`, `CaliburnDockTestApp`, `WinFormsTestApp`, `AvalonDocPanelMemoryLeaks`) + `source/.editorconfig` (**~13,000 SLOC**).
  - `source/Components/AvalonDock/Properties/*.resx` (**2,340 SLOC**) — 20 language translations of the same UI strings.
  - Repetitive theme XAML control templates (`AvalonDock.Themes.*/*.xaml`, **21,634 SLOC**) — only the C# theme classes, `ResourceKeys.cs`, and `.csproj` files (~1,720 C# SLOC across all 7 theme packages) need to be read in full, plus spot-checking resource dictionary filenames.

### Recommended 7-Chunk Documentation Plan

Each chunk below pairs the authoritative source files with their target `docs/` pages and stays within **~1,200–8,000 SLOC (~40 KB–500 KB of raw source text)** so a single model session can hold both the source code and target Markdown comfortably:

| Chunk | Source Scope to Load | Approx. Source SLOC | Target Documentation Pages |
|:---|:---|---:|:---|
| **Chunk 1: Build, Packages, Migration & Overview** | `source/Directory.Build.props`, all `source/Components/*/*.csproj`, `.github/workflows/*.yml`, `LICENSE`, `AGENTS.md`, v5.0.0–v5.0.2 release notes | **~1,400 SLOC** | `README.md`, `CONTRIBUTING.md`, `docs/_config.yml`, `docs/index.md`, `docs/contributing.md`, `docs/getting-started/*`, `docs/migration/*` |
| **Chunk 2: Core Abstractions, MVVM & DI** | `AvalonDock.Core` (excl. `Serialization/`), `AvalonDock.Mvvm`, `AvalonDock.Mvvm.CommunityToolkit`, `AvalonDock.DependencyInjection`, `AvalonDock/LayoutSyncBridge.cs` | **~2,200 SLOC** | `docs/concepts/architecture.md`, `docs/guides/mvvm.md`, `docs/guides/dependency-injection.md`, `docs/tutorials/dependency-injection-app.md` |
| **Chunk 3: ToggleDockingManager & MVVM IDE Tutorial** | `AvalonDock/ToggleDockingManager.cs`, `AvalonDock/ToggleLayoutEngine.cs`, `AvalonDock/Controls/Toggle*.cs`, `source/AvalonDockCodeApp/*` | **~4,800 SLOC** | `docs/guides/toggle-docking-manager.md`, `docs/tutorials/mvvm-ide.md`, `docs/tutorials/index.md` |
| **Chunk 4: Serialization (XML, JSON & DTOs)** | `AvalonDock.Core/Serialization/**`, `AvalonDock/Serialization/LayoutDtoMapper.cs`, `AvalonDock.Serializer.Xml`, `AvalonDock.Serializer.Json`, `source/MVVMTestApp/*` | **~2,100 SLOC** | `docs/guides/serialization.md`, `docs/tutorials/layout-persistence.md` |
| **Chunk 5: Layout Model & DockingManager Root** | `AvalonDock/Layout/**`, `AvalonDock/DockingManager.cs`, `AvalonDock/DefaultLayoutEngine.cs`, `AvalonDock/ILayoutEngine.cs`, root `*EventArgs.cs` | **~7,200 SLOC** | `docs/concepts/index.md`, `docs/concepts/layout-model.md`, `docs/concepts/documents-and-anchorables.md`, `docs/api-reference/index.md`, `docs/api-reference/docking-manager.md`, `docs/api-reference/layout-classes.md`, `docs/api-reference/events.md` |
| **Chunk 6: Floating/Detached Windows & UI Controls** | `AvalonDock/Controls/*Floating*`, `DetachedAnchorableWindow.cs`, `OverlayWindow.cs`, `*DropTarget*.cs`, `LayoutItem*.cs`, `TabControlEx.cs`, `NavigatorWindow.cs`, `AvalonDock/Commands/*`, `AvalonDock/Converters/*` | **~8,000 SLOC** *(or split into 6A Floating/Drop + 6B Controls/Items at ~4,000 SLOC each)* | `docs/concepts/floating-windows.md`, `docs/api-reference/controls.md` |
| **Chunk 7: Themes & Custom Styling** | All `.cs` files + file listings of `AvalonDock.Themes.*` (Arc, VS, VS2013, VS2010, Expression, Metro, Aero) + `AvalonDock/Themes/*.cs` | **~1,800 C# SLOC** *(+ directory listings of `.xaml`/`.vstheme` assets)* | `docs/themes/*`, `docs/guides/theming.md`, `docs/guides/custom-themes.md`, `docs/tutorials/styling-and-theming.md` |
