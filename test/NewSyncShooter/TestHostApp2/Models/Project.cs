using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHostApp2.Models
{
	public class Project
	{
		public string BaseFolderPath { get; set; }
		public string ProjectName { get; set; }
		public string Comment { get; set; } = string.Empty;
		public string ProjectFolderPath
		{
			get { return System.IO.Path.Combine( BaseFolderPath, ProjectName ); }
		}

		public Project()
		{
			BaseFolderPath = System.Environment.GetFolderPath( Environment.SpecialFolder.Personal );
			ProjectName = "NewProject01";
		}
	}
}
