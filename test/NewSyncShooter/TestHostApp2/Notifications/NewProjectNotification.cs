using Prism.Interactivity.InteractionRequest;
using TestHostApp2.Models;

namespace TestHostApp2.Notifications
{
	public class NewProjectNotification : Confirmation
	{
		public Project Project { get; set; }
		//public string BaseFolderPath { get; set; } = string.Empty;
		//public string ProjectName { get; set; } = string.Empty;
		//public string Comment { get; set; } = string.Empty;

		public NewProjectNotification()
		{
		}
	}
}
