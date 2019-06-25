using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using TestHostApp2.Notifications;

using Reactive.Bindings;
using System.Collections.Generic;
using System.Linq;
using NewSyncShooter;

namespace TestHostApp2.ViewModels
{
	public class CameraConnectionViewModel : BindableBase, IInteractionRequestAware
	{
		public string SelectedItem { get; set; }
		public DelegateCommand OkCommand { get; private set; }

		public CameraConnectionViewModel()
		{
			OkCommand = new DelegateCommand( CancelInteraction );
		}

		private void CancelInteraction()
		{
			_notification.Confirmed = false;
			FinishInteraction?.Invoke();
		}

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
