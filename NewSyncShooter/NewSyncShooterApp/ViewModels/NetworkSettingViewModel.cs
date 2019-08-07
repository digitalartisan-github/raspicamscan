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
using NewSyncShooterApp.Notifications;

namespace NewSyncShooterApp.ViewModels
{
	public class NetworkInfo
	{
		public ReactiveProperty<string> Selected { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> InterfaceName { get; set; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> IPAddress { get; set; } = new ReactiveProperty<string>( string.Empty );
	}

	public class NetworkSettingViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification = null;

		public List<NetworkInfo> NetworkInfoList { get; } = new List<NetworkInfo>();
		//public ReactiveCollection<NetworkInfo> NetworkInfoList { get; set; } = new ReactiveCollection<NetworkInfo>();
		public ReactiveProperty<int> SelectedIndex { get; } = new ReactiveProperty<int>( 0 );
		public ReactiveProperty<string> InterfaceName { get; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> IPAddress { get; } = new ReactiveProperty<string>( string.Empty );

		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public NetworkSettingViewModel()
		{
			collectNetworkInformation();

			// DataGrid の行が選択された時の動作
			SelectedIndex.Subscribe( idx => {
				this.NetworkInfoList.ForEach( info => info.Selected.Value = string.Empty );
				if ( idx != -1 ) {
					this.NetworkInfoList[idx].Selected.Value = "*";
					this.InterfaceName.Value = this.NetworkInfoList[idx].InterfaceName.Value;
					this.IPAddress.Value = this.NetworkInfoList[idx].IPAddress.Value;
					if ( _notification != null ) {
						var notification = _notification as NetworkSettingNotification;
						notification.LocalHostIP = this.IPAddress.Value;
					}
				}
			} );

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
					Selected = new ReactiveProperty<string>(" "),
					InterfaceName = new ReactiveProperty<string>( nic.Description ),
					IPAddress = new ReactiveProperty<string>( IPAddressString ),
					//Selected = " ",
					//InterfaceName = nic.Description,
					//IPAddress = IPAddressString,
				} ); ;
			} );
		}

		private void OKInteraction()
		{
			NetworkSettingNotification notification = _notification as NetworkSettingNotification;
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

				var notification = _notification as NetworkSettingNotification;
				this.SelectedIndex.Value = NetworkInfoList.ToList().FindIndex( 0, info => info.IPAddress.Value == notification.LocalHostIP );
			}
		}
	}
}
