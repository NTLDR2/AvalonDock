using System;
using System.Windows.Media;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Represents a single audited entry discovered inside a theme's root <c>ResourceDictionary</c> or its <c>MergedDictionaries</c>.
	/// </summary>
	public sealed class ResourceEntryRecord
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceEntryRecord"/> class.
		/// </summary>
		/// <param name="keyDisplay">Formatted human-readable key string.</param>
		/// <param name="semanticKeySignature">Normalized cross-assembly key signature used for cross-theme diffing.</param>
		/// <param name="category">Classification of the resource key.</param>
		/// <param name="valueTypeName">Concrete runtime CLR type name of the resource value.</param>
		/// <param name="colorHex">Hexadecimal color or gradient summary, if applicable.</param>
		/// <param name="swatchBrush">Frozen preview brush for UI rendering, or <see langword="null"/>.</param>
		/// <param name="freezeStatus">Evaluated <c>Freezable.IsFrozen</c> audit state.</param>
		/// <param name="sourceLocation">Provenance path within the dictionary tree.</param>
		/// <param name="dictionaryDepth">Nesting depth (<c>0</c> = root dictionary, <c>&gt;= 1</c> = merged dictionary).</param>
		/// <param name="lookupPrecedenceRank">Monotonic precedence rank (lower rank wins in WPF lookup).</param>
		public ResourceEntryRecord(
			string keyDisplay,
			string semanticKeySignature,
			ResourceEntryCategory category,
			string valueTypeName,
			string? colorHex,
			Brush? swatchBrush,
			ResourceFreezeStatus freezeStatus,
			string sourceLocation,
			int dictionaryDepth,
			int lookupPrecedenceRank)
		{
			KeyDisplay = keyDisplay ?? throw new ArgumentNullException(nameof(keyDisplay));
			SemanticKeySignature = semanticKeySignature ?? throw new ArgumentNullException(nameof(semanticKeySignature));
			Category = category;
			ValueTypeName = valueTypeName ?? throw new ArgumentNullException(nameof(valueTypeName));
			ColorHex = colorHex;
			SwatchBrush = swatchBrush;
			FreezeStatus = freezeStatus;
			SourceLocation = sourceLocation ?? throw new ArgumentNullException(nameof(sourceLocation));
			DictionaryDepth = dictionaryDepth;
			LookupPrecedenceRank = lookupPrecedenceRank;
		}

		/// <summary>Gets the formatted key display label.</summary>
		public string KeyDisplay { get; }

		/// <summary>Gets the normalized semantic signature used when diffing two themes.</summary>
		public string SemanticKeySignature { get; }

		/// <summary>Gets the key category.</summary>
		public ResourceEntryCategory Category { get; }

		/// <summary>Gets the runtime value type name.</summary>
		public string ValueTypeName { get; }

		/// <summary>Gets the <c>#AARRGGBB</c> color or gradient stop summary when the value is a brush or color.</summary>
		public string? ColorHex { get; }

		/// <summary>Gets a frozen preview <see cref="Brush"/> safe for UI swatch binding, or <see langword="null"/>.</summary>
		public Brush? SwatchBrush { get; }

		/// <summary>Gets a value indicating whether a preview swatch is available.</summary>
		public bool HasSwatch => SwatchBrush != null;

		/// <summary>Gets the <c>Freezable.IsFrozen</c> audit state.</summary>
		public ResourceFreezeStatus FreezeStatus { get; }

		/// <summary>Gets the concise label for <see cref="FreezeStatus"/>.</summary>
		public string FreezeStatusText
		{
			get
			{
				switch (FreezeStatus)
				{
					case ResourceFreezeStatus.Frozen:
						return "Frozen";
					case ResourceFreezeStatus.UnfrozenWarning:
						return "UNFROZEN (!)";
					case ResourceFreezeStatus.RealizationError:
						return "ERROR";
					default:
						return "N/A";
				}
			}
		}

		/// <summary>Gets the provenance description (e.g., <c>"Root Dictionary (Direct)"</c> or <c>"Merged[0]: ..."</c>).</summary>
		public string SourceLocation { get; }

		/// <summary>Gets the dictionary tree depth (<c>0</c> = root).</summary>
		public int DictionaryDepth { get; }

		/// <summary>Gets the lookup precedence rank (lower number = higher priority in WPF resource resolution).</summary>
		public int LookupPrecedenceRank { get; }
	}
}