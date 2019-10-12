using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
    public class CameraCapturingViewModel : BindableBase, IInteractionRequestAware
    {
        public Action FinishInteraction { get; set; }
        private IConfirmation _notification;

        public ReactiveProperty<string> CapturingName { get; } = new ReactiveProperty<string>( string.Empty );

        public ReactiveCommand OKCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CameraCapturingViewModel()
        {
            OKCommand.Subscribe( OKInteraction );
            CancelCommand.Subscribe( CancelInteraction );
        }

        private void OKInteraction()
        {
            var notification = _notification as CameraCapturingNotification;
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
            set
            {
                SetProperty( ref _notification, (IConfirmation) value );
                var notification = _notification as CameraCapturingNotification;
                this.CapturingName.Value = notification.CapturingName;
            }
        }
    }
}
