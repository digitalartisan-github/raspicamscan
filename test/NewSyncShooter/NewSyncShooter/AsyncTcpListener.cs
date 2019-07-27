using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace NewSyncShooter
{
	public class AcceptStateObject : object
	{
		public TcpListener Listener = null;
		public string AcceptedAddress = null;
	}

	public class AsyncTcpListener
	{
		static ManualResetEvent _tcpClientConnected = new ManualResetEvent(false);

		// Accept one client connection asynchronously.
		public IEnumerable<string> StartListening( string localHostIP, int portNo )
		{
			TcpListener listener = new TcpListener( IPAddress.Parse( localHostIP ), portNo );
			//TcpListener listener = new TcpListener( IPAddress.Any, portNo );
			List<string> connectedList = new List<string>();
			try {
				listener.Start();
				var waitTime1 = TimeSpan.FromMilliseconds( 1000 );
				var waitTime2 = TimeSpan.FromMilliseconds( 100 );
#if true
				do {
					var state = new AcceptStateObject() {
						Listener = listener,
						AcceptedAddress = string.Empty,
					};
					_tcpClientConnected.Reset();
					var acceptDone = listener.BeginAcceptSocket( new AsyncCallback( DoAcceptSocketCallback ), state );
					if ( _tcpClientConnected.WaitOne( waitTime1 ) ) {
						connectedList.Add( state.AcceptedAddress );
					} else {
						System.Diagnostics.Debug.WriteLine( "TIMEOUT" );
						break;
					}
					System.Threading.Thread.Sleep( waitTime2 );
				} while ( listener.Pending() );
#endif
#if false
				var state = new AcceptStateObject() {
					Listener = listener,
					AcceptedAddress = string.Empty,
				};
				while ( true ) {
					state.AcceptedAddress = string.Empty;
					_tcpClientConnected.Reset();
					var acceptDone = listener.BeginAcceptSocket( new AsyncCallback( DoAcceptSocketCallback ), state );
					if ( _tcpClientConnected.WaitOne( waitTime1 ) ) {
						connectedList.Add( state.AcceptedAddress );
					} else {
						//System.Threading.Thread.Sleep( waitTime2 );
						if ( listener.Pending() == false ) {
							System.Diagnostics.Debug.WriteLine( "TIMEOUT" );
							break;
						}
					}
				}
#endif
#if false
				do {
					var state = new AcceptStateObject() {
						Listener = listener,
						AcceptedAddress = string.Empty,
					};
					_tcpClientConnected.Reset();
					var acceptDone = listener.BeginAcceptTcpClient( new AsyncCallback( DoAcceptTcpClientCallback ), state );
					if ( _tcpClientConnected.WaitOne( waitTime1 ) ) {
						connectedList.Add( state.AcceptedAddress );
					} else {
						System.Diagnostics.Debug.WriteLine( "TIMEOUT" );
						break;
					}
					System.Threading.Thread.Sleep( waitTime2 );
				} while ( listener.Pending() );
#endif
#if false
				while ( true ) {
					var state = new AcceptStateObject() {
						Listener = listener,
						AcceptedAddress = string.Empty,
					};
					_tcpClientConnected.Reset();
					var acceptDone = listener.BeginAcceptTcpClient( new AsyncCallback( DoAcceptTcpClientCallback ), state );
					if ( _tcpClientConnected.WaitOne( waitTime1 ) ) {
						connectedList.Add( state.AcceptedAddress );
					} else {
						System.Threading.Thread.Sleep( waitTime2 );
						if ( listener.Pending() == false ) {
							System.Diagnostics.Debug.WriteLine( "TIMEOUT" );
							break;
						}
					}
				}
#endif
			} catch ( Exception e ) {
				System.Diagnostics.Debug.WriteLine( e.Message );
			} finally {
				listener.Stop();
			}
			// アドレスの第4オクテットの昇順でソート
			return connectedList.OrderBy( adrs => {
				int idx = adrs.LastIndexOf('.');
				return int.Parse( adrs.Substring( idx + 1 ) );
			} ).Distinct();
		}

		private static void DoAcceptSocketCallback( IAsyncResult ar )
		{
			AcceptStateObject state = ar.AsyncState as AcceptStateObject;
			TcpListener listener = state.Listener;
			try {
				Socket socket = listener.EndAcceptSocket(ar);
				byte[] bytes = new byte[socket.Available];
				socket.Receive( bytes );
				string msg = System.Text.Encoding.UTF8.GetString( bytes );
				if ( msg == "ACK" ) {
					var remoteEP = socket.RemoteEndPoint as IPEndPoint;
					state.AcceptedAddress = remoteEP.Address.ToString();
					_tcpClientConnected.Set();
				}
				socket.Close();
			} catch ( Exception e ) {
				System.Diagnostics.Debug.WriteLine( e.Message );
			}
		}

		// Process the client connection.
		private static void DoAcceptTcpClientCallback( IAsyncResult ar )
		{
			AcceptStateObject state = ar.AsyncState as AcceptStateObject;
			TcpListener listener = state.Listener;
			try {
				TcpClient client = listener.EndAcceptTcpClient(ar);
				NetworkStream ms = client.GetStream();
				byte[] bytes = new byte[client.Available];
				ms.Read( bytes, 0, client.Available );
				string msg = System.Text.Encoding.UTF8.GetString( bytes );
				if ( msg == "ACK" ) {
					var remoteEP = client.Client.RemoteEndPoint as IPEndPoint;
					state.AcceptedAddress = remoteEP.Address.ToString();
					_tcpClientConnected.Set();
				}
				client.Close();
			} catch ( Exception e ) {
				System.Diagnostics.Debug.WriteLine( e.Message );
			}
		}
	}
}
