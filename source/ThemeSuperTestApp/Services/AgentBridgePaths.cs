using System;
using System.IO;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Resolves and manages directory paths for the workspace-local <c>AgentBridge/</c> folder shared between
	/// <c>ThemeSuperTestApp</c> and Antigravity.
	/// </summary>
	public sealed class AgentBridgePaths
	{
		/// <summary>Default maximum number of archived capture sets retained in <see cref="CapturesDirectory"/>.</summary>
		public const int DefaultMaxRetainedCaptureSets = 40;

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgePaths"/> class resolving <c>source/ThemeSuperTestApp/AgentBridge</c>
		/// relative to the running executable.
		/// </summary>
		public AgentBridgePaths()
			: this(ResolveDefaultBridgeDirectory(), DefaultMaxRetainedCaptureSets)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgePaths"/> class rooted at <paramref name="bridgeRootDirectory"/>.
		/// </summary>
		/// <param name="bridgeRootDirectory">Root directory for bridge artifacts (supports isolated temp directories in unit tests).</param>
		/// <param name="maxRetainedCaptureSets">Maximum historical capture prefixes retained in <c>captures/</c>.</param>
		public AgentBridgePaths(string bridgeRootDirectory, int maxRetainedCaptureSets = DefaultMaxRetainedCaptureSets)
		{
			if (string.IsNullOrWhiteSpace(bridgeRootDirectory))
				throw new ArgumentNullException(nameof(bridgeRootDirectory));

			BridgeRootDirectory = Path.GetFullPath(bridgeRootDirectory);
			LatestDirectory = Path.Combine(BridgeRootDirectory, "latest");
			CapturesDirectory = Path.Combine(BridgeRootDirectory, "captures");
			InboxDirectory = Path.Combine(BridgeRootDirectory, "inbox");
			OutboxDirectory = Path.Combine(BridgeRootDirectory, "outbox");
			HeartbeatFilePath = Path.Combine(BridgeRootDirectory, "heartbeat.xml");
			MaxRetainedCaptureSets = Math.Max(1, maxRetainedCaptureSets);
		}

		/// <summary>Gets the root <c>AgentBridge</c> directory path.</summary>
		public string BridgeRootDirectory { get; }

		/// <summary>Gets the <c>AgentBridge/latest</c> directory holding the most recent <c>F12</c> / snapshot bundle.</summary>
		public string LatestDirectory { get; }

		/// <summary>Gets the <c>AgentBridge/captures</c> directory holding rolling timestamped/request-keyed captures.</summary>
		public string CapturesDirectory { get; }

		/// <summary>Gets the <c>AgentBridge/inbox</c> directory watched for incoming Antigravity XML commands.</summary>
		public string InboxDirectory { get; }

		/// <summary>Gets the <c>AgentBridge/outbox</c> directory receiving XML command responses.</summary>
		public string OutboxDirectory { get; }

		/// <summary>Gets the path to <c>AgentBridge/heartbeat.xml</c>.</summary>
		public string HeartbeatFilePath { get; }

		/// <summary>Gets the maximum number of archived capture sets kept in <see cref="CapturesDirectory"/>.</summary>
		public int MaxRetainedCaptureSets { get; }

		/// <summary>Gets the fixed path to <c>latest/latest-main-wpf.png</c>.</summary>
		public string LatestMainWpfPngPath => Path.Combine(LatestDirectory, "latest-main-wpf.png");

		/// <summary>Gets the fixed path to <c>latest/latest-main-dwm.png</c>.</summary>
		public string LatestMainDwmPngPath => Path.Combine(LatestDirectory, "latest-main-dwm.png");

		/// <summary>Gets the fixed path to <c>latest/latest-composite.png</c>.</summary>
		public string LatestCompositePngPath => Path.Combine(LatestDirectory, "latest-composite.png");

		/// <summary>Gets the fixed path to <c>latest/latest-context.md</c>.</summary>
		public string LatestContextMarkdownPath => Path.Combine(LatestDirectory, "latest-context.md");

		/// <summary>
		/// Ensures all <c>AgentBridge</c> subdirectories exist on disk.
		/// </summary>
		public void EnsureDirectoriesExist()
		{
			Directory.CreateDirectory(BridgeRootDirectory);
			Directory.CreateDirectory(LatestDirectory);
			Directory.CreateDirectory(CapturesDirectory);
			Directory.CreateDirectory(InboxDirectory);
			Directory.CreateDirectory(OutboxDirectory);
		}

		private static string ResolveDefaultBridgeDirectory()
		{
			var current = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
			while (current != null)
			{
				var candidateCsproj = Path.Combine(current.FullName, "ThemeSuperTestApp.csproj");
				if (File.Exists(candidateCsproj))
					return Path.Combine(current.FullName, "AgentBridge");

				var nestedProjectDir = Path.Combine(current.FullName, "source", "ThemeSuperTestApp");
				if (File.Exists(Path.Combine(nestedProjectDir, "ThemeSuperTestApp.csproj")))
					return Path.Combine(nestedProjectDir, "AgentBridge");

				current = current.Parent;
			}

			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AgentBridge");
		}
	}
}