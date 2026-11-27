# AvalonDock v5 Theme Architecture Analysis & WPF Best Practices (`net48` vs. Modern `.NET`)

> **Companion Document:** See [THEMES.md](THEMES.md) for the complete inventory of all 20 concrete themes across 8 assemblies and [ROADMAP.md](../../ROADMAP.md) for the planned Phase 5 `AvalonDox` cutover.

This document analyzes AvalonDock's two theme base-class hierarchies and namespaces — **`Theme` (`AvalonDock.Themes`)** versus **`VsTheme : DictionaryTheme` (`AvalonDock.Themes.VS`)** — across three engineering questions:

1. **Chronology:** Which architecture is newer, and why do two patterns and two namespaces exist?
2. **Trade-Offs:** What are the concrete runtime, memory, XAML tooling, and extensibility pros and cons of `Theme` (`GetResourceUri()`) vs. `VsTheme : DictionaryTheme` (`ThemeResourceDictionary`)?
3. **WPF Best Practices (`net48` vs. `.NET 8/9/10`):** Which approach aligns better with modern WPF engineering standards, and how do those standards differ between `.NET Framework 4.8` and modern `.NET` runtimes?

---

## 1. Chronology: Which Architecture Is Newer?

There is an important distinction between the **two abstract base classes in `AvalonDock.dll`** (which are equally old) and **`VsTheme : DictionaryTheme` in `AvalonDock.Themes.VS`** (which is the newest theme subsystem in the repository).

### 1.1 Class & Namespace Hierarchy

Both `Theme` (`AvalonDock/Themes/Theme.cs`) and `DictionaryTheme` (`AvalonDock/Themes/DictionaryTheme.cs`) live side-by-side in `namespace AvalonDock.Themes` inside core `AvalonDock.dll`, and **`DictionaryTheme` inherits directly from `Theme`**:

```text
System.Windows.DependencyObject
  └── AvalonDock.Themes.Theme                              (v2/v3 era — URI-based: GetResourceUri())
        ├── GenericTheme                                   (AvalonDock.Themes, in AvalonDock.dll)
        ├── ArcDarkTheme, ArcLightTheme                    (AvalonDock.Themes, added April 2026)
        ├── VS2010Theme                                    (AvalonDock.Themes, legacy v3/v4)
        ├── ExpressionDarkTheme, ExpressionLightTheme      (AvalonDock.Themes, legacy v3/v4)
        ├── MetroTheme                                     (AvalonDock.Themes, legacy v3/v4)
        ├── AeroTheme                                      (AvalonDock.Themes, legacy v3/v4)
        └── AvalonDock.Themes.DictionaryTheme              (v2/v3 era — instance-based: ThemeResourceDictionary)
              ├── Vs2013BlueTheme, Vs2013DarkTheme,        (AvalonDock.Themes — migrated onto DictionaryTheme
              │   Vs2013LightTheme                          in June 2026, kept in AvalonDock.Themes for v4 compat)
              └── AvalonDock.Themes.VS.VsTheme             (AvalonDock.Themes.VS — created June 2026)
                    ├── VS2015BlueTheme, VS2015DarkTheme, VS2015LightTheme   (June 2026)
                    ├── VS2022BlueTheme, VS2022DarkTheme, VS2022LightTheme   (June 2026)
                    └── VS2026BlueTheme, VS2026DarkTheme, VS2026LightTheme   (July 2026)
```

### 1.2 Git Commit Timeline

| Era / Date | Commit / PR | Architectural Milestone |
| :--- | :--- | :--- |
| **2012–2018** | `5dc8fae` (AvalonDock 2.x–3.4) | Both `Theme` and `DictionaryTheme` already existed in Xceed's AvalonDock, yet **every** built-in theme (`Aero`, `Metro`, `Expression`, `VS2010`, and v3/v4 `VS2013`) subclassed URI-based `Theme`. |
| **Jul–Aug 2020** | `54be57e` → `ec3c74f` (`#189`) | Because zero built-in themes subclassed `DictionaryTheme`, upstream deleted it as unused code in July 2020 — then restored it a month later when external consumers reported subclassing `DictionaryTheme` to pass runtime-generated `MahApps.Metro` / accent `ResourceDictionary` instances into AvalonDock. |
| **Apr 11, 2026** | `0a42be0` (`#537`) | `AvalonDock.Themes.Arc` (`ArcDarkTheme`, `ArcLightTheme`) was introduced using the classic URI-based `Theme` (`GetResourceUri()`) pattern in `namespace AvalonDock.Themes`. |
| **Jun 1, 2026** | `6086b15` (`#538`) | **`AvalonDock.Themes.VS` created** in `namespace AvalonDock.Themes.VS` (`VsTheme : DictionaryTheme`, `VsThemeParser`, `VsThemeResourceBuilder`, `{vs:VsTheme}`, `VS2015*Theme`) — the **first time first-party AvalonDock themes used `DictionaryTheme`**. |
| **Jun 4, 2026** | `6478689` (`#572`) | **`AvalonDock.Themes.VS2013` migrated** off static XAML brush dictionaries (`DarkBrushs.xaml` / `DarkTheme.xaml` via `Theme`) onto `DictionaryTheme` + embedded `.vstheme.gz` resources (`VsThemePaletteFactory`), while keeping `Vs2013*Theme` in `namespace AvalonDock.Themes` for v4 source compatibility. |
| **Jun 9, 2026** | `e32ac8f` (`#582`) | `VS2022{Blue,Dark,Light}Theme : VsTheme` added with rounded/modern `Themes/VS2022/Generic.xaml` templates sharing the same C# token builder. |
| **Jul 1, 2026** | `64f4b79` (`#597`) | `VS2026{Blue,Dark,Light}Theme : VsTheme` and `VsJsonThemeParser` added (refactored in the **NTLDR2 Fork `v5.1.0`** to a zero-dependency pure-C# parser on `net48`). |

### 1.3 Why Do Two Namespaces (`AvalonDock.Themes` vs. `AvalonDock.Themes.VS`) Exist?

Inspecting `AvalonDock.Themes.*/Properties/AssemblyInfo.cs` across all 7 theme libraries reveals that the namespace split was an **accidental upstream inconsistency**:

- All 7 theme assemblies — **including `AvalonDock.Themes.VS/Properties/AssemblyInfo.cs` (lines 13–15)** — declare:
  ```csharp
  [assembly: XmlnsDefinition("https://github.com/Dirkster99/AvalonDock", "AvalonDock.Themes")]
  [assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "AvalonDock.Themes")]
  ```
- Upstream copied `AssemblyInfo.cs` from `AvalonDock.Themes.Arc` into `AvalonDock.Themes.VS`, placed the new classes inside `namespace AvalonDock.Themes.VS`, and **omitted `[assembly: XmlnsDefinition(..., "AvalonDock.Themes.VS")]`**.
- Consequently, XAML consumers can instantiate `<avalonDock:ArcDarkTheme />` or `<avalonDock:Vs2013DarkTheme />` under the standard AvalonDock XML namespace, whereas `AvalonDock.Themes.VS` requires declaring a separate `xmlns:vs="clr-namespace:AvalonDock.Themes.VS;assembly=AvalonDock.Themes.VS"`.

---

## 2. Pros & Cons: `Theme` (URI / Pure BAML) vs. `VsTheme : DictionaryTheme`

### 2.1 How AvalonDock Applies Both Under the Hood

Crucially, **neither pattern builds `Style`s or `ControlTemplate`s in C#** — both keep 100% of control templates in compiled binary XAML (**BAML**). They diverge in **how brush tokens are materialized** and **how AvalonDock's 5 visual roots attach the dictionary**:

1. `DockingManager.OnThemeChanged` (`DockingManager.cs`)
2. `LayoutFloatingWindowControl.UpdateThemeResources` (`LayoutFloatingWindowControl.cs`)
3. `DetachedAnchorableWindow.UpdateThemeResources` (`DetachedAnchorableWindow.cs`)
4. `NavigatorWindow.UpdateThemeResources` (`NavigatorWindow.cs`)
5. `OverlayWindow.UpdateThemeResources` (`OverlayWindow.cs`)

In all 5 locations, AvalonDock branches on `theme is DictionaryTheme`:

```csharp
if (newTheme is DictionaryTheme dictionaryTheme)
{
    // Attaches the SAME pre-built in-memory ResourceDictionary reference:
    _currentThemeResourceDictionary = dictionaryTheme.ThemeResourceDictionary;
    Resources.MergedDictionaries.Add(_currentThemeResourceDictionary);
}
else
{
    // Allocates a NEW ResourceDictionary and parses the pack:// BAML stream per window:
    Resources.MergedDictionaries.Add(new ResourceDictionary { Source = newTheme.GetResourceUri() });
}
```

### 2.2 Side-by-Side Architectural Comparison

| Dimension | `Theme` (`GetResourceUri()` — `Arc`, `VS2010`, `Expression`, `Metro`, `Aero`) | `VsTheme : DictionaryTheme` (`ThemeResourceDictionary` — `VS`, `VS2013`) |
| :--- | :--- | :--- |
| **Palette Representation** | Compiled BAML XAML (`DarkBrushs.xaml` + `DarkTheme.xaml`) | Compressed `.vstheme.gz` (~3–4 KB) + optional `.json` delta + C# mapper (`VsThemeResourceBuilder`) |
| **Constructor Cost (`new XxxTheme()`)** | **Lazy (`~0 ms`, `~0 KB`):** Allocates a tiny object; nothing is parsed until `DockingManager.Theme` activates it. Registering 10 unused themes in DI/menus is free. | **Eager (`base(BuildFromStream(...))` runs in `.ctor`):** Decompresses `.vstheme.gz`, allocates ~85 frozen brushes, and loads `Generic.xaml` BAML *immediately* on construction — even for themes never selected. |
| **Multi-Window Tear-Off Cost** | **Re-parses BAML per window:** WPF's `ResourceDictionary.Source` does **not** globally cache non-system dictionaries (`DarkTheme.xaml` -> `DarkBrushs.xaml` + `Generic.xaml`). Opening 10 floating/detached windows re-parses those BAML streams **10 separate times**. | **`O(1)` shared reference:** `DockingManager`, floating windows, `DetachedAnchorableWindow`s, `OverlayWindow`, and `NavigatorWindow` all attach the **exact same `ThemeResourceDictionary` reference** with zero extra BAML parsing. |
| **App-Level XAML Merging & Tooling** | **First-class:** `<ResourceDictionary Source="pack://..."/>` works in `App.xaml` and custom theme XAML; full Visual Studio XAML Designer & Hot Reload support; `IThemeInfo.ResourceUri` is non-null. | **Restricted in pure XAML:** `GetResourceUri()` (and `IThemeInfo.ResourceUri`) returns **`null`**. Cannot be merged via `<ResourceDictionary Source="..."/>` in `App.xaml` without C# code or `{vs:VsTheme}`. |
| **External / Runtime Palettes** | **Static compile-time only:** Tied to compiled `pack://` URIs; cannot load user `.vstheme`/`.json` files on disk or generate dynamic OS accent palettes without switching to `DictionaryTheme`. | **First-class:** Parses external `.vstheme` XML files, VS2026 `.json` files, streams, or in-memory `VsThemeColorPalette` instances at runtime in ~1 ms. |
| **Color Fallback & Math Logic** | Difficult in pure XAML (requires verbose `IValueConverter`s or duplicating keys). | Trivial in C#: null-coalescing token fallbacks (`environmentTab ?? ...`) and perceptual luminance math (`DarkenOrLighten` in `VsThemeResourceBuilder`). |
| **Non-Color Theme Variations** | **Unrestricted XAML:** Any variant can freely customize `LinearGradientBrush`es (`Aero`/`VS2010`), `DrawingBrush`es, `CornerRadius`, `Thickness`, or `DropShadowEffect`s. | **Schema-bound:** Token builders map `Color` → `SolidColorBrush`; structural or gradient differences require a separate template BAML (`Themes/VS2022/Generic.xaml`) or C# builder changes. |
| **`Freezable.Freeze()` Discipline** | **Manual per XAML element:** Forgetting `po:Freeze="True"` on `<SolidColorBrush>` tags silently leaves unfrozen change-notification listeners attached to visuals. | **Guaranteed centrally:** A single 4-line `SetBrush()` / `Brush()` helper calls `brush.Freeze()` on 100% of generated brushes. |
| **Resource Lookup Priority** | `DarkTheme.xaml` places `DarkBrushs.xaml` and `Generic.xaml` as sibling `MergedDictionaries` (searched in reverse order). | Places ~85 brushes **directly** in `dict[key]` and `Generic.xaml` in `dict.MergedDictionaries` (direct entries always take top priority over `MergedDictionaries` with direct hash lookup). |

### 2.3 Summary of Pros & Cons

#### When `Theme` (`GetResourceUri()`) Wins
- **Best for 1–2 bespoke, designer-authored skins (`ArcDark` / `ArcLight`)** where contributors edit XAML visually with Hot Reload and host apps want `<ResourceDictionary Source="/AvalonDock.Themes.Arc;component/DarkBrushs.xaml" />` in `App.xaml` to style surrounding menus/status bars.
- **Zero startup cost** when pre-instantiating theme descriptors for a theme-switcher UI or DI container.
- **Main weakness in AvalonDock today:** Lack of a `SharedResourceDictionary` cache causes redundant BAML parsing across floating/detached windows.

#### When `VsTheme : DictionaryTheme` Wins
- **Best for large palette families (`9–12+` VS themes), external user theme files (`.vstheme` / `.json`), and runtime host/OS accent synchronization.** Eliminates thousands of lines of duplicated XAML brush dictionaries in favor of 3–4 KB compressed palettes, guarantees 100% frozen brushes, and shares a single `ResourceDictionary` instance across all floating and detached windows.
- **Main weakness in AvalonDock today:** `DictionaryTheme` populates `ThemeResourceDictionary` eagerly inside the constructor (`base(BuildDictionary(...))`), meaning `new VS2022DarkTheme()` immediately parses streams and BAML (and requires WPF's `pack://` URI scheme / STA context at construction time) even if the theme is never applied.

---

## 3. Alignment with WPF Best Practices (`net48` vs. Modern `.NET 8 / 9 / 10`)

### 3.1 General WPF Best Practices Assessment

Across modern industrial WPF toolkits (`MahApps.Metro` / `ControlzEx`, `MaterialDesignInXamlToolkit`, `WPF-UI`, `Actipro`, `DevExpress`), industry best practice has converged on a **Hybrid Token + Shared BAML Template** model:

1. **Structural `Style`s and `ControlTemplate`s MUST remain in compiled BAML (`Generic.xaml`)** — never constructed in C# (`FrameworkElementFactory` is deprecated). *Both `Theme` and `VsTheme : DictionaryTheme` follow this rule.*
2. **Semantic `ComponentResourceKey` brush tokens (`{DynamicResource}`) MUST be decoupled from templates, `Freeze()`-locked, and shared across windows.** *`VsTheme : DictionaryTheme` aligns closely with `ControlzEx.ThemeManager` and `MaterialDesignThemes.PaletteHelper` here, particularly for multi-window docking layouts.*
3. **Theme descriptors SHOULD be lightweight and lazy.** *Here `Theme` (`GetResourceUri()`) is currently cleaner than `DictionaryTheme`: textbook WPF best practice would make `DictionaryTheme` resolve its `ResourceDictionary` lazily on first activation (`Lazy<ResourceDictionary>` or virtual `GetOrCreateResourceDictionary()`) rather than inside `.ctor`.*
4. **Public XAML types SHOULD map uniformly via `[XmlnsDefinition]`.** *Unifying `AvalonDock.Themes` and `AvalonDock.Themes.VS` (or registering both CLR namespaces in `[XmlnsDefinition]`) aligns with WPF control-library design guidelines.*

### 3.2 Do Best Practices Change Between `.NET Framework 4.8` and Modern `.NET` (`.NET 8 / 9 / 10`)?

The **core WPF XAML/resource engine does not change**, whereas **OS theme integration and NuGet dependency rules diverge significantly**:

| Area | `.NET Framework 4.8` (`net48`) | Modern `.NET` (`net8.0-windows` / `net9.0-windows` / `net10.0-windows`) |
| :--- | :--- | :--- |
| **Core BAML, `ResourceDictionary`, `{DynamicResource}`, & `Freeze()` Engine** | **Identical architecture.** `new ResourceDictionary { Source = uri }` re-parses BAML per window; unfrozen `Freezable`s allocate event tables and pin UI threads. Because `net48` lacks minor `ResourceDictionary` lock/allocation micro-optimizations added in `.NET 7–9`, **`brush.Freeze()` and sharing a single `DictionaryTheme` dictionary across floating/detached windows matter *even more* on `net48`**. | **Same `Baml2006Reader` & lookup rules** (`dotnet/wpf` forked from `net48`). The exact same best practices apply: compiled BAML `ControlTemplate`s + `{DynamicResource}` `ComponentResourceKey` tokens + frozen brushes + shared dictionary instances. |
| **First-Party OS Dark Mode, Accent Brushes & Fluent Theme** | **None.** `net48`'s `PresentationFramework.dll` is frozen at the Windows 10 1903 era (`Aero2`). There is **no** `Application.ThemeMode`, **no** built-in `Fluent` theme, and **no** `SystemColors.AccentColorBrushKey`. Docking libraries on `net48` **must** ship self-contained palettes (`Arc`, `VS`, `VS2013`), and any Windows dark-mode or accent sync on `net48` *must* read Win32/DWM/Registry colors in C# and feed them into a **`DictionaryTheme`**. | **Major addition in `.NET 9+`:** WPF introduced `Application.ThemeMode` / `Window.ThemeMode` (`Light`, `Dark`, `System`), built-in Windows 11 `Fluent` theme dictionaries, and live `{DynamicResource {x:Static SystemColors.AccentColorBrushKey}}` tokens. On `.NET 9+`, libraries increasingly add a "System / Host-Adaptive" theme backed by WPF's native `Fluent` & `AccentColor*` keys. |
| **NuGet & BCL Dependency Hygiene in Theme Libraries** | **Strict Zero-NuGet Rule (Paramount on `net48`):** Referencing out-of-band NuGet packages (`System.Text.Json`, `System.Memory`, `System.Runtime.CompilerServices.Unsafe`, `Microsoft.Bcl.*`) in a `net48` theme library drags 6–8 extra DLLs into `bin/` and triggers `FileLoadException` binding-redirect failures in VSIX/CAD/plugin hosts. Sticking strictly to inbox `.NET Framework 4.8` BCL (`mscorlib`, `System.Xml.Linq`, `System.IO.Compression`, WPF) is mandatory best practice. | **`System.Text.Json` & `Span<T>` are inbox:** Because `Microsoft.NETCore.App` ships `System.Text.Json` and `Span<T>` inside the runtime, using them on `.NET 8+` adds zero external DLLs (which is why upstream introduced `System.Text.Json` without noticing the `net48` DLL-hell cost). |
| **Single-File Publishing (`PublishSingleFile`)** | Not applicable (classic folder, MSI, or ClickOnce deployment). | Common on `.NET 8+`. Both compiled `pack://` BAML (`Theme`) and `Assembly.GetManifestResourceStream` (`VsTheme` / `Vs2013*Theme`) are 100% single-file compatible; only `Assembly.Location` file paths break. |

---

## 4. Architectural Recommendations for Phase 5 (`AvalonDox`)

1. **Retain Both Base Classes (`Theme` + `DictionaryTheme`):** They are complementary — `Theme` is ideal for static XAML-first themes (`Arc`), while `DictionaryTheme` (`VsTheme`) is ideal for multi-palette families (`VS2013`–`VS2026`), user-supplied theme files, and runtime host/accent synchronization on `net48`.
2. **Make `DictionaryTheme` Lazy on First Activation:** Introduce a lazy factory (`protected virtual ResourceDictionary CreateResourceDictionary()` cached on first access to `ThemeResourceDictionary`) so registering all 20 themes in `ThemeManager` or DI at startup costs `0 ms` and `0 KB` until selected, and no longer requires `pack://` URI initialization inside theme constructors.
3. **Cache URI-Based `Theme` Dictionaries Across Floating/Detached Windows:** Cache the loaded `ResourceDictionary` for URI-based `Theme` instances on `DockingManager` (just like `currentThemeResourceDictionary` already does for `DictionaryTheme`) so tearing off floating or detached windows under `ArcDarkTheme` reuses the single parsed dictionary in `O(1)` instead of re-parsing BAML per window.
4. **Unify Theme Namespaces & `[XmlnsDefinition]`:** During the Phase 5 `AvalonDox` rename, place all built-in theme classes in `AvalonDox.Themes` (or register `AvalonDox.Themes.VS` alongside `AvalonDox.Themes` in `[assembly: XmlnsDefinition(...)]`) so XAML consumers have one consistent namespace across all 7 theme packages.
