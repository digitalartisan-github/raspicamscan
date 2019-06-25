using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace TestHostApp2.Notifications
{
	public class CameraCapturingNotification : Confirmation
	{
		public string CapturingName { get; set; }

		public CameraCapturingNotification()
		{
		}
	}
}
