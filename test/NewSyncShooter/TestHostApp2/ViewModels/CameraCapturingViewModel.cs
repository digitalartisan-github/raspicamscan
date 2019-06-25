using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class CameraCapturingViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public ReactiveProperty<string> CapturingName { get; set; } = new ReactiveProperty<string>( string.Empty );

		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		public CameraCapturingViewModel()
		{
			OkCommand = new DelegateCommand( OKInteraction );
			CancelCommand = new DelegateCommand( CancelInteraction );
		}

		private void OKInteraction()
		{
			CameraCapturingNotification notification = _notification as CameraCapturingNotification;
			notification.CapturingName = this.CapturingName.Value;
			_notification.Confirmed = true;
			FinishInteraction?.Invoke();
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

		public INotification Notification
		{
			get { return _notification; }
			set {
				SetProperty( ref _notification, (IConfirmation) value );
				CameraCapturingNotification notification = _notification as CameraCapturingNotification;
				this.CapturingName.Value = notification.CapturingName;
			}
		}
	}
}
