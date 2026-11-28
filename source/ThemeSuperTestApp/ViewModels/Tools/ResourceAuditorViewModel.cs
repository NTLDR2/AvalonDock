using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AvalonDock.Core;
using Caliburn.Micro;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Filter modes for Tool 2's live <c>ResourceDictionary</c> explorer.
	/// </summary>
	public enum ResourceExplorerFilterMode
	{
		/// <summary>Show all entries across root and merged dictionaries.</summary>
		AllEntries,

		/// <summary>Show only unfrozen <c>Freezable</c> entries (<c>IsFrozen == false</c>).</summary>
		UnfrozenFreezablesOnly,

		/// <summary>Show only brushes and colors.</summary>
		BrushesAndColorsOnly,

		/// <summary>Show only <c>Style</c>s and <c>FrameworkTemplate</c>s.</summary>
		StylesAndTemplatesOnly,

		/// <summary>Show only <c>ComponentResourceKey</c> entries.</summary>
		ComponentResourceKeysOnly,
	}

	/// <summary>
	/// Tool 2: Live <c>ResourceDictionary</c> &amp; <c>Freezable.IsFrozen</c> explorer plus cross-theme semantic key parity diff.
	/// </summary>
	public sealed class ResourceAuditorViewModel : ToolboxScreen
	{
		private readonly ThemeCatalogService _themeCatalog;
		private readonly ResourceDictionaryAuditService _auditService;

		private ThemeResourceAuditReport? _currentAuditReport;
		private ThemeKeyDiffReport? _currentDiffReport;
		private ThemeDescriptor? _comparisonTheme;
		private ResourceExplorerFilterMode _selectedFilterMode = ResourceExplorerFilterMode.AllEntries;
		private string _searchText = string.Empty;
		private string _diffSearchText = string.Empty;
		private string _auditorStatusText = "Ready";

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceAuditorViewModel"/> class.
		/// </summary>
		/// <param name="themeCatalog">The shared 18-theme catalog service.</param>
		/// <param name="auditService">The resource dictionary audit and diff service.</param>
		public ResourceAuditorViewModel(ThemeCatalogService themeCatalog, ResourceDictionaryAuditService auditService)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

			Id = "Tool.ResourceAuditor";
			DisplayName = "2. Resource & Freezable Auditor";
			ToolTipText = "Inspect active ResourceDictionary entries, color swatches, Freezable.IsFrozen warnings, and cross-theme key diffs (Ctrl+Alt+R)";
			Zone = DockZone.RightTop;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+R";

			FilterModes = new[]
			{
				ResourceExplorerFilterMode.AllEntries,
				ResourceExplorerFilterMode.UnfrozenFreezablesOnly,
				ResourceExplorerFilterMode.BrushesAndColorsOnly,
				ResourceExplorerFilterMode.StylesAndTemplatesOnly,
				ResourceExplorerFilterMode.ComponentResourceKeysOnly,
			};

			FilteredEntries = new BindableCollection<ResourceEntryRecord>();
			FilteredDiffEntries = new BindableCollection<ThemeKeyDiffEntry>();

			_comparisonTheme = _themeCatalog.FindById("VS2022Dark") ?? _themeCatalog.Themes.FirstOrDefault();
			_themeCatalog.ActiveThemeChanged += OnActiveThemeChanged;

			AuditCurrentTheme();
		}

		/// <summary>Gets the available explorer filter modes.</summary>
		public IReadOnlyList<ResourceExplorerFilterMode> FilterModes { get; }

		/// <summary>Gets the observable catalog of themes selectable as the diff comparison target.</summary>
		public BindableCollection<ThemeDescriptor> ComparisonThemes => _themeCatalog.Themes;

		/// <summary>Gets the filtered resource entries displayed in Tab 1.</summary>
		public BindableCollection<ResourceEntryRecord> FilteredEntries { get; }

		/// <summary>Gets the filtered cross-theme diff entries displayed in Tab 2.</summary>
		public BindableCollection<ThemeKeyDiffEntry> FilteredDiffEntries { get; }

		/// <summary>Gets the latest audit report for the active theme.</summary>
		public ThemeResourceAuditReport? CurrentAuditReport => _currentAuditReport;

		/// <summary>Gets the latest diff report against <see cref="ComparisonTheme"/>.</summary>
		public ThemeKeyDiffReport? CurrentDiffReport => _currentDiffReport;

		/// <summary>Gets a value indicating whether the active theme contains one or more unfrozen <c>Freezable</c> resources.</summary>
		public bool HasUnfrozenWarnings => (_currentAuditReport?.UnfrozenFreezableCount ?? 0) > 0;

		/// <summary>Gets or sets the filter text for Tab 1.</summary>
		public string SearchText
		{
			get => _searchText;
			set
			{
				if (Set(ref _searchText, value ?? string.Empty))
					ApplyEntryFilter();
			}
		}

		/// <summary>Gets or sets the selected category/freeze filter mode for Tab 1.</summary>
		public ResourceExplorerFilterMode SelectedFilterMode
		{
			get => _selectedFilterMode;
			set
			{
				if (Set(ref _selectedFilterMode, value))
					ApplyEntryFilter();
			}
		}

		/// <summary>Gets or sets the comparison baseline theme for Tab 2.</summary>
		public ThemeDescriptor? ComparisonTheme
		{
			get => _comparisonTheme;
			set
			{
				if (Set(ref _comparisonTheme, value))
					RebuildDiffReport();
			}
		}

		/// <summary>Gets or sets the filter text for Tab 2's diff table.</summary>
		public string DiffSearchText
		{
			get => _diffSearchText;
			set
			{
				if (Set(ref _diffSearchText, value ?? string.Empty))
					ApplyDiffFilter();
			}
		}

		/// <summary>Gets or sets the status message shown at the top of the toolbox.</summary>
		public string AuditorStatusText
		{
			get => _auditorStatusText;
			set => Set(ref _auditorStatusText, value);
		}

		/// <summary>
		/// Re-runs the `ResourceDictionary` and `Freezable` audit on the active theme and refreshes the diff.
		/// </summary>
		public void RefreshAudit()
		{
			AuditCurrentTheme();
		}

		/// <summary>
		/// Copies the Markdown audit &amp; diff summary to the Windows clipboard.
		/// </summary>
		public void CopyAuditSummaryMarkdown()
		{
			if (_currentAuditReport == null)
				return;

			var md = _auditService.BuildMarkdownAuditSummary(_currentAuditReport, _currentDiffReport);
			try
			{
				Clipboard.SetDataObject(md, copy: true);
				AuditorStatusText = $"Copied Markdown audit summary for '{_currentAuditReport.ThemeName}' ({md.Length} chars).";
			}
			catch (ExternalException ex)
			{
				AuditorStatusText = $"Clipboard busy ({ex.Message}).";
			}
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
				_themeCatalog.ActiveThemeChanged -= OnActiveThemeChanged;

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private static bool MatchesEntryFilter(ResourceEntryRecord record, ResourceExplorerFilterMode mode, string query)
		{
			switch (mode)
			{
				case ResourceExplorerFilterMode.UnfrozenFreezablesOnly:
					if (record.FreezeStatus != ResourceFreezeStatus.UnfrozenWarning)
						return false;
					break;
				case ResourceExplorerFilterMode.BrushesAndColorsOnly:
					if (!record.HasSwatch)
						return false;
					break;
				case ResourceExplorerFilterMode.StylesAndTemplatesOnly:
					if (record.Category != ResourceEntryCategory.ImplicitTypeStyle &&
						record.Category != ResourceEntryCategory.DataTemplateKey &&
						record.ValueTypeName != "Style" &&
						!record.ValueTypeName.EndsWith("Template", StringComparison.Ordinal))
					{
						return false;
					}

					break;
				case ResourceExplorerFilterMode.ComponentResourceKeysOnly:
					if (record.Category != ResourceEntryCategory.ComponentResourceKey)
						return false;
					break;
			}

			if (string.IsNullOrWhiteSpace(query))
				return true;

			return record.KeyDisplay.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
				record.ValueTypeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
				(record.ColorHex != null && record.ColorHex.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
				record.SourceLocation.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private void OnActiveThemeChanged(object? sender, EventArgs e)
		{
			AuditCurrentTheme();
		}

		private void AuditCurrentTheme()
		{
			var active = _themeCatalog.ActiveTheme ?? _themeCatalog.FindById(ThemeCatalogService.DefaultThemeId);
			if (active == null)
				return;

			_currentAuditReport = _auditService.AuditDescriptor(active, _themeCatalog.AttachedDockingManager);
			NotifyOfPropertyChange(nameof(CurrentAuditReport));
			NotifyOfPropertyChange(nameof(HasUnfrozenWarnings));

			ApplyEntryFilter();
			RebuildDiffReport();

			AuditorStatusText = _currentAuditReport.UnfrozenFreezableCount > 0
				? $"WARNING: '{active.DisplayName}' has {_currentAuditReport.UnfrozenFreezableCount} UNFROZEN Freezables out of {_currentAuditReport.TotalEntries} entries!"
				: $"OK: '{active.DisplayName}' — {_currentAuditReport.TotalEntries} entries, {_currentAuditReport.FrozenFreezableCount} frozen, 0 unfrozen.";
		}

		private void ApplyEntryFilter()
		{
			FilteredEntries.Clear();
			if (_currentAuditReport == null)
				return;

			var query = _searchText.Trim();
			var matches = _currentAuditReport.Entries
				.Where(r => MatchesEntryFilter(r, _selectedFilterMode, query))
				.ToList();

			FilteredEntries.AddRange(matches);
		}

		private void RebuildDiffReport()
		{
			if (_currentAuditReport == null || _comparisonTheme == null)
			{
				_currentDiffReport = null;
				NotifyOfPropertyChange(nameof(CurrentDiffReport));
				FilteredDiffEntries.Clear();
				return;
			}

			var compReport = _auditService.AuditDescriptor(_comparisonTheme, _themeCatalog.AttachedDockingManager);
			_currentDiffReport = _auditService.DiffThemes(_currentAuditReport, compReport);
			NotifyOfPropertyChange(nameof(CurrentDiffReport));
			ApplyDiffFilter();
		}

		private void ApplyDiffFilter()
		{
			FilteredDiffEntries.Clear();
			if (_currentDiffReport == null)
				return;

			var query = _diffSearchText.Trim();
			var items = string.IsNullOrWhiteSpace(query)
				? _currentDiffReport.Differences
				: _currentDiffReport.Differences
					.Where(d =>
						d.SemanticKeySignature.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
						d.DiffKindText.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
						d.ActiveValueType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
						d.ComparisonValueType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
					.ToList();

			FilteredDiffEntries.AddRange(items);
		}
	}
}