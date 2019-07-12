using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using NewSyncShooter;

namespace TestHostApp2.Notifications
{
	public class CameraSettingNotification : Confirmation
	{
		public CameraParam CameraParameter { get; set; } = new CameraParam();
		public string IPAddress { get; set; } = string.Empty;
		public bool IsApplyToAllCamera { get; set; } = false;

		public CameraSettingNotification()
		{
		}
	}
}
