using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace NewSyncShooter
{
	public class NewSyncShooter
	{
		// マルチキャストグループのIPアドレスとポート
		// （syncshooter.py 中の MCAST_GRP, MCAST_PORT に対応）
		private static readonly string MCAST_GRP				= "239.2.1.1";
		private static readonly int MCAST_PORT					= 27781;	// マルチキャスト送信ポート
		private static readonly int SENDBACK_PORT				= 27782;	// マルチキャストでラズパイからの返信ポート
		private static readonly int SHOOTIMAGESERVER_PORT		= 27783;	// ラズパイからの画像返信用ポート
		private static readonly int SHOOTIMAGESERVER_PORT_NUM	= 32;		// ラズパイからの画像返信用ポートの数

		private SyncshooterDefs _syncshooterDefs = null;
		private MultiCastClient _mcastClient = null;
		private Dictionary<string, int> _mapIP_Port = null;

		public NewSyncShooter()
		{
			_syncshooterDefs = null;
			_mcastClient = null;
			_mapIP_Port = null;
		}

		~NewSyncShooter()
		{
			if ( _mcastClient != null ) {
				_mcastClient.Close();
			}
		}

		public void Initialize( string jsonFilePath )
		{
			// Loading SyncshooterDefs
			_syncshooterDefs = SyncshooterDefs.Deserialize( jsonFilePath );
			if ( _mapIP_Port != null ) {
				_mapIP_Port.Clear();
			}
		}

		public SyncshooterDefs GetSyncshooterDefs()
		{
			return _syncshooterDefs;
		}

		/// <summary>
		/// カメラと接続する
		/// 戻り値：	接続できたカメラのIPアドレスの配列
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> ConnectCamera()
		{
			// UDP マルチキャストを開く
			if ( _mcastClient == null ) {
				_mcastClient = new MultiCastClient( MCAST_GRP, MCAST_PORT );
				if ( _mcastClient.Open() == false ) {
					return Array.Empty<string>();
				}
			}
			// 接続できたラズパイのアドレスを列挙する
			return GetConnectedHostAddressUDP();
			//return GetConnectedHostAddressTCP();
		}

		// 接続しているラズパイのアドレスを列挙する(UDP Protocol)（アドレスの第4オクテットの昇順でソート）
		private IEnumerable<string> GetConnectedHostAddressUDP()
		{
			UdpClient udp = new UdpClient( SENDBACK_PORT );
			udp.Client.ReceiveTimeout = 1000;

			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			_mcastClient.SendCommand( "INQ" );
			System.Threading.Thread.Sleep( 1000 );  // waitをおかないと、この後すぐに返事を受け取れない場合がある

			var mapIPvsPort = new Dictionary<string, int>();
			try {
				do {
					// 任意IP Address, 任意のポートからデータを受信する 
					IPEndPoint remoteEP = null;
					byte[] rcvBytes = udp.Receive( ref remoteEP );
					string rcvMsg = System.Text.Encoding.UTF8.GetString( rcvBytes );
					if ( rcvMsg == "ACK" ) {
						Console.Error.WriteLine( "送信元アドレス:{0}/ポート番号:{1}", remoteEP.Address.ToString(), remoteEP.Port );  // この送信元ポート番号は毎度変わる
						mapIPvsPort[remoteEP.Address.ToString()] = remoteEP.Port;
					}
				} while ( udp.Available > 0 );
			} catch ( Exception e ) {
				System.Diagnostics.Debug.WriteLine( e.Message );
				udp.Close();
				return Array.Empty<string>();
			}
			udp.Close();

			// アドレスの第4オクテットの昇順でソート
			return mapIPvsPort.OrderBy( pair => {
				int idx = pair.Key.LastIndexOf('.');
				int adrs4th = int.Parse(pair.Key.Substring( idx  + 1 ));
				return adrs4th;
			} ).Select( pair => pair.Key )  // (結局はIPアドレスのみを残しポート番号は捨てる)
											// SyncShooterDefsにある全IPアドレスリストにないものは除く
			.Intersect( _syncshooterDefs.GetAllCameraIPAddress() );
		}

		// 接続しているラズパイのアドレスを列挙する(TCP Protocol)（アドレスの第4オクテットの昇順でソート）
		public IEnumerable<string> GetConnectedHostAddressTCP()
		{
			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			_mcastClient.SendCommand( "INQ" );
			System.Threading.Thread.Sleep( 1000 );

			// "ACK"を返してきたカメラのIPアドレスを記録する
			var ipAddressList = _syncshooterDefs.GetAllCameraIPAddress();
			var connectedIpAddressList = new List<string>();
#if true
			var tcpSocketServer = new TcpSocketServer(SENDBACK_PORT);
			tcpSocketServer.Run();
			//var ipEndPoint = new IPEndPoint( IPAddress.Loopback, SENDBACK_PORT );
			//var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
			//socket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
			//socket.Bind( ipEndPoint );
			//socket.Listen( 10 );

#else
			foreach ( var ipAddress in ipAddressList ) {
				var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
				socket.Connect( ipAddress, SENDBACK_PORT );
				bool bConnected = socket.Connected;
				//var result = socket.BeginConnect( ipAddress, SENDBACK_PORT, null, null );
				//socket.ReceiveTimeout = 1000;
				//bool bConnected = result.AsyncWaitHandle.WaitOne( 100, true );
				if ( bConnected ) {
					while ( socket.Available == 0 ) {
					}
					var rcvBytes = new byte[socket.Available];
					socket.Receive( rcvBytes );
					var rcvString = System.Text.Encoding.UTF8.GetString( rcvBytes );
					if ( rcvString == "ACK" ) {
						connectedIpAddressList.Add( ipAddress );
					}
					socket.Close();
				}
			}
#endif
			return connectedIpAddressList;
		}

		/// <summary>
		/// 指定IPアドレスのカメラのパラメータを取得する
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		public CameraParam GetCameraParam( string ipAddress )
		{
			try {
				TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
				NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 5000;
				ns.WriteTimeout = 5000;

				// get parameter コマンドを送信
				string cmd = "PGT";
				byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
				ns.Write( cmdBytes, 0, cmdBytes.Length );

				// データを受信
				while ( tcp.Client.Available == 0 ) {
				}
				byte[] rcvBytes = new byte[tcp.Client.Available];
				ns.Read( rcvBytes, 0, tcp.Client.Available );
				string rcvString = System.Text.Encoding.UTF8.GetString( rcvBytes );

				ns.Close();
				tcp.Close();
				return CameraParam.DecodeFromJsonText( rcvString );

			} catch ( Exception e ) {
				Console.Error.WriteLine( e.Message );
				return null;
			}
		}

		/// <summary>
		/// 指定IPアドレスのカメラにパラメータを設定する
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="param"></param>
		public void SetCameraParam( string ipAddress, CameraParam param )
		{
			try {
				TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
				NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 5000;
				ns.WriteTimeout = 5000;

				// set parameter コマンドを送信
				string cmd = "PST";
				byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
				ns.Write( cmdBytes, 0, cmdBytes.Length );

				// "ACK"データを受信
				while ( tcp.Client.Available == 0 ) {
				}
				byte[] rcvBytes = new byte[tcp.Client.Available];
				ns.Read( rcvBytes, 0, tcp.Client.Available );
				string rcvString = System.Text.Encoding.UTF8.GetString( rcvBytes );
				if ( rcvString == "ACK" ) {
					// parameter を送信
					var jsonText = param.EncodeToJsonText();
					var sendBytes = System.Text.Encoding.UTF8.GetBytes( jsonText );
					// 送信するデータサイズを送る (little endian)
					byte[] sizeBytes = new byte[4];
					sizeBytes[0] = (byte) ( sendBytes.Length & 0xff );
					sizeBytes[1] = (byte) ( ( sendBytes.Length & 0xff00 ) >> 8 );
					sizeBytes[2] = (byte) ( ( sendBytes.Length & 0xff0000 ) >> 16 );
					sizeBytes[3] = (byte) ( ( sendBytes.Length & 0xff000000 ) >> 24 );
					ns.Write( sizeBytes, 0, sizeBytes.Length );
					// パラメータを送信する
					ns.Write( sendBytes, 0, sendBytes.Length );
				}
				ns.Close();
				tcp.Close();

			} catch ( Exception e ) {
				Console.Error.WriteLine( e.Message );
			}
		}

		/// <summary>
		/// 指定IPアドレスのカメラのプレビュー画像データを取得する
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <returns></returns>
		public byte[] GetPreviewImage( string ipAddress )
		{
			TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
			System.Net.Sockets.NetworkStream ns = tcp.GetStream();
			ns.ReadTimeout = 5000;
			ns.WriteTimeout = 5000;

			// get preview image コマンドを送信
			string cmd = "PRV";
			byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
			ns.Write( cmdBytes, 0, cmdBytes.Length );

			// データを受信
			ulong sum = 0;
			ulong bytes_to_read = 0;
			MemoryStream ms = new MemoryStream();
			do {
				if ( tcp.Available == 0 ) {
					continue;   // Retry
				}
				byte[] rcvBytes = new byte[tcp.Client.Available];
				int resSize = 0;
				try {
					resSize = ns.Read( rcvBytes, 0, rcvBytes.Length );
				} catch ( IOException e ) {
					if ( e.InnerException is SocketException ) {
						var socketException = e.InnerException as SocketException;
						if ( socketException.SocketErrorCode == SocketError.TimedOut ) {
							resSize = 0;    // 再試行させる
						} else {
							throw e;
						}
					} else {
						throw e;
					}
				}
				if ( resSize == 0 ) {
					continue;
				}
				if ( sum == 0 ) {
					// 先頭の4バイトには、次に続くデータのサイズが書かれている
					bytes_to_read = ( (ulong) rcvBytes[0] ) | ( (ulong) rcvBytes[1] << 8 ) | ( (ulong) rcvBytes[2] << 16 ) | ( (ulong) rcvBytes[3] << 24 );
				}
				sum += (ulong) resSize;
				ms.Write( rcvBytes, 0, resSize );
			} while ( sum < bytes_to_read + 4 );
			ms.Close();
			ns.Close();
			tcp.Close();
			return ms.GetBuffer().Skip( 4 ).ToArray();
		}

		public byte[] GetPreviewImageFront()
		{
			if ( _syncshooterDefs.front_camera == -1 ) {
				return Array.Empty<byte>();
			} else {
				return GetPreviewImage( _syncshooterDefs.FrontCameraAddress );
			}
		}
		public byte[] GetPreviewImageBack()
		{
			if ( _syncshooterDefs.back_camera == -1 ) {
				return Array.Empty<byte>();
			} else {
				return GetPreviewImage( _syncshooterDefs.BackCameraAddress );
			}
		}
		public byte[] GetPreviewImageRight()
		{
			if ( _syncshooterDefs.right_camera == -1 ) {
				return Array.Empty<byte>();
			} else {
				return GetPreviewImage( _syncshooterDefs.RightCameraAddress );
			}
		}
		public byte[] GetPreviewImageLeft()
		{
			if ( _syncshooterDefs.left_camera == -1 ) {
				return Array.Empty<byte>();
			} else {
				return GetPreviewImage( _syncshooterDefs.LeftCameraAddress );
			}
		}

		/// <summary>
		/// 撮影コマンドを全ラズパイカメラへ送信する
		/// </summary>
		public void SendCommandToGetFullImageInJpeg()
		{
			_mcastClient.SendCommand( "SHJ" );
		}

		/// <summary>
		/// 指定IPアドレスのカメラの撮影画像を取得する
		/// （先に SendCommandToGetFullImageInJpeg()で全ラズパイカメラに撮影命令を送信しておく ）
		/// </summary>
		/// <param name="ipAddress"></param>
		/// <param name="portNo"></param>
		/// <returns></returns>
		public byte[] GetFullImageInJpeg( string ipAddress, out int portNo )
		{
			// この関数はマルチスレッドで実行されるため、異なるIPアドレスでのデータ受信が
			// 同じポートで重ならないようにする必要がある。
			// そのため、IP Address の第4オクテットの数値に応じてポート番号を選択する。
			int idx = ipAddress.LastIndexOf('.');
			int adrs4th = int.Parse( ipAddress.Substring( idx  + 1 ) );
			portNo = SHOOTIMAGESERVER_PORT + ( adrs4th % SHOOTIMAGESERVER_PORT_NUM );
			System.Diagnostics.Debug.WriteLine( "{0}:{1}", ipAddress, portNo );

			var tcp = new TcpClient( ipAddress, portNo );
			var ns = tcp.GetStream();
			ns.ReadTimeout = 10000;
			ns.WriteTimeout = 10000;

			// full image 取得コマンドを送信
			string cmd = "IMG";
			byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
			ns.Write( cmdBytes, 0, cmdBytes.Length );

			// データを受信
			ulong sum = 0;
			ulong bytesToRead = 0;
			MemoryStream ms = new MemoryStream();
			do {
				if ( tcp.Available == 0 ) {
					continue;	// Retry
				}
				byte[] rcvBytes = new byte[tcp.Available];
				int resSize;
				try {
					resSize = ns.Read( rcvBytes, 0, rcvBytes.Length );
				} catch ( IOException e ) {
					if ( e.InnerException is SocketException ) {
						var socketException = e.InnerException as SocketException;
						if ( socketException.SocketErrorCode == SocketError.TimedOut ) {
							resSize = 0;    // 再試行させる
						} else {
							throw e;
						}
					} else {
						throw e;
					}
				}
				if ( resSize >= 4 ) {
					if ( sum == 0 ) {
						// 先頭の4バイトには、次に続くデータのサイズが書かれている
						bytesToRead = ( (ulong) rcvBytes[0] ) | ( (ulong) rcvBytes[1] << 8 ) | ( (ulong) rcvBytes[2] << 16 ) | ( (ulong) rcvBytes[3] << 24 );
					}
					sum += (ulong) resSize;
					ms.Write( rcvBytes, 0, resSize );
				}
			} while ( sum < bytesToRead + 4 );

			ms.Close();
			ns.Close();
			tcp.Close();
			return ms.GetBuffer().Skip( 4 ).ToArray();
		}

		/// <summary>
		/// カメラを停止する
		/// 入力：reboot : TRUE=再起動, FALSE=停止
		/// </summary>
		/// <param name="reboot"></param>
		public void StopCamera( bool reboot )
		{
			if ( _mcastClient != null ) {
				_mcastClient.SendCommand( reboot ? "RBT" : "SDW" );
				_mcastClient = null;
			}
		}

	}
}
