using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Reactive.Bindings;

namespace TestHostApp2.Models
{
	public class CameraTreeItem : TreeViewItem
	{
		List<string> _IPAddressList;
		private bool _Expanded { get; set; } = false;
		public ReactiveProperty<CameraTreeItem> _SelectionItem { get; set; } = new ReactiveProperty<CameraTreeItem>();

		public CameraTreeItem( List<string> IPAddressList )
		{
			_IPAddressList = IPAddressList;

			this.Header = CreateRootHeader();
			if ( IPAddressList.Count > 0 ) {
				this.Items.Add( new TreeViewItem() );
				this.Expanded += TreeViewItem_Expanded;
			}
			this.Selected += Model_TreeViewItem_Selected;
		}

		public CameraTreeItem( string adrs )
		{
			this.Header = CreateCameraHeader( adrs );
		}

		private void TreeViewItem_Expanded( object sender, RoutedEventArgs e )
		{
			if ( !_Expanded ) {
				this.Items.Clear();
				foreach ( var adrs in _IPAddressList ) {
					this.Items.Add( new CameraTreeItem( adrs ) );
				}
				_Expanded = true;
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
			} );
			sp.Children.Add( new TextBlock() { Text = string.Format( "Connected Camera ({0})", _IPAddressList.Count ) } );
			return sp;
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

		private void Model_TreeViewItem_Selected( object sender, RoutedEventArgs e )
		{
			_SelectionItem.Value = ( this.IsSelected ) ? this : (CameraTreeItem) e.Source;
		}
	}
}
