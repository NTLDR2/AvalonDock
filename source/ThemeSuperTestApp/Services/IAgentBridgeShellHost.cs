using System.Threading.Tasks;
using ThemeSuperTestApp.Models;

namespace ThemeSuperTestApp.Services
{
	/// <summary>
	/// Decoupled shell contract allowing <see cref="AgentBridgeService"/> to drive high-level shell layout, chrome,
	/// and stress-harness actions without circular constructor dependencies in IoC.
	/// </summary>
	public interface IAgentBridgeShellHost
	{
		/// <summary>Gets or sets the operator snapshot note embedded in <c>latest-context.md</c>.</summary>
		string UserSnapshotNote { get; set; }

		/// <summary>Gets or sets a value indicating whether outer shell chrome is tinted from the active theme polarity.</summary>
		bool StyleOuterShellFromTheme { get; set; }

		/// <summary>Gets or sets a value indicating whether the custom icon-enabled tab header template is active.</summary>
		bool UseCustomDocumentHeaderTemplate { get; set; }

		/// <summary>Gets or sets a value indicating whether theme selection auto-syncs to Windows OS mode.</summary>
		bool AutoSyncThemeToOsMode { get; set; }

		/// <summary>Applies a <see cref="DwmTitleBarMode"/> through the shell ViewModel so UI selectors stay synchronized.</summary>
		/// <param name="mode">Target DWM title-bar mode.</param>
		void SetDwmTitleBarModeFromAgent(DwmTitleBarMode mode);

		/// <summary>Spawns 10 overflow specimen tabs.</summary>
		/// <returns>A task that completes when tabs are open.</returns>
		Task SpawnOverflowTabs();

		/// <summary>Closes all spawned overflow specimen tabs.</summary>
		/// <returns>A task that completes when tabs are closed.</returns>
		Task CloseSpecimenTabs();

		/// <summary>Floats a specimen document and specimen toolbox.</summary>
		/// <returns>A task that completes when floating windows are open.</returns>
		Task SpawnFloatingToolAndDocument();

		/// <summary>Detaches the specimen toolbox into a standalone OS <c>DetachedAnchorableWindow</c>.</summary>
		void SpawnDetachedOsWindow();

		/// <summary>Re-docks all floating and detached windows.</summary>
		void DockAllWindows();

		/// <summary>Resets the layout to the startup factory snapshot.</summary>
		void ResetLayout();
	}
}