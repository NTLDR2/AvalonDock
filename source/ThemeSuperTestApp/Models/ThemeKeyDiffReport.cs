using System;
using System.Collections.Generic;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Classifies a key parity difference between an active theme and a comparison theme.
	/// </summary>
	public enum ThemeKeyDiffKind
	{
		/// <summary>Key is defined in the comparison theme, but missing in the active theme.</summary>
		MissingInActive,

		/// <summary>Key is defined in the active theme, but missing in the comparison theme.</summary>
		ExtraInActive,

		/// <summary>Key is defined in both themes with different value CLR types (e.g., <c>LinearGradientBrush</c> vs. <c>SolidColorBrush</c>).</summary>
		ValueTypeMismatch,
	}

	/// <summary>
	/// Single row in a cross-theme semantic key parity diff.
	/// </summary>
	public sealed class ThemeKeyDiffEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeKeyDiffEntry"/> class.
		/// </summary>
		/// <param name="semanticKeySignature">Normalized semantic key signature.</param>
		/// <param name="diffKind">Classification of the difference.</param>
		/// <param name="category">Key category.</param>
		/// <param name="activeValueType">Value type in the active theme, or <c>"-"</c>.</param>
		/// <param name="activeColorHex">Color hex in the active theme, if any.</param>
		/// <param name="comparisonValueType">Value type in the comparison theme, or <c>"-"</c>.</param>
		/// <param name="comparisonColorHex">Color hex in the comparison theme, if any.</param>
		public ThemeKeyDiffEntry(
			string semanticKeySignature,
			ThemeKeyDiffKind diffKind,
			ResourceEntryCategory category,
			string activeValueType,
			string? activeColorHex,
			string comparisonValueType,
			string? comparisonColorHex)
		{
			SemanticKeySignature = semanticKeySignature ?? throw new ArgumentNullException(nameof(semanticKeySignature));
			DiffKind = diffKind;
			Category = category;
			ActiveValueType = activeValueType ?? "-";
			ActiveColorHex = activeColorHex;
			ComparisonValueType = comparisonValueType ?? "-";
			ComparisonColorHex = comparisonColorHex;
		}

		/// <summary>Gets the normalized semantic key signature.</summary>
		public string SemanticKeySignature { get; }

		/// <summary>Gets the kind of parity difference.</summary>
		public ThemeKeyDiffKind DiffKind { get; }

		/// <summary>Gets the display label for <see cref="DiffKind"/>.</summary>
		public string DiffKindText
		{
			get
			{
				switch (DiffKind)
				{
					case ThemeKeyDiffKind.MissingInActive:
						return "Missing in Active";
					case ThemeKeyDiffKind.ExtraInActive:
						return "Extra in Active";
					default:
						return "Type Mismatch";
				}
			}
		}

		/// <summary>Gets the key category.</summary>
		public ResourceEntryCategory Category { get; }

		/// <summary>Gets the value type in the active theme.</summary>
		public string ActiveValueType { get; }

		/// <summary>Gets the color hex in the active theme, if applicable.</summary>
		public string? ActiveColorHex { get; }

		/// <summary>Gets the value type in the comparison theme.</summary>
		public string ComparisonValueType { get; }

		/// <summary>Gets the color hex in the comparison theme, if applicable.</summary>
		public string? ComparisonColorHex { get; }
	}

	/// <summary>
	/// Summary and entries of a semantic resource-key comparison between two themes.
	/// </summary>
	public sealed class ThemeKeyDiffReport
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeKeyDiffReport"/> class.
		/// </summary>
		/// <param name="activeThemeName">Active theme name.</param>
		/// <param name="comparisonThemeName">Comparison theme name.</param>
		/// <param name="sharedMatchingCount">Number of keys present in both themes with matching value types.</param>
		/// <param name="missingInActiveCount">Number of keys present only in the comparison theme.</param>
		/// <param name="extraInActiveCount">Number of keys present only in the active theme.</param>
		/// <param name="typeMismatchCount">Number of keys present in both themes with differing value types.</param>
		/// <param name="differences">Ordered list of all difference entries.</param>
		public ThemeKeyDiffReport(
			string activeThemeName,
			string comparisonThemeName,
			int sharedMatchingCount,
			int missingInActiveCount,
			int extraInActiveCount,
			int typeMismatchCount,
			IReadOnlyList<ThemeKeyDiffEntry> differences)
		{
			ActiveThemeName = activeThemeName ?? throw new ArgumentNullException(nameof(activeThemeName));
			ComparisonThemeName = comparisonThemeName ?? throw new ArgumentNullException(nameof(comparisonThemeName));
			SharedMatchingCount = sharedMatchingCount;
			MissingInActiveCount = missingInActiveCount;
			ExtraInActiveCount = extraInActiveCount;
			TypeMismatchCount = typeMismatchCount;
			Differences = differences ?? throw new ArgumentNullException(nameof(differences));
		}

		/// <summary>Gets the active theme name.</summary>
		public string ActiveThemeName { get; }

		/// <summary>Gets the comparison theme name.</summary>
		public string ComparisonThemeName { get; }

		/// <summary>Gets the count of shared keys with matching value types.</summary>
		public int SharedMatchingCount { get; }

		/// <summary>Gets the count of keys missing in the active theme.</summary>
		public int MissingInActiveCount { get; }

		/// <summary>Gets the count of extra keys in the active theme.</summary>
		public int ExtraInActiveCount { get; }

		/// <summary>Gets the count of keys with mismatched value types.</summary>
		public int TypeMismatchCount { get; }

		/// <summary>Gets all difference entries.</summary>
		public IReadOnlyList<ThemeKeyDiffEntry> Differences { get; }
	}
}