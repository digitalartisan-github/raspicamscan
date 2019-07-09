using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using PrismCommonDialog.Confirmations;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class MainWindowViewModel : BindableBase, IDisposable
	{
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		private readonly string _registryBaseKey = @"Software\DiGITAL ARTISAN";
		private readonly Models.Project _project = new Models.Project();
		private NewSyncShooter.NewSyncShooter _newSyncShooter;
		private DispatcherTimer _previewingTimer;

		#region Properties
		private readonly ObservableCollection<string> ConnectedIPAddressList;
		public ReactiveProperty<string> Title { get; } = new ReactiveProperty<string>( "NewSyncShooter" );
		public ReactiveProperty<string> ProjectName { get; }
		public ReactiveProperty<string> BaseFolderPath { get; }
		public ReactiveProperty<string> ProjectComment { get; }
		public ReactiveProperty<string> ThreeDDataFolderPath { get; }
		public ReactiveProperty<bool> IsCutPetTable { get; }
		public ReactiveProperty<bool> IsSkipAlreadyBuilt { get; }
		public ReadOnlyReactiveProperty<string> ProjectFolderPath { get; }
		public ReadOnlyReactiveProperty<bool> IsCameraConnected { get; }
		public ReactiveProperty<bool> IsCameraPreviewing { get; } = new ReactiveProperty<bool>();
		public ReactiveProperty<BitmapSource> PreviewingImage { get; } = new ReactiveProperty<BitmapSource>();
		public ObservableCollection<FileTreeItem> FileTree { get; } = new ObservableCollection<FileTreeItem>();
		public ObservableCollection<CameraTreeItem> CameraTree { get; } = new ObservableCollection<CameraTreeItem>();
		#endregion

		#region Window Requests
		public InteractionRequest<INotification> OpenMessageBoxRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> NewProjectRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> OpenFolderRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> CameraConnectionRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> CameraCapturingRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> ImageTransferingRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> NetworkSettingRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> ThreeDBuildingOneRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> ThreeDBuildingAllRequest { get; } = new InteractionRequest<INotification>();
		#endregion

		#region Commands
		public ReactiveCommand NewProjectCommand { get; }
		public ReactiveCommand OpenFolderCommand { get; }
		public ReactiveCommand CameraConnectionCommand { get; }
		public ReactiveCommand CameraSettingCommand { get; }
		public ReactiveCommand CameraCapturingCommand { get; }
		//public ReactiveCommand ImageTransferingCommand { get; }
		public ReactiveCommand CameraStopCommand { get; }
		public ReactiveCommand CameraRebootCommand { get; }
		public ReactiveCommand CameraFrontCommand { get; }
		public ReactiveCommand CameraBackCommand { get; }
		public ReactiveCommand CameraRightCommand { get; }
		public ReactiveCommand CameraLeftCommand { get; }
		public ReactiveCommand NetworkSettingCommand { get; }
		public ReactiveCommand ThreeDBuildingOneCommand { get; }
		public ReactiveCommand ThreeDBuildingAllCommand { get; }
		public ReactiveCommand ThreeDDataFolderOpeningCommand { get; }
		public ReactiveCommand FileViewOpenFolderCommand { get; }
		public ReactiveCommand FileViewDeleteFolderCommand { get; }
		public ReactiveCommand CameraViewShowPictureCommand { get; }
		#endregion

		public void Dispose()
		{
			_disposable?.Dispose();
		}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public MainWindowViewModel()
		{
			// レジストリからプロジェクト情報を復元する
			_project.Load( Path.Combine( _registryBaseKey, this.Title.Value ) );

			_newSyncShooter = new NewSyncShooter.NewSyncShooter();
			_newSyncShooter.Initialize( "syncshooterDefs.json" );

			// Previewing timer を 500msecでセット
			_previewingTimer = new DispatcherTimer( DispatcherPriority.Render );
			_previewingTimer.Interval = TimeSpan.FromMilliseconds( 500 );
			_previewingTimer.Tick += ( sender, args ) => {
				try {
					byte[] data = _newSyncShooter.GetPreviewImageFront();
					if ( data.Length > 0 ) {
						ShowPreviewImage( data );
					}
				} catch ( Exception e ) {
					Console.WriteLine( e.InnerException.Message );
				}
			};

			this.ConnectedIPAddressList = new ReactiveCollection<string>();
			this.ConnectedIPAddressList.PropertyChangedAsObservable().Subscribe( item => {
				CameraTree.Clear();
				CameraTree.Add( new CameraTreeItem( ConnectedIPAddressList ) );
			} );

			// Models.Project のプロパティと双方向で同期する
			this.ProjectName = _project.ProjectName.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );
			this.BaseFolderPath = _project.BaseFolderPath.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );
			this.ProjectComment = _project.Comment.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );
			this.ThreeDDataFolderPath = _project.ThreeDDataFolderPath.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );
			this.IsCutPetTable = _project.IsCutPetTable.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );
			this.IsSkipAlreadyBuilt = _project.IsSkipAlreadyBuilt.ToReactivePropertyAsSynchronized( val => val.Value ).AddTo( _disposable );

			// プロジェクトのフォルダパス名は、ベースフォルダ名とプロジェクト名と同期する
			this.ProjectFolderPath = this.BaseFolderPath.CombineLatest( this.ProjectName, ( b, p ) =>
				( !string.IsNullOrEmpty( b ) && !string.IsNullOrEmpty( p ) ) ? Path.Combine( b, p ) : string.Empty ).ToReadOnlyReactiveProperty();
			// ここでプロジェクトフォルダパスが有効であれば、ファイルビューのツリーを更新する
			if ( !string.IsNullOrEmpty( this.ProjectFolderPath.Value ) ) {
				FileTree.Clear();
				FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
			}

			// カメラの接続状態を示すプロパティは、接続しているIPアドレスのリストと同期する
			this.IsCameraConnected = this.ConnectedIPAddressList.CollectionChangedAsObservable().Select( e => {
				switch ( e.Action ) {
				case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
				default:
					return false;
				case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
					return e.NewItems.Count > 0;
				}
			} ).ToReadOnlyReactiveProperty();

			this.IsCameraPreviewing.Subscribe( val => {
				if ( val ) {
					_previewingTimer.Start();
				} else {
					_previewingTimer.Stop();
					this.PreviewingImage.Value = null;
				}
			} );

			// Commands
			NewProjectCommand = new ReactiveCommand();
			NewProjectCommand.Subscribe( RaiseNewProjectCommand );
			OpenFolderCommand = new ReactiveCommand();
			OpenFolderCommand.Subscribe( RaiseOpenFolderCommand );
			CameraConnectionCommand = new ReactiveCommand();
			CameraConnectionCommand.Subscribe( RaiseCameraConnection );
			CameraSettingCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraSettingCommand.Subscribe( RaiseCameraSetting );
			CameraCapturingCommand = this.IsCameraConnected.CombineLatest( this.ProjectName, ( c, p ) => c && !string.IsNullOrEmpty( p ) ).ToReactiveCommand();
			CameraCapturingCommand.Subscribe( RaiseCameraCapturing );
			CameraStopCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraStopCommand.Subscribe( RaiseCameraStop );
			CameraRebootCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraRebootCommand.Subscribe( RaiseCameraReboot );
			CameraFrontCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraFrontCommand.Subscribe( RaiseCameraFront );
			CameraBackCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraBackCommand.Subscribe( RaiseCameraBack );
			CameraRightCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraRightCommand.Subscribe( RaiseCameraRight );
			CameraLeftCommand = this.IsCameraConnected.ToReactiveCommand();
			CameraLeftCommand.Subscribe( RaiseCameraLeft );
			NetworkSettingCommand = new ReactiveCommand();
			NetworkSettingCommand.Subscribe( RaiseNetworkSetting );
			ThreeDBuildingOneCommand = this.ProjectName.Select( p => !string.IsNullOrEmpty( p ) ).ToReactiveCommand();
			ThreeDBuildingOneCommand.Subscribe( RaiseThreeDBuildingOne );
			ThreeDBuildingAllCommand = this.ProjectName.Select( p => !string.IsNullOrEmpty( p ) ).ToReactiveCommand();
			ThreeDBuildingAllCommand.Subscribe( RaiseThreeDBuildingAll );
			ThreeDDataFolderOpeningCommand = new[] { this.ProjectName, this.ThreeDDataFolderPath }.CombineLatest( x => x.All( y => !string.IsNullOrEmpty( y ) ) ).ToReactiveCommand();
			ThreeDDataFolderOpeningCommand.Subscribe( RaiseThreeDDataFolderOpening );
			FileViewOpenFolderCommand = new ReactiveCommand();
			FileViewOpenFolderCommand.Subscribe( RaiseFileViewOpenFolderCommand );
			FileViewDeleteFolderCommand = new ReactiveCommand();
			FileViewDeleteFolderCommand.Subscribe( RaiseFileViewDeleteFolderCommand );
			CameraViewShowPictureCommand = new ReactiveCommand();
			CameraViewShowPictureCommand.Subscribe( RaiseCameraViewShowPictureCommand );
		}

		~MainWindowViewModel()
		{
			// レジストリにプロジェクト情報を書き込む
			_project.Save( Path.Combine( _registryBaseKey, this.Title.Value ) );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="icon"></param>
		/// <param name="button"></param>
		/// <param name="defaultButton"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private MessageBoxResult OpenMessageBox( string title, MessageBoxImage icon, MessageBoxButton button, MessageBoxResult defaultButton, string message )
		{
			var notification = new MessageBoxNotification()
			{
				Title = title,
				Message = message,
				Button = button,
				Image = icon,
				DefaultButton = defaultButton
			};
			OpenMessageBoxRequest.Raise( notification );

			return notification.Result;
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
					OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK,
						this.ProjectFolderPath.Value + "は既に存在しています。新しい名前を指定してください。" );
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
				InitialDirectory = this.BaseFolderPath.Value,
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
			ConnectedIPAddressList.Clear();
			_newSyncShooter.ConnectCamera().ToList().ForEach( adrs => {
				ConnectedIPAddressList.Add( adrs );
				notification.ConnectedItems.Add( adrs );
			} );

			// SyncshooterDefs にあるIP Addressの中に接続できたカメラがない場合は、そのアドレスの一覧を作る
			var allList = _newSyncShooter.GetSyncshooterDefs().GetAllCameraIPAddress();
			var exceptList = allList.Except( ConnectedIPAddressList ).ToList();
			exceptList.ForEach( adrs => notification.NotConnectedItems.Add( adrs ) );

			CameraConnectionRequest.Raise( notification );
		}

		/// <summary>
		/// カメラ設定
		/// </summary>
		void RaiseCameraSetting()
		{
			IsCameraPreviewing.Value = false;
			// TODO: 仕様を確認すること
			ConnectedIPAddressList.ToList().ForEach( adrs => {
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
			// プレビュー中なら停止する
			IsCameraPreviewing.Value = false;

			try {
				// 撮影フォルダ名：
				// プロジェクトのフォルダ名＋現在の年月日時分秒＋撮影番号からなるフォルダ名のフォルダに画像を保存する
				string sTargetName = DateTime.Now.ToString("yyyyMMdd-HHmmss");
				if (! string.IsNullOrEmpty( notification.CapturingName )) {
					sTargetName += string.Format( "({0})", notification.CapturingName );
				}
				string sTargetDir = Path.Combine( this.ProjectFolderPath.Value, sTargetName );
				Directory.CreateDirectory( sTargetDir );

				//// 正面カメラの画像を取得する
				//byte[] imaegData = _newSyncShooter.GetPreviewImageFront();
				//if ( imaegData.Length == 0 ) {
				//	OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK,
				//		"正面カメラの画像を取得できませんでした。" );
				//	return;
				//}
				//var ms = new MemoryStream( imaegData );
				//BitmapSource bitmapSource = BitmapFrame.Create( ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad );

				// カメラ画像転送ダイアログを開く
				var notification2 = new ImagTransferingNotification()
				{
					Title = "カメラ画像転送",
					SyncShooter = _newSyncShooter,
					ConnectedIPAddressList = ConnectedIPAddressList,
					TargetDir = sTargetDir,
					//Image = bitmapSource,
				};
				var t = DateTime.Now;

				this.ImageTransferingRequest.Raise( notification2 );

				TimeSpan ts = DateTime.Now - t;
				OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK,
					"Elapsed : " + ts.ToString( "s\\.fff" ) + " sec" );

				// 「ファイルビュー」表示を更新
				FileTree.Clear();
				FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
				
			} catch ( Exception e ) {
				OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
			}
		}

		/// <summary>
		/// カメラ停止
		/// </summary>
		void RaiseCameraStop()
		{
			IsCameraPreviewing.Value = false;
			_newSyncShooter.StopCamera( false );
			ConnectedIPAddressList.Clear();
		}

		/// <summary>
		/// カメラ再起動
		/// </summary>
		void RaiseCameraReboot()
		{
			IsCameraPreviewing.Value = false;
			_newSyncShooter.StopCamera( true );
			ConnectedIPAddressList.Clear();
		}

		void RaiseCameraFront()
		{
			IsCameraPreviewing.Value = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageFront();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
				}
			} catch ( Exception e ) {
				OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
			}
		}

		void RaiseCameraBack()
		{
			IsCameraPreviewing.Value = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageBack();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
				}
			} catch ( Exception e ) {
				OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
			}
		}

		void RaiseCameraRight()
		{
			IsCameraPreviewing.Value = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageRight();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
				}
			} catch ( Exception e ) {
				OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
			}
		}

		void RaiseCameraLeft()
		{
			IsCameraPreviewing.Value = false;
			try {
				byte[] data = _newSyncShooter.GetPreviewImageLeft();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
				}
			} catch ( Exception e ) {
				OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
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
					OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK, "ネットワークに接続されています" );
				} else {
					OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK, "ネットワークに接続されていません" );
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
				ImageFolderPath = this.ProjectFolderPath.Value,	// TODO: プロジェクトフォルダの下の、TreeView上で選択中の画像フォルダ
				ThreeDDataFolderPath = this.ThreeDDataFolderPath.Value,
				IsCutPetTable = this.IsCutPetTable.Value,
				IsEnableSkipAlreadyBuilt = false,
			};
			ThreeDBuildingOneRequest.Raise( notification );
			if ( notification.Confirmed ) {
				this.ThreeDDataFolderPath.Value = notification.ThreeDDataFolderPath;
				this.IsCutPetTable.Value = notification.IsCutPetTable;

				var startInfo = new ProcessStartInfo();
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
				ImageFolderPath = this.ProjectFolderPath.Value,
				ThreeDDataFolderPath = this.ThreeDDataFolderPath.Value,
				IsCutPetTable = this.IsCutPetTable.Value,
				IsSkipAlreadyBuilt = this.IsSkipAlreadyBuilt.Value,
				IsEnableSkipAlreadyBuilt = true,
			};
			ThreeDBuildingAllRequest.Raise( notification );
			if ( notification.Confirmed ) {
				this.ThreeDDataFolderPath.Value = notification.ThreeDDataFolderPath;
				this.IsCutPetTable.Value = notification.IsCutPetTable;
				this.IsSkipAlreadyBuilt.Value = notification.IsSkipAlreadyBuilt;

				var startInfo = new ProcessStartInfo();
				startInfo.FileName = "cmd.exe";
				startInfo.Arguments = "/c ";
				startInfo.Arguments += @"..\..\UserRibbonButtons\button2.bat ";
				var srgString = this.ProjectFolderPath.Value + " " + this.ThreeDDataFolderPath.Value;
				startInfo.Arguments += srgString;
				var proc = Process.Start(startInfo);
				proc.WaitForExit();
			}
		}

		/// <summary>
		/// 3D - データフォルダを開く
		/// </summary>
		void RaiseThreeDDataFolderOpening()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = this.ThreeDDataFolderPath.Value,
			};
			var proc = Process.Start( startInfo );
			proc.WaitForExit();
		}

		/// <summary>
		/// ファイルビューのコンテキストメニュー　[フォルダを開く]
		/// </summary>
		void RaiseFileViewOpenFolderCommand()
		{
			var items = this.FileTree.First().Items.SourceCollection;
			foreach ( var item in items ) {
				var treeItem = item as FileTreeItem;
				if ( treeItem.IsSelected ) {
					var startInfo = new ProcessStartInfo
					{
						FileName = "explorer.exe",
						Arguments = treeItem._Directory.FullName,
					};
					var proc = Process.Start( startInfo );
					//proc.WaitForExit();
				}
			}
		}

	/// <summary>
		/// ファイルビューのコンテキストメニュー　[フォルダを削除]
		/// </summary>
		void RaiseFileViewDeleteFolderCommand()
		{
			var items = this.FileTree.First().Items.SourceCollection;
			foreach ( var item in items ) {
				var treeItem = item as FileTreeItem;
				if ( treeItem.IsSelected ) {
					var path = treeItem._Directory.FullName;
					if ( OpenMessageBox( this.Title.Value, MessageBoxImage.Question, MessageBoxButton.OKCancel, MessageBoxResult.None,
						string.Format( "{0} を削除してもよろしいですか？", path ) ) == MessageBoxResult.OK ) {
						try {
							Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory( path,
								Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
								Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
								Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing );
						} catch ( Exception e ) {
							OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
						}
						FileTree.Clear();
						FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );
					}
				}
			}
		}

		/// <summary>
		/// カメラビューのコンテキストメニュー [画像表示]
		/// </summary>
		void RaiseCameraViewShowPictureCommand()
		{
			var items = this.CameraTree.First().Items.SourceCollection;
			foreach ( var item in items ) {
				var treeItem = item as CameraSubTreeItem;
				if ( treeItem.IsSelected ) {
					IsCameraPreviewing.Value = false;
					try {
						string sIPAddress = treeItem._ipAddress;
						byte[] data = _newSyncShooter.GetPreviewImage(sIPAddress);
						if ( data.Length > 0 ) {
							ShowPreviewImage( data );
						}
					} catch ( Exception e ) {
						OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
					}
				}
			}
		}
	}
}
