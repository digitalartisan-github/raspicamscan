﻿using System;
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
		Dictionary<string, int> _mapIP_Port;

		public NewSyncShooter()
		{
			_syncshooterDefs = null;
			_mcastClient = null;
		}

		~NewSyncShooter()
		{
			if ( _mcastClient != null) {
				_mcastClient.Close();
			}
		}

		public void Initialize()
		{
			// Loading SyncshooterDefs
			_syncshooterDefs = SyncshooterDefs.Deserialize( "syncshooterDefs.json" );
			if ( _mapIP_Port  != null) {
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
			// 接続できたラズパイのアドレスとポートを列挙する
			_mapIP_Port = GetConnectedHostAddress( _mcastClient );
			return _mapIP_Port.Select( pair => { return pair.Key.ToString(); } );
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

				return CameraParam.DecodeFronJsonText( rcvString );
			} catch ( Exception e ) {
				Console.Error.WriteLine( e.Message );
				return null;
			}
		}

		// 指定IPアドレスのカメラにパラメータを設定する
		public void SetCameraParam( string ipAddress, CameraParam param )
		{

		}

		// 指定IPアドレスのカメラのプレビュー画像データを取得する
		public byte[] GetPreviewImage(string ipAddress)
		{
			TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
			System.Net.Sockets.NetworkStream ns = tcp.GetStream();
			ns.ReadTimeout = 10000;
			ns.WriteTimeout = 10000;
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
			ns.Close();
			tcp.Close();
			return ms.GetBuffer();
		}

		public byte[] GetFullImageInJpeg(string ipAddress)
		{
			_mcastClient.SendCommand( "SHJ" );
			TcpClient tcp = new TcpClient( ipAddress, SHOOTIMAGESERVER_PORT );
			System.Net.Sockets.NetworkStream ns = tcp.GetStream();
			ns.ReadTimeout = 10000;
			ns.WriteTimeout = 10000;
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
			ns.Close();
			tcp.Close();
			return ms.GetBuffer();
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
		private Dictionary<string, int> GetConnectedHostAddress( MultiCastClient mcastClient )
		{
			UdpClient udp = new UdpClient( SENDBACK_PORT );
			udp.Client.ReceiveTimeout = 1000;

			// マルチキャストに参加しているラズパイに"INQ"コマンドを送信
			mcastClient.SendCommand( "INQ" );
			System.Threading.Thread.Sleep( 1000 );	// waitをおかないと、この後すぐに返事を受け取れない場合がある

			var mapIPvsPort = new Dictionary<string, int>();
			try {
				do {
					// 任意IP Address, 任意のポートからデータを受信する 
					IPEndPoint remoteEP = null;
					byte[] rcvBytes = udp.Receive( ref remoteEP );
					string rcvMsg = System.Text.Encoding.UTF8.GetString( rcvBytes );
					if ( rcvMsg == "ACK" ) {
						Console.WriteLine( "受信したデータ:{0}", rcvMsg );
						Console.WriteLine( "送信元アドレス:{0}/ポート番号:{1}", remoteEP.Address.ToString(), remoteEP.Port );  // この送信元ポート番号は毎度変わる
						mapIPvsPort[remoteEP.Address.ToString()] = remoteEP.Port;
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
