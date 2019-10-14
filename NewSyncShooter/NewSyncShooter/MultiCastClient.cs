using System;
using System.Net;
using System.Net.Sockets;

namespace NewSyncShooter
{
    internal class MultiCastClient
    {
        public MultiCastClient( string mcastGroup, int mcastPort )
        {
            _mcastGroup = mcastGroup;
            _mcastPort = mcastPort;
            _mcastClient = null;
            _mcastPoint = null;
        }

        //public bool Open()
        //{
        //    try {
        //        IPAddress mcastGrpAdrs = IPAddress.Parse( _mcastGroup );
        //        _mcastClient = new UdpClient( _mcastPort, AddressFamily.InterNetwork );
        //        _mcastPoint = new IPEndPoint( mcastGrpAdrs, _mcastPort );
        //        _mcastClient.JoinMulticastGroup( mcastGrpAdrs );
        //    } catch ( Exception e ) {
        //        Console.Error.WriteLine( e.InnerException );
        //        return false;
        //    }
        //    return true;
        //}

        /// <summary>
        /// UDPマルチキャスト通信を開く
        /// </summary>
        /// <param name="localHostIP">マルチキャスト送信を行うネットワークアダプタ上の自身のIPアドレス</param>
        /// <returns></returns>
        public bool Open( string localHostIP )
        {
            try {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Parse(localHostIP), 0);
                _mcastClient = new UdpClient( localEP );
                IPAddress mcastGrpAdrs = IPAddress.Parse( _mcastGroup );
                _mcastPoint = new IPEndPoint( mcastGrpAdrs, _mcastPort );
                _mcastClient.JoinMulticastGroup( mcastGrpAdrs );
            } catch ( Exception e ) {
                Console.Error.WriteLine( e.InnerException );
                return false;
            }
            return true;
        }

#if false
        public bool SendCommandAsync( string cmd )
        {
            try {
                byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
                _mcastClient.BeginSend( sendBytes, sendBytes.Length, _mcastPoint, SendCallback, _mcastClient );
            } catch ( Exception e ) {
                Console.Error.WriteLine( e.InnerException );
                return false;
            }
            return true;
        }

        static void SendCallback( IAsyncResult ar )
        {
            UdpClient u = (UdpClient)ar.AsyncState;
            Console.WriteLine( $"number of bytes sent: {u.EndSend( ar )}" );
            u.Close();
        }
#endif

        /// <summary>
        /// コマンド文字列を送信する
        /// </summary>
        /// <param name="cmd">コマンド文字列</param>
        /// <returns></returns>
        public bool SendCommand( string cmd )
        {
            try {
                byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes( cmd );
                _mcastClient.Send( sendBytes, sendBytes.Length, _mcastPoint );
            } catch ( Exception e ) {
                Console.Error.WriteLine( e.InnerException );
                return false;
            }
            return true;
        }

        /// <summary>
        /// マルチキャスト通信を閉じる
        /// </summary>
        public void Close()
        {
            _mcastClient.Close();
        }

        private string _mcastGroup;
        private int _mcastPort;
        private UdpClient _mcastClient;
        private IPEndPoint _mcastPoint;
    }
}

