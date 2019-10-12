using System;
using System.Reactive.Linq;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NewSyncShooterApp.ViewModels
{
    public class FileTreeViewViewModel : BindableBase, IDisposable
    {
        /// <summary>TreeViewItemのテキストを取得します。</summary>
        //public ReadOnlyReactivePropertySlim<string> ItemText { get; }
        public ReactivePropertySlim<string> ItemText { get; }

        /// <summary>子ノードを取得します。</summary>
        public ReactiveCollection<FileTreeViewViewModel> Children { get; }

        /// <summary>TreeViewItem の元データを取得します。</summary>
        public object SourceData { get; } = null;

        public ReactiveProperty<string> SelectedValue { get; set; }

        /// <summary>ReactivePropertyのDispose用リスト</summary>
        private System.Reactive.Disposables.CompositeDisposable disposables = new System.Reactive.Disposables.CompositeDisposable();

        /// <summary>コンストラクタ</summary>
        /// <param name="treeItem">TreeViewItem の元データを表すobject。</param>
        public FileTreeViewViewModel( string baseFolderPath )
        {
            this.Children = new ReactiveCollection<FileTreeViewViewModel>().AddTo( this.disposables );

            this.ItemText = new ReactivePropertySlim<string>( baseFolderPath );

            this.SelectedValue = new ReactiveProperty<string>( string.Empty ).AddTo( this.disposables );
            this.SelectedValue.Subscribe( v => RaiseSelectedValueChanged( v ) );

            //this.SourceData = treeItem;
            //switch ( this.SourceData ) {
            //	case PersonalInformation p:
            //		this.ItemText = p.ObserveProperty( x => x.Name )
            //			.ToReadOnlyReactivePropertySlim()
            //			.AddTo( this.disposables );
            //		break;
            //	case PhysicalInformation ph:
            //		this.ItemText = ph.ObserveProperty( x => x.MeasurementDate )
            //			.Select( d => d.HasValue ? d.Value.ToString( "yyy年MM月dd日" ) : "新しい測定" )
            //			.ToReadOnlyReactivePropertySlim()
            //			.AddTo( this.disposables );
            //		break;
            //	case TestPointInformation t:
            //		this.ItemText = t.ObserveProperty( x => x.TestDate )
            //			.ToReadOnlyReactivePropertySlim()
            //			.AddTo( this.disposables );
            //		break;
            //	case string s:
            //		this.ItemText = this.ObserveProperty( x => x.SourceData )
            //			.Select( v => v as string )
            //			.ToReadOnlyReactivePropertySlim()
            //			.AddTo( this.disposables );
            //		break;
            //}
        }

        private void RaiseSelectedValueChanged( string value )
        {

        }

        /// <summary>オブジェクトを破棄します。</summary>
        void IDisposable.Dispose() { this.disposables.Dispose(); }
    }
}
