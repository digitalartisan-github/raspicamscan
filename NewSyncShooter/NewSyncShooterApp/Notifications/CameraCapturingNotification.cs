using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace NewSyncShooterApp.Notifications
{
	public class CameraCapturingNotification : Confirmation
	{
		public string CapturingName { get; set; }

		public CameraCapturingNotification()
		{
		}
	}
}
