# Plan 01 (of 05) — Restore `AvalonDock.Themes.VS` & Implement Zero-Dependency Pure-C# `VsJsonThemeParser`

> **Execution Order:** **Step 1 of 5** (Must complete and pass build/analyzer verification before starting [Plan 02](02-Restore-Themes-VS2013-Brush-Freezing-And-Solution-CI.md)).
> **Core Constraints:**
> 1. **`net48` Exclusively:** `AvalonDock.Themes.VS` must target `.NET Framework 4.8` (`net48`) **exclusively** — zero `net6.0-windows` / `net8.0-windows` / `net9.0-windows` / `net10.0-windows` / `net11.0-windows` targets, zero conditional TFM `<ItemGroup>` blocks, and 100% inbox `.NET Framework 4.8` BCL APIs.
> 2. **Zero `System.Text.Json` / `System.Memory`:** Zero external NuGet packages anywhere in `.csproj`, source files, or transitive `project.assets.json` on `net48`.

---

## 1. Objective & Scope

Restore the upstream `AvalonDock.Themes.VS` library (delivering **9 built-in themes**: `VS2015{Blue,Dark,Light}Theme`, `VS2022{Blue,Dark,Light}Theme`, `VS2026{Blue,Dark,Light}Theme`, plus `VsTheme` and `{vs:VsTheme}`) from local Git history (`b706128`, matching `Dirkster99/AvalonDock` `master`) and eliminate its sole `System.Text.Json` dependency by replacing the deserializer inside `VsJsonThemeParser.cs` with a self-contained, zero-dependency recursive-descent JSON reader in pure C#.

### Agreed Design Decisions
1. **Refactoring Strategy (Option A):** Keep 100% of upstream's public API (`VsJsonThemeParser.Parse`, `VsJsonThemeParser.ParseFile`, `VsTheme.FromJson`, `VS2026*Theme`, and embedded `Resources/vs2026*.json`) intact using a pure-C# parser with zero NuGet dependencies and zero extra BCL assembly references.
2. **Parser Error & Forward-Compatibility Contract:**
   - Throw `System.FormatException` (including 1-based line/column or character offset in the message) on malformed JSON syntax (unterminated strings/comments, invalid tokens, trailing non-trivia tokens) or a non-array root value (other than the JSON `null` literal, which returns an empty `VsThemeColorPalette` matching upstream's `entries == null` behavior).
   - Validate `null` / empty arguments (`ArgumentNullException` for `null` `json` or `stream`, and `null`/empty `filePath`, matching existing `CA1062` / upstream contracts).
   - Support `//` single-line comments, `/* ... */` multi-line comments, trailing commas in arrays and objects, optional leading UTF-8 BOM (`\uFEFF`), standard JSON string escapes (`\"`, `\\`, `\/`, `\b`, `\f`, `\n`, `\r`, `\t`, `\uXXXX`), and case-insensitive property matching (`Name`, `Background`, `Foreground`).
   - Silently skip unknown properties on color-entry objects—including nested objects `{...}`, nested arrays `[...]`, numbers, booleans, or `null`—so future Visual Studio JSON theme schema additions never break runtime loading.
3. **Exact Upstream Namespace Preservation:** Keep all `AvalonDock.Themes.VS` public types in `namespace AvalonDock.Themes.VS` (`VS2015*Theme`, `VS2022*Theme`, `VS2026*Theme`, `VsTheme`, `VsThemeExtension`, `VsThemeParser`, `VsJsonThemeParser`, `VsThemeColorPalette`, `VsThemeColorEntry`, `VsThemeResourceBuilder`, `VsThemeResourceLoader`) and resource keys in `AvalonDock.Themes.VS.Themes`.

---

## 2. Prerequisites

- Clean working tree on `master` (or feature branch off `master`).
- Verify commit `b706128` (`d0135f0~2`, exact upstream `Dirkster99/AvalonDock` `master` state including `#683` `ToggleDockingManager` support, `#684` `MenuItemEx` fix, and `DetachToWindowCommand` context menu items) is present locally:
  ```powershell
  git cat-file -t b706128183a8edce302b1b633396e2c98d48ba32
  ```

---

## 3. Step-by-Step Implementation Tasks

### Step 1.1 — Extract `AvalonDock.Themes.VS` from Local Git History

Restore the 40 upstream files of `source/Components/AvalonDock.Themes.VS` from `b706128`:

```powershell
git checkout b706128183a8edce302b1b633396e2c98d48ba32 -- source/Components/AvalonDock.Themes.VS
```

Verify the extracted directory contains:
- `AvalonDock.Themes.VS.csproj`
- `OverlayButtons.xaml`, `Themes/Generic.xaml`, `Themes/VS2022/Generic.xaml`, `Themes/Icons/IconGeometry.xaml`, `Themes/Menu/*`
- `Resources/vs2015{blue,dark,light}.vstheme` (6 XML palettes including `vs2022*`)
- `Resources/vs2026{blue,dark,light}.json` (3 JSON override palettes)
- `VS2015*Theme.cs`, `VS2022*Theme.cs`, `VS2026*Theme.cs`
- `VsTheme.cs`, `Vs2026ThemeFactory.cs`, `VsJsonThemeParser.cs`, `VsThemeParser.cs`, `VsColorParser.cs`, `VsThemeColorEntry.cs`, `VsThemeColorPalette.cs`, `VsThemeExtension.cs`, `VsThemeResourceBuilder.cs`, `VsThemeResourceLoader.cs`

### Step 1.2 — Lock `AvalonDock.Themes.VS.csproj` to `net48` Exclusively & Strip `System.Text.Json`

Edit `source/Components/AvalonDock.Themes.VS/AvalonDock.Themes.VS.csproj`:
1. **Remove** the multi-target comment and `System.Text.Json` `<ItemGroup>` completely:
   ```xml
   <!-- REMOVE THIS BLOCK: -->
   <!-- System.Text.Json ships in the shared framework on net8/net9/net10/net11; only net48 needs the package. -->
   <ItemGroup Condition="'$(TargetFramework)' == 'net48'">
       <PackageReference Include="System.Text.Json" Version="10.0.12" />
   </ItemGroup>
   ```
2. **Enforce `net48` exclusively:** Verify `<TargetFrameworks>$(ComponentTargetFrameworks)</TargetFrameworks>` (or `<TargetFramework>net48</TargetFramework>`) resolves **solely** to `net48` via `source/Directory.Build.props` (`<ComponentTargetFrameworks>net48</ComponentTargetFrameworks>`), matching `AvalonDock.Themes.Arc.csproj` and the other 4 theme packages — with **zero** `net6.0-windows`, `net8.0-windows`, `net9.0-windows`, `net10.0-windows`, or `net11.0-windows` targets and **zero** `<PackageReference>` items.
3. Confirm `<GenerateDocumentationFile>true</GenerateDocumentationFile>` remains enabled and the **only** `<ProjectReference>` is `..\AvalonDock\AvalonDock.csproj`.

### Step 1.3 — Rewrite `VsJsonThemeParser.cs` with a Pure-C# Recursive-Descent Reader (`net48` Inbox BCL Only)

Edit `source/Components/AvalonDock.Themes.VS/VsJsonThemeParser.cs`:
1. Remove `using System.Text.Json;` and `using System.Text.Json.Serialization;` (retain only `System`, `System.Collections.Generic`, `System.Globalization`, `System.IO`, `System.Text`).
   - **Critical `net48` BCL Guardrail:** Do **not** use `Span<T>`, `ReadOnlySpan<char>`, `string.AsSpan()`, `byte.TryParse(ReadOnlySpan<char>, ...)`, `System.HashCode`, or any `.NET Core` / `.NET 5+`-only BCL APIs! On `net48`, `Span<T>` is not in `mscorlib` and would require `System.Memory`. Use pure `.NET Framework 4.8` inbox `string` indexing (`json[i]`), `json.Substring(start, length)`, and `StringBuilder`.
2. Keep the exact public method signatures and XML documentation comments:
   - `public static VsThemeColorPalette Parse(string json)`
   - `public static VsThemeColorPalette Parse(Stream stream)` *(reads via `new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true)` or `StreamReader(stream)`)*
   - `public static VsThemeColorPalette ParseFile(string filePath)`
3. Implement an internal/private lightweight `JsonReader` struct or helper class inside `VsJsonThemeParser` decomposed into small, low-complexity methods (keeping every method well below `CodeMetricsConfig.txt`'s `CA1502: 40` cyclomatic complexity limit and above `CA1505: 9` maintainability index):
   - **`ParseRootArray()`**:
     - Skip leading UTF-8 BOM (`'\uFEFF'`) and trivia (whitespace + `//...` + `/*...*/`; throw `FormatException` on unclosed `/*` comment).
     - If the root token is `null`, consume `null`, verify only trailing trivia remains, and return an empty `VsThemeColorPalette`.
     - Require `'['` (otherwise throw `CreateFormatException("Expected '[' at root of VS JSON theme.")`).
     - Loop over array elements separated by `','`, permitting a trailing comma before `']'`:
       - Allow `null` array elements (skip cleanly, matching upstream `if (entry == null) continue;`) or `'{'` object elements (any other element token throws `FormatException`).
     - After closing `']'`, skip trailing trivia and assert end-of-input (throw `FormatException` if extra tokens follow).
   - **`ReadEntryObject()`**:
     - Expect `'{'`. Loop over `"propertyName" : value` pairs separated by `','`, permitting a trailing comma before `'}'`.
     - Read property name via `ReadString()`, skip trivia, require `':'`, skip trivia:
       - If key matches `"Name"`, `"Background"`, or `"Foreground"` (`StringComparison.OrdinalIgnoreCase`): read string or `null` via `ReadNullableString()` (throw `FormatException` if value is neither a string nor `null`).
       - For `"Category"` or any other unknown property name: call `SkipValue()` so arbitrary strings, numbers, `true`/`false`/`null`, nested objects `{...}`, or nested arrays `[...]` (up to a safe max nesting depth, e.g., `64`) are skipped cleanly without error.
     - If `!string.IsNullOrEmpty(name)`, convert `background` and `foreground` via `VsColorParser.FromArgbHex(...)` and assign `colors[name] = new VsThemeColorEntry(bg, fg)` (last entry wins on duplicate token names, matching upstream).
   - **`ReadString()`**:
     - Consume opening `'"'`. Fast-path unescaped slices or build via `StringBuilder`.
     - Reject unescaped control characters `< 0x20` (including raw unescaped newlines) with `FormatException`.
     - Handle `\"`, `\\`, `\/`, `\b`, `\f`, `\n`, `\r`, `\t`, and `\uXXXX` (4 hex digits parsed with `ushort.TryParse(..., NumberStyles.HexNumber, CultureInfo.InvariantCulture, ...)`); throw `FormatException` on invalid escape or EOF before closing `'"'`.
   - **`SkipValue()`**:
     - Dispatch on lookahead char: `'"'` → `ReadString()`, `'{'` → skip object fields recursively, `'['` → skip array items recursively, `'t'` → consume `"true"`, `'f'` → consume `"false"`, `'n'` → consume `"null"`, `'-'` or `'0'..'9'` → consume JSON number token; anything else throws `FormatException`.
   - **`CreateFormatException(string message)`**:
     - Compute 1-based line and column from `_position` and return `new FormatException($"{message} (Line {line}, Column {column}).")`.

### Step 1.4 — Static Analysis, Formatting & XML Doc Audit on `AvalonDock.Themes.VS`

Verify all `.cs` files in `source/Components/AvalonDock.Themes.VS` comply with `source/Directory.Build.props` and `source/.editorconfig`:
- **Tabs** (`indent_style = tab`), LF line endings, Allman braces, `using` directives outside `namespace`.
- Check `VsThemeExtension.cs` (which used file-scoped `namespace AvalonDock.Themes.VS;` upstream): ensure `StyleCop.Analyzers`, `SonarAnalyzer.CSharp`, `Microsoft.CodeAnalysis.NetAnalyzers`, and XML doc generation (`CS1591` / `SA1600`) pass with zero warnings under `--warnaserror` (convert to block-scoped `namespace` if needed for consistency).
- Update XML doc `<remarks>` on `VsTheme.FromJson` / `VsJsonThemeParser` if any doc comment mentions `JsonException` (document `<exception cref="FormatException">` instead).

---

## 4. Verification & Exit Gate to Plan 02

Run standalone restore, build, and asset inspection on `AvalonDock.Themes.VS.csproj`:

```powershell
dotnet restore source/Components/AvalonDock.Themes.VS/AvalonDock.Themes.VS.csproj
dotnet build source/Components/AvalonDock.Themes.VS/AvalonDock.Themes.VS.csproj --configuration Release --no-restore --warnaserror
```

Verify `bin/Release/` contains **only** `net48` (no modern `.NET` TFM folders) and **zero** `System.Text.Json`, `System.Memory`, `System.Buffers`, or `Microsoft.Bcl` references exist in the generated NuGet lockfile and output folder:

```powershell
Get-ChildItem "source/Components/AvalonDock.Themes.VS/bin/Release" -Directory | Select-Object Name
Select-String -Path "source/Components/AvalonDock.Themes.VS/obj/project.assets.json" -Pattern "System\.Text\.Json|System\.Memory|Microsoft\.Bcl|net[6-9]|net1[01]"
Get-ChildItem "source/Components/AvalonDock.Themes.VS/bin/Release/net48/*.dll" | Select-Object Name
```

**Expected Result:**
- `dotnet build --warnaserror` succeeds with **0 warnings, 0 errors**.
- `bin/Release/` has **only** `net48` as its target subfolder.
- `Select-String` returns **no matches**.
- `bin/Release/net48/` contains only `AvalonDock.Core.dll`, `AvalonDock.dll`, and `AvalonDock.Themes.VS.dll`.
