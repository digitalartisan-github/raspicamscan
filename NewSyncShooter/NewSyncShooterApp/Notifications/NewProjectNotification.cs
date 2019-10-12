using Prism.Interactivity.InteractionRequest;

namespace NewSyncShooterApp.Notifications
{
    public class NewProjectNotification : Confirmation
    {
        public string BaseFolderPath { get; set; }
        public string ProjectName { get; set; }
        public string ProjectComment { get; set; }

        public NewProjectNotification()
        {
        }
    }
}
