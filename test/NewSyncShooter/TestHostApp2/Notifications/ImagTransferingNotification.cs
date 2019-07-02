using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace TestHostApp2.Notifications
{
	public class ImagTransferingNotification : Confirmation
	{
		public NewSyncShooter.NewSyncShooter SyncShooter { get; set; }
		public List<string> ConnectedIPAddressList { get; set; }
		public string TargetDir { get; set; }
		public BitmapSource Image { get; set; }

		public ImagTransferingNotification()
		{
		}
	}
}
