using System;
using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using NewSyncShooter;

namespace NewSyncShooterApp.Notifications
{
	public class CameraSettingNotification : Confirmation
	{
		public CameraParam CameraParameter { get; set; } = new CameraParam();
		public string IPAddress { get; set; } = string.Empty;
		public bool IsApplyToAllCamera { get; set; } = false;
		public Action<CameraParam> ApplyOne { get; set; } = null;
		public Action<CameraParam> ApplyAll { get; set; } = null;

		public CameraSettingNotification()
		{
		}
	}
}
