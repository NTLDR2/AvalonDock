using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Models;
using ThemeSuperTestApp.Services;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Tool 6: Live Antigravity Bridge control panel, operator snapshot note editor, and command/snapshot history log.
	/// </summary>
	public sealed class AgentBridgeToolboxViewModel : ToolboxScreen
	{
		private readonly AgentBridgeService _bridgeService;
		private readonly WindowScreenshotService _screenshotService;
		private string _userSnapshotNote = string.Empty;
		private string _toolboxStatusText = "Auto-listening for Antigravity XML commands in AgentBridge/inbox/.";

		/// <summary>
		/// Initializes a new instance of the <see cref="AgentBridgeToolboxViewModel"/> class.
		/// </summary>
		/// <param name="bridgeService">The bidirectional Antigravity file-mailbox service.</param>
		/// <param name="screenshotService">The screenshot &amp; archive retention service.</param>
		public AgentBridgeToolboxViewModel(AgentBridgeService bridgeService, WindowScreenshotService screenshotService)
		{
			_bridgeService = bridgeService ?? throw new ArgumentNullException(nameof(bridgeService));
			_screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));

			Id = "Tool.AgentBridge";
			DisplayName = "6. Antigravity Live Bridge";
			ToolTipText = "Bidirectional XML mailbox, F12 multimodal screenshot pusher, and live Antigravity command history (Ctrl+Alt+A)";
			Zone = DockZone.LeftBottom;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+A";

			_bridgeService.PropertyChanged += OnBridgeServicePropertyChanged;
		}

		/// <summary>Gets the shared live activity log.</summary>
		public BindableCollection<AgentBridgeLogEntry> LogEntries => _bridgeService.LogEntries;

		/// <summary>Gets the root <c>AgentBridge/</c> directory path.</summary>
		public string BridgeDirectoryPath => _bridgeService.Paths.BridgeRootDirectory;

		/// <summary>Gets or sets a value indicating whether the inbox listener is active.</summary>
		public bool IsListening
		{
			get => _bridgeService.IsListening;
			set
			{
				if (_bridgeService.IsListening == value)
					return;

				_bridgeService.IsListening = value;
				NotifyOfPropertyChange(nameof(IsListening));
				ToolboxStatusText = value
					? "Resumed listening on AgentBridge/inbox/."
					: "Paused AgentBridge/inbox/ listener.";
			}
		}

		/// <summary>Gets the bridge summary status from <see cref="AgentBridgeService"/>.</summary>
		public string BridgeSummaryText => _bridgeService.LastBridgeSummary;

		/// <summary>Gets or sets an optional operator note embedded at the top of <c>latest-context.md</c> when pushing a snapshot.</summary>
		public string UserSnapshotNote
		{
			get => _userSnapshotNote;
			set => Set(ref _userSnapshotNote, value ?? string.Empty);
		}

		/// <summary>Gets or sets the local status text for Tool 6.</summary>
		public string ToolboxStatusText
		{
			get => _toolboxStatusText;
			set => Set(ref _toolboxStatusText, value);
		}

		/// <summary>Captures a dual-mode multimodal snapshot bundle with <see cref="UserSnapshotNote"/> into <c>AgentBridge/latest/</c>.</summary>
		/// <returns>A task that completes when the snapshot bundle is saved.</returns>
		public async Task SendSnapshotNow()
		{
			var bundle = await _bridgeService.CaptureManualSnapshotAsync(_userSnapshotNote);
			if (bundle != null)
				ToolboxStatusText = $"Saved snapshot '{bundle.CaptureId}' -> tell Antigravity: 'Inspect the latest snapshot'.";
		}

		/// <summary>Opens the workspace <c>AgentBridge</c> folder in Windows Explorer.</summary>
		public void OpenBridgeFolder()
		{
			_bridgeService.Paths.EnsureDirectoriesExist();
			try
			{
				_ = Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"\"{_bridgeService.Paths.BridgeRootDirectory}\"",
					UseShellExecute = true,
				});
			}
			catch (Win32Exception ex)
			{
				ToolboxStatusText = $"Could not launch Explorer: {ex.Message}";
			}
		}

		/// <summary>Deletes archived captures in <c>AgentBridge/captures/</c> while keeping <c>latest/</c> intact.</summary>
		public void CleanOldCaptures()
		{
			var deleted = _screenshotService.CleanAllArchivedCaptures();
			ToolboxStatusText = $"Deleted {deleted} archived capture file(s) from AgentBridge/captures/.";
		}

		/// <summary>Clears the command &amp; snapshot history table.</summary>
		public void ClearLog()
		{
			_bridgeService.ClearLog();
			ToolboxStatusText = "Cleared bridge activity log.";
		}

		/// <inheritdoc/>
		protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
		{
			if (close)
				_bridgeService.PropertyChanged -= OnBridgeServicePropertyChanged;

			return base.OnDeactivateAsync(close, cancellationToken);
		}

		private void OnBridgeServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AgentBridgeService.IsListening))
				NotifyOfPropertyChange(nameof(IsListening));
			else if (e.PropertyName == nameof(AgentBridgeService.LastBridgeSummary))
				NotifyOfPropertyChange(nameof(BridgeSummaryText));
		}
	}
}