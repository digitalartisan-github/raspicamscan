using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Reactive.Bindings;

namespace TestHostApp2.Models
{
	public class FileTreeItem : TreeViewItem
	{
		public DirectoryInfo _Directory { get; set; }
		private bool _Expanded { get; set; } = false;
		public ReactiveProperty<FileTreeItem> _SelectionItem { get; set; } = new ReactiveProperty<FileTreeItem>();

		public FileTreeItem( string path, bool isRoot = true )
		{
			if ( String.IsNullOrEmpty( path ) || Directory.Exists( path ) == false ) {
				this.Header = CreateRootHeader();
			} else {
				this._Directory = new DirectoryInfo( path );
				if ( isRoot ) {
					this.Header = CreateRootHeader();
					if ( _Directory.GetDirectories().Count() > 0 ) {
						this.Items.Add( new TreeViewItem() );
						this.Expanded += TreeViewItem_Expanded;
					}
				} else {
					this.Header = CreatePictureFolderHeader();
				}
				this.Selected += Model_TreeViewItem_Selected;
			}
		}

		private void TreeViewItem_Expanded( object sender, RoutedEventArgs e )
		{
			if ( !_Expanded ) {
				this.Items.Clear();
				foreach ( DirectoryInfo dir in _Directory.GetDirectories() ) {
					if ( dir.Attributes == FileAttributes.Directory ) {
						this.Items.Add( new FileTreeItem( dir.FullName, false ) );
					}
				}
				_Expanded = true;
			}
		}

		private StackPanel CreateRootHeader()
		{
			StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
			sp.Children.Add( new Image()
			{
				Source = new BitmapImage( new Uri( @"Images\folder_30px.png", UriKind.Relative ) ),
				Width = 15,
				Height = 18,
				Margin = new Thickness( 0, 0, 4, 0 )
			} );
			sp.Children.Add( new TextBlock() { Text = _Directory.Name } );
			return sp;
		}

		private StackPanel CreatePictureFolderHeader()
		{
			StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal };
			sp.Children.Add( new Image()
			{
				Source = new BitmapImage( new Uri( @"Images\pictures_folder_30px.png", UriKind.Relative ) ),
				Width = 15,
				Height = 18,
				Margin = new Thickness( 0, 0, 4, 0 )
			} );
			sp.Children.Add( new TextBlock() { Text = _Directory.Name } );
			return sp;
		}

		private void Model_TreeViewItem_Selected( object sender, RoutedEventArgs e )
		{
			_SelectionItem.Value = ( this.IsSelected ) ? this : (FileTreeItem) e.Source;
		}
	}
}
