using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;

namespace TestHostApp2.ViewModels
{
	public class CameraConnectionViewModel : BindableBase, IInteractionRequestAware
	{
		public string SelectedItem { get; set; }
		public DelegateCommand OKCommand { get; private set; }

		public CameraConnectionViewModel()
		{
			OKCommand = new DelegateCommand( AcceptSelectedItem );
		}

		//private void CancelInteraction()
		//{
		//	_notification.Confirmed = false;
		//	FinishInteraction?.Invoke();
		//}

		private void AcceptSelectedItem()
		{
			_notification.Confirmed = true;
			FinishInteraction?.Invoke();
		}

		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public INotification Notification
		{
			get { return _notification; }
			set { SetProperty( ref _notification, (IConfirmation)value); }
		}
	}
}
