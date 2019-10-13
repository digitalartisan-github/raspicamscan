using Prism.Interactivity.InteractionRequest;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace NewSyncShooterApp.Notifications
{
    public class ImagTransferingNotification : Confirmation
    {
        public NewSyncShooter.NewSyncShooter SyncShooter { get; set; }
        public IEnumerable<string> ConnectedIPAddressList { get; set; }
        public string LocalHostIP { get; set; }
        public string TargetDir { get; set; }
        public BitmapSource Image { get; set; }

        public ImagTransferingNotification()
        {
        }
    }
}
