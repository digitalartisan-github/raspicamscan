using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Reactive.Bindings;

namespace NewSyncShooterApp.ViewModels
{
    public class FileTreeItem : TreeViewItem
    {
        public DirectoryInfo _Directory { get; set; }
        public ReactiveProperty<FileTreeItem> _SelectionItem { get; set; } = new ReactiveProperty<FileTreeItem>();

        public FileTreeItem( string path, bool isRoot = true )
        {
            if ( String.IsNullOrEmpty( path ) || Directory.Exists( path ) == false ) {
                this.Header = CreateRootHeader();
            } else {
                this._Directory = new DirectoryInfo( path );
                if ( isRoot ) {
                    this.Header = CreateRootHeader();
                    if ( _Directory.GetDirectories().Length > 0 ) {
                        foreach ( DirectoryInfo dir in _Directory.GetDirectories() ) {
                            if ( dir.Attributes == FileAttributes.Directory ) {
                                this.Items.Add( new FileTreeItem( dir.FullName, false ) );
                            }
                        }
                        this.IsExpanded = true;
                    }
                } else {
                    this.Header = CreatePictureFolderHeader();
                }
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
            if ( _Directory != null ) {
                sp.Children.Add( new TextBlock() { Text = _Directory.Name, FontWeight = FontWeights.Bold } );
            }
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
    }
}
