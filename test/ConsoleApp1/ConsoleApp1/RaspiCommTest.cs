using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;

namespace ConsoleApp1
{
	class RaspiCommTest
	{
		// マルチキャストグループのIPアドレスとポート
		// （syncshooter.py 中の MCAST_GRP, MCAST_PORT に対応）
		private static readonly string MCAST_GRP = "239.2.1.1";
		private static readonly int MCAST_PORT = 27781;
		private static readonly int SENDBACK_PORT = 27782;
		private static readonly int SHOOTIMAGESERVER_PORT = 27783;

		// UDP マルチキャストを開く
		static bool OpenMultiCast(out UdpClient mcastClient, out IPEndPoint mcastPoint )
		{
			mcastClient = null;
			mcastPoint = null;
			try {
				mcastClient = new UdpClient();
				mcastPoint = new IPEndPoint( IPAddress.Parse( MCAST_GRP ), MCAST_PORT );
				mcastClient.JoinMulticastGroup( mcastPoint.Address );
			} catch (Exception e) {
				Console.Error.WriteLine( e.InnerException );
				return false;
			}
			return true;
		}

		// マルチキャストに参加しているラズパイにコマンドを送信
		static bool SendCommand_MultiCast( ref UdpClient mcastClient, ref IPEndPoint mcastPoint, string cmd )
		{
			try {
				byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
				mcastClient.Send( sendBytes, sendBytes.Length, mcastPoint );
			} catch ( Exception e ) {
				Console.Error.WriteLine( e.InnerException );
				return false;
			}
			return true;
		}

		// 接続しているラズパイのアドレスとポートを列挙する
		static Dictionary<IPAddress, int> GetConectedHostAddress( ref UdpClient mcastClient, ref IPEndPoint mcastPoint )
		{
			UdpClient udp = new UdpClient( SENDBACK_PORT );

			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			SendCommand_MultiCast( ref mcastClient, ref mcastPoint, "INQ" );

			var mapIPvsPort = new Dictionary<IPAddress, int>();
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
			udp.Close();
			return mapIPvsPort;
		}

		// Camera parameter を取得する
		static string GetCameraParameterInJson( string ipAddress )
		{
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
			return rcvString;
		}

		static void Main( string[] args )
		{
			// Loading SyncshooterDefs
			var defs = SyncshooterDefs.Deserialize( @"..\..\syncshooterDefs.json" );
			// TEST
			defs.Serialize( @"..\..\syncshooterDefs_copy.json" );

			// UDP マルチキャストを開く
			OpenMultiCast( out UdpClient mcastClient, out IPEndPoint mcastPoint );

			// 接続しているラズパイのアドレスとポートを列挙する
			var mapIPvsPort = GetConectedHostAddress( ref mcastClient, ref mcastPoint );

			// Camera parameter を取得する
			foreach ( var pair in mapIPvsPort ) {
				IPAddress adrs = pair.Key;
				string text = GetCameraParameterInJson( adrs.ToString() );
				Console.WriteLine( "{0}", text );
				var param = JsonConvert.DeserializeObject<CameraParam>( text );
				string path = string.Format(@"..\..\cameraParam_{0}.json", adrs.ToString());
				param.Serialize( path );
			}

			// Preview image の取得
			foreach ( var pair in mapIPvsPort ) {
				IPAddress adrs = pair.Key;
				TcpClient tcp = new TcpClient( adrs.ToString(), SHOOTIMAGESERVER_PORT );
				System.Net.Sockets.NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 10000;
				ns.WriteTimeout = 10000;
				Console.WriteLine( "IP Address: {0}", adrs );
				System.IO.MemoryStream ms = new System.IO.MemoryStream();

				// get preview image コマンドを送信
				string cmd = "PRV";
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
					if (sum == 0) {
						// 先頭の4バイトには、次に続くデータのサイズが書かれている
						bytes_to_read = ((ulong)rcvBytes[0]) | ( (ulong) rcvBytes[1] << 8) | ( (ulong) rcvBytes[2] << 16) | ( (ulong) rcvBytes[3] << 24);
						Console.WriteLine( "bytes_to_read = {0}", bytes_to_read );
					}
					sum += (ulong) resSize;
					ms.Write( rcvBytes, 0, resSize );
				} while ( sum < bytes_to_read + 4 );
				Console.WriteLine( "size = {0}", (int)sum - 4 );
				ms.Close();

				String path = string.Format( "preview_{0}.bmp", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( ms.GetBuffer(), 4, (int)sum - 4 );
				}

				ns.Close();
				tcp.Close();
			}

			//
			// Full image (JPG) の取得
			// Multicast で 撮影コマンドを送信
			SendCommand_MultiCast( ref mcastClient,ref  mcastPoint, "SHJ" );

			// Full image の取得
			foreach ( var pair in mapIPvsPort ) {
				IPAddress adrs = pair.Key;
				TcpClient tcp = new TcpClient( adrs.ToString(), SHOOTIMAGESERVER_PORT );
				System.Net.Sockets.NetworkStream ns = tcp.GetStream();
				ns.ReadTimeout = 10000;
				ns.WriteTimeout = 10000;
				Console.WriteLine( "IP Address: {0}", adrs );
				System.IO.MemoryStream ms = new System.IO.MemoryStream();

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
						Console.WriteLine( "bytes_to_read = {0}", bytes_to_read );
					}
					sum += (ulong) resSize;
					ms.Write( rcvBytes, 0, resSize );
				} while ( sum < bytes_to_read + 4 );
				Console.WriteLine( "size = {0}", (int) sum - 4 );
				ms.Close();

				String path = string.Format( "full_{0}.jpg", adrs.ToString() );
				using ( var fs = new FileStream( path, FileMode.Create, FileAccess.ReadWrite ) ) {
					fs.Write( ms.GetBuffer(), 4, (int) sum - 4 );
				}

				ns.Close();
				tcp.Close();
			}

			mcastClient.Close();
		}
	}
}
