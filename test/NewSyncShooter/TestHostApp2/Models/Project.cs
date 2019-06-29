using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.IO;
using Reactive.Bindings;
using Prism.Mvvm;

namespace TestHostApp2.Models
{
	public class Project
	{
		private readonly CompositeDisposable _disposable = new CompositeDisposable();

		public ReactivePropertySlim<string> ProjectName;
		public ReactivePropertySlim<string> BaseFolderPath;
		public ReactivePropertySlim<string> Comment;
		public ReactivePropertySlim<string> ThreeDDataFolderPath;

		//private string _projectName = string.Empty;
		//private string _baseFolderPath = string.Empty;

		//// プロジェクト名
		//public string ProjectName
		//{
		//	get { return _projectName; }
		//	set
		//	{
		//		SetProperty( ref _projectName, value );
		//		updateThreeDDataFolderPath();
		//	}
		//}

		//// プロジェクトのベースフォルダパス
		//public string BaseFolderPath
		//{
		//	get { return _baseFolderPath; }
		//	set
		//	{
		//		SetProperty( ref _baseFolderPath, value );
		//		updateThreeDDataFolderPath();
		//	}
		//}

		//// コメント
		//public string Comment { get; set; } = string.Empty;

		// プロジェクトのベースフォルダパス＋プロジェクト名からなるフォルダのパス名
		//public string ProjectFolderPath
		//{
		//	get { return Path.Combine( BaseFolderPath, ProjectName ); }
		//}

		// 3Dデータ作成フォルダ
		//public string ThreeDDataFolderPath { get; set; }

		//private void updateThreeDDataFolderPath()
		//{
		//	ThreeDDataFolderPath = Path.Combine( ProjectFolderPath, "ThreeD" );
		//}

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public Project()
		{
			ProjectName = new ReactivePropertySlim<string>( string.Empty );
			BaseFolderPath = new ReactivePropertySlim<string>( System.Environment.GetFolderPath( Environment.SpecialFolder.Personal ) );
			Comment = new ReactivePropertySlim<string>( string.Empty );
			ThreeDDataFolderPath = new ReactivePropertySlim<string>( string.Empty );
		}

		public void Dispose()
		{
			_disposable?.Dispose();
		}
	}
}
