using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NewSyncShooter;
using System.IO;

namespace TestHostApp
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		NewSyncShooter.NewSyncShooter _newSyncShooter;
		List<string>    _connectedIPAddressList;

		public MainWindow()
		{
			InitializeComponent();

			_newSyncShooter = new NewSyncShooter.NewSyncShooter();
			_newSyncShooter.Initialize( "syncshooterDefs.json" );
			_connectedIPAddressList = new List<string>();
		}

		private void ButtonConnectCamera_Click( object sender, RoutedEventArgs e )
		{
#if true
			var connectedArray = _newSyncShooter.ConnectCamera().ToArray();
			CameraConnectionDlg dlg = new CameraConnectionDlg();
			_connectedIPAddressList.Clear();
			foreach ( var adrs in connectedArray ) {
				dlg.ListBox_Connected.Items.Add( adrs );
				_connectedIPAddressList.Add( adrs );
			}

			// SyncshooterDefs にあるIP Addressの中に接続できたカメラがない場合は、そのアドレスの一覧を表示する
			var allArray = _newSyncShooter.GetSyncshooterDefs().GetAllCameraIPAddress().ToArray();
			var exceptArray = allArray.Except(connectedArray).ToArray();
			if ( exceptArray.Length > 0 ) {
				foreach (var adrs in exceptArray ) {
					dlg.ListBox_NotConnected.Items.Add( adrs );
				}
			}
			dlg.ShowDialog();
#else
			var allArray = _newSyncShooter.GetSyncshooterDefs().GetAllCameraIPAddress();
			var connectedArray = _newSyncShooter.GetConnectedHostAddress(allArray).ToArray();

			CameraConnectionDlg dlg = new CameraConnectionDlg();
			_connectedIPAddressList.Clear();
			foreach ( var adrs in connectedArray ) {
				dlg.ListBox_Connected.Items.Add( adrs );
				_connectedIPAddressList.Add( adrs );
			}

			// SyncshooterDefs にあるIP Addressの中に接続できたカメラがない場合は、そのアドレスの一覧を表示する
			var exceptArray = allArray.Except(connectedArray).ToArray();
			if ( exceptArray.Length > 0 ) {
				foreach ( var adrs in exceptArray ) {
					dlg.ListBox_NotConnected.Items.Add( adrs );
				}
			}
			dlg.ShowDialog();
#endif
		}

		private void ButtonCameraSetting_Click( object sender, RoutedEventArgs e )
		{
			// TEST
			_connectedIPAddressList.ForEach( adrs =>
			{
				var param = _newSyncShooter.GetCameraParam( adrs );
				param.Orientation = 1;
				_newSyncShooter.SetCameraParam( adrs, param );
			} );
		}

		private void ButtonPreview_Click( object sender, RoutedEventArgs e )
		{
			foreach (var adrs in _connectedIPAddressList ) {
				byte[] data = NewSyncShooter.NewSyncShooter.GetPreviewImage(adrs);
				String path = string.Format( "preview_{0}.bmp", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			}
		}

		private void ButtonCapture_Click( object sender, RoutedEventArgs e )
		{
			var t = DateTime.Now;
			_connectedIPAddressList.AsParallel().ForAll( adrs =>
			{
				byte[] data = NewSyncShooter.NewSyncShooter.GetFullImageInJpeg(adrs, out int portNo);
				String path = string.Format( "full_{0}.jpg", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			} );
			TimeSpan ts = DateTime.Now - t;
			MessageBox.Show( ts.ToString("s\\.fff") + " sec", "Capture", MessageBoxButton.OK, MessageBoxImage.Information );
		}

		private void ButtonStopCamera_Click( object sender, RoutedEventArgs e )
		{
			_newSyncShooter.StopCamera( false );
		}

		private void ButtonRebootCamera_Click( object sender, RoutedEventArgs e )
		{
			_newSyncShooter.StopCamera( true );
		}

		private void ButtonFrontCamera_Click( object sender, RoutedEventArgs e )
		{
			byte[] data = _newSyncShooter.GetPreviewImageFront();
			if ( data.Length > 0 ) {
				// bitmap を表示
				ShowPreviewImage( data );
				String path = string.Format( @".\preview_front.bmp" );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			}
		}

		private void ButtonBackCamera_Click( object sender, RoutedEventArgs e )
		{
			byte[] data = _newSyncShooter.GetPreviewImageBack();
			if ( data.Length > 0 ) {
				// bitmap を表示
				ShowPreviewImage( data );
				String path = string.Format( @".\preview_back.bmp" );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			}
		}

		private void ButtonRightCamera_Click( object sender, RoutedEventArgs e )
		{
			byte[] data = _newSyncShooter.GetPreviewImageRight();
			if ( data.Length > 0 ) {
				// bitmap を表示
				ShowPreviewImage( data );
				String path = string.Format( @".\preview_right.bmp" );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			}
		}

		private void ButtonLeftCamera_Click( object sender, RoutedEventArgs e )
		{
			byte[] data = _newSyncShooter.GetPreviewImageLeft();
			if ( data.Length > 0 ) {
				// bitmap を表示
				ShowPreviewImage( data );
				String path = string.Format( @".\preview_left.bmp" );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 0, (int) data.Length );
				}
			}
		}

		private void ShowPreviewImage( byte[] data )
		{
			MemoryStream ms = new MemoryStream( data );
			System.Windows.Media.Imaging.BitmapSource bitmapSource =
						System.Windows.Media.Imaging.BitmapFrame.Create(
							ms,
							System.Windows.Media.Imaging.BitmapCreateOptions.None,
							System.Windows.Media.Imaging.BitmapCacheOption.OnLoad
						);
			this.PreviewImage.Source = bitmapSource;
		}
	}
}
