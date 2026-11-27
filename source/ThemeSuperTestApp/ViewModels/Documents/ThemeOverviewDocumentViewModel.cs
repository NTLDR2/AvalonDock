using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Infrastructure;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Documents
{
	/// <summary>
	/// Specimen Document 1: Visual token &amp; brush swatch dashboard rendering every color/brush resource of the active theme.
	/// </summary>
	public sealed class ThemeOverviewDocumentViewModel : DocumentScreen
	{
		private readonly ThemeCatalogService _themeCatalog;
		private readonly ResourceDictionaryAuditService _auditService;

		private string _activeThemeHeadline = string.Empty;
		private string _activeThemeSubheadline = string.Empty;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeOverviewDocumentViewModel"/> class.
		/// </summary>
		/// <param name="themeCatalog">The shared 20-theme catalog service.</param>
		/// <param name="auditService">The resource dictionary audit service.</param>
		public ThemeOverviewDocumentViewModel(ThemeCatalogService themeCatalog, ResourceDictionaryAuditService auditService)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

			Id = "Doc.ThemeOverview";
			DisplayName = "Theme Swatch Sheet";
			ToolTipText = "Live visual brush & color token swatch cards for the currently active AvalonDock theme";
			CanClose = false;
			IconSource = SpecimenIconFactory.CreateBadgeIcon(Color.FromRgb(0x3B, 0x82, 0xF6), Color.FromRgb(0xDB, 0xEA, 0xFE));

			BrushSwatches = new BindableCollection<ResourceEntryRecord>();

			_themeCatalog.ActiveThemeChanged += OnActiveThemeChanged;
			RefreshSwatches();
		}

		/// <summary>Gets the visual brush/color tiles of the active theme.</summary>
		public BindableCollection<ResourceEntryRecord> BrushSwatches { get; }

		/// <summary>Gets the headline describing the active theme and its base-class pattern.</summary>
		public string ActiveThemeHeadline
		{
			get => _activeThemeHeadline;
			private set => Set(ref _activeThemeHeadline, value);
		}

		/// <summary>Gets the subheadline summarizing brush, frozen, and unfrozen counts.</summary>
		public string ActiveThemeSubheadline
		{
			get => _activeThemeSubheadline;
			private set => Set(ref _activeThemeSubheadline, value);
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
				_themeCatalog.ActiveThemeChanged -= OnActiveThemeChanged;

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private void OnActiveThemeChanged(object? sender, EventArgs e)
		{
			RefreshSwatches();
		}

		private void RefreshSwatches()
		{
			var active = _themeCatalog.ActiveTheme ?? _themeCatalog.FindById(ThemeCatalogService.DefaultThemeId);
			if (active == null)
				return;

			var report = _auditService.AuditDescriptor(active, _themeCatalog.AttachedDockingManager);
			var swatches = report.Entries
				.Where(e => e.HasSwatch)
				.OrderBy(e => e.LookupPrecedenceRank)
				.ToList();

			BrushSwatches.Clear();
			BrushSwatches.AddRange(swatches);

			ActiveThemeHeadline = $"{active.DisplayName}  —  {active.BaseClassBadgeText}  ({active.ClrNamespace})";
			ActiveThemeSubheadline = $"Inheritance: {active.InheritanceChain}  |  Brushes/Colors: {report.BrushAndColorCount}  |  Frozen: {report.FrozenFreezableCount}  |  Unfrozen Warnings: {report.UnfrozenFreezableCount}  |  ResourceUri: {active.ResourceUriText}";
		}
	}
}