using System.Windows.Media;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Infrastructure;

namespace ThemeSuperTestApp.ViewModels.Documents
{
	/// <summary>
	/// Multi-instance specimen document used for overflow-menu (<c>MenuItemEx</c>), <c>NavigatorWindow</c> (<c>Ctrl+Tab</c>),
	/// tab icon, dirty indicator, and floating document stress testing.
	/// </summary>
	public sealed class SpecimenNoteDocumentViewModel : DocumentScreen
	{
		private string _notesText;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpecimenNoteDocumentViewModel"/> class.
		/// </summary>
		/// <param name="index">1-based specimen number.</param>
		/// <param name="title">Initial tab title.</param>
		/// <param name="accentColor">Badge icon color.</param>
		/// <param name="isModified">Initial modified flag.</param>
		public SpecimenNoteDocumentViewModel(int index, string title, Color accentColor, bool isModified)
		{
			Id = $"Doc.Specimen.{index}";
			DisplayName = title;
			ToolTipText = $"Specimen #{index}: {title} (Use to test MenuItemEx overflow dropdown & Ctrl+Tab NavigatorWindow)";
			IsModified = isModified;
			IconSource = SpecimenIconFactory.CreateBadgeIcon(accentColor, Colors.White);
			_notesText = $"Specimen Document #{index} ({title})\n\n" +
				"- Click the document well's overflow dropdown arrow (top-right of tab strip) to exercise MenuItemEx.\n" +
				"- Press Ctrl+Tab to open NavigatorWindow and inspect Tool 3 & Tool 4.\n" +
				"- Toggle IsModified / CanClose / CanFloat below to verify live LayoutItem bindings.";
		}

		/// <summary>Gets or sets the editable body text.</summary>
		public string NotesText
		{
			get => _notesText;
			set => Set(ref _notesText, value ?? string.Empty);
		}
	}
}