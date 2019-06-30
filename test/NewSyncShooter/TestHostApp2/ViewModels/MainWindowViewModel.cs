using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using PrismCommonDialog.Confirmations;
using TestHostApp2.Notifications;
using TestHostApp2.Models;

namespace TestHostApp2.ViewModels
{
	public class MainWindowViewModel : BindableBase, IDisposable
	{
		private CompositeDisposable Disposable { get; } = new CompositeDisposable();

		NewSyncShooter.NewSyncShooter _newSyncShooter;
		List<string> _connectedIPAddressList;
		bool _isCameraPreviwing;
		DispatcherTimer _previewingTimer;

		public ReactiveProperty<string> Title { get; private set; } = new ReactiveProperty<string>( "NewSyncShooter" );
		public ReactiveProperty<string> ProjectName { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> BaseFolderPath { get; set; } = new ReactiveProperty<string>( System.Environment.GetFolderPath( Environment.SpecialFolder.Personal ) );
		public ReactiveProperty<string> ProjectComment { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReadOnlyReactiveProperty<string> ProjectFolderPath { get; }
		public ReactiveProperty<string> ThreeDDataFolderPath { get; set; } = new ReactiveProperty<string>( System.Environment.GetFolderPath( Environment.SpecialFolder.Personal ) );
		public ReactiveProperty<bool> IsCameraConnected { get; private set; } = new ReactiveProperty<bool>( false );
		public ReadOnlyReactiveProperty<bool> IsEnableToCapture { get; }
		public ReactiveProperty<BitmapSource> PreviewingImage { get; private set; } = new ReactiveProperty<BitmapSource>();
		public ReadOnlyReactiveProperty<bool> IsEnableToBuild3D { get; }
		public ReactiveProperty<bool> IsCutPetTable { get; set; } = new ReactiveProperty<bool>( true );
		public ReactiveProperty<bool> IsSkipAlreadyBuilt { get; set; } = new ReactiveProperty<bool>( true );
		public ObservableCollection<FileTreeItem> FileTree { get; } = new ObservableCollection<FileTreeItem>();
		public ObservableCollection<CameraTreeItem> CameraTree { get; } = new ObservableCollection<CameraTreeItem>();

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
		public InteractionRequest<INotification> NetworkSettingRequest { get; set; }
		public DelegateCommand NetworkSettingCommand { get; set; }
		public InteractionRequest<INotification> ThreeDBuildingOneRequest { get; set; }
		public DelegateCommand ThreeDBuildingOneCommand { get; set; }
		public InteractionRequest<INotification> ThreeDBuildingAllRequest { get; set; }
		public DelegateCommand ThreeDBuildingAllCommand { get; set; }
		public DelegateCommand FileViewOpenFolderCommand { get; set; }
		public DelegateCommand CameraViewShowPictureCommand { get; set; }

		public void Dispose()
		{
			this.Disposable.Dispose();
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public MainWindowViewModel()
		{
			_newSyncShooter = new NewSyncShooter.NewSyncShooter();
			_newSyncShooter.Initialize( "syncshooterDefs.json" );
			_connectedIPAddressList = new List<string>();
			_isCameraPreviwing = false;

			this.ProjectFolderPath = this.BaseFolderPath.CombineLatest( this.ProjectName, ( b, p ) => Path.Combine( b, p ) ).ToReadOnlyReactiveProperty();
			this.IsEnableToCapture = this.IsCameraConnected.CombineLatest( this.ProjectName, ( c, p ) => c && !string.IsNullOrEmpty( p ) ).ToReadOnlyReactiveProperty();
			this.IsEnableToBuild3D = this.ProjectName.Select( p => !string.IsNullOrEmpty( p ) ).ToReadOnlyReactiveProperty<bool>();
			//this.IsExpanded = new ReactiveProperty<bool>( true );

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
			NetworkSettingRequest = new InteractionRequest<INotification>();
			NetworkSettingCommand = new DelegateCommand( RaiseNetworkSetting );
			ThreeDBuildingOneRequest = new InteractionRequest<INotification>();
			ThreeDBuildingOneCommand = new DelegateCommand( RaiseThreeDBuildingOne );
			ThreeDBuildingAllRequest = new InteractionRequest<INotification>();
			ThreeDBuildingAllCommand = new DelegateCommand( RaiseThreeDBuildingAll );
			FileViewOpenFolderCommand = new DelegateCommand( RaiseFileViewOpenFolderCommand );
			CameraViewShowPictureCommand = new DelegateCommand( RaiseCameraViewShowPictureCommand );

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
			notification.ProjectName = this.ProjectName.Value;
			notification.BaseFolderPath = this.BaseFolderPath.Value;
			notification.ProjectComment = this.ProjectComment.Value;
			NewProjectRequest.Raise( notification );
			if ( notification.Confirmed ) {
				this.ProjectName.Value = notification.ProjectName;
				this.BaseFolderPath.Value = notification.BaseFolderPath;
				this.ProjectComment.Value = notification.ProjectComment;
				if ( Directory.Exists( this.ProjectFolderPath.Value ) ) {
					SowInformationMessage( this.ProjectFolderPath.Value + "は既に存在しています。新しい名前を指定してください。" );
				} else {
					Directory.CreateDirectory( this.ProjectFolderPath.Value );
					// コメントを出力
					using ( var fs = new FileStream( Path.Combine( this.ProjectFolderPath.Value, "Comment.txt" ), FileMode.CreateNew ) ) {
						using ( var sw = new StreamWriter( fs ) ) {
							sw.WriteLine( this.ProjectComment.Value );
						}
					}
					FileTree.Clear();
					FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
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
				SelectedPath = this.BaseFolderPath.Value,
				RootFolder = Environment.SpecialFolder.Personal,
				ShowNewFolderButton = false
			};
			OpenFolderRequest.Raise( notification );
			if ( notification.Confirmed ) {
				// SelectedPath の最後のディレクトリ名をプロジェクト名に
				// その前までをベースフォルダに
				int lastPos = notification.SelectedPath.LastIndexOf("\\");
				this.BaseFolderPath.Value = notification.SelectedPath.Substring( 0, lastPos + 1 );
				this.ProjectName.Value = notification.SelectedPath.Substring( lastPos + 1 );

				FileTree.Clear();
				FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
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
			string sTargetDir = Path.Combine( this.ProjectFolderPath.Value,
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
				FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
				//IsExpanded.Value = true;

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
					//String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_front.bmp" ) );
					//using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					//	fs.Write( data, 0, (int) data.Length );
					//}
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
					//String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_back.bmp" ) );
					//using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					//	fs.Write( data, 0, (int) data.Length );
					//}
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
					//String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_right.bmp" ) );
					//using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					//	fs.Write( data, 0, (int) data.Length );
					//}
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
					//String path = Path.Combine( _project.ProjectFolderPath, string.Format( @".\preview_left.bmp" ) );
					//using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					//	fs.Write( data, 0, (int) data.Length );
					//}
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
			// TEST
			var notification = new NetworkSettingNotification { Title = "ネットワークインターフェイス選択" };
			NetworkSettingRequest.Raise( notification );
			if ( notification.Confirmed ) {

				if ( NetworkInterface.GetIsNetworkAvailable() ) {
					MessageBox.Show( "ネットワークに接続されています", this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Information );
				} else {
					MessageBox.Show( "ネットワークに接続されていません", this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Warning );
				}
			}
		}

		/// <summary>
		/// 3D作成 - ワンショット
		/// </summary>
		void RaiseThreeDBuildingOne()
		{
			var notification = new ThreeDBuildingNotification
			{
				Title = "3Dモデル作成",
				IsEnableSkipAlreadyBuilt = false,
				ImageFolderPath = this.ProjectFolderPath.Value,	// TODO: プロジェクトフォルダの下の、TreeView上で選択中の画像フォルダ
				Output3DFolderPath = this.ThreeDDataFolderPath.Value,
				IsCutPetTable = this.IsCutPetTable.Value,
				//IsSkipAlreadyBuilt = this.IsSkipAlreadyBuilt.Value,
			};
			ThreeDBuildingOneRequest.Raise( notification );
			if ( !notification.Confirmed ) {
				this.ThreeDDataFolderPath.Value = notification.Output3DFolderPath;
				this.IsCutPetTable.Value = notification.IsCutPetTable;

				ProcessStartInfo startInfo = new ProcessStartInfo();
				// バッチファイルを起動する人は、cmd.exeさんなので
				startInfo.FileName = "cmd.exe";
				// コマンド処理実行後、コマンドウィンドウ終わるようにする。
				//（↓「/c」の後の最後のスペース1文字は重要！）
				startInfo.Arguments = "/c ";
				// コマンド処理であるバッチファイル （ここも最後のスペース重要）
				startInfo.Arguments += @"..\..\UserRibbonButtons\button1.bat ";
				// バッチファイルへの引数 
				var srgString = this.ProjectFolderPath.Value + " " + this.ThreeDDataFolderPath.Value;
				startInfo.Arguments += srgString;
				// ●バッチファイルを別プロセスとして起動
				var proc = Process.Start(startInfo);
				// ●上記バッチ処理が終了するまで待ちます。
				proc.WaitForExit();
			}
		}

		/// <summary>
		/// 3D作成 - 全てのショット
		/// </summary>
		void RaiseThreeDBuildingAll()
		{
			var notification = new ThreeDBuildingNotification
			{
				Title = "3Dモデル作成",
				IsEnableSkipAlreadyBuilt = true,
				ImageFolderPath = this.ProjectFolderPath.Value,
				Output3DFolderPath = this.ThreeDDataFolderPath.Value,
				IsCutPetTable = this.IsCutPetTable.Value,
				IsSkipAlreadyBuilt = this.IsSkipAlreadyBuilt.Value,
			};
			ThreeDBuildingOneRequest.Raise( notification );
			if ( !notification.Confirmed ) {
				this.ThreeDDataFolderPath.Value = notification.Output3DFolderPath;
				this.IsCutPetTable.Value = notification.IsCutPetTable;
				this.IsSkipAlreadyBuilt.Value = notification.IsSkipAlreadyBuilt;
			}
		}

		/// <summary>
		/// ファイルビューのコンテキストメニュー　[フォルダを開く]
		/// </summary>
		void RaiseFileViewOpenFolderCommand()
		{
			var items = this.FileTree.First().Items.SourceCollection;
			foreach ( var item in items ) {
				var treeItem = item as FileTreeItem;
				if (treeItem.IsSelected) {
					ProcessStartInfo startInfo = new ProcessStartInfo();
					startInfo.FileName = "explorer.exe";
					startInfo.Arguments = treeItem._Directory.FullName;
					var proc = Process.Start( startInfo );
					//proc.WaitForExit();
				}
			}
		}

		/// <summary>
		/// カメラビューのコンテキストメニュー [画像表示]
		/// </summary>
		void RaiseCameraViewShowPictureCommand()
		{
			var items = this.CameraTree.First().Items.SourceCollection;
			foreach (var item in items) {
				var treeItem = item as CameraTreeItem;
				if (treeItem.IsSelected) {
					IsCameraPreviwing = false;
					try {
						string sIPAddress = treeItem._ipAddress;
						byte[] data = _newSyncShooter.GetPreviewImage(sIPAddress);
						if ( data.Length > 0 ) {
							ShowPreviewImage( data );
						}
					} catch ( Exception e ) {
						MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
					}
				}
			}
		}
	}
}
