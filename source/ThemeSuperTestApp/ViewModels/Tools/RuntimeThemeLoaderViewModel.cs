using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using AvalonDock.Core;
using AvalonDock.Themes.Type3New;
using Microsoft.Win32;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Selectable XAML control-template set for runtime <see cref="VsTheme"/> construction.
	/// </summary>
	public sealed class VsTemplateSetOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsTemplateSetOption"/> class.
		/// </summary>
		/// <param name="displayName">Human-readable label.</param>
		/// <param name="genericXamlUri">Pack URI for the merged <c>Generic.xaml</c>.</param>
		public VsTemplateSetOption(string displayName, Uri genericXamlUri)
		{
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			GenericXamlUri = genericXamlUri ?? throw new ArgumentNullException(nameof(genericXamlUri));
		}

		/// <summary>Gets the display name.</summary>
		public string DisplayName { get; }

		/// <summary>Gets the pack URI of the control templates dictionary.</summary>
		public Uri GenericXamlUri { get; }
	}

	/// <summary>
	/// Selectable embedded base palette for layering VS2026 override-only <c>.json</c> themes.
	/// </summary>
	public sealed class VsBasePaletteOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsBasePaletteOption"/> class.
		/// </summary>
		/// <param name="displayName">Display label.</param>
		/// <param name="manifestResourceName">Embedded <c>.vstheme</c> resource name in <c>AvalonDock.Themes.Type3New</c>, or <see langword="null"/> for standalone fallbacks.</param>
		/// <param name="polarity">Visual polarity of the base palette.</param>
		public VsBasePaletteOption(string displayName, string? manifestResourceName, ThemePolarity polarity)
		{
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
			ManifestResourceName = manifestResourceName;
			Polarity = polarity;
		}

		/// <summary>Gets the display label.</summary>
		public string DisplayName { get; }

		/// <summary>Gets the manifest resource name in <c>AvalonDock.Themes.Type3New</c>, or <see langword="null"/>.</summary>
		public string? ManifestResourceName { get; }

		/// <summary>Gets the default visual polarity.</summary>
		public ThemePolarity Polarity { get; }
	}

	/// <summary>
	/// Tool 5: Loads external <c>.vstheme</c>, <c>.vstheme.gz</c>, or VS2026 <c>.json</c> files and provides an inline live JSON
	/// sandbox exercising the zero-dependency pure-C# <see cref="VsJsonThemeParser"/>.
	/// </summary>
	public sealed class RuntimeThemeLoaderViewModel : ToolboxScreen
	{
		private const string DefaultSampleJson = """
			[
				// Live VS2026 JSON Override Sandbox (pure-C# zero-dependency VsJsonThemeParser)
				{ "Name": "EnvironmentBackground",   "Background": "FF161925" },
				{ "Name": "EnvironmentTab",          "Background": "FF1F2437" },
				{ "Name": "EnvironmentTabInactive",  "Background": "FF191D2B" },
				{ "Name": "EnvironmentHeader",       "Background": "FF0EA5E9" },
				{ "Name": "FileTabSelectedBorder",   "Background": "FF14B8A6" },
				{ "Name": "ToolWindowBorder",        "Background": "FF334155" },
			]
			""";

		private readonly ThemeCatalogService _themeCatalog;
		private VsTemplateSetOption _selectedTemplateSet;
		private VsBasePaletteOption _selectedBasePalette;
		private string _filePath = string.Empty;
		private string _inlineJsonText = DefaultSampleJson;
		private bool _hasParserError;
		private string _loaderStatusText = "Load an external .vstheme / .vstheme.gz / .json file or edit the live VS2026 JSON array below.";

		/// <summary>
		/// Initializes a new instance of the <see cref="RuntimeThemeLoaderViewModel"/> class.
		/// </summary>
		/// <param name="themeCatalog">The shared theme catalog service.</param>
		public RuntimeThemeLoaderViewModel(ThemeCatalogService themeCatalog)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));

			Id = "Tool.RuntimeThemeLoader";
			DisplayName = "5. Runtime .vstheme / JSON Loader";
			ToolTipText = "Load external .vstheme, .vstheme.gz, or VS2026 .json theme files and test VsJsonThemeParser live (Ctrl+Alt+L)";
			Zone = DockZone.LeftTop;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+L";

			TemplateSets = new[]
			{
				new VsTemplateSetOption(
					"VS 2022 / 2026 Rounded (Themes/VS2022/Generic.xaml)",
					new Uri("/AvalonDock.Themes.Type3New;component/Themes/VS2022/Generic.xaml", UriKind.Relative)),
				new VsTemplateSetOption(
					"VS 2015 Classic (Themes/Generic.xaml)",
					new Uri("/AvalonDock.Themes.Type3New;component/Themes/Generic.xaml", UriKind.Relative)),
			};
			_selectedTemplateSet = TemplateSets[0];

			BasePalettes = new[]
			{
				new VsBasePaletteOption("VS 2022 Dark Base", "AvalonDock.Themes.Type3New.Resources.vs2022dark.vstheme", ThemePolarity.Dark),
				new VsBasePaletteOption("VS 2022 Light Base", "AvalonDock.Themes.Type3New.Resources.vs2022light.vstheme", ThemePolarity.Light),
				new VsBasePaletteOption("VS 2022 Blue Base", "AvalonDock.Themes.Type3New.Resources.vs2022blue.vstheme", ThemePolarity.Blue),
				new VsBasePaletteOption("VS 2015 Dark Base", "AvalonDock.Themes.Type3New.Resources.vs2015dark.vstheme", ThemePolarity.Dark),
				new VsBasePaletteOption("None (Builder Defaults Only)", null, ThemePolarity.Dark),
			};
			_selectedBasePalette = BasePalettes[0];
		}

		/// <summary>Gets the selectable control-template sets.</summary>
		public IReadOnlyList<VsTemplateSetOption> TemplateSets { get; }

		/// <summary>Gets the selectable base palettes for JSON override merging.</summary>
		public IReadOnlyList<VsBasePaletteOption> BasePalettes { get; }

		/// <summary>Gets or sets the selected control-template set.</summary>
		public VsTemplateSetOption SelectedTemplateSet
		{
			get => _selectedTemplateSet;
			set
			{
				if (value != null)
					Set(ref _selectedTemplateSet, value);
			}
		}

		/// <summary>Gets or sets the selected base palette for JSON overrides.</summary>
		public VsBasePaletteOption SelectedBasePalette
		{
			get => _selectedBasePalette;
			set
			{
				if (value != null)
					Set(ref _selectedBasePalette, value);
			}
		}

		/// <summary>Gets or sets the external theme file path.</summary>
		public string FilePath
		{
			get => _filePath;
			set
			{
				if (Set(ref _filePath, value ?? string.Empty))
					NotifyOfPropertyChange(nameof(CanLoadAndApplyFile));
			}
		}

		/// <summary>Gets or sets the editable VS2026 JSON override array text.</summary>
		public string InlineJsonText
		{
			get => _inlineJsonText;
			set => Set(ref _inlineJsonText, value ?? string.Empty);
		}

		/// <summary>Gets a value indicating whether the most recent parse failed with an error.</summary>
		public bool HasParserError
		{
			get => _hasParserError;
			private set => Set(ref _hasParserError, value);
		}

		/// <summary>Gets or sets the status or parser error message.</summary>
		public string LoaderStatusText
		{
			get => _loaderStatusText;
			set => Set(ref _loaderStatusText, value);
		}

		/// <summary>Gets a value indicating whether <see cref="LoadAndApplyFile"/> can execute.</summary>
		public bool CanLoadAndApplyFile => !string.IsNullOrWhiteSpace(FilePath);

		/// <summary>Opens a file picker for <c>.vstheme</c>, <c>.vstheme.gz</c>, or <c>.json</c> files.</summary>
		public void BrowseThemeFile()
		{
			var dialog = new OpenFileDialog
			{
				Title = "Select Visual Studio Theme File (.vstheme, .vstheme.gz, .json)",
				Filter = "Visual Studio Theme Files (*.vstheme;*.vstheme.gz;*.json)|*.vstheme;*.vstheme.gz;*.json|All Files (*.*)|*.*",
				CheckFileExists = true,
			};

			if (dialog.ShowDialog() == true)
				FilePath = dialog.FileName;
		}

		/// <summary>Loads <see cref="FilePath"/>, registers it in <see cref="ThemeCatalogService"/>, and applies it immediately.</summary>
		public void LoadAndApplyFile()
		{
			if (!string.IsNullOrWhiteSpace(FilePath))
				_ = TryLoadAndApplyFilePath(FilePath);
		}

		/// <summary>
		/// Loads a runtime theme from <paramref name="path"/> and applies it (exposed for deterministic STA testing).
		/// </summary>
		/// <param name="path">Path to a <c>.vstheme</c>, <c>.vstheme.gz</c>, or <c>.json</c> file.</param>
		/// <returns><see langword="true"/> when parsed and applied; <see langword="false"/> if parsing or I/O failed.</returns>
		public bool TryLoadAndApplyFilePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
			{
				HasParserError = true;
				LoaderStatusText = $"File not found: '{path}'.";
				return false;
			}

			try
			{
				var fileName = Path.GetFileName(path);
				var isJson = path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
				var genericUri = _selectedTemplateSet.GenericXamlUri;

				VsThemeColorPalette finalPalette;
				if (isJson)
				{
					var overrides = VsJsonThemeParser.ParseFile(path);
					var basePalette = LoadSelectedBasePalette();
					finalPalette = basePalette != null ? basePalette.Merge(overrides) : overrides;
				}
				else
				{
					finalPalette = VsThemeParser.ParseFile(path);
				}

				var customName = $"File: {fileName}";
				var id = $"Runtime.File.{fileName}";
				var descriptor = _themeCatalog.RegisterRuntimeTheme(
					id,
					customName,
					_selectedBasePalette.Polarity,
					typeof(CustomRuntimeVsTheme),
					() => new CustomRuntimeVsTheme(customName, VsThemeResourceBuilder.Build(finalPalette, genericUri)));

				_themeCatalog.ApplyTheme(descriptor, forceFreshInstance: true);
				HasParserError = false;
				LoaderStatusText = $"Loaded '{fileName}' ({finalPalette.Count} tokens) and applied as CustomRuntimeVsTheme.";
				return true;
			}
			catch (Exception ex) when (ex is FormatException || ex is XmlException || ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
			{
				HasParserError = true;
				LoaderStatusText = $"{ex.GetType().Name}: {ex.Message}";
				return false;
			}
		}

		/// <summary>
		/// Parses <see cref="InlineJsonText"/> via <see cref="VsJsonThemeParser.Parse(string)"/>, merges onto <see cref="SelectedBasePalette"/>,
		/// registers the resulting <see cref="CustomRuntimeVsTheme"/>, and applies it live.
		/// </summary>
		/// <returns><see langword="true"/> on valid JSON; <see langword="false"/> when <see cref="FormatException"/> is caught.</returns>
		public bool ApplyInlineJsonOverride()
		{
			try
			{
				var overrides = VsJsonThemeParser.Parse(InlineJsonText);
				var basePalette = LoadSelectedBasePalette();
				var merged = basePalette != null ? basePalette.Merge(overrides) : overrides;
				var genericUri = _selectedTemplateSet.GenericXamlUri;

				const string customName = "Live JSON Sandbox";
				var descriptor = _themeCatalog.RegisterRuntimeTheme(
					"Runtime.InlineJsonSandbox",
					customName,
					_selectedBasePalette.Polarity,
					typeof(CustomRuntimeVsTheme),
					() => new CustomRuntimeVsTheme(customName, VsThemeResourceBuilder.Build(merged, genericUri)));

				_themeCatalog.ApplyTheme(descriptor, forceFreshInstance: true);
				HasParserError = false;
				LoaderStatusText = $"Applied Live JSON Sandbox ({overrides.Count} override token(s), {merged.Count} total merged tokens).";
				return true;
			}
			catch (FormatException ex)
			{
				HasParserError = true;
				LoaderStatusText = $"FormatException: {ex.Message}";
				return false;
			}
		}

		/// <summary>Restores the default sample VS2026 JSON snippet.</summary>
		public void ResetSampleJson()
		{
			InlineJsonText = DefaultSampleJson;
			HasParserError = false;
			LoaderStatusText = "Restored sample VS2026 JSON override array.";
		}

		/// <summary>Loads a deliberately malformed JSON snippet and runs <see cref="ApplyInlineJsonOverride"/> to test line/column error reporting.</summary>
		public void TestMalformedJsonError()
		{
			InlineJsonText = "[\n  { \"Name\": \"EnvironmentBackground\", \"Background\": \"FF161925\" },\n  { \"Name\": UNQUOTED_TOKEN }\n]";
			_ = ApplyInlineJsonOverride();
		}

		private VsThemeColorPalette? LoadSelectedBasePalette()
		{
			var resourceName = _selectedBasePalette.ManifestResourceName;
			if (string.IsNullOrEmpty(resourceName))
				return null;

			var bytes = VsThemeResourceLoader.Load(typeof(VS2022DarkTheme).Assembly, resourceName!);
			using var ms = new MemoryStream(bytes);
			return VsThemeParser.Parse(ms);
		}
	}
}