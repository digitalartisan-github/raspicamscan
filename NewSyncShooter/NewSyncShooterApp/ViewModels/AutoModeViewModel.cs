using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using PrismCommonDialog.Confirmations;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
    public class AutoModeViewModel : BindableBase, IInteractionRequestAware
    {
        public Action FinishInteraction { get; set; }
        private IConfirmation _notification = null;

        public ReactiveCommand CaptureCommand { get; } = new ReactiveCommand();
        public ReactiveCommand OkCommand { get; } = new ReactiveCommand();

        public AutoModeViewModel()
        {
            CaptureCommand.Subscribe( Capture );
            OkCommand.Subscribe( OKInteraction );
            //OkCommand = new DelegateCommand( OKInteraction );
        }

        private void CaptureInteraction()
        {
        }

        private void Capture()
        {
            System.Threading.Thread.Sleep( 10000 );    // wait 10 seconds
            var notification = _notification as AutoModeNotification;
            notification.Capture();
        }

        private void OKInteraction()
        {
            _notification.Confirmed = true;
            FinishInteraction();
        }

        public INotification Notification
        {
            get { return _notification; }
            set
            {
                SetProperty( ref _notification, (IConfirmation) value );
            }
        }
    }
}
