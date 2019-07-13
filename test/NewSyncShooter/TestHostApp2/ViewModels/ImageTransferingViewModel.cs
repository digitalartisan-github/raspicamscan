using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Threading.Tasks;
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
		private SynchronizationContext _mainContext = null;
		private CancellationTokenSource _tokenSource = null;

		public ReactiveProperty<int> ProgressMaxValue { get; } = new ReactiveProperty<int>( 0 );
		public ReactiveProperty<int> ProgressValue { get; } = new ReactiveProperty<int>( 0 );
		public ReactiveProperty<string> ProgressText { get; } = new ReactiveProperty<string>( string.Empty );
		public ReactiveProperty<string> Information { get; } = new ReactiveProperty<string>( string.Empty );
		//public ReactiveProperty<BitmapSource> PreviewingImage { get; } = new ReactiveProperty<BitmapSource>();

		public InteractionRequest<Notification> CloseWindowRequest { get; } = new InteractionRequest<Notification>();

		//public DelegateCommand OkCommand { get; private set; }
		public DelegateCommand CancelCommand { get; private set; }

		public ImageTransferingViewModel()
		{
			CancelCommand = new DelegateCommand( CancelInteraction );
			//_tokenSource = new CancellationTokenSource();
		}

		~ImageTransferingViewModel()
		{
			//_tokenSource.Cancel();
			if ( _tokenSource != null ) {
				_tokenSource.Dispose();
			}
		}

		//private void OKInteraction()
		//{
		//	_notification.Confirmed = true;
		//	FinishInteraction();
		//}

		private void CancelInteraction()
		{
			_tokenSource.Cancel();
			_notification.Confirmed = false;
			FinishInteraction();
		}

		private async Task TransferImageAsync( CancellationTokenSource tokenSource )
		{
			CancellationToken token = tokenSource.Token;
			var notification = _notification as ImagTransferingNotification;
			this.ProgressMaxValue.Value = notification.ConnectedIPAddressList.Count();
				// 撮影コマンドを全ラズパイカメラへ送信する
				notification.SyncShooter.SendCommandToGetFullImageInJpeg();
				this.ProgressValue.Value = 0;
				this.Information.Value = string.Empty;
				await Task.Factory.StartNew( () => {
					int progressCount = 0;
					try {
						//object o = new object();
						notification.ConnectedIPAddressList.AsParallel().WithCancellation( token ).ForAll( ipAddress => {
							// 画像を撮影＆取得
							byte[] data = notification.SyncShooter.GetFullImageInJpeg( ipAddress, out int portNo );
							if ( data.Length > 0 ) {
								// IP Address の第4オクテットのファイル名で保存する
								int idx = ipAddress.LastIndexOf('.');
								int adrs4th = int.Parse( ipAddress.Substring( idx  + 1 ) );
								String path = Path.Combine( notification.TargetDir, string.Format( "{0}.jpg", adrs4th ) );
								using ( var fs = new FileStream( path, FileMode.Create, FileAccess.Write ) ) {
									fs.Write( data, 0, data.Length );
								}
								//lock ( o ) {
									this.Information.Value = string.Format( "{0}:{1} received.", ipAddress, portNo );
									this.ProgressValue.Value = ++progressCount;
								//}
							}
						} );
					} catch ( OperationCanceledException ex ) {
						System.Diagnostics.Debug.WriteLine( ex.Message );
						_notification.Confirmed = false;
					}
				} );
			// メインスレッドに処理を戻して、ウインドウを閉じる処理を実行する
			_mainContext.Post( _ => CloseWindowRequest.Raise( null ), null );
			_notification.Confirmed = true;
		}

		public INotification Notification
		{
			get { return _notification; }
			set
			{
				SetProperty( ref _notification, (IConfirmation) value );

				var notification = _notification as ImagTransferingNotification;
				//this.PreviewingImage.Value = notification.Image;

				_mainContext = SynchronizationContext.Current;
				_tokenSource = new CancellationTokenSource();
				Task tsk = Task.Run(() => TransferImageAsync(_tokenSource));
			}
		}

	}
}
