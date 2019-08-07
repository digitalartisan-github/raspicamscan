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
	public class AsyncSocketListener
	{
		public class AcceptStateObject
		{
			public Socket Listener = null;
			public string Accepted = null;
		}

		public class StateObject
		{
			public Socket workSocket = null;
			public const int BufferSize = 1024;
			public byte[] buffer = new byte[BufferSize];
			public StringBuilder sb = new StringBuilder();
		}

		private static ManualResetEvent acceptDone = new ManualResetEvent(false);

		public IEnumerable<string> StartListening( int portNo )
		{
			IPEndPoint localEP = new IPEndPoint( IPAddress.Any, portNo );
			Console.WriteLine( $"Local address and port : {localEP.ToString()}" );
			List<string> connectedList = new List<string>();
			Socket listener = new Socket( localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
			var start = DateTime.Now;
			try {
				listener.Bind( localEP );
				listener.Listen( 128 );
				TimeSpan ts = DateTime.Now - start;
				while ( ts.Seconds < 5 ) {
					acceptDone.Reset();
					Console.WriteLine( "Waiting for a connection..." + ts.Seconds.ToString() );
					var state = new AcceptStateObject() {
						Listener = listener,
					};
					var ar = listener.BeginAccept( new AsyncCallback( AcceptCallback ), state );
					if ( acceptDone.WaitOne( 1000 ) ) {
						connectedList.Add( state.Accepted );
					}
					ts = DateTime.Now - start;
				}
			} catch ( Exception e ) {
				Console.WriteLine( e.ToString() );
			}
			// アドレスの第4オクテットの昇順でソート
			return connectedList.OrderBy( adrs => {
				int idx = adrs.LastIndexOf('.');
				int adrs4th = int.Parse(adrs.Substring( idx  + 1 ));
				return adrs4th;
			} );
		}

		public static void AcceptCallback( IAsyncResult ar )
		{
			// Get the socket that handles the client request.  
			//Socket listener = (Socket) ar.AsyncState;
			//Socket handler = listener.EndAccept(ar);	// hander.RemoveEndPoint にリモートEPの情報が入っている
			AcceptStateObject acceptState = ar.AsyncState as AcceptStateObject;
			Socket listener = acceptState.Listener;
			Socket handler = listener.EndAccept( ar );
			acceptState.Accepted = ( handler.RemoteEndPoint as IPEndPoint ).Address.ToString();

			// Signal the main thread to continue.  
			acceptDone.Set();

			// Create the state object.  
			StateObject state = new StateObject();
			state.workSocket = handler;
			handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
				new AsyncCallback( ReadCallback ), state );
		}

		public static void ReadCallback( IAsyncResult ar )
		{
			StateObject state = (StateObject) ar.AsyncState;
			Socket handler = state.workSocket;

			// Read data from the client socket.  
			int read = handler.EndReceive(ar);

			// Data was read from the client socket.  
			if ( read > 0 ) {
				state.sb.Append( Encoding.UTF8.GetString( state.buffer, 0, read ) );
				handler.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
					new AsyncCallback( ReadCallback ), state );
			} else {
				if ( state.sb.Length > 1 ) {
					// All the data has been read from the client;  
					// display it on the console.  
					string content = state.sb.ToString();
					Console.WriteLine( $"Read {content.Length} bytes from socket.\n Data : {content}" );
					// content に "ACK" が入っている
					//state.Result = ( content == "ACK" );
					handler.Shutdown( SocketShutdown.Both );
					handler.Close();
					handler.Dispose();
				}
			}
		}
	}
}
