using Prism.Interactivity.InteractionRequest;

namespace TestHostApp2.Notifications
{
    public interface ICustomNotification : IConfirmation
    {
        string SelectedItem { get; set; }
    }
}
