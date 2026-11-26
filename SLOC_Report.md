# AvalonDock Source Lines of Code (SLOC) Report

**Updated:** 2026-11-25  
**Repository Path:** `C:\SOFTDEV\AvalonDock-V5-NEW`  
**Scope:** Source code and configuration files across the 11 shipped `Dirkster.AvalonDock*` NuGet packages, sample applications, and automated test suites (after surgical removal of `AvalonDock.Serializer.Json`, `AvalonDock.Themes.VS`, `AvalonDock.Themes.VS2013`, and `VS2013Test`).

---

## 1. Breakdown of Shipped Component Projects (`source/Components/*` — 11 Packages)

These 11 projects produce the published `Dirkster.AvalonDock*` NuGet packages:

| Component Project | Files | Total SLOC | C# SLOC | XAML SLOC | Other SLOC | Total Lines |
|:---|---:|---:|---:|---:|---:|---:|
| `AvalonDock` | 192 | **30,169** | 26,162 | 1,631 | 2,376 | 51,126 |
| `AvalonDock.Themes.Arc` | 16 | **4,375** | 150 | 4,208 | 17 | 5,494 |
| `AvalonDock.Themes.VS2010` | 6 | **2,043** | 33 | 1,993 | 17 | 2,259 |
| `AvalonDock.Themes.Aero` | 9 | **2,035** | 141 | 1,877 | 17 | 2,335 |
| `AvalonDock.Themes.Expression` | 12 | **2,002** | 111 | 1,874 | 17 | 2,347 |
| `AvalonDock.Themes.Metro` | 6 | **1,903** | 31 | 1,855 | 17 | 2,067 |
| `AvalonDock.Core` | 42 | **1,331** | 1,316 | 0 | 15 | 2,683 |
| `AvalonDock.Mvvm` | 8 | **510** | 492 | 0 | 18 | 819 |
| `AvalonDock.Mvvm.CommunityToolkit` | 5 | **231** | 210 | 0 | 21 | 368 |
| `AvalonDock.DependencyInjection` | 5 | **208** | 186 | 0 | 22 | 443 |
| `AvalonDock.Serializer.Xml` | 2 | **56** | 38 | 0 | 18 | 92 |
| **Components Subtotal** | **303** | **44,863** | **28,870** | **13,438** | **2,555** | **70,033** |

---

## 2. Subfolder Breakdown of `source/Components/AvalonDock`

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

## 3. Sample Projects & Root `source/` Files

| Folder | Role |
|:---|:---|
| `AvalonDockCodeApp/` | Primary v5 MVVM + DI + `ToggleDockingManager` + `Arc` reference app |
| `MVVMTestApp/` | Classic MVVM + `XmlLayoutSerializer` sample |
| `TestApp/` | Classic `DockingManager` feature showcase |
| `WinFormsTestApp/` | `WindowsFormsHost` / `ElementHost` interop sample (`VS2010Theme` default) |
| `MLibTest/` | MLib theming integration sample |
| `CaliburnDockTestApp/` | Caliburn.Micro integration sample |
| `AvalonDocPanelMemoryLeaks/` | Memory-leak regression harness |
