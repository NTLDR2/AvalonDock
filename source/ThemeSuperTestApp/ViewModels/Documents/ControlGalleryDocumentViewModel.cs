using System.Collections.Generic;
using System.Windows.Media;
using ThemeSuperTestApp.Boilerplate;
using ThemeSuperTestApp.Infrastructure;

namespace ThemeSuperTestApp.ViewModels.Documents
{
	/// <summary>
	/// Specimen Document 2: Three-column contrast matrix comparing unthemed <c>net48</c> <c>Aero2</c> WPF controls,
	/// <c>SystemColors.*</c> dynamic brushes, and inherited <c>DockingManager</c> foreground/background surfaces.
	/// </summary>
	public sealed class ControlGalleryDocumentViewModel : DocumentScreen
	{
		private string _sampleText = "Editable WPF TextBox on net48 Aero2";
		private double _sampleSliderValue = 65.0;

		/// <summary>
		/// Initializes a new instance of the <see cref="ControlGalleryDocumentViewModel"/> class.
		/// </summary>
		public ControlGalleryDocumentViewModel()
		{
			Id = "Doc.ControlGallery";
			DisplayName = "Control & SystemColors Matrix";
			ToolTipText = "Compare unthemed net48 Aero2 controls, SystemColors.*, and inherited DockingManager properties across themes";
			CanClose = true;
			IconSource = SpecimenIconFactory.CreateBadgeIcon(Color.FromRgb(0x10, 0xB9, 0x81), Color.FromRgb(0xD1, 0xFA, 0xE5));

			SampleComboItems = new[]
			{
				"Standard ComboBox Item 1",
				"Standard ComboBox Item 2",
				"Standard ComboBox Item 3",
			};
		}

		/// <summary>Gets sample items for the standard WPF <c>ComboBox</c> and <c>ListBox</c>.</summary>
		public IReadOnlyList<string> SampleComboItems { get; }

		/// <summary>Gets or sets the sample text box value.</summary>
		public string SampleText
		{
			get => _sampleText;
			set => Set(ref _sampleText, value ?? string.Empty);
		}

		/// <summary>Gets or sets the sample slider and progress bar value.</summary>
		public double SampleSliderValue
		{
			get => _sampleSliderValue;
			set => Set(ref _sampleSliderValue, value);
		}

		/// <summary>Toggles <see cref="DockableScreen.IsModified"/> to test the tab header's modified dot indicator.</summary>
		public void ToggleModifiedDot()
		{
			IsModified = !IsModified;
		}
	}
}