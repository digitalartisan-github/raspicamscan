using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using TestHostApp2.Notifications;
using NewSyncShooter;

namespace TestHostApp2.ViewModels
{
	public class AwbPresetItem
	{
		public string Name { get; set; }
		public string InnerName { get; set; }
		public int Value { get; set; }
	}

	public class CameraSettingViewModel: BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		#region Properties
		public ReactiveProperty<string> IPAddress { get; } = new ReactiveProperty<string>( String.Empty );
		public ReactiveProperty<CameraParam> CameraParameter { get; } = new ReactiveProperty<CameraParam>( new CameraParam() );
		public List<AwbPresetItem> AwbPresetItems { get; } = new List<AwbPresetItem>(
			new AwbPresetItem[] {
				new AwbPresetItem{ Name = "自動",			InnerName = "auto",			Value = 0 },
				new AwbPresetItem{ Name = "蛍光灯",			InnerName = "fluorescent",	Value = 1 },
				new AwbPresetItem{ Name = "太陽光",			InnerName = "sunlight",		Value = 2 },
				new AwbPresetItem{ Name = "曇天",			InnerName = "cloudy",		Value = 3 },
				new AwbPresetItem{ Name = "日陰",			InnerName = "shade",		Value = 4 },
				new AwbPresetItem{ Name = "タングステン",	InnerName = "tungsten",		Value = 5 },
				new AwbPresetItem{ Name = "白熱電球",		InnerName = "incandescent",	Value = 6 },
				new AwbPresetItem{ Name = "フラッシュ",		InnerName = "flash",		Value = 7 },
				new AwbPresetItem{ Name = "夕日",			InnerName = "horizon",		Value = 8 },
			} );
		public ReactiveProperty<int> AwbPresetValue { get; }
		public ReactiveProperty<bool> IsAwbPreset { get; }
		public ReactiveProperty<bool> IsAwbManual { get; }
		public ReactiveProperty<int> WbGreenIntValue { get; }
		public ReactiveProperty<string> WbGreenTextValue { get; }
		public ReactiveProperty<int> WbRedIntValue { get; }
		public ReactiveProperty<string> WbRedTextValue { get; }
		public ReactiveProperty<int> Brightness { get; }
		public ReactiveProperty<bool> IsShutterSpeedAuto { get; }
		public ReactiveProperty<bool> IsShutterSpeedManual { get; }
		public ReactiveProperty<int> ShutterSpeed { get; }
		public ReactiveProperty<string> WbOffsetMin { get; } = new ReactiveProperty<string>( String.Empty );
		public ReactiveProperty<string> WbOffsetMax { get; } = new ReactiveProperty<string>( String.Empty );
		#endregion

		#region Commands
		public ReactiveCommand ApplyToAllCameraCommand { get; } = new ReactiveCommand();
		public ReactiveCommand OKCommand { get; } = new ReactiveCommand();
		public ReactiveCommand CancelCommand { get; } = new ReactiveCommand();
		#endregion

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public CameraSettingViewModel()
		{
			this.AwbPresetValue = this.CameraParameter.Select( p => {
				if ( p.awb_mode == "off" ) {
					return 0;
				} else {
					return this.AwbPresetItems.Find( item => item.InnerName == p.awb_mode ).Value;
				}
			} ).ToReactiveProperty();
			this.AwbPresetValue.Subscribe( v => this.CameraParameter.Value.awb_mode = this.AwbPresetItems[v].InnerName );

			this.IsAwbPreset = this.CameraParameter.Select( p => p.awb_mode != "off" ).ToReactiveProperty();
			this.IsAwbPreset.Subscribe( b => {
				if ( b ) {
					this.CameraParameter.Value.awb_mode = "auto";
					RaisePropertyChanged( "AwbPresetValue");
				}
			} );

			this.IsAwbManual = this.CameraParameter.Select( p => p.awb_mode == "off" ).ToReactiveProperty();
			this.IsAwbManual.Subscribe( b => {
				if ( b ) {
					this.CameraParameter.Value.awb_mode = "manual";
				}
			} );

			this.WbGreenIntValue = this.CameraParameter.Select( p => (int) ( p.wb_gb * 100.0 ) ).ToReactiveProperty();
			this.WbGreenIntValue.Subscribe( v => {
				this.CameraParameter.Value.wb_gb = (double) v * 0.01;
			} );
			this.WbGreenTextValue = this.CameraParameter.Select( p => string.Format( "{0:0.00}", p.wb_gb ) ).ToReactiveProperty();
			this.WbGreenTextValue.Subscribe( v => {
				if ( Double.TryParse( v, out double d ) ) {
					this.CameraParameter.Value.wb_gb = d;
				}
			} );

			this.WbRedIntValue = this.CameraParameter.Select( p => (int) ( p.wb_rg * 100.0 ) ).ToReactiveProperty();
			this.WbRedIntValue.Subscribe( v => {
				this.CameraParameter.Value.wb_rg = (double) v * 0.01;
			} );
			this.WbRedTextValue = this.CameraParameter.Select( p => string.Format( "{0:0.00}", p.wb_rg ) ).ToReactiveProperty();
			this.WbRedTextValue.Subscribe( v => {
				if ( Double.TryParse( v, out double d ) ) {
					this.CameraParameter.Value.wb_rg = d;
				}
			} );

			this.Brightness = this.CameraParameter.Select( p => p.brightness ).ToReactiveProperty();
			this.Brightness.Subscribe( v => this.CameraParameter.Value.brightness = v );

			this.IsShutterSpeedAuto = this.CameraParameter.Select( p => p.Shutter_speed_mode == "auto" ).ToReactiveProperty();
			this.IsShutterSpeedAuto.Subscribe( v => this.CameraParameter.Value.Shutter_speed_mode = ( v ) ? "auto" : "manual" );

			this.IsShutterSpeedManual = this.CameraParameter.Select( p => p.Shutter_speed_mode == "manual" ).ToReactiveProperty();
			this.IsShutterSpeedManual.Subscribe( v => this.CameraParameter.Value.Shutter_speed_mode = ( v ) ? "manual" : "auto" );

			this.ShutterSpeed = this.CameraParameter.Select( p => 1000000 / p.shutter_speed ).ToReactiveProperty();
			this.ShutterSpeed.Subscribe( v => this.CameraParameter.Value.shutter_speed = 1000000 / v );

			this.WbOffsetMin.Value = CameraParam.WbOffsetMin.ToString( "0.0" );
			this.WbOffsetMax.Value = CameraParam.WbOffsetMax.ToString( "0.0" );

			ApplyToAllCameraCommand.Subscribe( ApplyToAllCameraInteraction );
			OKCommand.Subscribe( OKInteraction );
			CancelCommand.Subscribe( CancelInteraction );
		}

		private void ApplyToAllCameraInteraction()
		{
			var notification = _notification as CameraSettingNotification;
			notification.IsApplyToAllCamera = true;
			notification.CameraParameter = this.CameraParameter.Value;
			_notification.Confirmed = true;
			FinishInteraction?.Invoke();
		}

		private void OKInteraction()
		{
			var notification = _notification as CameraSettingNotification;
			notification.IsApplyToAllCamera = false;
			notification.CameraParameter = this.CameraParameter.Value;
			_notification.Confirmed = true;
			FinishInteraction?.Invoke();
		}

		private void CancelInteraction()
		{
			_notification.Confirmed = false;
			FinishInteraction?.Invoke();
		}

		public INotification Notification
		{
			get { return _notification; }
			set
			{
				SetProperty( ref _notification, (IConfirmation) value );
				var notification = _notification as CameraSettingNotification;
				this.IPAddress.Value = notification.IPAddress;
				this.CameraParameter.Value = notification.CameraParameter;
			}
		}
	}
}
