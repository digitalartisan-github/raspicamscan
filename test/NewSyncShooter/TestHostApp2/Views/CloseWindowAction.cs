using Prism.Interactivity.InteractionRequest;
using System.Windows;
using System.Windows.Interactivity;
using TestHostApp2.Notifications;

namespace TestHostApp2.Views
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
