using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using PrismCommonDialog.Confirmations;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class NetworkInfo
	{
		public string Selected { get; set; }
		public string InterfaceName { get; set; }
		public string IPAddress { get; set; }
	}

	public class NetworkSettingViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public List<NetworkInfo> NetworkInfoList { get; set; } = new List<NetworkInfo>();

		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NetworkSettingViewModel()
		{
			collectNetworkInformation();
			OkCommand = new DelegateCommand( OKInteraction );
			CancelCommand = new DelegateCommand( CancelInteraction );
		}

		private void collectNetworkInformation()
		{
			NetworkInfoList.Clear();
			NetworkInterface[] nicList = NetworkInterface.GetAllNetworkInterfaces();
			nicList.Where( nic => nic.Speed > 0 ).ToList().ForEach( nic => {
				IPInterfaceProperties ipInfo = nic.GetIPProperties();
				string IPAddressString;
				if ( ipInfo.UnicastAddresses.Count > 0 ) {
					IPAddressString = ipInfo.UnicastAddresses[ipInfo.UnicastAddresses.Count - 1].Address.ToString();
					Console.WriteLine( IPAddressString );
				} else {
					IPAddressString = "0.0.0.0";
				}
				NetworkInfoList.Add( new NetworkInfo
				{
					Selected = " ",
					InterfaceName = nic.Description,
					IPAddress = IPAddressString,
				} ); ;
			} );
		}

		private void OKInteraction()
		{
			NewProjectNotification notification = _notification as NewProjectNotification;
			_notification.Confirmed = true;
			FinishInteraction();
		}

		private void CancelInteraction()
		{
			_notification.Confirmed = false;
			FinishInteraction();
		}

		public INotification Notification
		{
			get { return _notification; }
			set
			{
				SetProperty( ref _notification, (IConfirmation) value );
			}
		}
	}
}
