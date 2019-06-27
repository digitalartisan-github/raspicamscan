using System;
using System.IO;
using Prism.Mvvm;

namespace TestHostApp2.Models
{
	public class Project : BindableBase
	{
		private string _projectName = string.Empty;
		private string _baseFolderPath = string.Empty;

		// プロジェクト名
		public string ProjectName
		{
			get { return _projectName; }
			set
			{
				SetProperty( ref _projectName, value );
				updateThreeDDataFolderPath();
			}
		}

		// プロジェクトのベースフォルダパス
		public string BaseFolderPath
		{
			get { return _baseFolderPath; }
			set
			{
				SetProperty( ref _baseFolderPath, value );
				updateThreeDDataFolderPath();
			}
		}

		// コメント
		public string Comment { get; set; } = string.Empty;

		// プロジェクトのベースフォルダパス＋プロジェクト名からなるフォルダのパス名
		public string ProjectFolderPath
		{
			get { return Path.Combine( BaseFolderPath, ProjectName ); }
		}
		
		// 3Dデータ作成フォルダ
		public string ThreeDDataFolderPath { get; set; }

		private void updateThreeDDataFolderPath()
		{
			ThreeDDataFolderPath = Path.Combine( ProjectFolderPath, "ThreeD" );
		}

		public Project()
		{
			BaseFolderPath = System.Environment.GetFolderPath( Environment.SpecialFolder.Personal );
		}
	}
}
