using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Themes;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Inspects <see cref="DockingManager"/> and all open AvalonDock windows (<see cref="LayoutFloatingWindowControl"/>,
	/// <see cref="DetachedAnchorableWindow"/>, <see cref="NavigatorWindow"/>, and <see cref="OverlayWindow"/>) to track
	/// <see cref="ResourceDictionary"/> object-reference sharing vs. per-window BAML duplication.
	/// </summary>
	public sealed class WindowDictionaryTrackerService
	{
		/// <summary>
		/// Captures a multi-window dictionary sharing report for <paramref name="activeTheme"/>.
		/// </summary>
		/// <param name="themeName">Display name of the active theme.</param>
		/// <param name="activeTheme">Active <see cref="Theme"/> instance (or <see langword="null"/> for default generic theme).</param>
		/// <param name="dockingManager">Attached <see cref="DockingManager"/>.</param>
		/// <param name="candidateWindows">
		/// Optional explicit window list (used by unit tests); when <see langword="null"/>, inspects <c>Application.Current.Windows</c>.
		/// </param>
		/// <returns>A populated <see cref="WindowSharingReport"/>.</returns>
		public WindowSharingReport CaptureReport(
			string themeName,
			Theme? activeTheme,
			DockingManager? dockingManager,
			IEnumerable<Window>? candidateWindows = null)
		{
			var refLabels = new Dictionary<ResourceDictionary, string>(RefComparer.Instance);
			var snapshots = new List<WindowDictionarySnapshot>();
			ResourceDictionary? mainManagerDict = null;

			if (dockingManager != null)
			{
				mainManagerDict = FindThemeDictionary(dockingManager.Resources, activeTheme);
				snapshots.Add(CreateSnapshot(
					"Main DockingManager",
					"ShellView.DockManager",
					dockingManager.IsVisible,
					mainManagerDict,
					mainManagerDict,
					refLabels));
			}

			var app = Application.Current;
			var windows = candidateWindows
				?? (app != null && app.CheckAccess() ? app.Windows.Cast<Window>() : Enumerable.Empty<Window>());

			foreach (var window in windows)
			{
				if (window == null || !window.CheckAccess())
					continue;

				var hostKind = ClassifyAvalonDockWindow(window);
				if (hostKind == null)
					continue;

				var winDict = FindThemeDictionary(window.Resources, activeTheme);
				snapshots.Add(CreateSnapshot(
					hostKind,
					string.IsNullOrWhiteSpace(window.Title) ? $"<{window.GetType().Name}>" : window.Title,
					window.IsVisible,
					winDict,
					mainManagerDict,
					refLabels));
			}

			var themedHostCount = snapshots.Count(s => s.RawIdentityHash != 0);
			return new WindowSharingReport(
				string.IsNullOrWhiteSpace(themeName) ? "(None)" : themeName,
				snapshots,
				themedHostCount,
				refLabels.Count);
		}

		/// <summary>
		/// Formats a Markdown summary table for <paramref name="report"/>.
		/// </summary>
		/// <param name="report">The report to format.</param>
		/// <returns>GitHub-Flavored Markdown text.</returns>
		public string BuildMarkdownReport(WindowSharingReport report)
		{
			if (report == null) throw new ArgumentNullException(nameof(report));

			var sb = new StringBuilder();
			sb.AppendLine($"## Multi-Window ResourceDictionary Sharing — {report.ThemeName}");
			sb.AppendLine();
			sb.AppendLine($"- **Verdict:** {report.VerdictText}");
			sb.AppendLine($"- **Themed Hosts:** `{report.TotalThemedHosts}` | **Distinct Instances:** `{report.DistinctDictionaryInstances}` | **Redundant BAML Parses:** `{report.RedundantBamlParsesCount}`");
			sb.AppendLine();
			sb.AppendLine("| Host Kind | Title | Visible | Dictionary Instance ID | Shared w/ Manager | Source |");
			sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");

			foreach (var h in report.Hosts)
			{
				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"| `{0}` | {1} | {2} | `{3}` | {4} | `{5}` |",
					h.HostKind,
					h.WindowTitle,
					h.IsVisible,
					h.ThemeDictionaryInstanceId,
					h.SharesRootInstanceWithDockingManager ? "Yes" : "No",
					h.SourceUri);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		private static string? ClassifyAvalonDockWindow(Window? window)
		{
			if (window == null)
				return null;

			if (window is DetachedAnchorableWindow)
				return "DetachedAnchorableWindow";
			if (window is LayoutDocumentFloatingWindowControl)
				return "LayoutDocumentFloatingWindowControl";
			if (window is LayoutAnchorableFloatingWindowControl)
				return "LayoutAnchorableFloatingWindowControl";
			if (window is LayoutFloatingWindowControl)
				return "LayoutFloatingWindowControl";
			if (window is NavigatorWindow)
				return "NavigatorWindow (Ctrl+Tab)";
			if (window is OverlayWindow)
				return "OverlayWindow (Drag Target)";

			return null;
		}

		private static ResourceDictionary? FindThemeDictionary(ResourceDictionary? hostResources, Theme? activeTheme)
		{
			if (hostResources == null || activeTheme == null)
				return null;

			if (activeTheme is DictionaryTheme dictTheme && dictTheme.ThemeResourceDictionary != null)
			{
				return hostResources.MergedDictionaries
					.FirstOrDefault(d => ReferenceEquals(d, dictTheme.ThemeResourceDictionary))
					?? hostResources.MergedDictionaries.LastOrDefault();
			}

			var expectedUri = activeTheme.GetResourceUri();
			if (expectedUri != null)
			{
				return hostResources.MergedDictionaries
					.FirstOrDefault(d => d != null && d.Source == expectedUri);
			}

			return null;
		}

		private static WindowDictionarySnapshot CreateSnapshot(
			string hostKind,
			string title,
			bool isVisible,
			ResourceDictionary? themeDict,
			ResourceDictionary? mainManagerDict,
			Dictionary<ResourceDictionary, string> refLabels)
		{
			if (themeDict == null)
			{
				return new WindowDictionarySnapshot(
					hostKind,
					title,
					isVisible,
					"<None Attached>",
					rawIdentityHash: 0,
					sourceUri: "-",
					nestedMergedCount: 0,
					sharesRootInstanceWithDockingManager: false);
			}

			var hash = RuntimeHelpers.GetHashCode(themeDict);
			if (!refLabels.TryGetValue(themeDict, out var label))
			{
				label = string.Format(CultureInfo.InvariantCulture, "Ref #{0} (0x{1:X8})", refLabels.Count + 1, hash);
				refLabels[themeDict] = label;
			}

			var sourceText = themeDict.Source != null
				? themeDict.Source.ToString()
				: "<In-Memory DictionaryTheme>";

			return new WindowDictionarySnapshot(
				hostKind,
				title,
				isVisible,
				label,
				hash,
				sourceText,
				themeDict.MergedDictionaries.Count,
				mainManagerDict != null && ReferenceEquals(themeDict, mainManagerDict));
		}

		private sealed class RefComparer : IEqualityComparer<ResourceDictionary>
		{
			public static readonly RefComparer Instance = new RefComparer();

			public bool Equals(ResourceDictionary? x, ResourceDictionary? y) => ReferenceEquals(x, y);

			public int GetHashCode(ResourceDictionary obj) => RuntimeHelpers.GetHashCode(obj);
		}
	}
}