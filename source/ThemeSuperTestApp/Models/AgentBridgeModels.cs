using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.Models
{
	/// <summary>
	/// Parsed incoming XML command dropped by Antigravity into <c>AgentBridge/inbox/*.xml</c>.
	/// </summary>
	public sealed class AgentBridgeCommand
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgeCommand"/> class.
		/// </summary>
		/// <param name="requestId">Unique request correlation ID.</param>
		/// <param name="action">Allow-listed command verb.</param>
		/// <param name="parameters">Case-insensitive parameter dictionary.</param>
		/// <param name="payload">Optional multiline payload text.</param>
		/// <param name="sourceFilePath">Original XML file path in <c>inbox/</c>.</param>
		public AgentBridgeCommand(
			string requestId,
			string action,
			IReadOnlyDictionary<string, string> parameters,
			string? payload,
			string sourceFilePath)
		{
			RequestId = string.IsNullOrWhiteSpace(requestId) ? "req-unnamed" : requestId;
			Action = action ?? string.Empty;
			Parameters = parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			Payload = payload;
			SourceFilePath = sourceFilePath ?? string.Empty;
		}

		/// <summary>Gets the correlation request ID.</summary>
		public string RequestId { get; }

		/// <summary>Gets the requested command verb.</summary>
		public string Action { get; }

		/// <summary>Gets the case-insensitive command parameters.</summary>
		public IReadOnlyDictionary<string, string> Parameters { get; }

		/// <summary>Gets the optional multiline payload body.</summary>
		public string? Payload { get; }

		/// <summary>Gets the source inbox file path.</summary>
		public string SourceFilePath { get; }

		/// <summary>
		/// Parses an <see cref="AgentBridgeCommand"/> from <paramref name="xmlContent"/> using inbox <see cref="XDocument"/>.
		/// </summary>
		/// <param name="xmlContent">Raw XML text.</param>
		/// <param name="sourceFilePath">Path of the inbox file.</param>
		/// <returns>Parsed command instance.</returns>
		public static AgentBridgeCommand ParseXml(string xmlContent, string sourceFilePath)
		{
			if (string.IsNullOrWhiteSpace(xmlContent))
				throw new FormatException("Command XML file is empty.");

			var fallbackId = string.IsNullOrWhiteSpace(sourceFilePath)
				? Guid.NewGuid().ToString("N")
				: Path.GetFileNameWithoutExtension(sourceFilePath);

			var doc = XDocument.Parse(xmlContent, LoadOptions.None);
			var root = doc.Root ?? throw new FormatException("Command XML has no root element.");

			var id = ReadAttributeIgnoreCase(root, "Id") ?? fallbackId;
			var action = ReadAttributeIgnoreCase(root, "Action") ?? string.Empty;

			var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var paramEl in root.Elements().Where(e => string.Equals(e.Name.LocalName, "Param", StringComparison.OrdinalIgnoreCase)))
			{
				var key = ReadAttributeIgnoreCase(paramEl, "Name");
				var val = ReadAttributeIgnoreCase(paramEl, "Value") ?? paramEl.Value;
				if (!string.IsNullOrWhiteSpace(key))
					parameters[key!] = val ?? string.Empty;
			}

			var payloadEl = root.Elements().FirstOrDefault(e => string.Equals(e.Name.LocalName, "Payload", StringComparison.OrdinalIgnoreCase));
			var payload = payloadEl?.Value;

			return new AgentBridgeCommand(id, action.Trim(), parameters, payload, sourceFilePath);
		}

		/// <summary>Gets a string parameter or <paramref name="defaultValue"/> when absent.</summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="defaultValue">Fallback value.</param>
		/// <returns>Resolved string value.</returns>
		public string? GetString(string name, string? defaultValue = null)
		{
			return Parameters.TryGetValue(name, out var val) ? val : defaultValue;
		}

		/// <summary>Gets a boolean parameter or <paramref name="defaultValue"/> when absent or invalid.</summary>
		/// <param name="name">Parameter name.</param>
		/// <param name="defaultValue">Fallback value.</param>
		/// <returns>Resolved boolean value.</returns>
		public bool GetBool(string name, bool defaultValue)
		{
			if (!Parameters.TryGetValue(name, out var raw) || string.IsNullOrWhiteSpace(raw))
				return defaultValue;

			if (bool.TryParse(raw.Trim(), out var parsed))
				return parsed;
			if (raw.Trim() == "1")
				return true;
			if (raw.Trim() == "0")
				return false;

			return defaultValue;
		}

		private static string? ReadAttributeIgnoreCase(XElement element, string name)
		{
			return element.Attributes()
				.FirstOrDefault(a => string.Equals(a.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
				?.Value;
		}
	}

	/// <summary>
	/// Single row in Tool 6's live command &amp; snapshot activity log.
	/// </summary>
	public sealed class AgentBridgeLogEntry
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgeLogEntry"/> class.
		/// </summary>
		/// <param name="timestamp">Event timestamp.</param>
		/// <param name="direction">Traffic direction (<c>Agent -&gt; App</c> or <c>User F12 -&gt; Agent</c>).</param>
		/// <param name="requestId">Request or capture identifier.</param>
		/// <param name="actionSummary">Concise action and parameter summary.</param>
		/// <param name="status">Execution outcome (<c>Ok</c> or <c>Error</c>).</param>
		/// <param name="durationMs">Elapsed execution time in milliseconds.</param>
		/// <param name="primaryArtifactPath">Primary generated screenshot or report path.</param>
		/// <param name="message">Result message.</param>
		public AgentBridgeLogEntry(
			DateTimeOffset timestamp,
			string direction,
			string requestId,
			string actionSummary,
			string status,
			double durationMs,
			string? primaryArtifactPath,
			string message)
		{
			Timestamp = timestamp;
			Direction = direction ?? string.Empty;
			RequestId = requestId ?? string.Empty;
			ActionSummary = actionSummary ?? string.Empty;
			Status = status ?? string.Empty;
			DurationMs = durationMs;
			PrimaryArtifactPath = primaryArtifactPath ?? "-";
			Message = message ?? string.Empty;
		}

		/// <summary>Gets when the activity completed.</summary>
		public DateTimeOffset Timestamp { get; }

		/// <summary>Gets the formatted timestamp (<c>HH:mm:ss.fff</c>).</summary>
		public string TimestampText => Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);

		/// <summary>Gets the communication direction.</summary>
		public string Direction { get; }

		/// <summary>Gets the request or capture ID.</summary>
		public string RequestId { get; }

		/// <summary>Gets the action and parameter summary.</summary>
		public string ActionSummary { get; }

		/// <summary>Gets the status string (<c>Ok</c> or <c>Error</c>).</summary>
		public string Status { get; }

		/// <summary>Gets the execution duration in milliseconds.</summary>
		public double DurationMs { get; }

		/// <summary>Gets the primary artifact file path.</summary>
		public string PrimaryArtifactPath { get; }

		/// <summary>Gets the human-readable status or error message.</summary>
		public string Message { get; }
	}

	/// <summary>
	/// Pure-XML (<see cref="XDocument"/>) serializer for <c>AgentBridge/outbox/*-response.xml</c> and <c>AgentBridge/heartbeat.xml</c>.
	/// </summary>
	public static class AgentBridgeResponse
	{
		/// <summary>
		/// Formats a successful <c>&lt;AgentResponse Status="Ok"&gt;</c> XML payload.
		/// </summary>
		/// <param name="cmd">Executed command.</param>
		/// <param name="durationMs">Elapsed execution time in milliseconds.</param>
		/// <param name="message">Summary message.</param>
		/// <param name="environment">Active theme and Windows OS snapshot.</param>
		/// <param name="diagnostics">Unfrozen Freezable, multi-window dictionary, and WPF trace counts.</param>
		/// <param name="artifacts">Generated screenshot bundle, report path, and bridge directory paths.</param>
		/// <returns>Serialized XML string.</returns>
		public static string FormatSuccessXml(
			AgentBridgeCommand cmd,
			double durationMs,
			string message,
			(ThemeDescriptor? Active, WindowsOsThemeSnapshot Os) environment,
			(int UnfrozenCount, int ThemedWindowHosts, int DistinctDictionaries, int TraceErrors) diagnostics,
			(ScreenshotCaptureBundle? Bundle, string? ReportPath, AgentBridgePaths Paths) artifacts)
		{
			var active = environment.Active;
			var os = environment.Os;
			var bundle = artifacts.Bundle;
			var paths = artifacts.Paths;

			var doc = new XDocument(
				new XElement(
					"AgentResponse",
					new XAttribute("Id", cmd.RequestId),
					new XAttribute("Action", cmd.Action),
					new XAttribute("Status", "Ok"),
					new XAttribute("DurationMs", durationMs.ToString("F2", CultureInfo.InvariantCulture)),
					new XAttribute("TimestampUtc", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
					new XElement("Message", message),
					new XElement(
						"ActiveTheme",
						new XAttribute("Id", active?.Id ?? string.Empty),
						new XAttribute("DisplayName", active?.DisplayName ?? string.Empty),
						new XAttribute("BaseClass", active?.BaseClassBadgeText ?? string.Empty),
						new XAttribute("ClrNamespace", active?.ClrNamespace ?? string.Empty),
						new XAttribute("XmlnsMapped", active?.IsMappedInXmlnsDefinition ?? false)),
					new XElement(
						"OsState",
						new XAttribute("Version", os.OsVersionCaption),
						new XAttribute("AppsMode", os.AppsThemeModeText),
						new XAttribute("SystemMode", os.SystemThemeModeText),
						new XAttribute("HighContrast", os.HighContrastDisplayText),
						new XAttribute("AccentHex", os.OsAccentHex)),
					new XElement(
						"Diagnostics",
						new XAttribute("UnfrozenFreezables", diagnostics.UnfrozenCount),
						new XAttribute("ThemedWindowHosts", diagnostics.ThemedWindowHosts),
						new XAttribute("DistinctDictionaries", diagnostics.DistinctDictionaries),
						new XAttribute("WpfTraceErrors", diagnostics.TraceErrors)),
					new XElement(
						"Artifacts",
						new XAttribute("LatestCompositePng", bundle?.LatestCompositePngPath ?? string.Empty),
						new XAttribute("LatestMainWpfPng", bundle != null ? paths.LatestMainWpfPngPath : string.Empty),
						new XAttribute("LatestMainDwmPng", bundle?.MainDwmPngPath != null ? paths.LatestMainDwmPngPath : string.Empty),
						new XAttribute("LatestContextMd", bundle?.LatestContextMarkdownPath ?? string.Empty),
						new XAttribute("ArchivedCompositePng", bundle?.CompositePngPath ?? string.Empty),
						new XAttribute("ReportPath", artifacts.ReportPath ?? string.Empty))));

			return doc.ToString();
		}

		/// <summary>
		/// Formats a structured <c>&lt;AgentResponse Status="Error"&gt;</c> XML payload.
		/// </summary>
		/// <param name="requestId">Request identifier.</param>
		/// <param name="action">Requested action.</param>
		/// <param name="durationMs">Elapsed milliseconds.</param>
		/// <param name="errorMessage">Failure message.</param>
		/// <returns>Serialized XML string.</returns>
		public static string FormatErrorXml(string requestId, string action, double durationMs, string errorMessage)
		{
			var doc = new XDocument(
				new XElement(
					"AgentResponse",
					new XAttribute("Id", requestId),
					new XAttribute("Action", action),
					new XAttribute("Status", "Error"),
					new XAttribute("DurationMs", durationMs.ToString("F2", CultureInfo.InvariantCulture)),
					new XAttribute("TimestampUtc", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
					new XElement("Message", errorMessage)));

			return doc.ToString();
		}

		/// <summary>
		/// Formats the live <c>&lt;AgentBridgeHeartbeat&gt;</c> XML presence payload.
		/// </summary>
		/// <param name="runtime">Process ID, state, start time, processed command count, and WPF trace error count.</param>
		/// <param name="active">Active theme descriptor, if any.</param>
		/// <param name="paths">Bridge folder configuration.</param>
		/// <returns>Serialized XML string.</returns>
		public static string FormatHeartbeatXml(
			(int ProcessId, string State, DateTimeOffset StartedAtUtc, int ProcessedCommandCount, int TraceErrors) runtime,
			ThemeDescriptor? active,
			AgentBridgePaths paths)
		{
			var doc = new XDocument(
				new XElement(
					"AgentBridgeHeartbeat",
					new XAttribute("ProcessId", runtime.ProcessId),
					new XAttribute("State", runtime.State),
					new XAttribute("StartedAtUtc", runtime.StartedAtUtc.ToString("o", CultureInfo.InvariantCulture)),
					new XAttribute("UpdatedAtUtc", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
					new XAttribute("ProcessedCommandCount", runtime.ProcessedCommandCount),
					new XAttribute("ActiveThemeId", active?.Id ?? string.Empty),
					new XAttribute("ActiveThemeName", active?.DisplayName ?? string.Empty),
					new XAttribute("ActiveBaseClass", active?.BaseClassBadgeText ?? string.Empty),
					new XAttribute("WpfTraceErrorCount", runtime.TraceErrors),
					new XAttribute("InboxDirectory", paths.InboxDirectory),
					new XAttribute("OutboxDirectory", paths.OutboxDirectory),
					new XAttribute("LatestDirectory", paths.LatestDirectory)));

			return doc.ToString();
		}
	}
}