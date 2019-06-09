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
			_newSyncShooter.Initialize();
			_connectedIPAddressList = new List<string>();
		}

		private void ButtonConnectCamera_Click( object sender, RoutedEventArgs e )
		{
			var connectedArray = _newSyncShooter.ConnectCamera().ToArray();
			if ( connectedArray.ToArray().Length == 0 ) {
				MessageBox.Show( "No camera connected.", "Camera Connection" );
			} else {
				CameraConnectionDlg dlg = new CameraConnectionDlg();
				_connectedIPAddressList.Clear();
				foreach ( var adrs in connectedArray ) {
					dlg.ListBox_Connected.Items.Add( adrs );
					_connectedIPAddressList.Add( adrs );
					// TEST: getting camera parameters
					var param = _newSyncShooter.GetCameraParam( adrs );
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
			}
		}

		private void ButtonStopCamera_Click( object sender, RoutedEventArgs e )
		{
			_newSyncShooter.StopCamera( false );
		}

		private void ButtonRebootCamera_Click( object sender, RoutedEventArgs e )
		{
			_newSyncShooter.StopCamera( true );
		}

		private void ButtonPreview_Click( object sender, RoutedEventArgs e )
		{
			foreach (var adrs in _connectedIPAddressList ) {
				byte[] data = _newSyncShooter.GetPreviewImage(adrs);
				String path = string.Format( "preview_{0}.bmp", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 4, (int) data.Length - 4 );
				}
			}
		}

		private void ButtonCapture_Click( object sender, RoutedEventArgs e )
		{
			var t = DateTime.Now;
			_connectedIPAddressList.AsParallel().ForAll( adrs =>
			{
				byte[] data = _newSyncShooter.GetFullImageInJpeg(adrs);
				String path = string.Format( "full_{0}.jpg", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( data, 4, (int) data.Length - 4 );
				}
			} );
			TimeSpan ts = DateTime.Now - t;
			MessageBox.Show( ts.ToString("ss\\.fff") + "[sec]" );
		}
	}
}
