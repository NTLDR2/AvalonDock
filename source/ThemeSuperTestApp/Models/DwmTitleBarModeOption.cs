using System;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Selectable UI option wrapping a <see cref="DwmTitleBarMode"/> value and its display label.
	/// </summary>
	public sealed class DwmTitleBarModeOption
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DwmTitleBarModeOption"/> class.
		/// </summary>
		/// <param name="mode">The underlying DWM title-bar mode.</param>
		/// <param name="displayName">Human-readable label shown in the selector.</param>
		public DwmTitleBarModeOption(DwmTitleBarMode mode, string displayName)
		{
			Mode = mode;
			DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
		}

		/// <summary>Gets the DWM title-bar mode.</summary>
		public DwmTitleBarMode Mode { get; }

		/// <summary>Gets the display label.</summary>
		public string DisplayName { get; }

		/// <inheritdoc/>
		public override string ToString() => DisplayName;
	}
}