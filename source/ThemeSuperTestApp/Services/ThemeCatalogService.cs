using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using AvalonDock;
using AvalonDock.Themes;
using AvalonDock.Themes.VS;
using Caliburn.Micro;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Catalogs all 20 built-in AvalonDock themes (plus any dynamically loaded runtime themes) using lazy factories
	/// and applies the active selection to the attached classic <see cref="DockingManager"/>.
	/// </summary>
	public sealed class ThemeCatalogService
	{
		/// <summary>
		/// Default startup theme ID (<c>ArcDark</c> — a lazy URI <see cref="Theme"/> so all 12 <see cref="DictionaryTheme"/>
		/// entries remain uninstantiated at startup until explicitly selected or benchmarked).
		/// </summary>
		public const string DefaultThemeId = "ArcDark";

		private DockingManager? _dockingManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeCatalogService"/> class with all 20 built-in themes registered lazily.
		/// </summary>
		public ThemeCatalogService()
		{
			Themes = new BindableCollection<ThemeDescriptor>(CreateBuiltInDescriptors());
		}

		/// <summary>
		/// Raised whenever <see cref="ActiveTheme"/> changes or is re-applied.
		/// </summary>
		public event EventHandler? ActiveThemeChanged;

		/// <summary>
		/// Raised whenever cold-constructor or apply-sweep benchmarks finish updating descriptor telemetry.
		/// </summary>
		public event EventHandler? BenchmarksUpdated;

		/// <summary>
		/// Gets the observable catalog of built-in and runtime-loaded theme descriptors.
		/// </summary>
		public BindableCollection<ThemeDescriptor> Themes { get; }

		/// <summary>
		/// Gets the currently active theme descriptor, or <see langword="null"/> before startup selection.
		/// </summary>
		public ThemeDescriptor? ActiveTheme { get; private set; }

		/// <summary>
		/// Gets the attached classic <see cref="DockingManager"/> instance, if attached.
		/// </summary>
		public DockingManager? AttachedDockingManager => _dockingManager;

		/// <summary>
		/// Attaches the classic <see cref="DockingManager"/> whose <see cref="DockingManager.Theme"/> is controlled by this service.
		/// </summary>
		/// <param name="dockingManager">The docking manager control.</param>
		public void Attach(DockingManager dockingManager)
		{
			_dockingManager = dockingManager ?? throw new ArgumentNullException(nameof(dockingManager));
		}

		/// <summary>
		/// Finds a registered <see cref="ThemeDescriptor"/> by its <see cref="ThemeDescriptor.Id"/>.
		/// </summary>
		/// <param name="id">Theme identifier (case-insensitive).</param>
		/// <returns>The matching descriptor, or <see langword="null"/>.</returns>
		public ThemeDescriptor? FindById(string? id)
		{
			if (string.IsNullOrWhiteSpace(id))
				return null;

			return Themes.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Applies the theme matching <paramref name="id"/> to the attached <see cref="DockingManager"/>.
		/// </summary>
		/// <param name="id">Theme identifier.</param>
		/// <param name="forceFreshInstance">Whether to force constructing a fresh <see cref="Theme"/> instance.</param>
		/// <returns><see langword="true"/> if found and applied; otherwise <see langword="false"/>.</returns>
		public bool ApplyTheme(string id, bool forceFreshInstance = false)
		{
			var descriptor = FindById(id);
			return descriptor != null && ApplyTheme(descriptor, forceFreshInstance);
		}

		/// <summary>
		/// Applies <paramref name="descriptor"/> to the attached <see cref="DockingManager"/> while measuring apply latency.
		/// </summary>
		/// <param name="descriptor">The theme descriptor to apply.</param>
		/// <param name="forceFreshInstance">Whether to force constructing a fresh <see cref="Theme"/> instance.</param>
		/// <returns><see langword="true"/> when applied.</returns>
		public bool ApplyTheme(ThemeDescriptor descriptor, bool forceFreshInstance = false)
		{
			if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));

			var instance = descriptor.GetOrCreateInstance(forceFreshInstance);
			if (_dockingManager != null)
			{
				var startTicks = Stopwatch.GetTimestamp();
				_dockingManager.Theme = instance;
				var endTicks = Stopwatch.GetTimestamp();
				descriptor.RecordApplyDuration((endTicks - startTicks) * 1000.0 / Stopwatch.Frequency);
			}

			ActiveTheme = descriptor;
			ActiveThemeChanged?.Invoke(this, EventArgs.Empty);
			return true;
		}

		/// <summary>
		/// Registers (or replaces by <paramref name="id"/>) a dynamically loaded runtime theme in <see cref="Themes"/>.
		/// </summary>
		/// <param name="id">Unique runtime theme identifier.</param>
		/// <param name="displayName">Display title shown in selectors.</param>
		/// <param name="polarity">Visual polarity.</param>
		/// <param name="themeType">Concrete <see cref="Theme"/> type (typically <see cref="VsTheme"/>).</param>
		/// <param name="factory">Factory returning the runtime <see cref="Theme"/>.</param>
		/// <returns>The registered <see cref="ThemeDescriptor"/>.</returns>
		public ThemeDescriptor RegisterRuntimeTheme(
			string id,
			string displayName,
			ThemePolarity polarity,
			Type themeType,
			Func<Theme> factory)
		{
			var existing = FindById(id);
			if (existing != null)
				Themes.Remove(existing);

			var descriptor = new ThemeDescriptor(
				id,
				displayName,
				"Custom Runtime",
				themeType,
				polarity,
				factory,
				isCustomRuntimeTheme: true);

			Themes.Add(descriptor);
			return descriptor;
		}

		/// <summary>
		/// Constructs a fresh instance of every registered theme to measure cold <c>.ctor</c> duration and managed memory delta.
		/// </summary>
		public void BenchmarkAllColdConstructors()
		{
			foreach (var descriptor in Themes.ToList())
				_ = descriptor.BenchmarkColdInstance();

			BenchmarksUpdated?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Sequentially applies every registered theme to the attached <see cref="DockingManager"/> and awaits a WPF
		/// <see cref="DispatcherPriority.Render"/> pass per theme, restoring the previous active theme when finished.
		/// </summary>
		/// <returns>A task that completes when the full sweep finishes.</returns>
		public async Task SweepAndBenchmarkApplyAllThemesAsync()
		{
			var previous = ActiveTheme;

			foreach (var descriptor in Themes.ToList())
			{
				var instance = descriptor.GetOrCreateInstance(forceFresh: false);
				var startTicks = Stopwatch.GetTimestamp();

				if (_dockingManager != null)
				{
					_dockingManager.Theme = instance;
					await _dockingManager.Dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Render);
				}

				var endTicks = Stopwatch.GetTimestamp();
				descriptor.RecordApplyDuration((endTicks - startTicks) * 1000.0 / Stopwatch.Frequency);
				ActiveTheme = descriptor;
				ActiveThemeChanged?.Invoke(this, EventArgs.Empty);
			}

			if (previous != null && !ReferenceEquals(ActiveTheme, previous))
			{
				var measuredSweepApplyMs = previous.LastApplyMs;
				ApplyTheme(previous, forceFreshInstance: false);
				if (measuredSweepApplyMs.HasValue)
					previous.RecordApplyDuration(measuredSweepApplyMs.Value);
			}

			BenchmarksUpdated?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Generates a GitHub-Flavored Markdown table summarizing architecture and measured timings across all registered themes.
		/// </summary>
		/// <returns>Formatted Markdown report text.</returns>
		public string BuildMarkdownBenchmarkReport()
		{
			var sb = new StringBuilder();
			sb.AppendLine("# AvalonDock v5 — 20-Theme Architecture & Timing Benchmark (`net48`)");
			sb.AppendLine();
			sb.AppendLine("| Theme | Base Class | CLR Namespace | Xmlns Mapped | Cold `.ctor` (ms) | Cold Alloc (KB) | Apply + Render (ms) | Root Keys | Merged Dicts | `GetResourceUri()` |");
			sb.AppendLine("| :--- | :--- | :--- | :--- | ---: | ---: | ---: | ---: | ---: | :--- |");

			foreach (var t in Themes)
			{
				var ctorText = t.LastColdConstructorMs.HasValue
					? string.Format(CultureInfo.InvariantCulture, "{0:F3}", t.LastColdConstructorMs.Value)
					: "-";
				var allocText = t.LastColdAllocatedKb.HasValue
					? string.Format(CultureInfo.InvariantCulture, "{0:F1}", t.LastColdAllocatedKb.Value)
					: "-";
				var applyText = t.LastApplyMs.HasValue
					? string.Format(CultureInfo.InvariantCulture, "{0:F2}", t.LastApplyMs.Value)
					: "-";
				var rootKeys = t.DirectEntryCount.HasValue
					? t.DirectEntryCount.Value.ToString(CultureInfo.InvariantCulture)
					: "-";
				var merged = t.MergedDictionaryCount.HasValue
					? t.MergedDictionaryCount.Value.ToString(CultureInfo.InvariantCulture)
					: "-";

				sb.AppendFormat(
					CultureInfo.InvariantCulture,
					"| {0} | `{1}` | `{2}` | {3} | {4} | {5} | {6} | {7} | {8} | `{9}` |",
					t.DisplayName,
					t.BaseClassBadgeText,
					t.ClrNamespace,
					t.IsMappedInXmlnsDefinition ? "Yes" : "**MISSING**",
					ctorText,
					allocText,
					applyText,
					rootKeys,
					merged,
					t.ResourceUriText);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		private static ThemeDescriptor[] CreateBuiltInDescriptors()
		{
			return new[]
			{
				// 1. Arc (AvalonDock.Themes.Arc - URI Theme)
				Create<ArcDarkTheme>("ArcDark", "Arc Dark", "Arc", ThemePolarity.Dark, () => new ArcDarkTheme()),
				Create<ArcLightTheme>("ArcLight", "Arc Light", "Arc", ThemePolarity.Light, () => new ArcLightTheme()),

				// 2. Visual Studio 2026 (AvalonDock.Themes.VS - VsTheme : DictionaryTheme, .vstheme + .json)
				Create<VS2026DarkTheme>("VS2026Dark", "VS 2026 Dark", "VS 2026", ThemePolarity.Dark, () => new VS2026DarkTheme()),
				Create<VS2026BlueTheme>("VS2026Blue", "VS 2026 Blue", "VS 2026", ThemePolarity.Blue, () => new VS2026BlueTheme()),
				Create<VS2026LightTheme>("VS2026Light", "VS 2026 Light", "VS 2026", ThemePolarity.Light, () => new VS2026LightTheme()),

				// 3. Visual Studio 2022 (AvalonDock.Themes.VS - VsTheme : DictionaryTheme, VS2022 Generic.xaml)
				Create<VS2022DarkTheme>("VS2022Dark", "VS 2022 Dark", "VS 2022", ThemePolarity.Dark, () => new VS2022DarkTheme()),
				Create<VS2022BlueTheme>("VS2022Blue", "VS 2022 Blue", "VS 2022", ThemePolarity.Blue, () => new VS2022BlueTheme()),
				Create<VS2022LightTheme>("VS2022Light", "VS 2022 Light", "VS 2022", ThemePolarity.Light, () => new VS2022LightTheme()),

				// 4. Visual Studio 2015 (AvalonDock.Themes.VS - VsTheme : DictionaryTheme, Classic Generic.xaml)
				Create<VS2015DarkTheme>("VS2015Dark", "VS 2015 Dark", "VS 2015", ThemePolarity.Dark, () => new VS2015DarkTheme()),
				Create<VS2015BlueTheme>("VS2015Blue", "VS 2015 Blue", "VS 2015", ThemePolarity.Blue, () => new VS2015BlueTheme()),
				Create<VS2015LightTheme>("VS2015Light", "VS 2015 Light", "VS 2015", ThemePolarity.Light, () => new VS2015LightTheme()),

				// 5. Visual Studio 2013 (AvalonDock.Themes.VS2013 - DictionaryTheme, .vstheme.gz)
				Create<Vs2013DarkTheme>("Vs2013Dark", "VS 2013 Dark", "VS 2013", ThemePolarity.Dark, () => new Vs2013DarkTheme()),
				Create<Vs2013BlueTheme>("Vs2013Blue", "VS 2013 Blue", "VS 2013", ThemePolarity.Blue, () => new Vs2013BlueTheme()),
				Create<Vs2013LightTheme>("Vs2013Light", "VS 2013 Light", "VS 2013", ThemePolarity.Light, () => new Vs2013LightTheme()),

				// 6. Legacy / Classic URI Themes (AvalonDock.Themes.* + Built-in GenericTheme)
				Create<VS2010Theme>("VS2010", "VS 2010", "Classic URI", ThemePolarity.ClassicSystem, () => new VS2010Theme()),
				Create<ExpressionDarkTheme>("ExpressionDark", "Expression Dark", "Expression", ThemePolarity.Dark, () => new ExpressionDarkTheme()),
				Create<ExpressionLightTheme>("ExpressionLight", "Expression Light", "Expression", ThemePolarity.Light, () => new ExpressionLightTheme()),
				Create<MetroTheme>("Metro", "Metro", "Classic URI", ThemePolarity.Light, () => new MetroTheme()),
				Create<AeroTheme>("Aero", "Aero", "Classic URI", ThemePolarity.ClassicSystem, () => new AeroTheme()),
				Create<GenericTheme>("Generic", "Generic (Default)", "Built-in Core", ThemePolarity.ClassicSystem, () => new GenericTheme()),
			};
		}

		private static ThemeDescriptor Create<TTheme>(
			string id,
			string displayName,
			string familyName,
			ThemePolarity polarity,
			Func<Theme> factory)
			where TTheme : Theme
		{
			return new ThemeDescriptor(id, displayName, familyName, typeof(TTheme), polarity, factory);
		}
	}
}