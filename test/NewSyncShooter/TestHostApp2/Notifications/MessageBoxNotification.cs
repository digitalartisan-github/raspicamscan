﻿using Prism.Interactivity.InteractionRequest;
using System.Windows;
namespace TestHostApp2.Notifications
{
	public class MessageBoxNotification : Notification
	{
		public string Message { get; set; }
		public MessageBoxButton Button { get; set; }
		public MessageBoxImage Image { get; set; }
		public MessageBoxResult DefaultButton { get; set; }
		public MessageBoxResult Result { get; set; }
	}
}
