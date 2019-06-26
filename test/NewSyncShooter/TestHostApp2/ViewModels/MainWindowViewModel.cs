using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using PrismCommonDialog;
using PrismCommonDialog.Confirmations;
//using Ookii.Dialogs.Wpf;
using TestHostApp2.Notifications;
using TestHostApp2.Models;

namespace TestHostApp2.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		NewSyncShooter.NewSyncShooter _newSyncShooter;
		List<string> _connectedIPAddressList;
		Project _project;
		bool _isCameraPreviwing;
		DispatcherTimer _previewingTimer;

		public ReactiveProperty<string> Title { get; private set; } = new ReactiveProperty<string>( "NewSyncShooter" );
		public ReactiveProperty<bool> IsCameraConnected { get; private set; } = new ReactiveProperty<bool>( false );
		public ReactiveProperty<BitmapSource> PreviewingImage { get; private set; } = new ReactiveProperty<BitmapSource>();
		public ObservableCollection<FileTreeItem> FileTree { get; } = new ObservableCollection<FileTreeItem>();
		public ObservableCollection<CameraTreeItem> CameraTree { get; } = new ObservableCollection<CameraTreeItem>();
		public ReactiveProperty<bool> IsExpanded { get; set; }

		/// <summary>
		/// カメラプレビュー中か
		/// </summary>
		public bool IsCameraPreviwing
		{
			get { return _isCameraPreviwing; }
			set
			{
				_isCameraPreviwing = value;
				if ( _isCameraPreviwing ) {
					_previewingTimer.Start();
				} else {
					_previewingTimer.Stop();
					this.PreviewingImage.Value = null;
				}
				RaisePropertyChanged();
			}
		}

		/// <summary>情報MessageBoxを表示します。</summary>
		public InteractionRequest<INotification> MessageBoxRequest { get; private set; }

		/// <summary>情報メッセージボックスを表示します。</summary>
		/// <param name="message">メッセージボックスに表示する内容を表す文字列。</param>
		/// <param name="title">メッセージボックスのタイトルを表す文字列。</param>
		private void SowInformationMessage( string message, string title = "Information" )
		{
			var notify = new Notification()
			{
				Content = message,
				Title = title
			};
			this.MessageBoxRequest.Raise( notify );
		}

		public InteractionRequest<INotification> NewProjectRequest { get; set; }
		public DelegateCommand NewProjectCommand { get; set; }
		public InteractionRequest<INotification> OpenFolderRequest { get; set; }
		public DelegateCommand OpenFolderCommand { get; set; }
		public InteractionRequest<INotification> CameraConnectionRequest { get; set; }
		public DelegateCommand CameraConnectionCommand { get; set; }
		public DelegateCommand CameraSettingCommand { get; set; }
		public InteractionRequest<INotification> CameraCapturingRequest { get; set; }
		public DelegateCommand CameraCapturingCommand { get; set; }
		public DelegateCommand CameraStopCommand { get; set; }
		public DelegateCommand CameraRebootCommand { get; set; }
		public DelegateCommand CameraFrontCommand { get; set; }
		public DelegateCommand CameraBackCommand { get; set; }
		public DelegateCommand CameraRightCommand { get; set; }
		public DelegateCommand CameraLeftCommand { get; set; }
		public DelegateCommand NetworkSettingCommand { get; set; }

		public MainWindowViewModel()
		{
			_newSyncShooter = new NewSyncShooter.NewSyncShooter();
			_newSyncShooter.Initialize( "syncshooterDefs.json" );

			_project = new Project();

			_connectedIPAddressList = new List<string>();
			_isCameraPreviwing = false;

			this.IsExpanded = new ReactiveProperty<bool>( true );

			MessageBoxRequest = new InteractionRequest<INotification>();
			NewProjectRequest = new InteractionRequest<INotification>();
			NewProjectCommand = new DelegateCommand( RaiseNewProjectCommand );
			OpenFolderRequest = new InteractionRequest<INotification>();
			OpenFolderCommand = new DelegateCommand( RaiseOpenFolderCommand );
			CameraConnectionRequest = new InteractionRequest<INotification>();
			CameraConnectionCommand = new DelegateCommand( RaiseCameraConnection );
			CameraSettingCommand = new DelegateCommand( RaiseCameraSetting );
			CameraCapturingRequest = new InteractionRequest<INotification>();
			CameraCapturingCommand = new DelegateCommand( RaiseCameraCapturing );
			CameraStopCommand = new DelegateCommand( RaiseCameraStop );
			CameraRebootCommand = new DelegateCommand( RaiseCameraReboot );
			CameraFrontCommand = new DelegateCommand( RaiseCameraFront );
			CameraBackCommand = new DelegateCommand( RaiseCameraBack );
			CameraRightCommand = new DelegateCommand( RaiseCameraRight );
			CameraLeftCommand = new DelegateCommand( RaiseCameraLeft );
			NetworkSettingCommand = new DelegateCommand( RaiseNetworkSetting );

			// Previewing timer を 500msecでセット
			_previewingTimer = new DispatcherTimer( DispatcherPriority.Render );
			_previewingTimer.Interval = TimeSpan.FromMilliseconds( 500 );
			_previewingTimer.Tick += ( sender, args ) =>
			{
				try {
					byte[] data = _newSyncShooter.GetPreviewImageFront();
					if ( data.Length > 0 ) {
						ShowPreviewImage( data );
					}
				} catch ( Exception ) {
					//MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
				}
			};
		}

		/// <summary>
		/// 新規プロジェクト作成ダイアログを開く
		/// </summary>
		void RaiseNewProjectCommand()
		{
			var notification = new NewProjectNotification { Title = "New Project" };
			notification.Project = _project;
			NewProjectRequest.Raise( notification );
			if ( notification.Confirmed ) {
				_project = notification.Project;
				if ( Directory.Exists( _project.ProjectFolderPath ) ) {
					SowInformationMessage( _project.ProjectFolderPath + "は既に存在しています。新しい名前を指定してください。" );
				} else {
					Directory.CreateDirectory( _project.ProjectFolderPath );
					// コメントを出力
					using ( var fs = new FileStream( Path.Combine( _project.ProjectFolderPath, "Comment.txt" ), FileMode.CreateNew ) ) {
						using ( var sw = new StreamWriter( fs ) ) {
							sw.WriteLine( _project.Comment );
						}
					}
					FileTree.Clear();
					FileTree.Add( new FileTreeItem( _project.ProjectFolderPath ) );
					IsExpanded.Value = true;
				}
			}
		}

		/// <summary>
		/// プロジェクトを開く
		/// </summary>
		void RaiseOpenFolderCommand()
		{
			var notification = new FolderSelectDialogConfirmation()
			{
				Title = "Open Project",
				SelectedPath = _project.BaseFolderPath,
				RootFolder = Environment.SpecialFolder.Personal,
				ShowNewFolderButton = false
			};
			OpenFolderRequest.Raise( notification );
			if ( notification.Confirmed ) {
				// SelectedPath の最後のディレクトリ名をプロジェクト名に
				// その前までをベースフォルダに
				int lastPos = notification.SelectedPath.LastIndexOf("\\");
				_project.ProjectName = notification.SelectedPath.Substring( lastPos + 1 );
				_project.BaseFolderPath = notification.SelectedPath.Substring( 0, lastPos + 1 );

				FileTree.Clear();
				FileTree.Add( new FileTreeItem( _project.ProjectFolderPath ) );
				IsExpanded.Value = true;
			}
		}

		/// <summary>
		/// カメラ検索
		/// </summary>
		void RaiseCameraConnection()
		{
			var notification = new CameraConnectionNotification { Title = "Camera Connection" };
			_connectedIPAddressList.Clear();
			_newSyncShooter.ConnectCamera().ToList().ForEach( adrs =>
			{
				_connectedIPAddressList.Add( adrs );
				notification.ConnectedItems.Add( adrs );
			} );
			// SyncshooterDefs にあるIP Addressの中に接続できたカメラがない場合は、そのアドレスの一覧を表示する
			var allList = _newSyncShooter.GetSyncshooterDefs().GetAllCameraIPAddress();
			var exceptList = allList.Except( _connectedIPAddressList ).ToList();
			exceptList.ForEach( adrs => notification.NotConnectedItems.Add( adrs ) );
			CameraConnectionRequest.Raise( notification );
			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );

			CameraTree.Clear();
			CameraTree.Add( new CameraTreeItem( _connectedIPAddressList ) );
			//CameraTree.Add( new CameraTreeItem( notification.NotConnectedItems.ToList() ) );
		}

		/// <summary>
		/// カメラ設定
		/// </summary>
		void RaiseCameraSetting()
		{
			IsCameraPreviwing = false;
			// TODO: 仕様を確認すること
			_connectedIPAddressList.ToList().ForEach( adrs =>
			{
				var param = _newSyncShooter.GetCameraParam( adrs );
				param.Orientation = 1;
				_newSyncShooter.SetCameraParam( adrs, param );
			} );
		}

		/// <summary>
		/// 撮影
		/// </summary>
		void RaiseCameraCapturing()
		{
			var notification = new CameraCapturingNotification { Title = "撮影番号設定" };
			CameraCapturingRequest.Raise( notification );
			if ( !notification.Confirmed ) {
				return;
			}

			// 撮影フォルダ名：
			// プロジェクトのフォルダ名＋現在の年月日時分秒＋撮影番号からなるフォルダ名のフォルダに画像を保存する
			string sTargetDir = Path.Combine( _project.ProjectFolderPath,
				DateTime.Now.ToString("yyyyMMdd-HHmmss") + string.Format("({0})", notification.CapturingName) );
			try {
				Directory.CreateDirectory( sTargetDir );

				IsCameraPreviwing = false;
				var t = DateTime.Now;
				_connectedIPAddressList.AsParallel().ForAll( adrs =>
				{
					// 画像を撮影＆取得
					byte[] data = _newSyncShooter.GetFullImageInJpeg( adrs );
					// IP Address の第4オクテットのファイル名で保存する
					int idx = adrs.LastIndexOf('.');
					int adrs4th = int.Parse(adrs.Substring( idx  + 1 ));
					String path = Path.Combine( sTargetDir, string.Format( "{0}.jpg", adrs4th ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				} );

				// 「ファイルビュー」表示を更新
				FileTree.Clear();
				FileTree.Add( new FileTreeItem( _project.ProjectFolderPath ) );
				IsExpanded.Value = true;

				TimeSpan ts = DateTime.Now - t;
				SowInformationMessage( sTargetDir + "\n\nElapsed: " + ts.ToString( "s\\.fff" ) + " sec" );

			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void RaiseCameraStop()
		{
			IsCameraPreviwing = false;
			_newSyncShooter.StopCamera( false );
			_connectedIPAddressList.Clear();
			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );
		}

		void RaiseCameraReboot()
		{
			IsCameraPreviwing = false;
			_newSyncShooter.StopCamera( true );
			_connectedIPAddressList.Clear();
			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );
		}

		void RaiseCameraFront()
		{
			IsCameraPreviwing = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageFront();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_front.bmp" ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				}
			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void RaiseCameraBack()
		{
			IsCameraPreviwing = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageBack();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_back.bmp" ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				}
			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void RaiseCameraRight()
		{
			IsCameraPreviwing = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageRight();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_right.bmp" ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				}
			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void RaiseCameraLeft()
		{
			IsCameraPreviwing = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageLeft();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_left.bmp" ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				}
			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void ShowPreviewImage( byte[] data )
		{
			var ms = new MemoryStream( data );
			BitmapSource bitmapSource = BitmapFrame.Create( ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad );
			this.PreviewingImage.Value = bitmapSource;
		}

		void RaiseNetworkSetting()
		{
			if ( NetworkInterface.GetIsNetworkAvailable() ) {
				MessageBox.Show( "ネットワークに接続されています", this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Information );
			} else {
				MessageBox.Show( "ネットワークに接続されていません", this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Warning );
			}
		}
	}
}
