using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace NewSyncShooterApp.Notifications
{
	public class CameraConnectionNotification : Confirmation
	{
		public IList<string> ConnectedItems { get; private set; }
		public IList<string> NotConnectedItems { get; private set; }

		public CameraConnectionNotification()
		{
			this.ConnectedItems = new List<string>();
			this.NotConnectedItems = new List<string>();
		}
	}
}
