﻿using Prism.Interactivity.InteractionRequest;

namespace TestHostApp2.Notifications
{
	public class NetworkSettingNotification : Confirmation
	{
		public string LocalHostIP { get; set; }

		public NetworkSettingNotification()
		{
		}
	}
}
