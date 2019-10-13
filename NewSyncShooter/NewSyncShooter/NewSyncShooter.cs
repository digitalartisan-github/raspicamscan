using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;

namespace NewSyncShooter
{
    public class NewSyncShooter
    {
        // マルチキャストグループのIPアドレスとポート
        // （syncshooter.py 中の MCAST_GRP, MCAST_PORT に対応）
        private static readonly string MCAST_GRP                = "239.2.1.1";
        private static readonly int MCAST_PORT                  = 27781;    // マルチキャスト送信ポート
        private static readonly int SENDBACK_PORT               = 27782;    // マルチキャストでラズパイからの返信ポート
        private static readonly int SHOOTIMAGESERVER_PORT       = 27783;    // ラズパイからの画像返信用ポート
        private static readonly int SHOOTIMAGESERVER_PORT_NUM   = 32;       // ラズパイからの画像返信用ポートの数

        private SyncshooterDefs _syncshooterDefs = null;
        private MultiCastClient _mcastClient = null;
        private Dictionary<string, int> _mapIP_Port = null;

        public NewSyncShooter()
        {
            _mcastClient = new MultiCastClient( MCAST_GRP, MCAST_PORT );
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
        public IEnumerable<string> ConnectCamera( string localHostIP )
        {
            // 接続できたラズパイのアドレスを列挙する
            //return GetConnectedHostAddressUDP();
            return GetConnectedHostAddressTCP( localHostIP );
        }

#if false
        // 接続しているラズパイのアドレスを列挙する(UDP Protocol)（アドレスの第4オクテットの昇順でソート）
        private IEnumerable<string> GetConnectedHostAddressUDP()
        {
            UdpClient udp = new UdpClient( SENDBACK_PORT );
            udp.Client.ReceiveTimeout = 1000;

            // マルチキャストに参加しているラズパイに"INQ"コマンドを送信
            if ( _mcastClient.Open() == false ) {
                return Array.Empty<string>();
            }
            _mcastClient.SendCommand( "INQ" );
            _mcastClient.Close();
            //System.Threading.Thread.Sleep( 1000 );  // waitをおかないと、この後すぐに返事を受け取れない場合がある

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
            return mapIPvsPort.OrderBy( pair =>
            {
                int idx = pair.Key.LastIndexOf('.');
                int adrs4th = int.Parse(pair.Key.Substring( idx  + 1 ));
                return adrs4th; // (結局はIPアドレスのみを残しポート番号は捨てる)
            } ).Select( pair => pair.Key )
            // SyncShooterDefsにある全IPアドレスリストにないものは除く
            .Intersect( _syncshooterDefs.GetAllCameraIPAddress() );
        }
#endif

        // 接続しているラズパイのアドレスを列挙する(TCP Protocol)（アドレスの第4オクテットの昇順でソート）
        public IEnumerable<string> GetConnectedHostAddressTCP( string localHostIP )
        {
            // マルチキャストに参加しているラズパイに"INQ"コマンドを送信
            if ( _mcastClient.Open( localHostIP ) == false ) {
                return Array.Empty<string>();
            }
#if false
            _mcastClient.SendCommandAsync( "INQ" );
#else
            _mcastClient.SendCommand( "INQ" );
            _mcastClient.Close();
            //System.Threading.Thread.Sleep( 1000 );  // waitをおかないと、この後すぐに返事を受け取れない場合がある	// →　むしろない方がよい。池尻のPCでは。
#endif

            var listener = new AsyncTcpListener();
            var connectedList = listener.StartListening( localHostIP, SENDBACK_PORT );
            return connectedList;
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
        public static byte[] GetPreviewImage( string ipAddress )
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
            ulong bytesToRead = 0;
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
                ms.Write( rcvBytes, 0, resSize );
                if ( ( bytesToRead == 0 ) && ( ms.Length >= 4 ) ) {
                    // 先頭の4バイトには、次に続くデータのサイズが書かれている
                    byte[] buffer = ms.GetBuffer();
                    bytesToRead = ( (ulong) buffer[0] ) | ( (ulong) buffer[1] << 8 ) | ( (ulong) buffer[2] << 16 ) | ( (ulong) buffer[3] << 24 );
                }
                sum += (ulong) resSize;
            } while ( sum < bytesToRead + 4 );
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
        public void SendCommandToGetFullImageInJpeg( string localHostIP )
        {
            if ( _mcastClient.Open( localHostIP ) ) {
                _mcastClient.SendCommand( "SHJ" );
                _mcastClient.Close();
            }
        }

        /// <summary>
        /// 指定IPアドレスのカメラの撮影画像を取得する
        /// （先に SendCommandToGetFullImageInJpeg()で全ラズパイカメラに撮影命令を送信しておく ）
        /// throws IOException
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="portNo"></param>
        /// <returns></returns>
        public static byte[] GetFullImageInJpeg( string ipAddress, out int portNo )
        {
            byte[] imageData = null;
            portNo = SHOOTIMAGESERVER_PORT;
            int offset = 0;
            while ( true ) {
                var receiver = new AsyncImageReceiver();
                imageData = receiver.ReceiveImage( ipAddress, portNo );
                if ( imageData != null ) {
                    break;
                } else {
                    // 画像データを受信できなかった場合はポート番号を変えて再試行
                    offset = ( ++offset ) % SHOOTIMAGESERVER_PORT_NUM;
                    portNo = SHOOTIMAGESERVER_PORT + offset;
                    System.Diagnostics.Debug.WriteLine( " -> {0}:{1}", ipAddress, portNo );
                }
            }
            return imageData;
        }

        /// <summary>
        /// カメラを停止する
        /// 入力：reboot : TRUE=再起動, FALSE=停止
        /// </summary>
        /// <param name="reboot"></param>
        public void StopCamera( string localHostIP, bool reboot )
        {
            if ( _mcastClient.Open( localHostIP ) ) {
                _mcastClient.SendCommand( reboot ? "RBT" : "SDW" );
                _mcastClient.Close();
            }
        }
    }
}
