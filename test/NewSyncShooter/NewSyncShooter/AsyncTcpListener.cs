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
		public IEnumerable<string> StartListening( int portNo )
		{
			TcpListener listener = new TcpListener( IPAddress.Any, portNo );
			List<string> connectedList = new List<string>();
			try {
				listener.Start();
				TimeSpan waitTime = TimeSpan.FromMilliseconds( 500 );
				System.Threading.Thread.Sleep( waitTime );              // ここでウエイトを置かないと、下で Pending()がfalseのままですぐに抜けてしまう
				while ( listener.Pending() ) {
					var state = new AcceptStateObject() {
						Listener = listener,
						AcceptedAddress = string.Empty,
					};
					_tcpClientConnected.Reset();
					var acceptDone = listener.BeginAcceptTcpClient( new AsyncCallback( DoAcceptTcpClientCallback ), state );
					if ( _tcpClientConnected.WaitOne( waitTime ) ) {
						//System.Diagnostics.Debug.WriteLine( state.Accepted );
						connectedList.Add( state.AcceptedAddress );
					} else {
						System.Diagnostics.Debug.WriteLine( "TIMEOUT" );
					}
				}
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
