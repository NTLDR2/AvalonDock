using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AvalonDock.Core;
using Caliburn.Micro;

namespace ExperimentalTestApp.ViewModels.Dialogs
{
	/// <summary>
	/// The About dialog: shows the versions of the libraries in use.
	/// </summary>
	public sealed class AboutViewModel : Screen
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AboutViewModel"/> class.
		/// </summary>
		/// <param name="themeManager">The theme manager (for the current theme name).</param>
		public AboutViewModel(IThemeManager themeManager)
		{
			DisplayName = "About ExperimentalTestApp";
			AvalonDockVersion = GetVersion(typeof(IDockable).Assembly);
			CaliburnMicroVersion = GetVersion(typeof(Screen).Assembly);
			Framework = RuntimeInformation.FrameworkDescription;
			Theme = themeManager?.CurrentThemeName ?? "(none)";
		}

		/// <summary>Gets the AvalonDock version.</summary>
		public string AvalonDockVersion { get; }

		/// <summary>Gets the Caliburn.Micro version (informational version, including the source commit).</summary>
		public string CaliburnMicroVersion { get; }

		/// <summary>Gets the .NET runtime description.</summary>
		public string Framework { get; }

		/// <summary>Gets the current AvalonDock theme.</summary>
		public string Theme { get; }

		/// <summary>Closes the dialog (button <c>Close</c>).</summary>
		/// <returns>A task that completes when the dialog is closed.</returns>
		public Task Close() => TryCloseAsync(true);

		private static string GetVersion(Assembly assembly)
		{
			var informational = assembly
				.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
				.OfType<AssemblyInformationalVersionAttribute>()
				.FirstOrDefault()?.InformationalVersion;
			return informational ?? assembly.GetName().Version?.ToString() ?? "?";
		}
	}
}
