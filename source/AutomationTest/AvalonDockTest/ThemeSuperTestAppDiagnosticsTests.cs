namespace AvalonDockTest
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Markup;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;

	using NUnit.Framework;

	using AvalonDock;
	using AvalonDock.Controls;
	using AvalonDock.Layout;
	using AvalonDock.Themes;
	using AvalonDock.Themes.Type1Legacy;
	using AvalonDock.Themes.Type2Hybrid;
	using AvalonDock.Themes.Type3New;

	using ThemeSuperTestApp.Models;
	using ThemeSuperTestApp.Services;
	using ThemeSuperTestApp.ViewModels.Tools;

	/// <summary>
	/// STA verification tests for <c>ThemeSuperTestApp</c>'s non-invasive theme diagnostic engines:
	/// lazy 18-theme catalog reflection metadata, <c>[XmlnsDefinition]</c> auditing, <c>ResourceDictionary</c>
	/// and <c>Freezable.IsFrozen</c> auditing, multi-window dictionary sharing tracking, live WPF
	/// <c>PresentationTraceSources</c> capture, runtime <c>VsJsonThemeParser</c> sandbox, and Windows OS snapshot capture.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ThemeSuperTestAppDiagnosticsTests
	{
		[Test]
		public void Catalog_ContainsAll18BuiltInThemes_LazilyWithoutEagerInstantiation()
		{
			var catalog = new ThemeCatalogService();

			Assert.That(catalog.Themes, Has.Count.EqualTo(18));
			Assert.That(catalog.Themes.All(t => !t.IsInstantiated), Is.True, "All 18 built-in themes must remain lazy at catalog construction.");
			Assert.That(catalog.Themes.All(t => t.CachedInstance == null), Is.True);
		}

		[Test]
		public void Catalog_ClassifiesBaseClassesNamespacesAndXmlnsMappingsAccurately()
		{
			var catalog = new ThemeCatalogService();

			Assert.That(typeof(Theme).Namespace, Is.EqualTo("AvalonDock.Themes"));
			Assert.That(typeof(DictionaryTheme).Namespace, Is.EqualTo("AvalonDock.Themes"));

			var uriThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.UriTheme).ToList();
			var dictThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.DictionaryTheme).ToList();
			var vsThemes = catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.VsTheme).ToList();

			Assert.That(uriThemes, Has.Count.EqualTo(6));
			Assert.That(uriThemes.All(t => t.ClrNamespace == "AvalonDock.Themes.Type1Legacy"), Is.True);
			Assert.That(uriThemes.Single(t => t.Id == "Generic").AssemblyName, Is.EqualTo("AvalonDock"));
			Assert.That(uriThemes.Where(t => t.Id != "Generic").All(t => t.AssemblyName == "AvalonDock.Themes.Type1Legacy"), Is.True);
			Assert.That(uriThemes.All(t => t.IsMappedInXmlnsDefinition), Is.True);

			Assert.That(dictThemes, Has.Count.EqualTo(3));
			Assert.That(dictThemes.All(t => t.ClrNamespace == "AvalonDock.Themes.Type2Hybrid"), Is.True);
			Assert.That(dictThemes.All(t => t.AssemblyName == "AvalonDock.Themes.Type2Hybrid"), Is.True);
			Assert.That(dictThemes.All(t => t.IsMappedInXmlnsDefinition), Is.True);

			Assert.That(vsThemes, Has.Count.EqualTo(9));
			Assert.That(vsThemes.All(t => t.ClrNamespace == "AvalonDock.Themes.Type3New"), Is.True);
			Assert.That(vsThemes.All(t => t.AssemblyName == "AvalonDock.Themes.Type3New"), Is.True);
			Assert.That(vsThemes.All(t => t.IsMappedInXmlnsDefinition), Is.True, "All 18 built-in themes must map to https://github.com/NTLDR2/AvalonDock via [XmlnsDefinition].");
		}

		[Test]
		public void Catalog_BenchmarkAllColdConstructors_PopulatesTelemetryAndMarkdownTable()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();

			catalog.BenchmarkAllColdConstructors();

			Assert.That(catalog.Themes.All(t => t.LastColdConstructorMs.HasValue && t.LastColdConstructorMs.Value >= 0.0), Is.True);
			Assert.That(
				catalog.Themes.Where(t => t.BaseClassKind == ThemeBaseClassKind.DictionaryTheme).All(t => t.MergedDictionaryCount == 1),
				Is.True,
				"Type2Hybrid DictionaryThemes must have MergedDictionaryCount == 1 after removing duplicate OverlayButtons.xaml.");
			var markdown = catalog.BuildMarkdownBenchmarkReport();
			Assert.That(markdown, Does.Contain("VS 2026 Dark"));
			Assert.That(markdown, Does.Contain("Expression Dark"));
			Assert.That(markdown, Does.Not.Contain("**MISSING**"));
		}

		[Test]
		public void VsThemeAssembly_XmlnsDefinition_ResolvesElementsDefaultPresentationAndMarkupExtensionInXaml()
		{
			_ = new Border();
			_ = typeof(DockingManager).Assembly;
			_ = typeof(ExpressionDarkTheme).Assembly;
			_ = typeof(Vs2013DarkTheme).Assembly;
			_ = typeof(VS2026DarkTheme).Assembly;

			var assembliesToVerify = new[]
			{
				typeof(DockingManager).Assembly,
				typeof(ExpressionDarkTheme).Assembly,
				typeof(Vs2013DarkTheme).Assembly,
				typeof(VS2026DarkTheme).Assembly,
			};
			foreach (var asm in assembliesToVerify)
			{
				var legacyAttrs = Attribute.GetCustomAttributes(asm, typeof(XmlnsDefinitionAttribute))
					.OfType<XmlnsDefinitionAttribute>()
					.Where(a => string.Equals(a.XmlNamespace, "https://github.com/Dirkster99/AvalonDock", StringComparison.Ordinal))
					.ToList();
				Assert.That(
					legacyAttrs,
					Is.Empty,
					$"Assembly '{asm.GetName().Name}' must not retain legacy XmlnsDefinition mappings for https://github.com/Dirkster99/AvalonDock.");
			}

			Assert.Throws<XamlParseException>(() =>
				XamlReader.Parse(
					"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
					"xmlns:avalonDock=\"https://github.com/Dirkster99/AvalonDock\" />"));

			var prefixedType1Core = (DockingManager)XamlReader.Parse(
				"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\">" +
				"<avalonDock:DockingManager.Theme><avalonDock:GenericTheme /></avalonDock:DockingManager.Theme>" +
				"</avalonDock:DockingManager>");
			Assert.That(prefixedType1Core.Theme, Is.TypeOf<GenericTheme>());

			var prefixedType1Pkg = (DockingManager)XamlReader.Parse(
				"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\">" +
				"<avalonDock:DockingManager.Theme><avalonDock:ExpressionDarkTheme /></avalonDock:DockingManager.Theme>" +
				"</avalonDock:DockingManager>");
			Assert.That(prefixedType1Pkg.Theme, Is.TypeOf<ExpressionDarkTheme>());

			var prefixedType2 = (DockingManager)XamlReader.Parse(
				"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\">" +
				"<avalonDock:DockingManager.Theme><avalonDock:Vs2013DarkTheme /></avalonDock:DockingManager.Theme>" +
				"</avalonDock:DockingManager>");
			Assert.That(prefixedType2.Theme, Is.TypeOf<Vs2013DarkTheme>());

			var prefixed2026 = (DockingManager)XamlReader.Parse(
				"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\">" +
				"<avalonDock:DockingManager.Theme><avalonDock:VS2026DarkTheme /></avalonDock:DockingManager.Theme>" +
				"</avalonDock:DockingManager>");
			Assert.That(prefixed2026.Theme, Is.TypeOf<VS2026DarkTheme>());

			var prefixed2015 = (DockingManager)XamlReader.Parse(
				"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\">" +
				"<avalonDock:DockingManager.Theme><avalonDock:VS2015LightTheme /></avalonDock:DockingManager.Theme>" +
				"</avalonDock:DockingManager>");
			Assert.That(prefixed2015.Theme, Is.TypeOf<VS2015LightTheme>());

			var unprefixedDefaultNs = (DockingManager)XamlReader.Parse(
				"<DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
				"<DockingManager.Theme><VS2022BlueTheme /></DockingManager.Theme>" +
				"</DockingManager>");
			Assert.That(unprefixedDefaultNs.Theme, Is.TypeOf<VS2022BlueTheme>());

			var tempVsThemePath = Path.Combine(
				Path.GetTempPath(),
				"AvalonDockTest_XmlnsVsTheme_" + Guid.NewGuid().ToString("N") + ".vstheme");

			try
			{
				const string vsThemeXml =
					"<Themes>" +
					"<Theme Name=\"CustomXmlnsTest\">" +
					"<Category Name=\"Environment\">" +
					"<Color Name=\"FileTabSelectedBorder\"><Background Type=\"CT_RAW\" Source=\"FF10B981\" /></Color>" +
					"</Category>" +
					"</Theme>" +
					"</Themes>";
				File.WriteAllText(tempVsThemePath, vsThemeXml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				var xamlSafePath = tempVsThemePath.Replace('\\', '/');

				var markupExtManager = (DockingManager)XamlReader.Parse(
					"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
					"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\" " +
					$"Theme=\"{{avalonDock:VsTheme Path={xamlSafePath}}}\" />");
				Assert.That(markupExtManager.Theme, Is.TypeOf<VsTheme>());
				var parsedVsTheme = (VsTheme)markupExtManager.Theme;
				Assert.That(
					parsedVsTheme.ThemeResourceDictionary[AvalonDock.Themes.Type3New.Themes.ResourceKeys.ControlAccentColorKey],
					Is.EqualTo(Color.FromRgb(0x10, 0xB9, 0x81)));

				var explicitClrNsManager = (DockingManager)XamlReader.Parse(
					"<avalonDock:DockingManager xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
					"xmlns:avalonDock=\"https://github.com/NTLDR2/AvalonDock\" " +
					"xmlns:vs=\"clr-namespace:AvalonDock.Themes.Type3New;assembly=AvalonDock.Themes.Type3New\" " +
					$"Theme=\"{{vs:VsTheme Path={xamlSafePath}}}\" />");
				Assert.That(explicitClrNsManager.Theme, Is.TypeOf<VsTheme>());
			}
			finally
			{
				if (File.Exists(tempVsThemePath))
					File.Delete(tempVsThemePath);
			}
		}

		[Test]
		public void ResourceAuditor_AuditsThemesWithoutPollutingLazyFlag_AndDetectsUnfrozenBrushesWithoutMutating()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var auditor = new ResourceDictionaryAuditService();

			var uriDesc = catalog.FindById("ExpressionDark");
			var vs13Desc = catalog.FindById("Vs2013Dark");
			var vsDesc = catalog.FindById("VS2022Dark");
			Assert.That(uriDesc, Is.Not.Null);
			Assert.That(vs13Desc, Is.Not.Null);
			Assert.That(vsDesc, Is.Not.Null);

			var uriReport = auditor.AuditDescriptor(uriDesc!);
			var vs13Report = auditor.AuditDescriptor(vs13Desc!);
			var vsReport = auditor.AuditDescriptor(vsDesc!);

			Assert.That(uriDesc!.IsInstantiated, Is.False, "Read-only inspection must not mark descriptor as instantiated.");
			Assert.That(vs13Desc!.IsInstantiated, Is.False, "Read-only inspection must not mark descriptor as instantiated.");
			Assert.That(vsDesc!.IsInstantiated, Is.False, "Read-only inspection must not mark descriptor as instantiated.");
			Assert.That(uriReport.Entries, Is.Not.Empty);
			Assert.That(vs13Report.FrozenFreezableCount, Is.GreaterThan(80));
			Assert.That(vsReport.FrozenFreezableCount, Is.GreaterThan(50));

			var diff = auditor.DiffThemes(vs13Report, vsReport);
			Assert.That(diff.SharedMatchingCount, Is.GreaterThan(80));

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
				var uriDesc = catalog.FindById("ExpressionDark")!;
				catalog.ApplyTheme(uriDesc);
				detachedWindow.UpdateThemeResources(oldTheme, uriDesc.CachedInstance);

				var uriSharing = tracker.CaptureReport(uriDesc.DisplayName, uriDesc.CachedInstance, manager, windows);
				Assert.That(uriSharing.TotalThemedHosts, Is.EqualTo(2));
				Assert.That(uriSharing.DistinctDictionaryInstances, Is.EqualTo(2));
				Assert.That(uriSharing.RedundantBamlParsesCount, Is.EqualTo(1));
				Assert.That(uriSharing.HasDuplicatedDictionaries, Is.True);
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
			catalog.ApplyTheme("ExpressionDark");

			using var traceCapture = new WpfTraceCaptureService(catalog);
			traceCapture.Clear();
			traceCapture.TriggerSyntheticBindingErrorForVerification();

			Assert.That(traceCapture.Events, Is.Not.Empty);
			var first = traceCapture.Events[0];
			Assert.That(first.Category, Is.EqualTo("DataBinding"));
			Assert.That(first.Severity, Is.EqualTo(TraceEventType.Error));
			Assert.That(first.EventId, Is.EqualTo(40));
			Assert.That(first.ThemeName, Is.EqualTo("Expression Dark"));
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
			Assert.That(catalog.Themes, Has.Count.EqualTo(19));
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

		[Test]
		public void All18BuiltInThemes_ApplyCleanlyWithZeroResourceTraceErrors_AndType1LegacyPackImagesDecode()
		{
			_ = new Border();
			var catalog = new ThemeCatalogService();
			var docPane = new LayoutDocumentPane(new LayoutDocument { Title = "Doc1", ContentId = "doc1", Content = new TextBlock { Text = "Body" } });
			var toolPane = new LayoutAnchorablePane(new LayoutAnchorable { Title = "Tool1", ContentId = "tool1", Content = new TextBlock { Text = "Tool" } });
			var rootPanel = new LayoutPanel();
			rootPanel.Children.Add(docPane);
			rootPanel.Children.Add(toolPane);
			var manager = new DockingManager
			{
				Width = 800,
				Height = 600,
				Layout = new LayoutRoot
				{
					RootPanel = rootPanel,
				},
			};

			catalog.Attach(manager);
			using var traceCapture = new WpfTraceCaptureService(catalog);
			traceCapture.Clear();

			manager.Measure(new Size(800, 600));
			manager.Arrange(new Rect(0, 0, 800, 600));
			manager.ApplyTemplate();

			foreach (var descriptor in catalog.Themes)
			{
				Assert.DoesNotThrow(
					() =>
					{
						catalog.ApplyTheme(descriptor, forceFreshInstance: true);
						manager.UpdateLayout();
					},
					$"Applying theme '{descriptor.DisplayName}' ({descriptor.Id}) threw an exception.");
			}

			var resourceErrors = traceCapture.Events
				.Where(e => e.Severity <= TraceEventType.Error)
				.ToList();
			Assert.That(
				resourceErrors,
				Is.Empty,
				() => "Expected 0 WPF trace errors across 18-theme sweep, but captured: " +
					string.Join(" | ", resourceErrors.Select(e => $"[{e.ThemeName}/{e.Category}#{e.EventId}] {e.Message}")));

			var samplePackImages = new[]
			{
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Images/DockAnchorableLeft.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Images/DockPaneLargeEmpty.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Aero/Images/PinDocMenu_Black.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/Images/PinMenu_Light.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Expression/Images/DockAnchorableTop.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Metro/Images/DockAnchorableRight.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/Metro/Images/PinClose_Black.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/VS2010/Images/PinDocMenu_Black.png",
				"pack://application:,,,/AvalonDock.Themes.Type1Legacy;component/Themes/VS2010/Images/DockPaneLargeEmpty.png",
			};

			foreach (var packUri in samplePackImages)
			{
				var bmp = new BitmapImage(new Uri(packUri, UriKind.Absolute));
				Assert.That(bmp.PixelWidth, Is.GreaterThan(0), $"Failed to decode PNG width for {packUri}.");
				Assert.That(bmp.PixelHeight, Is.GreaterThan(0), $"Failed to decode PNG height for {packUri}.");
			}
		}
	}
}