using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.IO;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Prism.Mvvm;

namespace NewSyncShooterApp.Models
{
    public class Project : IDisposable
    {
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        // プロジェクト名
        public ReactivePropertySlim<string> ProjectName;
        // プロジェクトのベースフォルダパス
        public ReactivePropertySlim<string> BaseFolderPath;
        // コメント
        public ReactivePropertySlim<string> Comment;
        // 3Dデータ作成フォルダ
        public ReactivePropertySlim<string> ThreeDDataFolderPath;
        public ReactiveProperty<bool> IsCutPetTable;
        public ReactiveProperty<bool> IsSkipAlreadyBuilt;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Project()
        {
            ProjectName = new ReactivePropertySlim<string>( string.Empty ).AddTo( _disposable );
            BaseFolderPath = new ReactivePropertySlim<string>( System.Environment.GetFolderPath( Environment.SpecialFolder.Personal ) ).AddTo( _disposable );
            Comment = new ReactivePropertySlim<string>( string.Empty ).AddTo( _disposable );
            ThreeDDataFolderPath = new ReactivePropertySlim<string>( System.Environment.GetFolderPath( Environment.SpecialFolder.Personal ) ).AddTo( _disposable );
            IsCutPetTable = new ReactiveProperty<bool>( true ).AddTo( _disposable );
            IsSkipAlreadyBuilt = new ReactiveProperty<bool>( true ).AddTo( _disposable );
        }

        public void Load( string sBaseRegKey )
        {
            var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( Path.Combine(sBaseRegKey, "Project" ) );
            this.ProjectName.Value = regkey.GetValue( "ProjectName", this.ProjectName.Value ) as string;
            this.BaseFolderPath.Value = regkey.GetValue( "BaseFolerPath", this.BaseFolderPath.Value ) as string;
            this.Comment.Value = regkey.GetValue( "Comment", this.Comment.Value ) as string;
            this.ThreeDDataFolderPath.Value = regkey.GetValue( "ThreeDDataFolderPath", this.ThreeDDataFolderPath.Value ) as string;

            Int32 val = this.IsCutPetTable.Value ? 1 : 0;
            this.IsCutPetTable.Value = ( (Int32) regkey.GetValue( "IsCutPetTable", val ) != 0 ) ? true : false;
            val = this.IsCutPetTable.Value ? 1 : 0;
            this.IsSkipAlreadyBuilt.Value = ( (Int32) regkey.GetValue( "IsSkipAlreadyBuilt", val ) != 0 ) ? true : false;
        }

        public void Save( string sBaseRegKey )
        {
            var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey( Path.Combine(sBaseRegKey, "Project" ) );
            regkey.SetValue( "BaseFolerPath", this.BaseFolderPath.Value );
            regkey.SetValue( "ProjectName", this.ProjectName.Value );
            regkey.SetValue( "Comment", this.Comment.Value );
            regkey.SetValue( "ThreeDDataFolderPath", this.ThreeDDataFolderPath.Value );
            regkey.SetValue( "IsCutPetTable", this.IsCutPetTable.Value ? 1 : 0 );
            regkey.SetValue( "IsSkipAlreadyBuilt", this.IsSkipAlreadyBuilt.Value ? 1 : 0 );
        }

        public void Dispose()
        {
            _disposable?.Dispose();
        }
    }
}
