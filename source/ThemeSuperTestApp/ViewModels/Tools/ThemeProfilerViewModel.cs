using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AvalonDock.Core;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Tool 1: 20-theme architecture inspector, <c>[XmlnsDefinition]</c> auditor, and cold <c>.ctor</c> vs. <c>OnThemeChanged</c> profiler.
	/// </summary>
	public sealed class ThemeProfilerViewModel : ToolboxScreen
	{
		private readonly ThemeCatalogService _themeCatalog;
		private ThemeDescriptor? _selectedRow;
		private bool _isBusy;
		private string _profilerStatusText = "Click 'Benchmark Cold Constructors' to measure eager DictionaryTheme vs. lazy URI Theme overhead.";

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeProfilerViewModel"/> class.
		/// </summary>
		/// <param name="themeCatalog">The shared 20-theme catalog service.</param>
		public ThemeProfilerViewModel(ThemeCatalogService themeCatalog)
		{
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));

			Id = "Tool.ThemeProfiler";
			DisplayName = "1. Theme Architecture & Profiler";
			ToolTipText = "Inspect base classes, XmlnsDefinition mappings, cold .ctor cost, and apply+render latency across all 20 themes";
			Zone = DockZone.BottomLeft;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+P";

			_selectedRow = _themeCatalog.ActiveTheme ?? _themeCatalog.Themes.FirstOrDefault();
			_themeCatalog.ActiveThemeChanged += OnCatalogUpdated;
			_themeCatalog.BenchmarksUpdated += OnCatalogUpdated;
			_themeCatalog.Themes.CollectionChanged += OnThemesCollectionChanged;

			foreach (var descriptor in _themeCatalog.Themes)
				descriptor.PropertyChanged += OnDescriptorPropertyChanged;
		}

		/// <summary>Gets the observable collection of theme descriptors bound to the profiler grid.</summary>
		public Caliburn.Micro.BindableCollection<ThemeDescriptor> Themes => _themeCatalog.Themes;

		/// <summary>Gets or sets the currently selected row in the profiler grid.</summary>
		public ThemeDescriptor? SelectedRow
		{
			get => _selectedRow;
			set
			{
				if (Set(ref _selectedRow, value))
					NotifyOfPropertyChange(nameof(CanApplySelectedRowTheme));
			}
		}

		/// <summary>Gets or sets a value indicating whether a benchmark sweep is currently executing.</summary>
		public bool IsBusy
		{
			get => _isBusy;
			private set
			{
				if (!Set(ref _isBusy, value))
					return;

				NotifyOfPropertyChange(nameof(CanBenchmarkColdConstructors));
				NotifyOfPropertyChange(nameof(CanSweepAllThemes));
				NotifyOfPropertyChange(nameof(CanApplySelectedRowTheme));
			}
		}

		/// <summary>Gets or sets the status message shown in the profiler toolbar.</summary>
		public string ProfilerStatusText
		{
			get => _profilerStatusText;
			set => Set(ref _profilerStatusText, value);
		}

		/// <summary>Gets a value indicating whether <see cref="BenchmarkColdConstructors"/> can execute.</summary>
		public bool CanBenchmarkColdConstructors => !IsBusy;

		/// <summary>Gets a value indicating whether <see cref="SweepAllThemes"/> can execute.</summary>
		public bool CanSweepAllThemes => !IsBusy;

		/// <summary>Gets a value indicating whether <see cref="ApplySelectedRowTheme"/> can execute.</summary>
		public bool CanApplySelectedRowTheme => !IsBusy && SelectedRow != null;

		/// <summary>Gets the average cold constructor duration across URI-based <c>Theme</c> entries.</summary>
		public string UriThemeAvgColdMsText
		{
			get
			{
				var measured = _themeCatalog.Themes
					.Where(t => t.BaseClassKind == ThemeBaseClassKind.UriTheme && t.LastColdConstructorMs.HasValue)
					.Select(t => t.LastColdConstructorMs!.Value)
					.ToList();

				return measured.Count == 0
					? "Not measured yet"
					: string.Format(CultureInfo.InvariantCulture, "{0:F3} ms avg (n={1})", measured.Average(), measured.Count);
			}
		}

		/// <summary>Gets the average cold constructor duration across <c>DictionaryTheme</c> / <c>VsTheme</c> entries (plus eager ratio).</summary>
		public string DictionaryThemeAvgColdMsText
		{
			get
			{
				var dictMeasured = _themeCatalog.Themes
					.Where(t => t.BaseClassKind != ThemeBaseClassKind.UriTheme && t.LastColdConstructorMs.HasValue)
					.Select(t => t.LastColdConstructorMs!.Value)
					.ToList();

				if (dictMeasured.Count == 0)
					return "Not measured yet";

				var dictAvg = dictMeasured.Average();
				var uriMeasured = _themeCatalog.Themes
					.Where(t => t.BaseClassKind == ThemeBaseClassKind.UriTheme && t.LastColdConstructorMs.HasValue)
					.Select(t => t.LastColdConstructorMs!.Value)
					.ToList();

				if (uriMeasured.Count > 0)
				{
					var uriAvg = Math.Max(0.0005, uriMeasured.Average());
					var ratio = dictAvg / uriAvg;
					return string.Format(CultureInfo.InvariantCulture, "{0:F3} ms avg (n={1}, ~{2:F0}x vs URI)", dictAvg, dictMeasured.Count, ratio);
				}

				return string.Format(CultureInfo.InvariantCulture, "{0:F3} ms avg (n={1})", dictAvg, dictMeasured.Count);
			}
		}

		/// <summary>Gets the summary of themes missing <c>[assembly: XmlnsDefinition]</c> CLR namespace registration.</summary>
		public string XmlnsMissingSummaryText
		{
			get
			{
				var total = _themeCatalog.Themes.Count;
				var missing = _themeCatalog.Themes.Count(t => !t.IsMappedInXmlnsDefinition);
				return $"{missing} / {total} missing XmlnsDefinition";
			}
		}

		/// <summary>Gets the summary of themes whose <c>GetResourceUri()</c> returns <see langword="null"/>.</summary>
		public string NullResourceUriSummaryText
		{
			get
			{
				var total = _themeCatalog.Themes.Count;
				var dictCount = _themeCatalog.Themes.Count(t => t.BaseClassKind != ThemeBaseClassKind.UriTheme);
				return $"{dictCount} / {total} return null ResourceUri";
			}
		}

		/// <summary>
		/// Runs a cold constructor benchmark (<c>new XxxTheme()</c>) across every registered theme without changing the active theme.
		/// </summary>
		public void BenchmarkColdConstructors()
		{
			if (IsBusy)
				return;

			IsBusy = true;
			try
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				_themeCatalog.BenchmarkAllColdConstructors();
				sw.Stop();

				foreach (var descriptor in _themeCatalog.Themes)
				{
					descriptor.PropertyChanged -= OnDescriptorPropertyChanged;
					descriptor.PropertyChanged += OnDescriptorPropertyChanged;
				}

				RefreshSummaryProperties();
				ProfilerStatusText = string.Format(
					CultureInfo.InvariantCulture,
					"Benchmarked {0} cold constructors in {1:F1} ms ({2} vs. {3}).",
					_themeCatalog.Themes.Count,
					sw.Elapsed.TotalMilliseconds,
					UriThemeAvgColdMsText,
					DictionaryThemeAvgColdMsText);
			}
			finally
			{
				IsBusy = false;
			}
		}

		/// <summary>
		/// Cycles through every registered theme on the attached <c>DockingManager</c>, measuring full apply + WPF render pass latency.
		/// </summary>
		/// <returns>A task that completes once all themes have been swept and the previous active theme is restored.</returns>
		public async Task SweepAllThemes()
		{
			if (IsBusy)
				return;

			IsBusy = true;
			try
			{
				ProfilerStatusText = $"Sweeping apply + render pass across {_themeCatalog.Themes.Count} themes...";
				var sw = System.Diagnostics.Stopwatch.StartNew();
				await _themeCatalog.SweepAndBenchmarkApplyAllThemesAsync();
				sw.Stop();

				RefreshSummaryProperties();
				ProfilerStatusText = string.Format(
					CultureInfo.InvariantCulture,
					"Completed full {0}-theme apply + render sweep in {1:F0} ms.",
					_themeCatalog.Themes.Count,
					sw.Elapsed.TotalMilliseconds);
			}
			finally
			{
				IsBusy = false;
			}
		}

		/// <summary>
		/// Applies <see cref="SelectedRow"/> to the attached <c>DockingManager</c>.
		/// </summary>
		public void ApplySelectedRowTheme()
		{
			if (IsBusy || SelectedRow == null)
				return;

			_themeCatalog.ApplyTheme(SelectedRow);
			ProfilerStatusText = $"Applied '{SelectedRow.DisplayName}' [{SelectedRow.BaseClassBadgeText}].";
		}

		/// <summary>
		/// Guardless double-click handler for the <c>DataGrid</c> (keeps <c>DataGrid.IsEnabled</c> permanently <see langword="true"/>).
		/// </summary>
		public void ApplyRowOnDoubleClick()
		{
			if (!IsBusy && SelectedRow != null)
				ApplySelectedRowTheme();
		}

		/// <summary>
		/// Copies the Markdown architecture and benchmark table for all themes to the Windows clipboard.
		/// </summary>
		public void CopyBenchmarkMarkdown()
		{
			var markdown = _themeCatalog.BuildMarkdownBenchmarkReport();
			try
			{
				Clipboard.SetDataObject(markdown, copy: true);
				ProfilerStatusText = $"Copied {_themeCatalog.Themes.Count}-row Markdown benchmark report to clipboard ({markdown.Length} chars).";
			}
			catch (ExternalException ex)
			{
				ProfilerStatusText = $"Clipboard busy ({ex.Message}).";
			}
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
			{
				_themeCatalog.ActiveThemeChanged -= OnCatalogUpdated;
				_themeCatalog.BenchmarksUpdated -= OnCatalogUpdated;
				_themeCatalog.Themes.CollectionChanged -= OnThemesCollectionChanged;
				foreach (var descriptor in _themeCatalog.Themes)
					descriptor.PropertyChanged -= OnDescriptorPropertyChanged;
			}

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private void OnThemesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.OldItems != null)
			{
				foreach (var oldItem in e.OldItems.OfType<ThemeDescriptor>())
					oldItem.PropertyChanged -= OnDescriptorPropertyChanged;
			}

			if (e.NewItems != null)
			{
				foreach (var newItem in e.NewItems.OfType<ThemeDescriptor>())
				{
					newItem.PropertyChanged -= OnDescriptorPropertyChanged;
					newItem.PropertyChanged += OnDescriptorPropertyChanged;
				}
			}

			RefreshSummaryProperties();
		}

		private void OnCatalogUpdated(object? sender, EventArgs e)
		{
			if (_themeCatalog.ActiveTheme != null)
				SelectedRow = _themeCatalog.ActiveTheme;

			RefreshSummaryProperties();
		}

		private void OnDescriptorPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ThemeDescriptor.LastColdConstructorMs) ||
				e.PropertyName == nameof(ThemeDescriptor.LastApplyMs))
			{
				RefreshSummaryProperties();
			}
		}

		private void RefreshSummaryProperties()
		{
			NotifyOfPropertyChange(nameof(UriThemeAvgColdMsText));
			NotifyOfPropertyChange(nameof(DictionaryThemeAvgColdMsText));
			NotifyOfPropertyChange(nameof(XmlnsMissingSummaryText));
			NotifyOfPropertyChange(nameof(NullResourceUriSummaryText));
		}
	}
}