using System.Windows.Media;
using AvalonDock.Core;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Infrastructure;

namespace ThemeSuperTestApp.ViewModels.Tools
{
	/// <summary>
	/// Dedicated tear-off specimen toolbox used by the one-click <c>Spawn Floating</c> and <c>Spawn Detached OS Window</c>
	/// stress actions so Tools 1–5 remain docked and visible while testing multi-window chrome and dictionary sharing.
	/// </summary>
	public sealed class StressSpecimenToolboxViewModel : ToolboxScreen
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StressSpecimenToolboxViewModel"/> class.
		/// </summary>
		public StressSpecimenToolboxViewModel()
		{
			Id = "Tool.StressSpecimen";
			DisplayName = "Tear-Off Specimen Tool";
			ToolTipText = "Dedicated anchorable specimen for testing Float, Auto-Hide, and DetachedAnchorableWindow ('Window' mode)";
			Zone = DockZone.LeftBottom;
			IsOpenByDefault = true;
			Shortcut = "Ctrl+Alt+S";
			IconSource = SpecimenIconFactory.CreateBadgeIcon(Color.FromRgb(0xF5, 0x9E, 0x0B), Colors.White);
		}
	}
}