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
		private static readonly string MCAST_GRP = "239.2.1.1";
		private static readonly int MCAST_PORT = 27781;					// マルチキャスト送信ポート
		private static readonly int SENDBACK_PORT = 27782;				// マルチキャストでラズパイからの返信ポート
		private static readonly int SHOOTIMAGESERVER_PORT = 27783;		// ラズパイからの画像返信用ポート

		private SyncshooterDefs _syncshooterDefs;
		private MultiCastClient _mcastClient;
		private Dictionary<string, int> _mapIP_Port;

		public NewSyncShooter()
		{
			_syncshooterDefs = null;
			_mcastClient = null;
			_mapIP_Port = null;
		}

		~NewSyncShooter()
		{
			if ( _mcastClient != null) {
				_mcastClient.Close();
			}
		}

		public void Initialize( string jsonFilePath )
		{
			// Loading SyncshooterDefs
			_syncshooterDefs = SyncshooterDefs.Deserialize( jsonFilePath );
			if (_mapIP_Port != null) {
				_mapIP_Port.Clear();
			}
		}

		public SyncshooterDefs GetSyncshooterDefs()
		{
			return _syncshooterDefs;
		}

		// カメラと接続する
		// 戻り値：	接続できたカメラのIPアドレスの配列
		public IEnumerable<string> ConnectCamera()
		{
			// UDP マルチキャストを開く
			_mcastClient = new MultiCastClient( MCAST_GRP, MCAST_PORT );
			if ( _mcastClient.Open() == false ) {
				return Array.Empty<string>();
			}
			// 接続できたラズパイのアドレスを列挙する
			return GetConnectedHostAddress();
		}

		// 指定IPアドレスのカメラのパラメータを取得する
		public CameraParam GetCameraParam( string ipAddress )
		{
			try {
				TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
				System.Net.Sockets.NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 10000;
				ns.WriteTimeout = 10000;

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
				tcp.Close();

				return CameraParam.DecodeFromJsonText( rcvString );
			} catch ( Exception e ) {
				Console.Error.WriteLine( e.Message );
				return null;
			}
		}

		// 指定IPアドレスのカメラにパラメータを設定する
		public void SetCameraParam( string ipAddress, CameraParam param )
		{
			try {
				TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
				System.Net.Sockets.NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 10000;
				ns.WriteTimeout = 10000;

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
				if ( rcvString != "ACK") {
					return;
				}

				// parameter を送信
				var jsonText = param.EncodeToJsonText();
				cmdBytes = System.Text.Encoding.UTF8.GetBytes( jsonText );
				ns.Write( cmdBytes, 0, cmdBytes.Length );

			} catch ( Exception e ) {
				Console.Error.WriteLine( e.Message );
			}
		}

		// 指定IPアドレスのカメラのプレビュー画像データを取得する
		public byte[] GetPreviewImage(string ipAddress)
		{
			TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
			System.Net.Sockets.NetworkStream ns = tcp.GetStream();
			ns.ReadTimeout = 10000;
			ns.WriteTimeout = 10000;
			MemoryStream ms = new MemoryStream();

			// get preview image コマンドを送信
			string cmd = "PRV";
			byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
			ns.Write( cmdBytes, 0, cmdBytes.Length );

			// データを受信
			while (ns.DataAvailable == false) {
			}
			ulong sum = 0;
			ulong bytes_to_read = 0;
			do {
				byte[] rcvBytes = new byte[tcp.Client.Available];
				int resSize = ns.Read( rcvBytes, 0, rcvBytes.Length );
				if (sum == 0) {
					// 先頭の4バイトには、次に続くデータのサイズが書かれている
					bytes_to_read = ((ulong)rcvBytes[0]) | ((ulong)rcvBytes[1] << 8) | ((ulong)rcvBytes[2] << 16) | ((ulong)rcvBytes[3] << 24);
					Console.Error.WriteLine( "bytes_to_read = {0}", bytes_to_read );
				}
				sum += (ulong)resSize;
				ms.Write( rcvBytes, 0, resSize );
			} while (sum < bytes_to_read + 4);
			//Console.Error.WriteLine( "size = {0}", (int) sum - 4 );
			ms.Close();
			ns.Close();
			tcp.Close();
			return ms.GetBuffer().Skip( 4 ).ToArray();
		}

		public byte[] GetPreviewImageFront()
		{
			if (_syncshooterDefs.front_camera == -1 ) {
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

		public byte[] GetFullImageInJpeg(string ipAddress)
		{
			_mcastClient.SendCommand( "SHJ" );
			TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
			System.Net.Sockets.NetworkStream ns = tcp.GetStream();
			ns.ReadTimeout = 10000;
			ns.WriteTimeout = 10000;
			MemoryStream ms = new MemoryStream();

			// full image 取得コマンドを送信
			string cmd = "IMG";
			byte[] cmdBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
			ns.Write( cmdBytes, 0, cmdBytes.Length );

			// データを受信
			while ( ns.DataAvailable == false ) {
			}
			ulong sum = 0;
			ulong bytes_to_read = 0;
			do {
				byte[] rcvBytes = new byte[tcp.Client.Available];
				int resSize = ns.Read( rcvBytes, 0, rcvBytes.Length );
				if ( sum == 0 ) {
					// 先頭の4バイトには、次に続くデータのサイズが書かれている
					bytes_to_read = ( (ulong) rcvBytes[0] ) | ( (ulong) rcvBytes[1] << 8 ) | ( (ulong) rcvBytes[2] << 16 ) | ( (ulong) rcvBytes[3] << 24 );
					//Console.WriteLine( "{0}: bytes_to_read = {1}", ipAddress, bytes_to_read );
				}
				sum += (ulong) resSize;
				ms.Write( rcvBytes, 0, resSize );
			} while ( sum < bytes_to_read + 4 );
			//Console.WriteLine( "size = {0}", (int) sum - 4 );
			ms.Close();
			ns.Close();
			tcp.Close();
			return ms.GetBuffer().Skip(4).ToArray();
		}

		// カメラを停止する
		// 入力：reboot : TRUE=再起動, FALSE=停止
		public void StopCamera(bool reboot)
		{
			if ( _mcastClient  != null) {
				_mcastClient.SendCommand( reboot ? "RBT" : "SDW" );
				_mcastClient = null;
			}
		}

		// 接続しているラズパイのアドレスを列挙する（アドレスの第4オクテットの昇順でソート）
		private IEnumerable<string> GetConnectedHostAddress()
		{
			UdpClient udp = new UdpClient( SENDBACK_PORT );
			udp.Client.ReceiveTimeout = 1000;

			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			_mcastClient.SendCommand( "INQ" );
			System.Threading.Thread.Sleep( 1000 );	// waitをおかないと、この後すぐに返事を受け取れない場合がある

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
			} catch (Exception e) {
				Console.Error.WriteLine( e.Message );
				udp.Close();
				return Array.Empty<string>();
			}
			udp.Close();

			// アドレスの第4オクテットの昇順でソート
			var connectedList = mapIPvsPort.OrderBy( pair =>
			{
				int idx = pair.Key.LastIndexOf('.');
				int adrs4th = int.Parse(pair.Key.Substring( idx  + 1 ));
				return adrs4th;
			} ).Select( pair => pair.Key );

			// SyncShooterDefsにある全IPアドレスリストにないものは除く
			return connectedList.Intersect( _syncshooterDefs.GetAllCameraIPAddress() );
		}

		//public IEnumerable<string> GetConnectedHostAddress( IEnumerable<string> ipAddressList )
		//{
		//	// UDP マルチキャストを開く
		//	_mcastClient = new MultiCastClient( MCAST_GRP, MCAST_PORT );
		//	if ( _mcastClient.Open() == false ) {
		//		return Array.Empty<string>();
		//	}

		//	// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
		//	_mcastClient.SendCommand( "INQ" );
		//	System.Threading.Thread.Sleep( 1000 );  // waitをおかないと、この後すぐに返事を受け取れない場合がある

		//	// "ACK"を返してきたカメラのIPアドレスを記録する
		//	List<string> connectedIpAddressList = new List<string>();
		//	foreach ( var ipAddress in ipAddressList ) {
		//		TcpClient tcp = new TcpClient( ipAddress, SENDBACK_PORT );
		//		System.Net.Sockets.NetworkStream ns = tcp.GetStream();
		//		ns.ReadTimeout = 10000;
		//		ns.WriteTimeout = 10000;

		//		// データを受信
		//		while ( tcp.Client.Available == 0 ) {
		//		}
		//		byte[] rcvBytes = new byte[tcp.Client.Available];
		//		ns.Read( rcvBytes, 0, tcp.Client.Available );
		//		string rcvString = System.Text.Encoding.UTF8.GetString( rcvBytes );
		//		tcp.Close();

		//		if ( rcvString == "ACK" ) {
		//			connectedIpAddressList.Add( ipAddress );
		//		}
		//	}

		//	// SyncShooterDefsにある全IPアドレスリストにないものは除く
		//	return connectedIpAddressList.Union( _syncshooterDefs.GetAllCameraIPAddress() );
		//}

	}
}
