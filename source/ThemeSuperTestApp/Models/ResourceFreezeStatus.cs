namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Indicates the <see cref="System.Windows.Freezable.IsFrozen"/> state of a resource dictionary value.
	/// </summary>
	public enum ResourceFreezeStatus
	{
		/// <summary>Value is a <see cref="System.Windows.Freezable"/> and <c>IsFrozen == true</c>.</summary>
		Frozen,

		/// <summary>Value is an unfrozen <see cref="System.Windows.Freezable"/> (<c>IsFrozen == false</c>) — potential perf/thread-affinity hazard.</summary>
		UnfrozenWarning,

		/// <summary>Value does not derive from <see cref="System.Windows.Freezable"/> (e.g., <c>Style</c>, <c>ControlTemplate</c>, <c>Color</c>, <c>Double</c>).</summary>
		NotFreezable,

		/// <summary>Deferred BAML resource threw an exception when realized.</summary>
		RealizationError,
	}
}