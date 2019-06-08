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

namespace TestHostApp
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		NewSyncShooter.NewSyncShooter _newSyncShooter;

		public MainWindow()
		{
			InitializeComponent();

			_newSyncShooter = new NewSyncShooter.NewSyncShooter();
			_newSyncShooter.Initialize();
		}

		private void ButtonConnectCamera_Click( object sender, RoutedEventArgs e )
		{
			var ipAddressList = _newSyncShooter.ConnectCamera();
			if ( ipAddressList.ToArray().Length == 0 ) {
				MessageBox.Show( "No camera connected.", "Camera Connection" );
			} else {
				foreach ( var address in ipAddressList ) {
					Console.WriteLine( address );
				}
				String s = new string(ipAddressList.SelectMany( adrs => { return adrs + "\n"; } ).ToArray());
				MessageBox.Show( s, "Camera Connection" );
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
	}
}
