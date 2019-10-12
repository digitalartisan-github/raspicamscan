using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using PrismCommonDialog.Confirmations;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
    public class ThreeDBuildingViewModel : BindableBase, IInteractionRequestAware
    {
        public Action FinishInteraction { get; set; }
        private IConfirmation _notification;

        public ReactiveProperty<string> ImageFolderPath { get; set; } = new ReactiveProperty<string>( string.Empty );
        public ReactiveProperty<string> ThreeDDataFolderPath { get; set; } = new ReactiveProperty<string>( string.Empty );
        public ReactiveProperty<bool> IsCutPetTable { get; set; } = new ReactiveProperty<bool>( false );
        public ReactiveProperty<bool> IsSkipAlreadyBuilt { get; set; } = new ReactiveProperty<bool>( false );
        public ReactiveProperty<bool> IsEnableSkipAlreadyBuilt { get; set; } = new ReactiveProperty<bool>( false );

        public InteractionRequest<INotification> BrowseFolderRequest { get; set; }
        public DelegateCommand BrowseFolderCommand { get; private set; }
        public DelegateCommand OkCommand { get; private set; }
        public DelegateCommand CancelCommand { get; private set; }

        public ThreeDBuildingViewModel()
        {
            BrowseFolderRequest = new InteractionRequest<INotification>();
            BrowseFolderCommand = new DelegateCommand( RaiseBrowseFolderCommand );
            OkCommand = new DelegateCommand( OKInteraction );
            CancelCommand = new DelegateCommand( CancelInteraction );
        }

        private void RaiseBrowseFolderCommand()
        {
            ThreeDBuildingNotification notification = _notification as ThreeDBuildingNotification;
            var folderNotification = new FolderSelectDialogConfirmation()
            {
                InitialDirectory = notification.ThreeDDataFolderPath,
                SelectedPath = notification.ThreeDDataFolderPath,
                RootFolder = Environment.SpecialFolder.Personal,
                ShowNewFolderButton = false
            };
            BrowseFolderRequest.Raise( folderNotification );
            if ( folderNotification.Confirmed ) {
                this.ThreeDDataFolderPath.Value =
                notification.ThreeDDataFolderPath = folderNotification.SelectedPath; ;
            }
        }

        private void OKInteraction()
        {
            ThreeDBuildingNotification notification = _notification as ThreeDBuildingNotification;
            notification.ThreeDDataFolderPath = ThreeDDataFolderPath.Value;
            notification.IsCutPetTable = IsCutPetTable.Value;
            notification.IsSkipAlreadyBuilt = IsSkipAlreadyBuilt.Value;

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
                var notification = _notification as ThreeDBuildingNotification;
                ImageFolderPath.Value = notification.ImageFolderPath;
                ThreeDDataFolderPath.Value = notification.ThreeDDataFolderPath;
                IsCutPetTable.Value = notification.IsCutPetTable;
                IsSkipAlreadyBuilt.Value = notification.IsSkipAlreadyBuilt;
                IsEnableSkipAlreadyBuilt.Value = notification.IsEnableSkipAlreadyBuilt;
            }
        }
    }
}
