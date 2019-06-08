using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace NewSyncShooter
{
	public class NewSyncShooter
	{
		// マルチキャストグループのIPアドレスとポート
		// （syncshooter.py 中の MCAST_GRP, MCAST_PORT に対応）
		private static readonly string MCAST_GRP = "239.2.1.1";
		private static readonly int MCAST_PORT = 27781;
		private static readonly int SENDBACK_PORT = 27782;
		private static readonly int SHOOTIMAGESERVER_PORT = 27783;

		SyncshooterDefs _syncshooterDefs;
		MultiCastClient _mcastClient;

		public NewSyncShooter()
		{
			_syncshooterDefs = null;
			_mcastClient = null;
		}

		public void Initialize()
		{
			// Loading SyncshooterDefs
			_syncshooterDefs = SyncshooterDefs.Deserialize( "syncshooterDefs.json" );
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

			// 接続しているラズパイのアドレスとポートを列挙する
			var mapIPvsPort = GetConectedHostAddress( _mcastClient );
			return mapIPvsPort.Select( pair => { return pair.Key.ToString(); } );
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

		// 接続しているラズパイのアドレスとポートを列挙する
		Dictionary<IPAddress, int> GetConectedHostAddress( MultiCastClient mcastClient )
		{
			UdpClient udp = new UdpClient( SENDBACK_PORT );
			udp.Client.ReceiveTimeout = 1000;

			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			mcastClient.SendCommand( "INQ" );

			var mapIPvsPort = new Dictionary<IPAddress, int>();
			try {
				do {
					// 任意IP Address, 任意のポートからデータを受信する 
					IPEndPoint remoteEP = null;
					byte[] rcvBytes = udp.Receive( ref remoteEP );
					string rcvMsg = System.Text.Encoding.UTF8.GetString( rcvBytes );
					if ( rcvMsg == "ACK" ) {
						Console.WriteLine( "受信したデータ:{0}", rcvMsg );
						Console.WriteLine( "送信元アドレス:{0}/ポート番号:{1}", remoteEP.Address, remoteEP.Port );  // この送信元ポート番号は毎度変わる
						mapIPvsPort[remoteEP.Address] = remoteEP.Port;
					}
				} while ( udp.Available > 0 );
			} catch (Exception e) {
				Console.Error.WriteLine( e.Message );
				udp.Close();
				return mapIPvsPort;
			}
			udp.Close();
			return mapIPvsPort;
		}

	}
}
