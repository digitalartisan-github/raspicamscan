using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Linq;

namespace TestHostApp2.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

		private void TreeView_SelectedItemChanged( object sender, RoutedPropertyChangedEventArgs<object> e )
		{
			var obj = sender as TreeView;
			foreach (var item in obj.ItemsSource) {
				var treeItem = item as TreeViewItem;
				treeItem.IsExpanded = true;
			}
		}
	}
}
