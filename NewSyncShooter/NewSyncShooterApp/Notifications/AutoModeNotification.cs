using System;
using Prism.Interactivity.InteractionRequest;

namespace NewSyncShooterApp.Notifications
{
    public class AutoModeNotification : Confirmation
    {
        public Action Capture { get; set; } = null;

        public AutoModeNotification() { }
    }
}
