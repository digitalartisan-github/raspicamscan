using Prism.Interactivity.InteractionRequest;
using System.Windows;
using System.Windows.Interactivity;
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.Views
{
    public class CloseWindowAction : TriggerAction<FrameworkElement>
    {
        protected override void Invoke( object parameter )
        {
            var win = Window.GetWindow( AssociatedObject );
            if ( win != null ) {
                win.Close();
            }
        }
    }
}
