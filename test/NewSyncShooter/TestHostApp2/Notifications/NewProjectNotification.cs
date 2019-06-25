using Prism.Interactivity.InteractionRequest;
using TestHostApp2.Models;

namespace TestHostApp2.Notifications
{
	public class NewProjectNotification : Confirmation
	{
		public Project Project { get; set; }

		public NewProjectNotification()
		{
		}
	}
}
