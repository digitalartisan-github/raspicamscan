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

        public bool Open()
        {
            try {
                _mcastClient = new UdpClient();
                _mcastPoint = new IPEndPoint( IPAddress.Parse( _mcastGroup ), _mcastPort );
                _mcastClient.JoinMulticastGroup( _mcastPoint.Address );
            } catch ( Exception e ) {
                Console.Error.WriteLine( e.InnerException );
                return false;
            }
            return true;
        }

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

