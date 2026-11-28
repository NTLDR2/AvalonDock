using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Markup;
using AvalonDock.Themes;
using AvalonDock.Themes.Type3New;
using Caliburn.Micro;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Lazy descriptor and runtime telemetry record for a single AvalonDock theme.
	/// </summary>
	/// <remarks>
	/// Static architectural metadata (base class kind, inheritance chain, CLR namespace, and <see cref="XmlnsDefinitionAttribute"/>
	/// coverage) is extracted from <see cref="ThemeType"/> via reflection without calling the theme constructor, preserving
	/// cold constructor behavior until the theme is explicitly selected or benchmarked.
	/// </remarks>
	public sealed class ThemeDescriptor : PropertyChangedBase
	{
		private const string AvalonDockXmlNamespace = "https://github.com/NTLDR2/AvalonDock";

		private readonly Func<Theme> _factory;
		private Theme? _cachedInstance;
		private bool _isInstantiated;
		private string _resourceUriText;
		private double? _lastColdConstructorMs;
		private long? _lastColdAllocatedBytes;
		private double? _lastApplyMs;
		private int? _directEntryCount;
		private int? _mergedDictionaryCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeDescriptor"/> class.
		/// </summary>
		/// <param name="id">Stable unique identifier for the theme entry.</param>
		/// <param name="displayName">Human-readable theme title shown in the selector.</param>
		/// <param name="familyName">Logical theme family (e.g., <c>VS 2026</c>, <c>VS 2022</c>, <c>VS 2013</c>).</param>
		/// <param name="themeType">Concrete CLR type of the theme.</param>
		/// <param name="polarity">Visual polarity of the theme.</param>
		/// <param name="factory">Lazy factory that constructs a new <see cref="Theme"/> instance.</param>
		/// <param name="isCustomRuntimeTheme">Whether this entry was loaded dynamically at runtime.</param>
		public ThemeDescriptor(
			string id,
			string displayName,
			string familyName,
			Type themeType,
			ThemePolarity polarity,
			Func<Theme> factory,
			bool isCustomRuntimeTheme = false)
		{
			if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException(nameof(id));
			if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentNullException(nameof(displayName));
			if (string.IsNullOrWhiteSpace(familyName)) throw new ArgumentNullException(nameof(familyName));
			ThemeType = themeType ?? throw new ArgumentNullException(nameof(themeType));
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));

			if (!typeof(Theme).IsAssignableFrom(themeType))
				throw new ArgumentException($"Type '{themeType.FullName}' does not inherit from '{typeof(Theme).FullName}'.", nameof(themeType));

			Id = id;
			DisplayName = displayName;
			FamilyName = familyName;
			Polarity = polarity;
			IsCustomRuntimeTheme = isCustomRuntimeTheme;

			BaseClassKind = ClassifyBaseClass(themeType);
			BaseClassBadgeText = FormatBaseClassBadge(BaseClassKind);
			InheritanceChain = BuildInheritanceChain(themeType);
			AssemblyName = themeType.Assembly.GetName().Name ?? string.Empty;
			ClrNamespace = themeType.Namespace ?? string.Empty;
			IsMappedInXmlnsDefinition = CheckXmlnsMapping(themeType);

			_resourceUriText = BaseClassKind == ThemeBaseClassKind.UriTheme
				? "(lazy - uninstantiated URI theme)"
				: "<null - DictionaryTheme>";
		}

		/// <summary>Gets the stable unique identifier.</summary>
		public string Id { get; }

		/// <summary>Gets the display name shown in UI selectors.</summary>
		public string DisplayName { get; }

		/// <summary>Gets the theme family group name.</summary>
		public string FamilyName { get; }

		/// <summary>Gets the concrete <see cref="Theme"/> CLR type.</summary>
		public Type ThemeType { get; }

		/// <summary>Gets the visual luminance/color polarity.</summary>
		public ThemePolarity Polarity { get; }

		/// <summary>Gets a value indicating whether this entry was loaded dynamically from an external file/JSON snippet.</summary>
		public bool IsCustomRuntimeTheme { get; }

		/// <summary>Gets which base-class pattern this theme uses (<c>Theme</c>, <c>DictionaryTheme</c>, or <c>VsTheme</c>).</summary>
		public ThemeBaseClassKind BaseClassKind { get; }

		/// <summary>Gets the concise base-class badge label (e.g., <c>"VsTheme : DictionaryTheme"</c>).</summary>
		public string BaseClassBadgeText { get; }

		/// <summary>Gets the full CLR inheritance chain up to <see cref="Theme"/>.</summary>
		public string InheritanceChain { get; }

		/// <summary>Gets the simple name of the assembly defining <see cref="ThemeType"/>.</summary>
		public string AssemblyName { get; }

		/// <summary>Gets the CLR namespace of <see cref="ThemeType"/>.</summary>
		public string ClrNamespace { get; }

		/// <summary>
		/// Gets a value indicating whether <see cref="ClrNamespace"/> is mapped to the AvalonDock XAML namespace via <see cref="XmlnsDefinitionAttribute"/>.
		/// </summary>
		public bool IsMappedInXmlnsDefinition { get; }

		/// <summary>Gets a human-readable summary of the XAML <see cref="XmlnsDefinitionAttribute"/> status.</summary>
		public string XmlnsStatusText => IsMappedInXmlnsDefinition
			? "Xmlns Mapped"
			: "Missing XmlnsDefinition (clr-namespace required)";

		/// <summary>Gets the cached <see cref="Theme"/> instance once instantiated, or <see langword="null"/> while still lazy.</summary>
		public Theme? CachedInstance => _cachedInstance;

		/// <summary>Gets a value indicating whether an instance has been constructed and cached.</summary>
		public bool IsInstantiated
		{
			get => _isInstantiated;
			private set => Set(ref _isInstantiated, value);
		}

		/// <summary>Gets the resolved <see cref="Theme.GetResourceUri"/> text or null marker.</summary>
		public string ResourceUriText
		{
			get => _resourceUriText;
			private set => Set(ref _resourceUriText, value);
		}

		/// <summary>Gets the duration in milliseconds of the most recent cold <c>new XxxTheme()</c> constructor call.</summary>
		public double? LastColdConstructorMs
		{
			get => _lastColdConstructorMs;
			private set => Set(ref _lastColdConstructorMs, value);
		}

		/// <summary>Gets the approximate managed byte delta measured around the most recent cold constructor call.</summary>
		public long? LastColdAllocatedBytes
		{
			get => _lastColdAllocatedBytes;
			private set
			{
				if (Set(ref _lastColdAllocatedBytes, value))
					NotifyOfPropertyChange(nameof(LastColdAllocatedKb));
			}
		}

		/// <summary>Gets <see cref="LastColdAllocatedBytes"/> expressed in kilobytes (<c>KiB</c>) for sortable grid display.</summary>
		public double? LastColdAllocatedKb => _lastColdAllocatedBytes.HasValue
			? _lastColdAllocatedBytes.Value / 1024.0
			: null;

		/// <summary>Gets the duration in milliseconds of the most recent <c>DockingManager.Theme</c> assignment.</summary>
		public double? LastApplyMs
		{
			get => _lastApplyMs;
			private set => Set(ref _lastApplyMs, value);
		}

		/// <summary>Gets the number of direct entries in <c>ThemeResourceDictionary</c> when applicable.</summary>
		public int? DirectEntryCount
		{
			get => _directEntryCount;
			private set => Set(ref _directEntryCount, value);
		}

		/// <summary>Gets the number of merged dictionaries in <c>ThemeResourceDictionary</c> when applicable.</summary>
		public int? MergedDictionaryCount
		{
			get => _mergedDictionaryCount;
			private set => Set(ref _mergedDictionaryCount, value);
		}

		/// <summary>
		/// Returns the cached <see cref="Theme"/> instance, or constructs and caches a new one while recording cold constructor telemetry.
		/// </summary>
		/// <param name="forceFresh">When <see langword="true"/>, always invokes the factory to construct a fresh instance.</param>
		/// <returns>The <see cref="Theme"/> instance.</returns>
		public Theme GetOrCreateInstance(bool forceFresh = false)
		{
			if (_cachedInstance != null && !forceFresh)
				return _cachedInstance;

			var instance = InstantiateColdCore(measureGcClean: false);
			_cachedInstance = instance;
			IsInstantiated = true;
			NotifyOfPropertyChange(nameof(CachedInstance));
			return instance;
		}

		/// <summary>
		/// Constructs a brand-new <see cref="Theme"/> instance solely for cold-constructor benchmarking without replacing <see cref="CachedInstance"/>
		/// if one is already active on <c>DockingManager</c> (caches it if none existed yet).
		/// </summary>
		/// <returns>The newly constructed <see cref="Theme"/> instance.</returns>
		public Theme BenchmarkColdInstance()
		{
			var instance = InstantiateColdCore(measureGcClean: true);
			if (_cachedInstance == null)
			{
				_cachedInstance = instance;
				IsInstantiated = true;
				NotifyOfPropertyChange(nameof(CachedInstance));
			}

			return instance;
		}

		/// <summary>
		/// Returns <see cref="CachedInstance"/> when already instantiated, or creates a transient read-only instance
		/// for dictionary auditing/diffing without marking <see cref="IsInstantiated"/> or altering profiler telemetry.
		/// </summary>
		/// <returns>A <see cref="Theme"/> instance suitable for read-only inspection.</returns>
		public Theme GetInspectionInstance() => _cachedInstance ?? _factory();

		/// <summary>
		/// Records the measured <c>DockingManager.Theme</c> switch duration in milliseconds.
		/// </summary>
		/// <param name="applyMs">Elapsed milliseconds.</param>
		public void RecordApplyDuration(double applyMs)
		{
			LastApplyMs = applyMs;
		}

		/// <inheritdoc/>
		public override string ToString() => $"{DisplayName} [{BaseClassBadgeText}]";

		private static ThemeBaseClassKind ClassifyBaseClass(Type themeType)
		{
			if (typeof(VsTheme).IsAssignableFrom(themeType))
				return ThemeBaseClassKind.VsTheme;
			if (typeof(DictionaryTheme).IsAssignableFrom(themeType))
				return ThemeBaseClassKind.DictionaryTheme;
			return ThemeBaseClassKind.UriTheme;
		}

		private static string FormatBaseClassBadge(ThemeBaseClassKind kind)
		{
			switch (kind)
			{
				case ThemeBaseClassKind.VsTheme:
					return "VsTheme : DictionaryTheme";
				case ThemeBaseClassKind.DictionaryTheme:
					return "DictionaryTheme";
				default:
					return "Theme (URI)";
			}
		}

		private static string BuildInheritanceChain(Type themeType)
		{
			var names = new List<string>();
			var current = themeType;
			while (current != null && typeof(Theme).IsAssignableFrom(current))
			{
				names.Add(current.Name);
				current = current.BaseType;
			}

			return string.Join(" -> ", names);
		}

		private static bool CheckXmlnsMapping(Type themeType)
		{
			var clrNs = themeType.Namespace;
			if (string.IsNullOrEmpty(clrNs))
				return false;

			return Attribute.GetCustomAttributes(themeType.Assembly, typeof(XmlnsDefinitionAttribute))
				.OfType<XmlnsDefinitionAttribute>()
				.Any(attr =>
					string.Equals(attr.XmlNamespace, AvalonDockXmlNamespace, StringComparison.Ordinal) &&
					string.Equals(attr.ClrNamespace, clrNs, StringComparison.Ordinal));
		}

		private Theme InstantiateColdCore(bool measureGcClean)
		{
			if (measureGcClean)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}

			var memBefore = GC.GetTotalMemory(false);
			var startTicks = Stopwatch.GetTimestamp();
			var instance = _factory();
			var endTicks = Stopwatch.GetTimestamp();
			var memAfter = GC.GetTotalMemory(false);

			LastColdConstructorMs = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
			LastColdAllocatedBytes = Math.Max(0L, memAfter - memBefore);

			var uri = instance.GetResourceUri();
			ResourceUriText = uri != null ? uri.ToString() : "<null - DictionaryTheme>";

			if (instance is DictionaryTheme dictTheme && dictTheme.ThemeResourceDictionary != null)
			{
				DirectEntryCount = dictTheme.ThemeResourceDictionary.Count;
				MergedDictionaryCount = dictTheme.ThemeResourceDictionary.MergedDictionaries.Count;
			}
			else
			{
				DirectEntryCount = null;
				MergedDictionaryCount = null;
			}

			return instance;
		}
	}
}