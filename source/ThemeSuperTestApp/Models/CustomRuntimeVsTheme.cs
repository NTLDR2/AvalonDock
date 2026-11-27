using System;
using System.IO;
using System.Windows;
using AvalonDock.Themes.VS;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Concrete <see cref="VsTheme"/> subclass used for custom runtime <c>.vstheme</c>, <c>.vstheme.gz</c>, and VS2026 <c>.json</c>
	/// themes built with user-selected control templates (<c>Themes/Generic.xaml</c> or <c>Themes/VS2022/Generic.xaml</c>).
	/// </summary>
	public sealed class CustomRuntimeVsTheme : VsTheme
	{
		private readonly string _customName;

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomRuntimeVsTheme"/> class.
		/// </summary>
		/// <param name="customName">Display name reported by <see cref="Name"/>.</param>
		/// <param name="resourceDictionary">Pre-built theme <see cref="ResourceDictionary"/>.</param>
		public CustomRuntimeVsTheme(string customName, ResourceDictionary resourceDictionary)
			: base(resourceDictionary ?? throw new ArgumentNullException(nameof(resourceDictionary)))
		{
			_customName = string.IsNullOrWhiteSpace(customName) ? "CustomRuntimeVsTheme" : customName;
		}

		/// <inheritdoc/>
		public override string Name => _customName;

		/// <summary>
		/// Parses a VS2026 inline JSON token snippet, optionally merges it onto an embedded VS base palette, and returns
		/// a factory creating the merged <see cref="CustomRuntimeVsTheme"/> along with the parsed override token count.
		/// </summary>
		/// <param name="json">Raw VS2026 JSON array text.</param>
		/// <param name="baseKey">Optional base palette identifier (<c>VS2022Dark</c>, <c>VS2022Light</c>, <c>VS2022Blue</c>, <c>VS2015Dark</c>, or <c>None</c>).</param>
		/// <param name="templateKey">Control template set (<c>VS2022</c> or <c>VS2015</c>).</param>
		/// <param name="customTitle">Display name for the runtime theme.</param>
		/// <returns>Factory function and parsed override token count.</returns>
		public static (Func<CustomRuntimeVsTheme> Factory, int OverrideCount) PrepareInlineJsonFactory(
			string json,
			string baseKey,
			string templateKey,
			string customTitle)
		{
			var overrides = VsJsonThemeParser.Parse(json);
			var basePalette = TryLoadEmbeddedVsPalette(baseKey);
			var merged = basePalette != null ? basePalette.Merge(overrides) : overrides;

			var genericUri = templateKey.IndexOf("2015", StringComparison.OrdinalIgnoreCase) >= 0
				? new Uri("/AvalonDock.Themes.VS;component/Themes/Generic.xaml", UriKind.Relative)
				: new Uri("/AvalonDock.Themes.VS;component/Themes/VS2022/Generic.xaml", UriKind.Relative);

			return (() => new CustomRuntimeVsTheme(customTitle, VsThemeResourceBuilder.Build(merged, genericUri)), overrides.Count);
		}

		private static VsThemeColorPalette? TryLoadEmbeddedVsPalette(string baseKey)
		{
			if (string.Equals(baseKey, "None", StringComparison.OrdinalIgnoreCase))
				return null;

			var resourceName = "AvalonDock.Themes.Resources.vs2022dark.vstheme";
			if (baseKey.IndexOf("2022Light", StringComparison.OrdinalIgnoreCase) >= 0)
				resourceName = "AvalonDock.Themes.Resources.vs2022light.vstheme";
			else if (baseKey.IndexOf("2022Blue", StringComparison.OrdinalIgnoreCase) >= 0)
				resourceName = "AvalonDock.Themes.Resources.vs2022blue.vstheme";
			else if (baseKey.IndexOf("2015Dark", StringComparison.OrdinalIgnoreCase) >= 0)
				resourceName = "AvalonDock.Themes.Resources.vs2015dark.vstheme";

			var bytes = VsThemeResourceLoader.Load(typeof(VS2022DarkTheme).Assembly, resourceName);
			using var ms = new MemoryStream(bytes);
			return VsThemeParser.Parse(ms);
		}
	}
}