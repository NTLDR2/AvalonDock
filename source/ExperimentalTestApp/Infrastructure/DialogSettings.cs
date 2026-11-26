using System.Collections.Generic;
using System.Windows;

namespace ExperimentalTestApp.Infrastructure
{
	/// <summary>
	/// Window settings passed to <c>IWindowManager.ShowDialogAsync</c>.
	/// </summary>
	/// <remarks>
	/// Caliburn.Micro wraps a dialog's <c>UserControl</c> view in a generated <see cref="Window"/> and applies this dictionary
	/// to it by property name. Keeping the WPF enum values here keeps them out of the ViewModels.
	/// </remarks>
	public static class DialogSettings
	{
		/// <summary>
		/// Creates the settings for a small, fixed-size modal dialog centred on its owner.
		/// </summary>
		/// <returns>A new settings dictionary.</returns>
		public static IDictionary<string, object> CreateFixedDialog()
		{
			return new Dictionary<string, object>
			{
				[nameof(Window.ResizeMode)] = ResizeMode.NoResize,
				[nameof(Window.ShowInTaskbar)] = false,
				[nameof(Window.WindowStartupLocation)] = WindowStartupLocation.CenterOwner,
				[nameof(Window.SizeToContent)] = SizeToContent.WidthAndHeight,
			};
		}
	}
}
