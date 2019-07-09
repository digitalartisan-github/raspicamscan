using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class CameraCapturingViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public ReactiveProperty<string> CapturingName { get; } = new ReactiveProperty<string>( string.Empty );

		public ReactiveCommand OkCommand { get; }
		public ReactiveCommand CancelCommand { get; }

		public CameraCapturingViewModel()
		{
			//OkCommand = CapturingName.Select( s => !string.IsNullOrEmpty( s ) ).ToReactiveCommand();
			OkCommand = new ReactiveCommand();
			OkCommand.Subscribe( OKInteraction );
			CancelCommand = new ReactiveCommand();
			CancelCommand.Subscribe( CancelInteraction );
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
