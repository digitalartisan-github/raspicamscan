using System;
using System.Reactive.Linq;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NewSyncShooterApp.ViewModels
{
	public class TreeViewItemViewModel : BindableBase, IDisposable
	{
		/// <summary>TreeViewItemのテキストを取得します。</summary>
		public ReadOnlyReactivePropertySlim<string> ItemText { get; }

		/// <summary>TreeViewItem のImageを取得します</summary>
		public ReactiveProperty<System.Windows.Media.ImageSource> ItemImage { get; }

		/// <summary>子ノードを取得します。</summary>
		public ReactiveCollection<TreeViewItemViewModel> Children { get; }

		/// <summary>TreeViewItem の元データを取得します。</summary>
		public object SourceData { get; } = null;

		/// <summary>TreeViewItemが展開されているかを取得・設定します。</summary>
		public ReactivePropertySlim<bool> IsExpanded { get; set; }

		/// <summary>TreeViewItemが選択されているかを取得・設定します。</summary>
		public ReactivePropertySlim<bool> IsSelected { get; set; }

		/// <summary>ReactivePropertyのDispose用リスト</summary>
		private System.Reactive.Disposables.CompositeDisposable disposables
			= new System.Reactive.Disposables.CompositeDisposable();

		/// <summary>コンストラクタ</summary>
		/// <param name="treeItem">TreeViewItem の元データを表すobject。</param>
		public TreeViewItemViewModel( object treeItem )
		{
			this.Children = new ReactiveCollection<TreeViewItemViewModel>().AddTo( this.disposables );

			this.SourceData = treeItem;
			var imageFileName = string.Empty;

			switch ( this.SourceData ) {
			case string s:
				this.ItemText = this.ObserveProperty( x => x.SourceData )
					.Select( v => v as string )
					.ToReadOnlyReactivePropertySlim()
					.AddTo( this.disposables );
				break;
			}

			var img = new System.Windows.Media.Imaging.BitmapImage(
				new Uri("pack://application:,,,/NavigationTree;component/Resources/" + imageFileName,
						UriKind.Absolute));
			this.ItemImage = new ReactiveProperty<System.Windows.Media.ImageSource>( img ).AddTo( this.disposables );

			this.IsExpanded = new ReactivePropertySlim<bool>( true ).AddTo( this.disposables );
			this.IsSelected = new ReactivePropertySlim<bool>( false ).AddTo( this.disposables );
		}

		/// <summary>オブジェクトを破棄します。</summary>
		void IDisposable.Dispose() { this.disposables.Dispose(); }
	}
}
