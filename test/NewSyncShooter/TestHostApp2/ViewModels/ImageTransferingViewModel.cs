using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Interactivity.InteractionRequest;
using Reactive.Bindings;
using TestHostApp2.Notifications;

namespace TestHostApp2.ViewModels
{
	public class ImageTransferingViewModel : BindableBase, IInteractionRequestAware
	{
		public Action FinishInteraction { get; set; }
		private IConfirmation _notification;

		public ReactiveProperty<BitmapSource> PreviewingImage { get; set; } = new ReactiveProperty<BitmapSource>();

		public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		public ImageTransferingViewModel()
		{
			CancelCommand = new DelegateCommand( CancelInteraction );
		}

		private void OKInteraction()
		{
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
				var notification = _notification as ImagTransferingNotification;

				// 正面カメラの画像を取得する
				byte[] imaegData = notification.SyncShooter.GetPreviewImageFront();
				if ( imaegData.Length == 0 ) {
					// TODO: 「正面カメラの画像を取得できませんでした」
					return;
				}
				var ms = new MemoryStream( imaegData );
				BitmapSource bitmapSource = BitmapFrame.Create( ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad );
				this.PreviewingImage.Value = bitmapSource;

				notification.ConnectedIPAddressList.AsParallel().ForAll( adrs =>
				{
					// 画像を撮影＆取得
					byte[] data = notification.SyncShooter.GetFullImageInJpeg( adrs );
					// IP Address の第4オクテットのファイル名で保存する
					int idx = adrs.LastIndexOf('.');
					int adrs4th = int.Parse(adrs.Substring( idx  + 1 ));
					String path = Path.Combine( notification.TargetDir, string.Format( "{0}.jpg", adrs4th ) );
					using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
						fs.Write( data, 0, (int) data.Length );
					}
				} );

				//OKInteraction();
			}
		}

	}
}
