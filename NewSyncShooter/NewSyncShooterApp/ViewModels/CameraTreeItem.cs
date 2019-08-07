using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Reactive.Bindings;

namespace NewSyncShooterApp.ViewModels
{
	public class CameraTreeItem : TreeViewItem
	{
		IEnumerable<string> _IPAddressList;

		public CameraTreeItem( IEnumerable<string> IPAddressList )
		{
			_IPAddressList = IPAddressList;

			this.Header = CreateRootHeader();
			if ( IPAddressList.Count() > 0 ) {
				this.Items.Clear();
				foreach ( var adrs in _IPAddressList ) {
					this.Items.Add( new CameraSubTreeItem( adrs ) );
				}
				this.IsExpanded = true;
			}
		}

		private StackPanel CreateRootHeader()
		{
			StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
			sp.Children.Add( new Image()
			{
				Source = new BitmapImage( new Uri( @"Images\imac_30px.png", UriKind.Relative ) ),
				Width = 15,
				Height = 18,
				Margin = new Thickness( 0, 0, 4, 0 )
			} );
			sp.Children.Add( new TextBlock() { Text = string.Format( "Connected Camera ({0})", _IPAddressList.Count() ), FontWeight = FontWeights.Bold } );
			return sp;
		}
	}

	public class CameraSubTreeItem : TreeViewItem
	{
		public string _ipAddress;

		public CameraSubTreeItem( string adrs )
		{
			_ipAddress = adrs;
			this.Header = CreateCameraHeader( adrs );
		}

		private StackPanel CreateCameraHeader( string adrs )
		{
			StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
			sp.Children.Add( new Image()
			{
				Source = new BitmapImage( new Uri( @"Images\camera_30px.png", UriKind.Relative ) ),
				Width = 15,
				Height = 18,
				Margin = new Thickness( 0, 0, 4, 0 )
			} );
			sp.Children.Add( new TextBlock() { Text = adrs } );
			return sp;
		}
	}
}
