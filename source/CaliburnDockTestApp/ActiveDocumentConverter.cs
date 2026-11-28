using CaliburnDockTestApp.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace CaliburnDockTestApp
{
	/// <summary>
	/// Returns <see langword="true"/> when the bound <c>ActiveItem</c> (<see cref="ViewModelBase"/>) matches
	/// the target document <see cref="ViewModelBase.Id"/> supplied in <c>ConverterParameter</c>; otherwise <see langword="false"/>.
	/// Because <c>Conductor&lt;Screen&gt;.Collection.OneActive.ActiveItem</c> holds at most one active document
	/// (even across side-by-side split tab groups or floating windows), at most one <c>MenuItem</c> can ever be
	/// checked at a time—and zero when all documents are closed (<c>ActiveItem == null</c>).
	/// </summary>
	public sealed class ActiveDocumentConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is ViewModelBase activeDocument && parameter is string expectedId)
				return string.Equals(activeDocument.Id, expectedId, StringComparison.Ordinal);

			return false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Binding.DoNothing;
	}
}