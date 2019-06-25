using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using TestHostApp2.Notifications;
using Ookii.Dialogs.Wpf;

namespace TestHostApp2.ViewModels
{
	public class NewProjectViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public ReactiveProperty<string> BaseFolderPath { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> ProjectName { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> Comment { get; set; } = new ReactiveProperty<string>( string.Empty );

		public DelegateCommand BrowseFolderCommand { get; private set; }
		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		public NewProjectViewModel()
		{
			BrowseFolderCommand = new DelegateCommand( RaiseBrowseFolderCommand );
			OkCommand = new DelegateCommand( OKInteraction );
			CancelCommand = new DelegateCommand( CancelInteraction );
		}

		private void RaiseBrowseFolderCommand()
		{
			NewProjectNotification notification = _notification as NewProjectNotification;

			VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
			dlg.SelectedPath = notification.Project.BaseFolderPath;
			dlg.RootFolder = Environment.SpecialFolder.Personal;
			dlg.ShowNewFolderButton = true;
			var response = dlg.ShowDialog();
			if ( response.HasValue && response.Value ) {
				notification.Project.BaseFolderPath = dlg.SelectedPath;
				BaseFolderPath.Value = notification.Project.BaseFolderPath;
			}
		}

		private void OKInteraction()
		{
			NewProjectNotification notification = _notification as NewProjectNotification;
			notification.Project.BaseFolderPath = BaseFolderPath.Value;
			notification.Project.ProjectName = ProjectName.Value;
			notification.Project.Comment = Comment.Value;
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
				NewProjectNotification notification = _notification as NewProjectNotification;
				BaseFolderPath.Value = notification.Project.BaseFolderPath;
				ProjectName.Value = notification.Project.ProjectName;
				Comment.Value = notification.Project.Comment;
			}
		}
	}
}
