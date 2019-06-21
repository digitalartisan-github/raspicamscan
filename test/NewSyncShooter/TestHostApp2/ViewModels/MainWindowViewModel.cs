using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Net.NetworkInformation;
using Prism.Mvvm;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		NewSyncShooter.NewSyncShooter _newSyncShooter;
		List<string> _connectedIPAddressList;
		string _imageFolderPath;

		public ReactiveProperty<string> Title { get; private set; } = new ReactiveProperty<string>( "NewSyncShooter" );
		public ReactiveProperty<bool> IsCameraConnected { get; private set; } = new ReactiveProperty<bool>( false );
		public ReactiveProperty<BitmapSource> PreviewingImage { get; private set; } = new ReactiveProperty<BitmapSource>();

		public InteractionRequest<INotification> CameraConnectionRequest { get; set; }
		public DelegateCommand CameraConnectionCommand { get; set; }
		public DelegateCommand CameraSettingCommand { get; set; }
		public DelegateCommand CameraPreviewingCommand { get; set; }
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
			_connectedIPAddressList = new List<string>();
			_imageFolderPath = System.Environment.GetFolderPath( Environment.SpecialFolder.Personal );

			CameraConnectionRequest = new InteractionRequest<INotification>();
			CameraConnectionCommand = new DelegateCommand( RaiseCameraConnection );
			CameraSettingCommand = new DelegateCommand( RaiseCameraSetting );
			CameraPreviewingCommand = new DelegateCommand( RaiseCameraPreviewing );
			CameraCapturingCommand = new DelegateCommand( RaiseCameraCapturing );
			CameraStopCommand = new DelegateCommand( RaiseCameraStop );
			CameraRebootCommand = new DelegateCommand( RaiseCameraReboot );
			CameraFrontCommand = new DelegateCommand( RaiseCameraFront );
			CameraBackCommand = new DelegateCommand( RaiseCameraBack );
			CameraRightCommand = new DelegateCommand( RaiseCameraRight );
			CameraLeftCommand = new DelegateCommand( RaiseCameraLeft );
			NetworkSettingCommand = new DelegateCommand( RaiseNetworkSetting );
		}

		void RaiseCameraConnection()
		{
			var notification = new CustomNotification { Title = "Camera Connection" };
			_connectedIPAddressList.Clear();
			_newSyncShooter.ConnectCamera().ToList().ForEach( adrs => {
				_connectedIPAddressList.Add( adrs );
				notification.ConnectedItems.Add( adrs );
			} );
			// SyncshooterDefs にあるIP Addressの中に接続できたカメラがない場合は、そのアドレスの一覧を表示する
			var allList = _newSyncShooter.GetSyncshooterDefs().GetAllCameraIPAddress();
			var exceptList = allList.Except( _connectedIPAddressList ).ToList();
			exceptList.ForEach( adrs => notification.NotConnectedItems.Add( adrs ) );

			CameraConnectionRequest.Raise( notification );

			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );
		}

		void RaiseCameraSetting()
		{
			// TEST
			_connectedIPAddressList.ToList().ForEach( adrs => {
				var param = _newSyncShooter.GetCameraParam( adrs );
				param.Orientation = 1;
				_newSyncShooter.SetCameraParam( adrs, param );
			} );
		}

		void RaiseCameraPreviewing()
		{
			try {
				_connectedIPAddressList.AsParallel().ForAll( adrs => {
					byte[] data = _newSyncShooter.GetPreviewImage( adrs );
					String path = Path.Combine( _imageFolderPath, string.Format( "preview_{0}.bmp", adrs.ToString() ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				} );
			} catch ( Exception e ) {
				MessageBox.Show( e.Message, this.Title.Value, MessageBoxButton.OK, MessageBoxImage.Error );
			}
		}

		void RaiseCameraCapturing()
		{
			var t = DateTime.Now;
			_connectedIPAddressList.AsParallel().ForAll( adrs => {
				byte[] data = _newSyncShooter.GetFullImageInJpeg( adrs );
				String path = Path.Combine( _imageFolderPath, string.Format( "full_{0}.jpg", adrs.ToString() ) );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			} );
			TimeSpan ts = DateTime.Now - t;
			MessageBox.Show( ts.ToString( "s\\.fff" ) + " sec", "Capture", MessageBoxButton.OK, MessageBoxImage.Information );
		}

		void RaiseCameraStop()
		{
			_newSyncShooter.StopCamera( false );
			_connectedIPAddressList.Clear();
			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );
		}

		void RaiseCameraReboot()
		{
			_newSyncShooter.StopCamera( true );
			_connectedIPAddressList.Clear();
			this.IsCameraConnected.Value = ( _connectedIPAddressList.Count > 0 );
		}

		void RaiseCameraFront()
		{
			try {
				byte[] data = _newSyncShooter.GetPreviewImageFront();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _imageFolderPath, string.Format( @".\preview_front.bmp" ) );
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
			try {
				byte[] data = _newSyncShooter.GetPreviewImageBack();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _imageFolderPath, string.Format( @".\preview_back.bmp" ) );
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
			try {
				byte[] data = _newSyncShooter.GetPreviewImageRight();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _imageFolderPath, string.Format( @".\preview_right.bmp" ) );
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
			try {
				byte[] data = _newSyncShooter.GetPreviewImageLeft();
				if ( data.Length > 0 ) {
					// bitmap を表示
					ShowPreviewImage( data );
					String path = Path.Combine( _imageFolderPath, string.Format( @".\preview_left.bmp" ) );
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
