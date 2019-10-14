using System;
//using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
    public class AutoModeViewModel : BindableBase, IInteractionRequestAware
    {
        public Action FinishInteraction { get; set; }
        private IConfirmation _notification = null;
        private SynchronizationContext _mainContext = null;
        public InteractionRequest<Notification> CloseWindowRequest { get; } = new InteractionRequest<Notification>();

        static readonly int _maxSeconds = 10;
        public ReactiveProperty<int> RemainingTime { get; } = new ReactiveProperty<int>( _maxSeconds );
        public ReactiveCommand CaptureCommand { get; } = new ReactiveCommand();
        public ReactiveCommand OkCommand { get; } = new ReactiveCommand();

        public AutoModeViewModel()
        {
            CaptureCommand.Subscribe( CaptureInteraction );
            OkCommand.Subscribe( OKInteraction );
        }

        private void CaptureInteraction()
        {
            _mainContext = SynchronizationContext.Current;
            Task tsk = Task.Run( () => CountdownAndCapture() );
        }

        private async Task CountdownAndCapture()
        {
            await Task.Factory.StartNew( () =>
            {
                RemainingTime.Value = _maxSeconds;
                while ( --RemainingTime.Value > 0 ) {
                    Task.Delay( 1000 ).Wait();
                }
            } );
            _mainContext.Post(_ =>
            {
                var notification = _notification as AutoModeNotification;
                notification.Capture();
                RemainingTime.Value = _maxSeconds;
            }, null );
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
