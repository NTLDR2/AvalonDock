using System;
using System.Globalization;
using System.IO;
using System.Text;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Compiles a complete GitHub-Flavored Markdown diagnostic snapshot combining OS theme state,
	/// 18-theme architecture &amp; constructor/apply benchmarks, active theme <c>Freezable</c> audit,
	/// multi-window dictionary sharing, and captured WPF <c>PresentationTraceSources</c> events.
	/// </summary>
	public sealed class DiagnosticReportExporterService
	{
		private readonly WindowsOsThemeService _osThemeService;
		private readonly ThemeCatalogService _themeCatalog;
		private readonly ResourceDictionaryAuditService _auditService;
		private readonly WindowDictionaryTrackerService _windowTracker;
		private readonly WpfTraceCaptureService _traceCapture;

		/// <summary>
		/// Initializes a new instance of the <see cref="DiagnosticReportExporterService"/> class.
		/// </summary>
		/// <param name="osThemeService">Windows OS theme service.</param>
		/// <param name="themeCatalog">18-theme catalog service.</param>
		/// <param name="auditService">ResourceDictionary &amp; Freezable audit service.</param>
		/// <param name="windowTracker">Multi-window dictionary sharing service.</param>
		/// <param name="traceCapture">WPF trace capture service.</param>
		public DiagnosticReportExporterService(
			WindowsOsThemeService osThemeService,
			ThemeCatalogService themeCatalog,
			ResourceDictionaryAuditService auditService,
			WindowDictionaryTrackerService windowTracker,
			WpfTraceCaptureService traceCapture)
		{
			_osThemeService = osThemeService ?? throw new ArgumentNullException(nameof(osThemeService));
			_themeCatalog = themeCatalog ?? throw new ArgumentNullException(nameof(themeCatalog));
			_auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
			_windowTracker = windowTracker ?? throw new ArgumentNullException(nameof(windowTracker));
			_traceCapture = traceCapture ?? throw new ArgumentNullException(nameof(traceCapture));
		}

		/// <summary>
		/// Builds the complete Markdown diagnostic report in memory.
		/// </summary>
		/// <returns>Complete Markdown document string.</returns>
		public string BuildFullMarkdownReport()
		{
			_traceCapture.FlushPendingNow();
			var os = _osThemeService.CurrentSnapshot;
			var active = _themeCatalog.ActiveTheme;

			var sb = new StringBuilder();
			sb.AppendLine("# ThemeSuperTestApp — Full AvalonDock v5 Theme Diagnostic Report");
			sb.AppendLine();
			sb.AppendFormat(CultureInfo.InvariantCulture, "- **Generated At:** `{0:yyyy-MM-dd HH:mm:ss zzz}`", DateTimeOffset.Now);
			sb.AppendLine();
			sb.AppendLine("- **Target Runtime:** `.NET Framework 4.8 (net48)`");
			sb.AppendLine();

			// 1. OS Theme Snapshot
			sb.AppendLine("## 1. Host Windows OS Theme & DWM State");
			sb.AppendLine();
			sb.AppendLine($"- **OS Version:** `{os.OsVersionCaption}`");
			sb.AppendLine($"- **Apps Mode (`AppsUseLightTheme`):** `{os.AppsThemeModeText}`");
			sb.AppendLine($"- **System Mode (`SystemUsesLightTheme`):** `{os.SystemThemeModeText}`");
			sb.AppendLine($"- **High Contrast:** `{os.HighContrastDisplayText}`");
			sb.AppendLine($"- **OS Accent Color:** `{os.OsAccentHex}` (Title-Bar Accent Prevalence: `{os.IsTitleBarAccentColorEnabled}`)");
			sb.AppendLine($"- **DWM Immersive Dark Mode Supported:** `{os.SupportsDwmImmersiveDarkMode}`");
			sb.AppendLine();

			// 2. Active Theme Metadata
			sb.AppendLine("## 2. Active AvalonDock Theme Summary");
			sb.AppendLine();
			if (active != null)
			{
				sb.AppendLine($"- **Theme:** `{active.DisplayName}` (`{active.ThemeType.FullName}`)");
				sb.AppendLine($"- **Base Class:** `{active.BaseClassBadgeText}`");
				sb.AppendLine($"- **Inheritance Chain:** `{active.InheritanceChain}`");
				sb.AppendLine($"- **Assembly / CLR Namespace:** `{active.AssemblyName}` / `{active.ClrNamespace}`");
				sb.AppendLine($"- **`[XmlnsDefinition]` Status:** `{active.XmlnsStatusText}`");
				sb.AppendLine($"- **`GetResourceUri()`:** `{active.ResourceUriText}`");
			}
			else
			{
				sb.AppendLine("_No active theme selected._");
			}

			sb.AppendLine();

			// 3. 18-Theme Architecture & Timing Table
			sb.AppendLine(_themeCatalog.BuildMarkdownBenchmarkReport().TrimEnd());
			sb.AppendLine();

			// 4. Active Theme ResourceDictionary & Unfrozen Freezable Audit
			if (active != null)
			{
				var audit = _auditService.AuditDescriptor(active, _themeCatalog.AttachedDockingManager);
				sb.AppendLine(_auditService.BuildMarkdownAuditSummary(audit).TrimEnd());
				sb.AppendLine();
			}

			// 5. Multi-Window Sharing Snapshot
			var sharing = _windowTracker.CaptureReport(
				active?.DisplayName ?? "(None)",
				active?.CachedInstance,
				_themeCatalog.AttachedDockingManager);
			sb.AppendLine(_windowTracker.BuildMarkdownReport(sharing).TrimEnd());
			sb.AppendLine();

			// 6. Captured WPF Binding / Resource Errors
			sb.AppendLine("## Captured WPF PresentationTraceSources Events");
			sb.AppendLine();
			if (_traceCapture.Events.Count == 0)
			{
				sb.AppendLine("_0 XAML binding or resource warnings/errors recorded._");
			}
			else
			{
				sb.AppendLine($"Total Recorded Events: `{_traceCapture.Events.Count}`");
				sb.AppendLine();
				sb.AppendLine("| # | Time | Source | Severity | ID | Active Theme | Message |");
				sb.AppendLine("| ---: | :--- | :--- | :--- | ---: | :--- | :--- |");
				foreach (var ev in _traceCapture.Events)
				{
					var cleanMsg = ev.Message.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
					sb.AppendFormat(
						CultureInfo.InvariantCulture,
						"| {0} | `{1}` | `{2}` | `{3}` | {4} | {5} | {6} |",
						ev.SequenceNumber,
						ev.TimestampText,
						ev.Category,
						ev.Severity,
						ev.EventId,
						ev.ThemeName,
						cleanMsg);
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// Writes <see cref="BuildFullMarkdownReport"/> to <paramref name="filePath"/> encoded as UTF-8 without BOM.
		/// </summary>
		/// <param name="filePath">Destination file path.</param>
		public void ExportToFile(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(filePath));

			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			var content = BuildFullMarkdownReport().Replace("\r\n", "\n");
			File.WriteAllText(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
	}
}