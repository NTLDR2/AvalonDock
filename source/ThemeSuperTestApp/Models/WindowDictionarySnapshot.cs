using System;
using System.Collections.Generic;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Snapshot of the theme <c>ResourceDictionary</c> attached to a single AvalonDock visual root or window.
	/// </summary>
	public sealed class WindowDictionarySnapshot
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowDictionarySnapshot"/> class.
		/// </summary>
		/// <param name="hostKind">Type of AvalonDock host (<c>Main DockingManager</c>, <c>DetachedAnchorableWindow</c>, etc.).</param>
		/// <param name="windowTitle">Window or control title.</param>
		/// <param name="isVisible">Whether the host is currently visible.</param>
		/// <param name="themeDictionaryInstanceId">Reference identity label (<c>Ref #1 (0x...)</c>).</param>
		/// <param name="rawIdentityHash">Underlying <c>RuntimeHelpers.GetHashCode</c> of the dictionary instance, or <c>0</c>.</param>
		/// <param name="sourceUri">Resolved <c>ResourceDictionary.Source</c> or in-memory marker.</param>
		/// <param name="nestedMergedCount">Number of child <c>MergedDictionaries</c> inside the theme dictionary.</param>
		/// <param name="sharesRootInstanceWithDockingManager">Whether this host shares the exact same dictionary reference as <c>DockingManager</c>.</param>
		public WindowDictionarySnapshot(
			string hostKind,
			string windowTitle,
			bool isVisible,
			string themeDictionaryInstanceId,
			int rawIdentityHash,
			string sourceUri,
			int nestedMergedCount,
			bool sharesRootInstanceWithDockingManager)
		{
			HostKind = hostKind ?? throw new ArgumentNullException(nameof(hostKind));
			WindowTitle = windowTitle ?? string.Empty;
			IsVisible = isVisible;
			ThemeDictionaryInstanceId = themeDictionaryInstanceId ?? throw new ArgumentNullException(nameof(themeDictionaryInstanceId));
			RawIdentityHash = rawIdentityHash;
			SourceUri = sourceUri ?? string.Empty;
			NestedMergedCount = nestedMergedCount;
			SharesRootInstanceWithDockingManager = sharesRootInstanceWithDockingManager;
		}

		/// <summary>Gets the AvalonDock host control/window kind.</summary>
		public string HostKind { get; }

		/// <summary>Gets the window or host title.</summary>
		public string WindowTitle { get; }

		/// <summary>Gets a value indicating whether the host is currently visible.</summary>
		public bool IsVisible { get; }

		/// <summary>Gets the human-friendly object reference identity label (e.g., <c>"Ref #1 (0x02A4F110)"</c>).</summary>
		public string ThemeDictionaryInstanceId { get; }

		/// <summary>Gets the raw object identity hash code (<c>0</c> if no theme dictionary is attached).</summary>
		public int RawIdentityHash { get; }

		/// <summary>Gets the <c>ResourceDictionary.Source</c> URI text or in-memory marker.</summary>
		public string SourceUri { get; }

		/// <summary>Gets the count of nested <c>MergedDictionaries</c> on this theme dictionary.</summary>
		public int NestedMergedCount { get; }

		/// <summary>Gets a value indicating whether <c>ReferenceEquals(hostDict, dockingManagerDict)</c> is <see langword="true"/>.</summary>
		public bool SharesRootInstanceWithDockingManager { get; }
	}

	/// <summary>
	/// Aggregated multi-window theme `ResourceDictionary` sharing report across all active AvalonDock hosts.
	/// </summary>
	public sealed class WindowSharingReport
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowSharingReport"/> class.
		/// </summary>
		/// <param name="themeName">Active theme display name.</param>
		/// <param name="hosts">Per-host dictionary identity snapshots.</param>
		/// <param name="totalThemedHosts">Number of hosts carrying a theme dictionary.</param>
		/// <param name="distinctDictionaryInstances">Number of distinct <c>ResourceDictionary</c> object instances in memory.</param>
		public WindowSharingReport(
			string themeName,
			IReadOnlyList<WindowDictionarySnapshot> hosts,
			int totalThemedHosts,
			int distinctDictionaryInstances)
		{
			ThemeName = themeName ?? throw new ArgumentNullException(nameof(themeName));
			Hosts = hosts ?? throw new ArgumentNullException(nameof(hosts));
			TotalThemedHosts = totalThemedHosts;
			DistinctDictionaryInstances = distinctDictionaryInstances;
			RedundantBamlParsesCount = Math.Max(0, distinctDictionaryInstances - 1);
		}

		/// <summary>Gets the active theme name.</summary>
		public string ThemeName { get; }

		/// <summary>Gets the per-window/host snapshots.</summary>
		public IReadOnlyList<WindowDictionarySnapshot> Hosts { get; }

		/// <summary>Gets the count of AvalonDock hosts carrying a theme dictionary.</summary>
		public int TotalThemedHosts { get; }

		/// <summary>Gets the number of unique <c>ResourceDictionary</c> object references across all hosts.</summary>
		public int DistinctDictionaryInstances { get; }

		/// <summary>Gets the number of redundant duplicate <c>ResourceDictionary</c> allocations (<c>DistinctDictionaryInstances - 1</c>).</summary>
		public int RedundantBamlParsesCount { get; }

		/// <summary>Gets a value indicating whether multiple hosts are open and duplicating dictionaries instead of sharing one instance.</summary>
		public bool HasDuplicatedDictionaries => TotalThemedHosts > 1 && DistinctDictionaryInstances > 1;

		/// <summary>Gets a human-readable architectural verdict summarizing the current sharing state.</summary>
		public string VerdictText
		{
			get
			{
				if (TotalThemedHosts <= 1)
					return "1 host active (Main DockingManager). Undock a Floating or Detached window to test multi-window dictionary sharing.";

				if (DistinctDictionaryInstances == 1)
					return $"SHARED O(1): All {TotalThemedHosts} hosts reference the exact same ResourceDictionary instance (Ref #1, 0 redundant BAML parses).";

				return $"DUPLICATED ({DistinctDictionaryInstances} instances across {TotalThemedHosts} hosts): Each window allocated & parsed its own separate BAML ResourceDictionary!";
			}
		}
	}
}