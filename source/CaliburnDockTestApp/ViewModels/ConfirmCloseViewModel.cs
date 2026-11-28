using Caliburn.Micro;
using System;
using System.Threading.Tasks;

namespace CaliburnDockTestApp.ViewModels
{
	public sealed class ConfirmCloseViewModel : Screen
	{
		public ConfirmCloseViewModel(string documentName)
		{
			if (documentName == null)
				throw new ArgumentNullException(nameof(documentName));

			DisplayName = documentName;
			Message = $"Are you sure you want to close '{documentName}'?";
		}

		public string Message { get; }

		public Task ConfirmAsync() => TryCloseAsync(true);

		public Task CancelAsync() => TryCloseAsync(false);
	}
}