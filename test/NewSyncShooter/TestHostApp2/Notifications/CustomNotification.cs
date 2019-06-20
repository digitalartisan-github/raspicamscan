using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;

namespace TestHostApp2.Notifications
{
	public class CustomNotification : Confirmation, ICustomNotification
	{
		public IList<string> ConnectedItems { get; private set; }
		public IList<string> NotConnectedItems { get; private set; }
		public string SelectedItem { get; set; }

		public CustomNotification()
		{
			this.ConnectedItems = new List<string>();
			this.NotConnectedItems = new List<string>();
			this.SelectedItem = null;
		}
	}
}
