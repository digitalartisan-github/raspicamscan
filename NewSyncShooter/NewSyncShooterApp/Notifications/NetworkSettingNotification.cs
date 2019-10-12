using Prism.Interactivity.InteractionRequest;

namespace NewSyncShooterApp.Notifications
{
    public class NetworkSettingNotification : Confirmation
    {
        public string LocalHostIP { get; set; }

        public NetworkSettingNotification()
        {
        }
    }
}
