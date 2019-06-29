using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using PrismCommonDialog.Confirmations;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class NewProjectViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public ReactiveProperty<string> BaseFolderPath { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> ProjectName { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReadOnlyReactiveProperty<bool> IsProjectNameValid { get; }
		public ReactiveProperty<string> ProjectComment { get; set; } = new ReactiveProperty<string>( string.Empty );

		public InteractionRequest<INotification> BrowseFolderRequest { get; set; }
		public DelegateCommand BrowseFolderCommand { get; private set; }
		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NewProjectViewModel()
		{
			this.IsProjectNameValid = this.ProjectName.Select( n => ! string.IsNullOrEmpty( n ) ).ToReadOnlyReactiveProperty<bool>();
			BrowseFolderRequest = new InteractionRequest<INotification>();
			BrowseFolderCommand = new DelegateCommand( RaiseBrowseFolderCommand );
			OkCommand = new DelegateCommand( OKInteraction );
			CancelCommand = new DelegateCommand( CancelInteraction );
		}

		/// <summary>
		/// [参照] ボタン
		/// </summary>
		private void RaiseBrowseFolderCommand()
		{
			NewProjectNotification notification = _notification as NewProjectNotification;
			var folderNotification = new FolderSelectDialogConfirmation()
			{
				SelectedPath = notification.BaseFolderPath,
				RootFolder = Environment.SpecialFolder.Personal,
				ShowNewFolderButton = false,
			};
			BrowseFolderRequest.Raise( folderNotification );
			if ( folderNotification.Confirmed ) {
				BaseFolderPath.Value = notification.BaseFolderPath = folderNotification.SelectedPath;
			}
		}

		private void OKInteraction()
		{
			NewProjectNotification notification = _notification as NewProjectNotification;
			notification.BaseFolderPath = BaseFolderPath.Value;
			notification.ProjectName = ProjectName.Value;
			notification.ProjectComment = ProjectComment.Value;
			_notification.Confirmed = true;
			FinishInteraction();
		}

		private void CancelInteraction()
		{
			_notification.Confirmed = false;
			FinishInteraction();
		}

		public INotification Notification
		{
			get { return _notification; }
			set {
				SetProperty( ref _notification, (IConfirmation) value );
				NewProjectNotification notification = _notification as NewProjectNotification;
				this.BaseFolderPath.Value = notification.BaseFolderPath;
				this.ProjectName.Value = notification.ProjectName;
				this.ProjectComment.Value = notification.ProjectComment;
			}
		}
	}
}
