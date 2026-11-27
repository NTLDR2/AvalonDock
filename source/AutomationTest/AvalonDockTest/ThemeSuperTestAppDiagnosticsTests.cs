namespace AvalonDockTest
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Media;

	using NUnit.Framework;

	using AvalonDock;
	using AvalonDock.Controls;
	using AvalonDock.Layout;

	using ThemeSuperTestApp.Models;
	using ThemeSuperTestApp.Services;
	using ThemeSuperTestApp.ViewModels.Tools;

	/// <summary>
	/// STA verification tests for <c>ThemeSuperTestApp</c>'s non-invasive theme diagnostic engines:
	/// lazy 20-theme catalog reflection metadata, <c>[XmlnsDefinition]</c> auditing, <c>ResourceDictionary</c>
	/// and <c>Freezable.IsFrozen</c> auditing, multi-window dictionary sharing tracking, live WPF
	/// <c>PresentationTraceSources</c> capture, runtime <c>VsJsonThemeParser</c> sandbox, and Windows OS snapshot capture.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ThemeSuperTestAppDiagnosticsTests
	{
		[Test]
		public void Catalog_ContainsAll20BuiltInThemes_LazilyWithoutEagerInstantiation()
		{
			var catalog = new ThemeCatalogService();

			Assert.That(catalog.Themes, Has.Count.EqualTo(20));
			Assert.That(catalog.Themes.All(t => !t.IsInstantiated), Is.True, "All 20 built-in themes must remain lazy at catalog construction.");
			Assert.That(catalog.Themes.All(t => t.CachedInstance == null), Is.True);
		}

		[Test]
		public void Catalog_ClassifiesBaseClassesNamespacesAndXmlnsMappingsAccurately()
		{
			var catalog = new ThemeCatalogService();

			var uriThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.UriTheme).ToList();
			var dictThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.DictionaryTheme).ToList();
			var vsThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.VsTheme).ToList();

			Assert.That(uriThemes, Has.Count.EqualTo(8));
			Assert.That(uriThemes.All(t => t.IsMappedInXmlnsDefinition), Is.True);
			Assert.That(uriThemes.All(t => t.ClrNamespace == "AvalonDock.Themes"), Is.True);

			Assert.That(dictThemes, Has.Count.EqualTo(3));
			Assert.That(dictThemes.All(t => t.ClrNamespace == "AvalonDock.Themes"), Is.True);
			Assert.That(dictThemes.All(t => t.IsMappedInXmlnsDefinition), Is.True);

			Assert.That(vsThemes, Has.Count.EqualTo(9));
			Assert.That(vsThemes.All(t => t.ClrNamespace == "AvalonDock.Themes.VS"), Is.True);
			Assert.That(vsThemes.All(t => !t.IsMappedInXmlnsDefinition), Is.True, "AvalonDock.Themes.VS omits 'AvalonDock.Themes.VS' from [XmlnsDefinition].");
		}

		[Test]
		public void Catalog_BenchmarkAllColdConstructors_PopulatesTelemetryAndMarkdownTable()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();

			catalog.BenchmarkAllColdConstructors();

			Assert.That(catalog.Themes.All(t => t.LastColdConstructorMs.HasValue && t.LastColdConstructorMs.Value >= 0.0), Is.True);
			var markdown = catalog.BuildMarkdownBenchmarkReport();
			Assert.That(markdown, Does.Contain("VS 2026 Dark"));
			Assert.That(markdown, Does.Contain("Arc Dark"));
			Assert.That(markdown, Does.Contain("**MISSING**"));
		}

		[Test]
		public void ResourceAuditor_AuditsThemesWithoutPollutingLazyFlag_AndDetectsUnfrozenBrushesWithoutMutating()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var auditor = new ResourceDictionaryAuditService();

			var arcDesc = catalog.FindById("ArcDark");
			var vsDesc = catalog.FindById("VS2022Dark");
			Assert.That(arcDesc, Is.Not.Null);
			Assert.That(vsDesc, Is.Not.Null);

			var arcReport = auditor.AuditDescriptor(arcDesc!);
			var vsReport = auditor.AuditDescriptor(vsDesc!);

			Assert.That(arcDesc!.IsInstantiated, Is.False, "Read-only inspection must not mark descriptor as instantiated.");
			Assert.That(vsDesc!.IsInstantiated, Is.False, "Read-only inspection must not mark descriptor as instantiated.");
			Assert.That(vsReport.FrozenFreezableCount, Is.GreaterThan(50));

			var diff = auditor.DiffThemes(arcReport, vsReport);
			Assert.That(diff.SharedMatchingCount, Is.GreaterThan(50));

			var syntheticDict = new ResourceDictionary();
			var unfrozenRed = new SolidColorBrush(Colors.Red);
			var frozenBlue = new SolidColorBrush(Colors.Blue);
			frozenBlue.Freeze();
			syntheticDict["UnfrozenRed"] = unfrozenRed;
			syntheticDict["FrozenBlue"] = frozenBlue;

			var syntheticReport = auditor.AuditDictionary("Synthetic", syntheticDict);
			Assert.That(syntheticReport.FrozenFreezableCount, Is.EqualTo(1));
			Assert.That(syntheticReport.UnfrozenFreezableCount, Is.EqualTo(1));
			Assert.That(unfrozenRed.IsFrozen, Is.False, "Auditor must never mutate the audited Freezable instance.");
		}

		[Test]
		public void WindowSharingTracker_DetectsSharedDictionaryForDictionaryTheme_AndDuplicateInstancesForUriTheme()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var tracker = new WindowDictionaryTrackerService();
			var manager = new DockingManager();
			catalog.Attach(manager);

			var anchorable = new LayoutAnchorable { Title = "DetachedProbe" };
			var detachedWindow = new DetachedAnchorableWindow(anchorable, new ContentPresenter());
			var windows = new List<Window> { detachedWindow };

			try
			{
				var vsDesc = catalog.FindById("VS2022Dark")!;
				catalog.ApplyTheme(vsDesc);
				detachedWindow.UpdateThemeResources(null, vsDesc.CachedInstance);

				var vsSharing = tracker.CaptureReport(vsDesc.DisplayName, vsDesc.CachedInstance, manager, windows);
				Assert.That(vsSharing.TotalThemedHosts, Is.EqualTo(2));
				Assert.That(vsSharing.DistinctDictionaryInstances, Is.EqualTo(1));
				Assert.That(vsSharing.RedundantBamlParsesCount, Is.EqualTo(0));
				Assert.That(vsSharing.HasDuplicatedDictionaries, Is.False);

				var oldTheme = vsDesc.CachedInstance;
				var arcDesc = catalog.FindById("ArcDark")!;
				catalog.ApplyTheme(arcDesc);
				detachedWindow.UpdateThemeResources(oldTheme, arcDesc.CachedInstance);

				var arcSharing = tracker.CaptureReport(arcDesc.DisplayName, arcDesc.CachedInstance, manager, windows);
				Assert.That(arcSharing.TotalThemedHosts, Is.EqualTo(2));
				Assert.That(arcSharing.DistinctDictionaryInstances, Is.EqualTo(2));
				Assert.That(arcSharing.RedundantBamlParsesCount, Is.EqualTo(1));
				Assert.That(arcSharing.HasDuplicatedDictionaries, Is.True);
			}
			finally
			{
				detachedWindow.Close();
			}
		}

		[Test]
		public void WpfTraceCaptureService_CapturesSyntheticBindingError40_TaggedWithActiveTheme()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var manager = new DockingManager();
			catalog.Attach(manager);
			catalog.ApplyTheme("ArcDark");

			using var traceCapture = new WpfTraceCaptureService(catalog);
			traceCapture.Clear();
			traceCapture.TriggerSyntheticBindingErrorForVerification();

			Assert.That(traceCapture.Events, Is.Not.Empty);
			var first = traceCapture.Events[0];
			Assert.That(first.Category, Is.EqualTo("DataBinding"));
			Assert.That(first.Severity, Is.EqualTo(TraceEventType.Error));
			Assert.That(first.EventId, Is.EqualTo(40));
			Assert.That(first.ThemeName, Is.EqualTo("Arc Dark"));
		}

		[Test]
		public void RuntimeThemeLoader_OsThemeService_AndReportExporter_WorkEndToEndOnNet48()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var manager = new DockingManager();
			catalog.Attach(manager);

			using var osService = new WindowsOsThemeService();
			Assert.That(osService.CurrentSnapshot.OsVersionCaption, Is.Not.Empty);
			Assert.That(osService.CurrentSnapshot.OsAccentBrush.IsFrozen, Is.True);

			var loaderVm = new RuntimeThemeLoaderViewModel(catalog);
			var validApplied = loaderVm.ApplyInlineJsonOverride();
			Assert.That(validApplied, Is.True);
			Assert.That(loaderVm.HasParserError, Is.False);
			Assert.That(catalog.Themes, Has.Count.EqualTo(21));
			Assert.That(catalog.ActiveTheme?.DisplayName, Is.EqualTo("Live JSON Sandbox"));

			loaderVm.TestMalformedJsonError();
			Assert.That(loaderVm.HasParserError, Is.True);
			Assert.That(loaderVm.LoaderStatusText, Does.Contain("Line 3"));

			using var traceCapture = new WpfTraceCaptureService(catalog);
			var exporter = new DiagnosticReportExporterService(
				osService,
				catalog,
				new ResourceDictionaryAuditService(),
				new WindowDictionaryTrackerService(),
				traceCapture);

			var fullReport = exporter.BuildFullMarkdownReport();
			Assert.That(fullReport, Does.Contain("## 1. Host Windows OS Theme & DWM State"));
			Assert.That(fullReport, Does.Contain("Live JSON Sandbox"));
		}
	}
}