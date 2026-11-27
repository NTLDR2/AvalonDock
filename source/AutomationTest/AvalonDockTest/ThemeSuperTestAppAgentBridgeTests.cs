using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using AvalonDock;
using AvalonDock.Layout;
using NUnit.Framework;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace AvalonDockTest
{
	/// <summary>
	/// Isolated temp-directory STA verification tests for <c>ThemeSuperTestApp</c>'s Antigravity multimodal bridge:
	/// dual-mode WPF + composite PNG capture, rolling archive retention, <c>heartbeat.xml</c> lifecycle states,
	/// deterministic XML <c>inbox/</c> -&gt; <c>outbox/</c> command processing, and runtime inline JSON error reporting.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ThemeSuperTestAppAgentBridgeTests
	{
		private static readonly byte[] PngMagicBytes = { 0x89, 0x50, 0x4E, 0x47 };
		private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

		private string _tempDir;

		[SetUp]
		public void SetUp()
		{
			_ = new Border();
			_tempDir = Path.Combine(Path.GetTempPath(), "AvalonDockTest_AgentBridge_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_tempDir);
		}

		[TearDown]
		public void TearDown()
		{
			DeleteDirectorySafe(_tempDir);
		}

		[Test]
		public void ScreenshotService_CapturesValidWpfPngAndCompositePng_AndEnforcesRollingRetentionCap()
		{
			var paths = new AgentBridgePaths(_tempDir, maxRetainedCaptureSets: 3);
			var catalog = new ThemeCatalogService();
			using var osService = new WindowsOsThemeService();
			var auditService = new ResourceDictionaryAuditService();
			var windowTracker = new WindowDictionaryTrackerService();
			using var traceCapture = new WpfTraceCaptureService(catalog);
			var exporter = new DiagnosticReportExporterService(osService, catalog, auditService, windowTracker, traceCapture);
			var screenshotService = new WindowScreenshotService(paths, exporter);

			var manager = CreateArrangedDockingManager(480.0, 300.0);
			catalog.Attach(manager);
			Assert.That(catalog.ApplyTheme("VS2022Dark"), Is.True);
			ArrangeDockingManager(manager, 480.0, 300.0);

			var bundles = new List<ScreenshotCaptureBundle>();
			for (var i = 1; i <= 5; i++)
			{
				if (i > 1)
				{
					Thread.Sleep(5);
				}

				var bundle = screenshotService.CaptureSnapshotBundle(
					manager,
					$"Snap{i:D2}",
					$"Operator note #{i}: verify VS2022Dark tab contrast",
					includeDwmCapture: true,
					updateLatestFolder: true);
				bundles.Add(bundle);
			}

			Assert.That(File.Exists(paths.LatestMainWpfPngPath), Is.True);
			Assert.That(File.Exists(paths.LatestCompositePngPath), Is.True);
			Assert.That(File.Exists(paths.LatestContextMarkdownPath), Is.True);
			AssertHasPngMagicBytes(paths.LatestMainWpfPngPath);
			AssertHasPngMagicBytes(paths.LatestCompositePngPath);

			var latestContextMd = File.ReadAllText(paths.LatestContextMarkdownPath, Encoding.UTF8);
			Assert.That(latestContextMd, Does.Contain("Operator note #5: verify VS2022Dark tab contrast"));
			Assert.That(latestContextMd, Does.Contain("VS 2022 Dark"));

			var archivedFiles = Directory.GetFiles(paths.CapturesDirectory);
			var retainedBundles = bundles
				.Where(b => archivedFiles.Any(f => Path.GetFileName(f).StartsWith(b.CaptureId, StringComparison.OrdinalIgnoreCase)))
				.ToList();

			Assert.That(retainedBundles, Has.Count.LessThanOrEqualTo(3));
			Assert.That(retainedBundles, Has.Count.EqualTo(3));
			Assert.That(File.Exists(bundles[0].CompositePngPath), Is.False, "Oldest capture set #1 should be pruned.");
			Assert.That(File.Exists(bundles[1].CompositePngPath), Is.False, "Second oldest capture set #2 should be pruned.");
			Assert.That(File.Exists(bundles[2].CompositePngPath), Is.True);
			Assert.That(File.Exists(bundles[3].CompositePngPath), Is.True);
			Assert.That(File.Exists(bundles[4].CompositePngPath), Is.True);

			var cleanedFiles = screenshotService.CleanAllArchivedCaptures();
			Assert.That(cleanedFiles, Is.GreaterThan(0));
			Assert.That(Directory.GetFiles(paths.CapturesDirectory), Is.Empty);
			Assert.That(File.Exists(paths.LatestMainWpfPngPath), Is.True, "Cleaning archived captures must leave latest/* intact.");
			Assert.That(File.Exists(paths.LatestCompositePngPath), Is.True, "Cleaning archived captures must leave latest/* intact.");
			Assert.That(File.Exists(paths.LatestContextMarkdownPath), Is.True, "Cleaning archived captures must leave latest/* intact.");
		}

		[Test]
		public async Task AgentBridgeService_WritesHeartbeat_AndProcessesPingAndApplyThemeXmlCommands()
		{
			var paths = new AgentBridgePaths(_tempDir, maxRetainedCaptureSets: 5);
			var catalog = new ThemeCatalogService();
			using var osService = new WindowsOsThemeService();
			var auditService = new ResourceDictionaryAuditService();
			var windowTracker = new WindowDictionaryTrackerService();
			using var traceCapture = new WpfTraceCaptureService(catalog);
			var exporter = new DiagnosticReportExporterService(osService, catalog, auditService, windowTracker, traceCapture);
			var screenshotService = new WindowScreenshotService(paths, exporter);
			using var bridge = new AgentBridgeService(
				paths,
				catalog,
				osService,
				auditService,
				windowTracker,
				traceCapture,
				screenshotService,
				exporter);

			var manager = CreateArrangedDockingManager(480.0, 300.0);
			catalog.Attach(manager);
			Assert.That(catalog.ApplyTheme("ArcDark"), Is.True);
			ArrangeDockingManager(manager, 480.0, 300.0);
			bridge.AttachVisualRoot(manager);

			bridge.Start();

			Assert.That(File.Exists(paths.HeartbeatFilePath), Is.True);
			var initialHeartbeat = XDocument.Load(paths.HeartbeatFilePath);
			Assert.That(initialHeartbeat.Root, Is.Not.Null);
			Assert.That(initialHeartbeat.Root.Name.LocalName, Is.EqualTo("AgentBridgeHeartbeat"));
			Assert.That((string)initialHeartbeat.Root.Attribute("State"), Is.EqualTo("Listening"));
			Assert.That((string)initialHeartbeat.Root.Attribute("ActiveThemeId"), Is.EqualTo("ArcDark"));

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "01-test-ping.xml"),
				"<AgentCommand Id=\"test-ping\" Action=\"ping\" />",
				Utf8NoBom);

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "02-test-theme.xml"),
				"<AgentCommand Id=\"test-theme\" Action=\"apply-theme\"><Param Name=\"ThemeId\" Value=\"VS2026Dark\" /><Param Name=\"Capture\" Value=\"true\" /></AgentCommand>",
				Utf8NoBom);

			var processedCount = await bridge.ProcessPendingInboxAsync();
			Assert.That(processedCount, Is.EqualTo(2));
			Assert.That(Directory.GetFiles(paths.InboxDirectory, "*.xml"), Is.Empty);

			var pingResponsePath = Path.Combine(paths.OutboxDirectory, "test-ping-response.xml");
			var themeResponsePath = Path.Combine(paths.OutboxDirectory, "test-theme-response.xml");
			Assert.That(File.Exists(pingResponsePath), Is.True);
			Assert.That(File.Exists(themeResponsePath), Is.True);

			var pingResponse = XDocument.Load(pingResponsePath);
			Assert.That(pingResponse.Root, Is.Not.Null);
			Assert.That(pingResponse.Root.Name.LocalName, Is.EqualTo("AgentResponse"));
			Assert.That((string)pingResponse.Root.Attribute("Id"), Is.EqualTo("test-ping"));
			Assert.That((string)pingResponse.Root.Attribute("Status"), Is.EqualTo("Ok"));

			var themeResponse = XDocument.Load(themeResponsePath);
			Assert.That(themeResponse.Root, Is.Not.Null);
			Assert.That((string)themeResponse.Root.Attribute("Id"), Is.EqualTo("test-theme"));
			Assert.That((string)themeResponse.Root.Attribute("Status"), Is.EqualTo("Ok"));

			var activeThemeEl = themeResponse.Root.Element("ActiveTheme");
			Assert.That(activeThemeEl, Is.Not.Null);
			Assert.That((string)activeThemeEl.Attribute("Id"), Is.EqualTo("VS2026Dark"));
			Assert.That((string)activeThemeEl.Attribute("DisplayName"), Is.EqualTo("VS 2026 Dark"));

			var artifactsEl = themeResponse.Root.Element("Artifacts");
			Assert.That(artifactsEl, Is.Not.Null);
			var latestCompositePath = (string)artifactsEl.Attribute("LatestCompositePng");
			Assert.That(latestCompositePath, Is.Not.Null.And.Not.Empty);
			Assert.That(File.Exists(latestCompositePath), Is.True);
			AssertHasPngMagicBytes(latestCompositePath);

			Assert.That(bridge.ProcessedCommandCount, Is.EqualTo(2));
			Assert.That(bridge.LogEntries, Has.Count.EqualTo(2));
			Assert.That(bridge.LogEntries[0].RequestId, Is.EqualTo("test-theme"));
			Assert.That(bridge.LogEntries[1].RequestId, Is.EqualTo("test-ping"));

			bridge.IsListening = false;
			var pausedHeartbeat = XDocument.Load(paths.HeartbeatFilePath);
			Assert.That(pausedHeartbeat.Root, Is.Not.Null);
			Assert.That((string)pausedHeartbeat.Root.Attribute("State"), Is.EqualTo("Paused"));

			bridge.Dispose();
			var stoppedHeartbeat = XDocument.Load(paths.HeartbeatFilePath);
			Assert.That(stoppedHeartbeat.Root, Is.Not.Null);
			Assert.That((string)stoppedHeartbeat.Root.Attribute("State"), Is.EqualTo("Stopped"));
		}

		[Test]
		public async Task AgentBridgeService_LoadInlineJson_ReportsOkOnValidJson_AndErrorOnMalformedJson()
		{
			var paths = new AgentBridgePaths(_tempDir, maxRetainedCaptureSets: 5);
			var catalog = new ThemeCatalogService();
			using var osService = new WindowsOsThemeService();
			var auditService = new ResourceDictionaryAuditService();
			var windowTracker = new WindowDictionaryTrackerService();
			using var traceCapture = new WpfTraceCaptureService(catalog);
			var exporter = new DiagnosticReportExporterService(osService, catalog, auditService, windowTracker, traceCapture);
			var screenshotService = new WindowScreenshotService(paths, exporter);
			using var bridge = new AgentBridgeService(
				paths,
				catalog,
				osService,
				auditService,
				windowTracker,
				traceCapture,
				screenshotService,
				exporter);

			var manager = CreateArrangedDockingManager(480.0, 300.0);
			catalog.Attach(manager);
			bridge.AttachVisualRoot(manager);
			bridge.Start();

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "01-json-valid.xml"),
				"<AgentCommand Id=\"json-valid\" Action=\"load-inline-json\"><Param Name=\"Capture\" Value=\"false\" /><Payload>[ { \"Name\": \"EnvironmentBackground\", \"Background\": \"FF112233\" } ]</Payload></AgentCommand>",
				Utf8NoBom);

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "02-json-malformed.xml"),
				"<AgentCommand Id=\"json-malformed\" Action=\"load-inline-json\"><Param Name=\"Capture\" Value=\"false\" /><Payload>[ { \"Name\": \"Broken\", \"Background\": 123 </Payload></AgentCommand>",
				Utf8NoBom);

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "03-bad-verb.xml"),
				"<AgentCommand Id=\"bad-verb\" Action=\"run-shell\" />",
				Utf8NoBom);

			var processedCount = await bridge.ProcessPendingInboxAsync();
			Assert.That(processedCount, Is.EqualTo(3));
			Assert.That(bridge.ProcessedCommandCount, Is.EqualTo(3));
			Assert.That(Directory.GetFiles(paths.InboxDirectory, "*.xml"), Is.Empty);

			var validResponse = XDocument.Load(Path.Combine(paths.OutboxDirectory, "json-valid-response.xml"));
			Assert.That(validResponse.Root, Is.Not.Null);
			Assert.That((string)validResponse.Root.Attribute("Status"), Is.EqualTo("Ok"));
			var activeThemeEl = validResponse.Root.Element("ActiveTheme");
			Assert.That(activeThemeEl, Is.Not.Null);
			Assert.That((string)activeThemeEl.Attribute("Id"), Is.EqualTo("Runtime.AgentInlineJson"));

			var malformedResponse = XDocument.Load(Path.Combine(paths.OutboxDirectory, "json-malformed-response.xml"));
			Assert.That(malformedResponse.Root, Is.Not.Null);
			Assert.That((string)malformedResponse.Root.Attribute("Status"), Is.EqualTo("Error"));
			var malformedMessage = (string)malformedResponse.Root.Element("Message");
			Assert.That(malformedMessage, Does.Contain("Line"));
			Assert.That(malformedMessage, Does.Contain("Column"));

			var badVerbResponse = XDocument.Load(Path.Combine(paths.OutboxDirectory, "bad-verb-response.xml"));
			Assert.That(badVerbResponse.Root, Is.Not.Null);
			Assert.That((string)badVerbResponse.Root.Attribute("Status"), Is.EqualTo("Error"));
			var badVerbMessage = (string)badVerbResponse.Root.Element("Message");
			Assert.That(badVerbMessage, Does.Contain("Unsupported AgentBridge command Action"));
		}

		[Test]
		public async Task AgentBridgeService_ManualF12Snapshot_ExportReport_AndMalformedXml_SucceedWithoutCrashing()
		{
			var paths = new AgentBridgePaths(_tempDir, maxRetainedCaptureSets: 5);
			var catalog = new ThemeCatalogService();
			using var osService = new WindowsOsThemeService();
			var auditService = new ResourceDictionaryAuditService();
			var windowTracker = new WindowDictionaryTrackerService();
			using var traceCapture = new WpfTraceCaptureService(catalog);
			var exporter = new DiagnosticReportExporterService(osService, catalog, auditService, windowTracker, traceCapture);
			var screenshotService = new WindowScreenshotService(paths, exporter);
			using var bridge = new AgentBridgeService(
				paths,
				catalog,
				osService,
				auditService,
				windowTracker,
				traceCapture,
				screenshotService,
				exporter);

			var manager = CreateArrangedDockingManager(480.0, 300.0);
			catalog.Attach(manager);
			Assert.That(catalog.ApplyTheme("VS2022Dark"), Is.True);
			ArrangeDockingManager(manager, 480.0, 300.0);
			bridge.AttachVisualRoot(manager);
			bridge.Start();

			var manualBundle = await bridge.CaptureManualSnapshotAsync("F12 manual operator inspection note");
			Assert.That(manualBundle, Is.Not.Null);
			Assert.That(File.Exists(manualBundle.LatestCompositePngPath), Is.True);
			Assert.That(File.ReadAllText(manualBundle.LatestContextMarkdownPath, Encoding.UTF8), Does.Contain("F12 manual operator inspection note"));
			Assert.That(bridge.LogEntries, Has.Count.EqualTo(1));
			Assert.That(bridge.LogEntries[0].Direction, Is.EqualTo("User F12 -> Agent"));

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "01-broken-xml.xml"),
				"<AgentCommand Id=\"broken\" unclosed",
				Utf8NoBom);

			File.WriteAllText(
				Path.Combine(paths.InboxDirectory, "02-export-report.xml"),
				"<AgentCommand Id=\"test-report\" Action=\"export-full-report\" />",
				Utf8NoBom);

			var processed = await bridge.ProcessPendingInboxAsync();
			Assert.That(processed, Is.EqualTo(2));

			var brokenXmlResponse = XDocument.Load(Path.Combine(paths.OutboxDirectory, "01-broken-xml-response.xml"));
			Assert.That(brokenXmlResponse.Root, Is.Not.Null);
			Assert.That((string)brokenXmlResponse.Root.Attribute("Status"), Is.EqualTo("Error"));
			Assert.That((string)brokenXmlResponse.Root.Attribute("Action"), Is.EqualTo("parse-xml"));

			var reportResponse = XDocument.Load(Path.Combine(paths.OutboxDirectory, "test-report-response.xml"));
			Assert.That(reportResponse.Root, Is.Not.Null);
			Assert.That((string)reportResponse.Root.Attribute("Status"), Is.EqualTo("Ok"));
			var reportPath = (string)reportResponse.Root.Element("Artifacts")?.Attribute("ReportPath");
			Assert.That(reportPath, Is.Not.Null.And.Not.Empty);
			Assert.That(File.Exists(reportPath), Is.True);
		}

		private static DockingManager CreateArrangedDockingManager(double width, double height)
		{
			var documentPane = new LayoutDocumentPane();
			documentPane.Children.Add(new LayoutDocument
			{
				Title = "BridgeSpecimen.cs",
				ContentId = "doc-bridge-specimen",
				Content = new TextBlock { Text = "AgentBridge Visual Specimen" },
			});

			var anchorablePane = new LayoutAnchorablePane();
			anchorablePane.Children.Add(new LayoutAnchorable
			{
				Title = "Bridge Inspector",
				ContentId = "tool-bridge-inspector",
				Content = new Border { Background = Brushes.SlateGray },
			});

			var rootPanel = new LayoutPanel { Orientation = Orientation.Horizontal };
			rootPanel.Children.Add(documentPane);
			rootPanel.Children.Add(anchorablePane);

			var manager = new DockingManager
			{
				Width = width,
				Height = height,
				Layout = new LayoutRoot { RootPanel = rootPanel },
			};

			ArrangeDockingManager(manager, width, height);
			return manager;
		}

		private static void ArrangeDockingManager(DockingManager manager, double width, double height)
		{
			var size = new Size(width, height);
			manager.Measure(size);
			manager.Arrange(new Rect(size));
			manager.UpdateLayout();
		}

		private static void AssertHasPngMagicBytes(string pngFilePath)
		{
			var bytes = File.ReadAllBytes(pngFilePath);
			Assert.That(bytes, Has.Length.GreaterThanOrEqualTo(8));
			Assert.That(bytes.Take(4).ToArray(), Is.EqualTo(PngMagicBytes));
		}

		private static void DeleteDirectorySafe(string directoryPath)
		{
			if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
			{
				return;
			}

			for (var attempt = 0; attempt < 6; attempt++)
			{
				try
				{
					Directory.Delete(directoryPath, recursive: true);
					return;
				}
				catch (IOException) when (attempt < 5)
				{
					Thread.Sleep(25);
				}
				catch (UnauthorizedAccessException) when (attempt < 5)
				{
					Thread.Sleep(25);
				}
			}
		}
	}
}