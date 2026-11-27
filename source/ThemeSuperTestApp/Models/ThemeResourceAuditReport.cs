using System;
using System.Collections.Generic;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Complete `ResourceDictionary` and `Freezable.IsFrozen` audit snapshot for a single theme.
	/// </summary>
	public sealed class ThemeResourceAuditReport
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeResourceAuditReport"/> class.
		/// </summary>
		/// <param name="themeName">Audited theme display name.</param>
		/// <param name="entries">All discovered resource entries in lookup precedence order.</param>
		/// <param name="totalDictionariesVisited">Total root + merged dictionaries traversed.</param>
		/// <param name="brushAndColorCount">Count of brush and color entries.</param>
		/// <param name="frozenFreezableCount">Count of frozen <c>Freezable</c> entries.</param>
		/// <param name="unfrozenFreezableCount">Count of unfrozen <c>Freezable</c> entries.</param>
		/// <param name="styleAndTemplateCount">Count of <c>Style</c> and <c>FrameworkTemplate</c> entries.</param>
		/// <param name="treeSummary">Concise summary of the dictionary hierarchy.</param>
		public ThemeResourceAuditReport(
			string themeName,
			IReadOnlyList<ResourceEntryRecord> entries,
			int totalDictionariesVisited,
			int brushAndColorCount,
			int frozenFreezableCount,
			int unfrozenFreezableCount,
			int styleAndTemplateCount,
			string treeSummary)
		{
			ThemeName = themeName ?? throw new ArgumentNullException(nameof(themeName));
			Entries = entries ?? throw new ArgumentNullException(nameof(entries));
			TotalDictionariesVisited = totalDictionariesVisited;
			BrushAndColorCount = brushAndColorCount;
			FrozenFreezableCount = frozenFreezableCount;
			UnfrozenFreezableCount = unfrozenFreezableCount;
			StyleAndTemplateCount = styleAndTemplateCount;
			TreeSummary = treeSummary ?? string.Empty;
		}

		/// <summary>Gets the name of the audited theme.</summary>
		public string ThemeName { get; }

		/// <summary>Gets all discovered resource entries across the root and merged dictionaries.</summary>
		public IReadOnlyList<ResourceEntryRecord> Entries { get; }

		/// <summary>Gets the total number of entries across all visited dictionaries.</summary>
		public int TotalEntries => Entries.Count;

		/// <summary>Gets the number of distinct <c>ResourceDictionary</c> nodes visited.</summary>
		public int TotalDictionariesVisited { get; }

		/// <summary>Gets the count of <c>Brush</c> and <c>Color</c> entries.</summary>
		public int BrushAndColorCount { get; }

		/// <summary>Gets the count of <c>Freezable</c> entries with <c>IsFrozen == true</c>.</summary>
		public int FrozenFreezableCount { get; }

		/// <summary>Gets the count of <c>Freezable</c> entries with <c>IsFrozen == false</c>.</summary>
		public int UnfrozenFreezableCount { get; }

		/// <summary>Gets the count of <c>Style</c>, <c>ControlTemplate</c>, and <c>DataTemplate</c> entries.</summary>
		public int StyleAndTemplateCount { get; }

		/// <summary>Gets a human-readable summary of the dictionary hierarchy.</summary>
		public string TreeSummary { get; }
	}
}