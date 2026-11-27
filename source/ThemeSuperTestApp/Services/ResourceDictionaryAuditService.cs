using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using AvalonDock;
using AvalonDock.Themes;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Walks WPF <see cref="ResourceDictionary"/> hierarchies to audit <see cref="Freezable.IsFrozen"/> compliance,
	/// extract color swatches and lookup precedence, and compute cross-theme semantic key diffs without mutating any theme resources.
	/// </summary>
	public sealed class ResourceDictionaryAuditService
	{
		/// <summary>
		/// Audits a <see cref="ThemeDescriptor"/>, using the live dictionary attached to <paramref name="attachedDockingManager"/> when active,
		/// or resolving an inspection dictionary safely otherwise.
		/// </summary>
		/// <param name="descriptor">The theme descriptor to audit.</param>
		/// <param name="attachedDockingManager">Optional attached <see cref="DockingManager"/>.</param>
		/// <returns>A complete <see cref="ThemeResourceAuditReport"/>.</returns>
		public ThemeResourceAuditReport AuditDescriptor(ThemeDescriptor descriptor, DockingManager? attachedDockingManager = null)
		{
			if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

			var theme = descriptor.GetInspectionInstance();
			return AuditTheme(descriptor.DisplayName, theme, attachedDockingManager);
		}

		/// <summary>
		/// Audits a concrete <see cref="Theme"/> instance (works for both <see cref="DictionaryTheme"/> and URI-based <see cref="Theme"/>).
		/// </summary>
		/// <param name="themeName">Display label for the report.</param>
		/// <param name="theme">The theme instance to inspect.</param>
		/// <param name="attachedDockingManager">Optional attached <see cref="DockingManager"/> hosting the active URI dictionary.</param>
		/// <returns>A complete <see cref="ThemeResourceAuditReport"/>.</returns>
		public ThemeResourceAuditReport AuditTheme(string themeName, Theme theme, DockingManager? attachedDockingManager = null)
		{
			if (string.IsNullOrWhiteSpace(themeName)) throw new ArgumentNullException(nameof(themeName));
			if (theme == null) throw new ArgumentNullException(nameof(theme));

			if (theme is DictionaryTheme dictTheme)
			{
				var rootDict = dictTheme.ThemeResourceDictionary ?? new ResourceDictionary();
				return AuditDictionary(themeName, rootDict, "Root ThemeResourceDictionary (Direct)");
			}

			var uri = theme.GetResourceUri();
			if (uri == null)
				return AuditDictionary(themeName, new ResourceDictionary(), "<Empty - null GetResourceUri()>");

			ResourceDictionary? liveMatch = null;
			if (attachedDockingManager != null)
			{
				liveMatch = attachedDockingManager.Resources.MergedDictionaries
					.FirstOrDefault(d => d.Source == uri);
			}

			var targetDict = liveMatch ?? new ResourceDictionary { Source = uri };
			return AuditDictionary(themeName, targetDict, $"Root URI: {uri}");
		}

		/// <summary>
		/// Audits a raw <see cref="ResourceDictionary"/> and all of its nested <see cref="ResourceDictionary.MergedDictionaries"/>
		/// in exact WPF lookup precedence order without mutating any contained <see cref="Freezable"/> instances.
		/// </summary>
		/// <param name="themeName">Report title.</param>
		/// <param name="rootDictionary">Root dictionary to traverse.</param>
		/// <param name="rootLabel">Provenance label for the root dictionary.</param>
		/// <returns>A complete <see cref="ThemeResourceAuditReport"/>.</returns>
		public ThemeResourceAuditReport AuditDictionary(
			string themeName,
			ResourceDictionary rootDictionary,
			string rootLabel = "Root Dictionary (Direct)")
		{
			if (string.IsNullOrWhiteSpace(themeName)) throw new ArgumentNullException(nameof(themeName));
			if (rootDictionary == null) throw new ArgumentNullException(nameof(rootDictionary));

			var entries = new List<ResourceEntryRecord>();
			var visited = new HashSet<ResourceDictionary>(ReferenceEqualityComparerInstance.Instance);
			var rank = 0;

			TraverseInLookupOrder(rootDictionary, rootLabel ?? "Root Dictionary", depth: 0, visited, entries, ref rank);

			var brushAndColorCount = entries.Count(e => e.HasSwatch);
			var frozenCount = entries.Count(e => e.FreezeStatus == ResourceFreezeStatus.Frozen);
			var unfrozenCount = entries.Count(e => e.FreezeStatus == ResourceFreezeStatus.UnfrozenWarning);
			var styleAndTemplateCount = entries.Count(e =>
				e.Category == ResourceEntryCategory.ImplicitTypeStyle ||
				e.Category == ResourceEntryCategory.DataTemplateKey ||
				e.ValueTypeName == nameof(Style) ||
				e.ValueTypeName.EndsWith("Template", StringComparison.Ordinal));

			var treeSummary = $"Root ({rootDictionary.Count} direct) + {Math.Max(0, visited.Count - 1)} merged dict(s)";

			return new ThemeResourceAuditReport(
				themeName,
				entries,
				visited.Count,
				brushAndColorCount,
				frozenCount,
				unfrozenCount,
				styleAndTemplateCount,
				treeSummary);
		}

		/// <summary>
		/// Computes a semantic key parity diff between <paramref name="activeReport"/> and <paramref name="comparisonReport"/>
		/// using the effective highest-precedence entry for each normalized key signature.
		/// </summary>
		/// <param name="activeReport">Audit report of the currently active theme.</param>
		/// <param name="comparisonReport">Audit report of the comparison baseline theme.</param>
		/// <returns>A populated <see cref="ThemeKeyDiffReport"/>.</returns>
		public ThemeKeyDiffReport DiffThemes(ThemeResourceAuditReport activeReport, ThemeResourceAuditReport comparisonReport)
		{
			if (activeReport == null) throw new ArgumentNullException(nameof(activeReport));
			if (comparisonReport == null) throw new ArgumentNullException(nameof(comparisonReport));

			var activeMap = BuildEffectiveKeyMap(activeReport.Entries);
			var comparisonMap = BuildEffectiveKeyMap(comparisonReport.Entries);

			var allSignatures = activeMap.Keys
				.Union(comparisonMap.Keys, StringComparer.Ordinal)
				.OrderBy(k => k, StringComparer.Ordinal)
				.ToList();

			var diffs = new List<ThemeKeyDiffEntry>();
			var sharedMatching = 0;
			var missingInActive = 0;
			var extraInActive = 0;
			var typeMismatch = 0;

			foreach (var sig in allSignatures)
			{
				var inActive = activeMap.TryGetValue(sig, out var activeEntry);
				var inComp = comparisonMap.TryGetValue(sig, out var compEntry);

				if (inActive && inComp)
				{
					if (string.Equals(activeEntry!.ValueTypeName, compEntry!.ValueTypeName, StringComparison.Ordinal))
					{
						sharedMatching++;
					}
					else
					{
						typeMismatch++;
						diffs.Add(new ThemeKeyDiffEntry(
							sig,
							ThemeKeyDiffKind.ValueTypeMismatch,
							activeEntry.Category,
							activeEntry.ValueTypeName,
							activeEntry.ColorHex,
							compEntry.ValueTypeName,
							compEntry.ColorHex));
					}
				}
				else if (!inActive && inComp)
				{
					missingInActive++;
					diffs.Add(new ThemeKeyDiffEntry(
						sig,
						ThemeKeyDiffKind.MissingInActive,
						compEntry!.Category,
						"-",
						null,
						compEntry.ValueTypeName,
						compEntry.ColorHex));
				}
				else if (inActive && !inComp)
				{
					extraInActive++;
					diffs.Add(new ThemeKeyDiffEntry(
						sig,
						ThemeKeyDiffKind.ExtraInActive,
						activeEntry!.Category,
						activeEntry.ValueTypeName,
						activeEntry.ColorHex,
						"-",
						null));
				}
			}

			return new ThemeKeyDiffReport(
				activeReport.ThemeName,
				comparisonReport.ThemeName,
				sharedMatching,
				missingInActive,
				extraInActive,
				typeMismatch,
				diffs);
		}

		/// <summary>
		/// Formats a Markdown summary of <paramref name="report"/> and optional <paramref name="diffReport"/>.
		/// </summary>
		/// <param name="report">The active theme audit report.</param>
		/// <param name="diffReport">Optional cross-theme key diff report.</param>
		/// <returns>Markdown text suitable for clipboard or report export.</returns>
		public string BuildMarkdownAuditSummary(ThemeResourceAuditReport report, ThemeKeyDiffReport? diffReport = null)
		{
			if (report == null) throw new ArgumentNullException(nameof(report));

			var sb = new StringBuilder();
			sb.AppendLine($"## ResourceDictionary & Freezable Audit — {report.ThemeName}");
			sb.AppendLine();
			sb.AppendLine($"- **Hierarchy:** {report.TreeSummary}");
			sb.AppendLine($"- **Total Entries:** `{report.TotalEntries}` (`{report.BrushAndColorCount}` brushes/colors, `{report.StyleAndTemplateCount}` styles/templates)");
			sb.AppendLine($"- **Freezable Compliance:** `{report.FrozenFreezableCount}` Frozen, **`{report.UnfrozenFreezableCount}` Unfrozen Warnings**");

			var unfrozen = report.Entries.Where(e => e.FreezeStatus == ResourceFreezeStatus.UnfrozenWarning).Take(25).ToList();
			if (unfrozen.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("### Unfrozen Freezables (Top 25)");
				sb.AppendLine("| Key | Value Type | Color | Source |");
				sb.AppendLine("| :--- | :--- | :--- | :--- |");
				foreach (var u in unfrozen)
				{
					sb.AppendLine($"| `{u.KeyDisplay}` | `{u.ValueTypeName}` | `{u.ColorHex ?? "-"}` | `{u.SourceLocation}` |");
				}
			}

			if (diffReport != null)
			{
				sb.AppendLine();
				sb.AppendLine($"### Semantic Key Diff vs. {diffReport.ComparisonThemeName}");
				sb.AppendLine($"- **Matching Keys:** `{diffReport.SharedMatchingCount}` | **Missing in {diffReport.ActiveThemeName}:** `{diffReport.MissingInActiveCount}` | **Extra in {diffReport.ActiveThemeName}:** `{diffReport.ExtraInActiveCount}` | **Type Mismatches:** `{diffReport.TypeMismatchCount}`");
			}

			return sb.ToString();
		}

		private static Dictionary<string, ResourceEntryRecord> BuildEffectiveKeyMap(IReadOnlyList<ResourceEntryRecord> entries)
		{
			var map = new Dictionary<string, ResourceEntryRecord>(StringComparer.Ordinal);
			foreach (var entry in entries)
			{
				// Entries are ordered in WPF lookup precedence (lowest LookupPrecedenceRank first), so first seen wins.
				if (!map.ContainsKey(entry.SemanticKeySignature))
					map[entry.SemanticKeySignature] = entry;
			}

			return map;
		}

		private static void TraverseInLookupOrder(
			ResourceDictionary dict,
			string sourceLabel,
			int depth,
			HashSet<ResourceDictionary> visited,
			List<ResourceEntryRecord> output,
			ref int rank)
		{
			if (!visited.Add(dict))
				return;

			// 1. Direct entries on a ResourceDictionary always take precedence over its MergedDictionaries.
			List<object> keys;
			try
			{
				keys = dict.Keys.Cast<object>().ToList();
			}
			catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
			{
				return;
			}

			var inspectedRecords = new List<ResourceEntryRecord>(keys.Count);
			foreach (var key in keys)
			{
				rank++;
				inspectedRecords.Add(InspectEntry(dict, key, sourceLabel, depth, rank));
			}

			inspectedRecords.Sort(static (a, b) => string.CompareOrdinal(a.KeyDisplay, b.KeyDisplay));
			output.AddRange(inspectedRecords);

			// 2. WPF searches MergedDictionaries in REVERSE order (last merged dictionary wins).
			for (var i = dict.MergedDictionaries.Count - 1; i >= 0; i--)
			{
				var child = dict.MergedDictionaries[i];
				if (child == null)
					continue;

				var childSource = child.Source != null ? child.Source.ToString() : "<In-Memory Dictionary>";
				var childLabel = depth == 0
					? $"Merged[{i}]: {childSource}"
					: $"{sourceLabel} -> Merged[{i}]: {childSource}";

				TraverseInLookupOrder(child, childLabel, depth + 1, visited, output, ref rank);
			}
		}

		private static ResourceEntryRecord InspectEntry(
			ResourceDictionary dict,
			object key,
			string sourceLocation,
			int depth,
			int rank)
		{
			ClassifyKey(key, out var keyDisplay, out var semanticSignature, out var category);

			object? value;
			try
			{
				value = dict[key];
			}
			catch (Exception ex)
			{
				return new ResourceEntryRecord(
					keyDisplay,
					semanticSignature,
					category,
					$"<{ex.GetType().Name}: {ex.Message}>",
					colorHex: null,
					swatchBrush: null,
					ResourceFreezeStatus.RealizationError,
					sourceLocation,
					depth,
					rank);
			}

			if (value == null)
			{
				return new ResourceEntryRecord(
					keyDisplay,
					semanticSignature,
					category,
					"<null>",
					colorHex: null,
					swatchBrush: null,
					ResourceFreezeStatus.NotFreezable,
					sourceLocation,
					depth,
					rank);
			}

			// Evaluate IsFrozen BEFORE creating any preview clone — never mutate the theme's own Freezable!
			var freezeStatus = value is Freezable freezable
				? (freezable.IsFrozen ? ResourceFreezeStatus.Frozen : ResourceFreezeStatus.UnfrozenWarning)
				: ResourceFreezeStatus.NotFreezable;

			ExtractSwatchAndHex(value, out var colorHex, out var swatchBrush);

			return new ResourceEntryRecord(
				keyDisplay,
				semanticSignature,
				category,
				value.GetType().Name,
				colorHex,
				swatchBrush,
				freezeStatus,
				sourceLocation,
				depth,
				rank);
		}

		private static void ClassifyKey(
			object key,
			out string keyDisplay,
			out string semanticSignature,
			out ResourceEntryCategory category)
		{
			if (key is ComponentResourceKey crk)
			{
				category = ResourceEntryCategory.ComponentResourceKey;
				var ownerType = crk.TypeInTargetAssembly;
				var ownerLabel = ownerType != null
					? (ownerType.Name == "ResourceKeys" ? $"{ownerType.Namespace}.ResourceKeys" : ownerType.Name)
					: "?";
				var idText = crk.ResourceId?.ToString() ?? "?";
				keyDisplay = $"{ownerLabel} :: {idText}";

				// Normalize per-assembly ResourceKeys classes to share the semantic token name across themes.
				semanticSignature = ownerType != null && ownerType.Name != "ResourceKeys"
					? $"ComponentKey:{ownerType.Name}.{idText}"
					: $"ComponentKey:{idText}";
				return;
			}

			if (key is Type targetType)
			{
				category = ResourceEntryCategory.ImplicitTypeStyle;
				keyDisplay = $"Implicit Style -> {targetType.Name} ({targetType.Namespace})";
				semanticSignature = $"ImplicitStyle:{targetType.FullName}";
				return;
			}

			if (key is DataTemplateKey dtk)
			{
				category = ResourceEntryCategory.DataTemplateKey;
				var dtName = dtk.DataType is Type dtType ? dtType.FullName : dtk.DataType?.ToString();
				keyDisplay = $"Implicit DataTemplate -> {dtName}";
				semanticSignature = $"DataTemplate:{dtName}";
				return;
			}

			category = ResourceEntryCategory.StringOrOther;
			keyDisplay = key.ToString() ?? "<unnamed>";
			semanticSignature = $"Key:{keyDisplay}";
		}

		private static void ExtractSwatchAndHex(object value, out string? colorHex, out Brush? swatchBrush)
		{
			if (value is SolidColorBrush scb)
			{
				var c = scb.Color;
				colorHex = FormatArgbHex(c, scb.Opacity);
				var preview = new SolidColorBrush(c) { Opacity = scb.Opacity };
				preview.Freeze();
				swatchBrush = preview;
				return;
			}

			if (value is Color color)
			{
				colorHex = FormatArgbHex(color, opacity: 1.0);
				var preview = new SolidColorBrush(color);
				preview.Freeze();
				swatchBrush = preview;
				return;
			}

			if (value is GradientBrush gradient && gradient.GradientStops.Count > 0)
			{
				var first = gradient.GradientStops[0].Color;
				var last = gradient.GradientStops[gradient.GradientStops.Count - 1].Color;
				colorHex = $"{FormatArgbHex(first, 1.0)} -> {FormatArgbHex(last, 1.0)}";
				var clone = gradient.CloneCurrentValue();
				if (clone.CanFreeze)
					clone.Freeze();
				swatchBrush = clone;
				return;
			}

			if (value is Brush otherBrush)
			{
				colorHex = $"<{otherBrush.GetType().Name}>";
				var clone = otherBrush.CloneCurrentValue();
				if (clone.CanFreeze)
					clone.Freeze();
				swatchBrush = clone;
				return;
			}

			colorHex = null;
			swatchBrush = null;
		}

		private static string FormatArgbHex(Color c, double opacity)
		{
			if (Math.Abs(opacity - 1.0) > 0.001)
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					"#{0:X2}{1:X2}{2:X2}{3:X2} (Op={4:F2})",
					c.A,
					c.R,
					c.G,
					c.B,
					opacity);
			}

			return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B);
		}

		private sealed class ReferenceEqualityComparerInstance : IEqualityComparer<ResourceDictionary>
		{
			public static readonly ReferenceEqualityComparerInstance Instance = new ReferenceEqualityComparerInstance();

			public bool Equals(ResourceDictionary? x, ResourceDictionary? y) => ReferenceEquals(x, y);

			public int GetHashCode(ResourceDictionary obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}