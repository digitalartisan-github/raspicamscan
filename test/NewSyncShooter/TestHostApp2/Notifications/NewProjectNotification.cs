using Prism.Interactivity.InteractionRequest;
using TestHostApp2.Models;

namespace TestHostApp2.Notifications
{
	public class NewProjectNotification : Confirmation
	{
		public string BaseFolderPath { get; set; }
		public string ProjectName { get; set; }
		public string ProjectComment { get; set; }

		public NewProjectNotification()
		{
		}
	}
}
