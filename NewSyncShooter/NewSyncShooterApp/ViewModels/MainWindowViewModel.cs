using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using PrismCommonDialog.Confirmations;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        private readonly string _registryBaseKey = @"Software\DiGITAL ARTISAN";
        private readonly Models.Project _project = new Models.Project();
        private NewSyncShooter.NewSyncShooter _newSyncShooter;
        private string _localHostIP = "0.0.0.0";
        private DispatcherTimer _previewingTimer;
        private static readonly string _threeDBuildingApp = "RealityCapture.exe";
        private readonly List<Task> _runRCTaskList = new List<Task>();

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
        public ReactiveProperty<bool> IsAutoModeSelected { get; } = new ReactiveProperty<bool>();
        #endregion

        #region Window Requests
        public InteractionRequest<INotification> OpenMessageBoxRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> NewProjectRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> OpenFolderRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> CameraConnectionRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> CameraSettingRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> CameraCapturingRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> ImageTransferingRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> NetworkSettingRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> ThreeDBuildingOneRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> ThreeDBuildingAllRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> AutoModeRequest { get; } = new InteractionRequest<INotification>();
        #endregion

        #region Commands
        public ReactiveCommand ClosingCommand { get; }
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
        public ReactiveCommand ThreeDBuildingStopCommand { get; }
        public ReactiveCommand ThreeDDataFolderOpeningCommand { get; }
        public ReactiveCommand FileViewOpenFolderCommand { get; }
        public ReactiveCommand FileViewDeleteFolderCommand { get; }
        public ReactiveCommand<FileTreeItem> FileViewSelectedItemChanged { get; }
        public ReactiveCommand CameraViewShowPictureCommand { get; }
        public ReactiveCommand AutoModeCommand { get; }
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
            string sBaseRegKey = Path.Combine( _registryBaseKey, this.Title.Value );
            _project.Load( sBaseRegKey );
            // その他環境変数
            var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( Path.Combine( sBaseRegKey, "Environment" ) );
            _localHostIP = regkey.GetValue( "LocalHostIP", _localHostIP ) as string;


            _newSyncShooter = new NewSyncShooter.NewSyncShooter();
            _newSyncShooter.Initialize( "syncshooterDefs.json" );

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
                } catch ( Exception e ) {
                    Console.WriteLine( e.InnerException.Message );
                }
            };

            this.ConnectedIPAddressList = new ReactiveCollection<string>();
            this.ConnectedIPAddressList.PropertyChangedAsObservable().Subscribe( item =>
            {
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
            this.IsCameraConnected = this.ConnectedIPAddressList.CollectionChangedAsObservable().Select( e =>
            {
                switch ( e.Action ) {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    return false;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    return e.NewItems.Count > 0;
                }
            } ).ToReadOnlyReactiveProperty();

            // プレビュー開始／終了
            this.IsCameraPreviewing.Subscribe( val =>
            {
                if ( val ) {
                    _previewingTimer.Start();
                } else {
                    _previewingTimer.Stop();
                    this.PreviewingImage.Value = null;
                }
            } );

            // 自動モードウインドウを開く
            this.IsAutoModeSelected.Subscribe( val =>
            {
                if ( val ) {
                    // 自動モードウインドウを開く
                    this.RaiseAutoMode();
                    //this.IsAutoModeSelected.Value = false;
                }
            } );

            // Commands
            ClosingCommand = new ReactiveCommand();
            ClosingCommand.Subscribe( RaiseCloingCommand );
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
            ThreeDBuildingStopCommand = this.ProjectName.Select( p => !string.IsNullOrEmpty( p ) ).ToReactiveCommand();
            ThreeDBuildingStopCommand.Subscribe( RaiseThreeDBuildingStop );
            ThreeDDataFolderOpeningCommand = new[] { this.ProjectName, this.ThreeDDataFolderPath }.CombineLatest( x => x.All( y => !string.IsNullOrEmpty( y ) ) ).ToReactiveCommand();
            ThreeDDataFolderOpeningCommand.Subscribe( RaiseThreeDDataFolderOpening );
            FileViewOpenFolderCommand = new ReactiveCommand();
            FileViewOpenFolderCommand.Subscribe( RaiseFileViewOpenFolderCommand );
            FileViewDeleteFolderCommand = new ReactiveCommand();
            FileViewDeleteFolderCommand.Subscribe( RaiseFileViewDeleteFolderCommand );
            FileViewSelectedItemChanged = new ReactiveCommand<FileTreeItem>();
            FileViewSelectedItemChanged.Subscribe( RaiseFileViewSelectedItemChanged );
            CameraViewShowPictureCommand = new ReactiveCommand();
            CameraViewShowPictureCommand.Subscribe( RaiseCameraViewShowPictureCommand );
            AutoModeCommand = this.IsCameraConnected.CombineLatest( this.ProjectName, ( c, p ) => c && !string.IsNullOrEmpty( p ) ).ToReactiveCommand();
            AutoModeCommand.Subscribe( RaiseAutoMode );
        }

        ~MainWindowViewModel()
        {
            string sBaseRegKey = Path.Combine( _registryBaseKey, this.Title.Value );
            // レジストリにプロジェクト情報を書き込む
            _project.Save( sBaseRegKey );
            // その他環境変数
            var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( Path.Combine( sBaseRegKey, "Environment" ) );
            regkey.SetValue( "LocalHostIP", _localHostIP );

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

        void RaiseCloingCommand()
        {
            if ( _runRCTaskList.Count > 0) {
                OpenMessageBox( this.Title.Value, MessageBoxImage.Warning, MessageBoxButton.OK, MessageBoxResult.OK,
                        "Reality Capture はまだ実行中です。" );
            }
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
            _newSyncShooter.ConnectCamera( _localHostIP ).ToList().ForEach( adrs =>
            {
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
            // プレビューを停止
            IsCameraPreviewing.Value = false;

            if ( this.CameraTree.First().Items.Count == 0 ) {
                return;
            }
            // カメラビュー上で選択されているアドレスを取得する
            CameraSubTreeItem selectedItem = this.CameraTree.First().Items[0] as CameraSubTreeItem;
            for ( int i = 0; i < this.CameraTree.First().Items.Count; ++i ) {
                var item = this.CameraTree.First().Items[i] as CameraSubTreeItem;
                if ( item.IsSelected ) {
                    selectedItem = item;
                    break;
                }
            }
            var selectedIPAddress = selectedItem._ipAddress;
            // そのアドレスのカメラのカメラパラメータを取得する
            var selectedCameraParam = _newSyncShooter.GetCameraParam( selectedIPAddress );
            if ( selectedCameraParam == null ) {
                return;
            }
            selectedCameraParam.Serialize( selectedIPAddress + ".json" );

            var notification = new CameraSettingNotification()
            {
                Title = "カメラパラメータ",
                IPAddress = selectedIPAddress,
                CameraParameter = selectedCameraParam,
                ApplyOne = param => _newSyncShooter.SetCameraParam( selectedIPAddress, param ),
                ApplyAll = param => this.ConnectedIPAddressList.AsParallel().ForAll( adrs => _newSyncShooter.SetCameraParam( adrs, param ) ),
            };
            CameraSettingRequest.Raise( notification );
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
                if ( !string.IsNullOrEmpty( notification.CapturingName ) ) {
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
                    LocalHostIP = _localHostIP,
                    TargetDir = sTargetDir,
					//Image = bitmapSource,
				};
                var t = DateTime.Now;

                this.ImageTransferingRequest.Raise( notification2 );

                if ( notification2.Confirmed ) {
                    TimeSpan ts = DateTime.Now - t;
                    OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK,
                        "画像転送完了\n\nElapsed : " + ts.ToString( "s\\.fff" ) + " sec" );
                }

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
            if ( OpenMessageBox( this.Title.Value, MessageBoxImage.Question, MessageBoxButton.YesNo, MessageBoxResult.No,
                "カメラを停止します。よろしいですか？" ) == MessageBoxResult.Yes ) {
                IsCameraPreviewing.Value = false;
                _newSyncShooter.StopCamera( _localHostIP, false );
                ConnectedIPAddressList.Clear();
            }
        }

        /// <summary>
        /// カメラ再起動
        /// </summary>
        void RaiseCameraReboot()
        {
            if ( OpenMessageBox( this.Title.Value, MessageBoxImage.Question, MessageBoxButton.YesNo, MessageBoxResult.No,
                "カメラを再起動します。よろしいですか？" ) == MessageBoxResult.Yes ) {
                IsCameraPreviewing.Value = false;
                _newSyncShooter.StopCamera( _localHostIP, true );
                ConnectedIPAddressList.Clear();
            }
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
            using ( var ms = new MemoryStream( data ) ) {
                BitmapSource bitmapSource = BitmapFrame.Create( ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad );
                this.PreviewingImage.Value = new TransformedBitmap( bitmapSource, new RotateTransform( 270 ) );
            }
        }

        void RaiseNetworkSetting()
        {
            var notification = new NetworkSettingNotification
            {
                Title = "ネットワークインターフェイス選択",
                LocalHostIP = _localHostIP,
            };
            NetworkSettingRequest.Raise( notification );
            if ( notification.Confirmed ) {
                _localHostIP = notification.LocalHostIP;
                //if ( NetworkInterface.GetIsNetworkAvailable() ) {
                //	OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK, "ネットワークに接続されています" );
                //} else {
                //	OpenMessageBox( this.Title.Value, MessageBoxImage.Information, MessageBoxButton.OK, MessageBoxResult.OK, "ネットワークに接続されていません" );
                //}
            }
        }

        /// <summary>
        /// 3D作成 - ワンショット
        /// </summary>
        void RaiseThreeDBuildingOne()
        {
            if ( this.FileTree.First().Items.IsEmpty ) {
                return;
            }
            // ファイルビューで選択中のフォルダ、または選択されているフォルダがなければ最後のフォルダを選択する
            var items = new FileTreeItem[this.FileTree.First().Items.Count];
            this.FileTree.First().Items.CopyTo( items, 0 );
            var treeItem = items.FirstOrDefault( x => x.IsSelected );
            if ( treeItem == null ) {
                treeItem = items.Last();
            }
            var notification = new ThreeDBuildingNotification
            {
                Title = "3Dモデル作成",
                ImageFolderPath = treeItem._Directory.FullName,// プロジェクトフォルダの下の、TreeView上で選択中の画像フォルダ
				ThreeDDataFolderPath = this.ThreeDDataFolderPath.Value,
                IsCutPetTable = this.IsCutPetTable.Value,
                IsEnableSkipAlreadyBuilt = false,
            };
            ThreeDBuildingOneRequest.Raise( notification );
            if ( notification.Confirmed ) {
                this.ThreeDDataFolderPath.Value = notification.ThreeDDataFolderPath;
                this.IsCutPetTable.Value = notification.IsCutPetTable;

                if ( Directory.Exists( this.ThreeDDataFolderPath.Value ) == false ) {
                    if ( OpenMessageBox( this.Title.Value, MessageBoxImage.Question, MessageBoxButton.YesNo, MessageBoxResult.Yes,
                        "フォルダ\n" + this.ThreeDDataFolderPath.Value + " は存在しません。\n作成しますか？" ) == MessageBoxResult.Yes ) {
                        try {
                            Directory.CreateDirectory( this.ThreeDDataFolderPath.Value );
                        } catch ( Exception e ) {
                            OpenMessageBox( this.Title.Value, MessageBoxImage.Stop, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
                            return;
                        }
                    } else {
                        return;
                    }
                }

                var startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.WorkingDirectory = @".\UserRibbonButtons";
                //startInfo.Arguments = "/k ";    // <- 本番は "/c"
                startInfo.Arguments = "/c ";    // <- 本番は "/c"
                                                //startInfo.Arguments += @"C:\DN3D\SyncShooter\UserRibbonButtons\button1.bat ";
                startInfo.Arguments += @".\button1.bat ";
                var argString = "\"" + notification.ImageFolderPath + "\" " +
                                "\"" + this.ThreeDDataFolderPath.Value + "\" " +
                                (this.IsCutPetTable.Value ? "yes" : "no");
                startInfo.Arguments += argString;
                var proc = Process.Start(startInfo);
                //proc.WaitForExit();
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

                if ( Directory.Exists( this.ThreeDDataFolderPath.Value ) == false ) {
                    if ( OpenMessageBox( this.Title.Value, MessageBoxImage.Question, MessageBoxButton.YesNo, MessageBoxResult.Yes,
                        "フォルダ\n" + this.ThreeDDataFolderPath.Value + " は存在しません。\n作成しますか？" ) == MessageBoxResult.Yes ) {
                        try {
                            Directory.CreateDirectory( this.ThreeDDataFolderPath.Value );
                        } catch ( Exception e ) {
                            OpenMessageBox( this.Title.Value, MessageBoxImage.Stop, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
                            return;
                        }
                    } else {
                        return;
                    }
                }

                var startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.WorkingDirectory = @".\UserRibbonButtons";
                //startInfo.Arguments = "/k ";    // <- 本番は "/c"
                startInfo.Arguments = "/c ";    // <- 本番は "/c"
                                                //startInfo.Arguments += @"C:\DN3D\SyncShooter\UserRibbonButtons\button2.bat ";
                startInfo.Arguments += @".\button2.bat ";
                var argString = "\"" + this.ProjectFolderPath.Value + "\" " +
                                 "\"" + this.ThreeDDataFolderPath.Value + "\" " +
                                (this.IsCutPetTable.Value ? "yes" : "no") + " " +
                                (this.IsSkipAlreadyBuilt.Value ? "skip" : "??");
                startInfo.Arguments += argString;
                var proc = Process.Start(startInfo);
                //proc.WaitForExit();
            }
        }

        /// <summary>
        /// 3D - 作成中断
        /// </summary>
        void RaiseThreeDBuildingStop()
        {
            // 3Dデータ出力先フォルダに "_stop.txt"　ファイルを置く
            using ( var sw = new StreamWriter( Path.Combine( this.ThreeDDataFolderPath.Value, "_stop.txt" ) ) ) {
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
                        Arguments = "\"" + treeItem._Directory.FullName + "\"",
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

        void RaiseFileViewSelectedItemChanged( FileTreeItem item )
        {
            if ( item == null ) {
                return;
            }
            var path = item._Directory.FullName;
            // TODO: このディレクトリ内の最初のIPアドレスの画像をView上に表示する
            string[] fileNames = Directory.GetFiles( path, "*.jpg" );
            if ( fileNames.Length > 0 ) {
                // ファイル名の数値が最小のファイル名を得る
                var fileName = fileNames.OrderBy( name =>
                {
                    var text = name.Split( new char[] { '\\' } ).Last();
                    text = text.Split( new char[] { '.' } ).First();
                    return int.Parse(text);
                } ).First();
                using ( Stream BitmapStream = System.IO.File.Open( fileName, System.IO.FileMode.Open ) ) {
                    BitmapSource bitmapSource = BitmapFrame.Create( BitmapStream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad );
                    this.PreviewingImage.Value = new TransformedBitmap( bitmapSource, new RotateTransform( 270 ) );
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
                        byte[] data = NewSyncShooter.NewSyncShooter.GetPreviewImage( sIPAddress );
                        if ( data.Length > 0 ) {
                            ShowPreviewImage( data );
                        }
                    } catch ( Exception e ) {
                        OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
                    }
                }
            }
        }

        /// <summary>
        /// 自動モードウインドウを開く
        /// </summary>
        void RaiseAutoMode()
        {
            var notification = new AutoModeNotification()
            {
                Title = "自動モード",
                Capture = () => CatureAutomatically(),
            };
            AutoModeRequest.Raise( notification );
            if ( notification.Confirmed ) {
            }
        }

        /// <summary>
        /// 自動撮影を行う
        /// </summary>
        void CatureAutomatically()
        {
            // プレビュー中なら停止する
            IsCameraPreviewing.Value = false;

            try {
                // 現在のプロジェクトフォルダ内で、"Auto" で始まるフォルダ一覧を取得する
                // それらがある場合は、その最後の番号を、新規作成フォルダの番号とする
                // ない場合は "Auto0000" とする
                var directories = Directory.GetDirectories( this.ProjectFolderPath.Value );
                int nMaxIndex;
                if ( directories.Length == 0 ) {
                    nMaxIndex = 0;
                } else {
                    directories = directories
                                .Select( dir => Path.GetFileName( dir ).ToUpper() )
                                .Where( dir => dir.StartsWith( "AUTO(" ) ).ToArray();
                    if ( directories.Length == 0 ) {
                        nMaxIndex = 0;
                    } else {
                        nMaxIndex = directories.Max( dir =>
                        {
                            string sName = dir.Remove(0, 5);
                            sName = sName.Remove(sName.Length - 1);
                            return int.Parse(sName);
                        });
                    }
                }
                string sTargetName = String.Format("Auto({0:0000})", nMaxIndex + 1);
                string sTargetDir = Path.Combine( this.ProjectFolderPath.Value, sTargetName );
                Directory.CreateDirectory( sTargetDir );

                // カメラ画像転送ダイアログを開く
                var notification2 = new ImagTransferingNotification()
                {
                    Title = "カメラ画像転送",
                    SyncShooter = _newSyncShooter,
                    ConnectedIPAddressList = ConnectedIPAddressList,
                    LocalHostIP = _localHostIP,
                    TargetDir = sTargetDir,
                };
                this.ImageTransferingRequest.Raise( notification2 );

                // 「ファイルビュー」表示を更新
                FileTree.Clear();
                FileTree.Add( new FileTreeItem( this.ProjectFolderPath.Value ) );

                // 3D Dataを置くディレクトリを作る
                string sThreeDDataDir = Path.Combine(this.ThreeDDataFolderPath.Value, sTargetName);
                Directory.CreateDirectory( sThreeDDataDir );

                // 3D化(RC実行)タスクを実行
                var task = new Task( () => RunRCTask( sTargetDir, sThreeDDataDir, this.IsCutPetTable.Value) );
                task.ContinueWith( t => _runRCTaskList.Remove( t ) );
                _runRCTaskList.Add( task );
                task.Start();

            } catch ( Exception e ) {
                OpenMessageBox( this.Title.Value, MessageBoxImage.Error, MessageBoxButton.OK, MessageBoxResult.OK, e.Message );
            }
        }

        /// <summary>
        /// RealityCapture 実行タスク
        /// </summary>
        /// <param name="sOutputDir"></param>
        static void RunRCTask(string sImageDir, string sThreeDDataDir, bool isCutPetTable )
        {
#if true
            // アプリケーションが実行中の間は待つ
            ManualResetEvent waiting = new ManualResetEvent(false);
            waiting.Reset();

            string sRcBatchLockPath = Path.Combine(Path.GetTempPath(), "_rc_bacth_lock)";

            // 一定時間間隔で RC が起動しているかどうかを RCが存在しなくなるまで調べる
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += (sender, e) =>
            {
                
                ProcessStartInfo psInfo = new ProcessStartInfo();
                psInfo.FileName = @"c:\windows\system32\tasklist.exe";
                psInfo.Arguments = string.Format("/fi \"imagename eq {0}\" /nh", _threeDBuildingApp);
                psInfo.CreateNoWindow = true; // コンソール・ウィンドウを開かない
                psInfo.UseShellExecute = false; // シェル機能を使用しない
                psInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
                Process p = Process.Start(psInfo); // アプリの実行開始
                string output = p.StandardOutput.ReadToEnd(); // 標準出力の読み取り
                Debug.WriteLine(output);
                if (!output.Contains(_threeDBuildingApp))
                {
                    timer.Stop();
                    waiting.Set();
                }
            };
            timer.Start();
            waiting.WaitOne();

            // 3D化実行
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = @".\UserRibbonButtons";
            startInfo.Arguments = "/c ";
            startInfo.Arguments += @".\button1.bat ";
            var argString = "\"" + sImageDir + "\" " +
                            "\"" + sThreeDDataDir + "\" " +
                            (isCutPetTable ? "yes" : "no");
            startInfo.Arguments += argString;
            var proc = Process.Start(startInfo);
#else
            // アプリケーションが実行中の間は待つ
            ManualResetEvent waiting = new ManualResetEvent(false);
            waiting.Reset();

            // 一定時間間隔で RC が起動しているかどうかを RCが存在しなくなるまで調べる
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += ( sender, e ) =>
            {
                ProcessStartInfo psInfo = new ProcessStartInfo();
                psInfo.FileName = @"c:\windows\system32\tasklist.exe";
                psInfo.Arguments = string.Format( "/fi \"imagename eq {0}\" /nh", _threeDBuildingApp );
                psInfo.CreateNoWindow = true; // コンソール・ウィンドウを開かない
                psInfo.UseShellExecute = false; // シェル機能を使用しない
                psInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト
                Process p = Process.Start(psInfo); // アプリの実行開始
                string output = p.StandardOutput.ReadToEnd(); // 標準出力の読み取り
                Debug.WriteLine( output );
                if ( !output.Contains( _threeDBuildingApp ) ) {
                    timer.Stop();
                    waiting.Set();
                }
            };
            timer.Start();
            waiting.WaitOne();

            // 3D化実行
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.WorkingDirectory = @".\UserRibbonButtons";
            startInfo.Arguments = "/c ";
            startInfo.Arguments += @".\button1.bat ";
            var argString = "\"" + sImageDir + "\" " +
                            "\"" + sThreeDDataDir + "\" " +
                            (isCutPetTable ? "yes" : "no");
            startInfo.Arguments += argString;
            var proc = Process.Start(startInfo);
#endif
        }

    }
}
