using Prism.Interactivity.InteractionRequest;
using System.Windows;
using System.Windows.Interactivity;
using TestHostApp2.Notifications;

namespace TestHostApp2.Views
{
	public class MessageBoxAction : TriggerAction<FrameworkElement>
	{
		protected override void Invoke( object parameter )
		{
			var args = parameter as InteractionRequestedEventArgs;
			if ( args == null )
				return;

			var confirmation = args.Context as MessageBoxNotification;
			if ( confirmation != null )
				confirmation.Result = MessageBox.Show(
					confirmation.Message,
					confirmation.Title,
					confirmation.Button,
					confirmation.Image,
					confirmation.DefaultButton
				);

			args.Callback();
		}
	}
}
