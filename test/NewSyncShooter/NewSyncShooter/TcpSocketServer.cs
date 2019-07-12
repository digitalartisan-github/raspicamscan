using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NewSyncShooter
{
	public class StateObject
	{
		public Socket ClientSocket { get; set; }
		public const int BufferSize = 1024;
		public byte[] Buffer { get; } = new byte[BufferSize];
	}

	public class TcpSocketServer
	{
		// スレッド待機用
		private ManualResetEvent AllDone = new ManualResetEvent(false);

		// サーバーのエンドポイント
		public IPEndPoint IPEndPoint { get; }

		// 接続中のクライアント(スレッドセーフコレクション)
		public SynchronizedCollection<Socket> ClientSockets { get; } = new SynchronizedCollection<Socket>();

		public TcpSocketServer( int port )
		{
			this.IPEndPoint = new IPEndPoint( IPAddress.Loopback, port );
		}

		public void Run()
		{
			using ( var listenerSocket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) ) {
				// ソケットをアドレスにバインドする
				listenerSocket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
				listenerSocket.Bind( this.IPEndPoint );
				// 接続待機開始
				listenerSocket.Listen( 10 );
				Console.WriteLine( $"サーバーを起動しました ... [{listenerSocket.LocalEndPoint}]" );
				while ( true ) {
					AllDone.Reset();
					listenerSocket.BeginAccept( new AsyncCallback( AcceptCallback ), listenerSocket );
					AllDone.WaitOne();
				}
			}
		}

		// 接続受付時のコールバック処理
		private void AcceptCallback( IAsyncResult asyncResult )
		{
			// 待機スレッドが進行するようにシグナルをセット
			AllDone.Set();

			// ソケットを取得
			var listenerSocket = asyncResult.AsyncState as Socket;
			var clientSocket = listenerSocket.EndAccept(asyncResult);

			// 接続中のクライアントを追加
			ClientSockets.Add( clientSocket );
			Console.WriteLine( $"接続: {clientSocket.RemoteEndPoint}" );

			// StateObjectを作成
			var state = new StateObject();
			state.ClientSocket = clientSocket;

			// 受信時のコードバック処理を設定
			clientSocket.BeginReceive( state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback( ReceiveCallback ), state );
		}

		private void ReceiveCallback( IAsyncResult asyncResult )
		{
			// StateObjectとクライアントソケットを取得
			var state = asyncResult.AsyncState as StateObject;
			var clientSocket = state.ClientSocket;

			// クライアントソケットから受信データを取得終了
			int bytes = clientSocket.EndReceive(asyncResult);

			if ( bytes > 0 ) {
				// 受信した文字列を表示
				var content = System.Text.Encoding.UTF8.GetString( state.Buffer, 0, bytes );
				Console.WriteLine( $"受信データ: {content} [{state.ClientSocket.RemoteEndPoint}]" );

				//// 受信文字列を接続中全クライアントに送信。
				//SendAllClient( content );

				// 受信時のコードバック処理を再設定
				clientSocket.BeginReceive( state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback( ReceiveCallback ), state );
			} else {
				// 0バイトデータの受信時は、切断されたとき?
				clientSocket.Close();
				this.ClientSockets.Remove( clientSocket );
			}
		}
	}
}
