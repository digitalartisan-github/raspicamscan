using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace NewSyncShooter
{
	public class AsyncSocketListener
	{
		public class StateObject
		{
			public Socket workSocket = null;
			public const int BufferSize = 1024;
			public byte[] buffer = new byte[BufferSize];
			public StringBuilder sb = new StringBuilder();
			public bool Result = false;
			//public List<string> ConnectedList = new List<string>();
		}

		private static ManualResetEvent allDone = new ManualResetEvent(false);

		public List<string> StartListening( int portNo )
		{
			IPEndPoint localEP = new IPEndPoint( IPAddress.Loopback, portNo );
			Console.WriteLine( $"Local address and port : {localEP.ToString()}" );
			Socket listener = new Socket(localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			List<string> connectedList = new List<string>();
			var start = DateTime.Now;
			try {
				listener.Bind( localEP );
				listener.Listen( 128 );
				TimeSpan ts = DateTime.Now - start;
				while ( ts.Seconds < 5 ) {
					allDone.Reset();
					Console.WriteLine( "Waiting for a connection..." + ts.Seconds.ToString() );
					var ar = listener.BeginAccept( new AsyncCallback( AcceptCallback ), listener );
					if ( allDone.WaitOne( 1000 ) ) {
						var state = ar.AsyncState as StateObject;
						if ( state.Result ) {
							connectedList.Add( state.workSocket.RemoteEndPoint.ToString() );
						}
					}
					ts = DateTime.Now - start;
				}
			} catch ( Exception e ) {
				Console.WriteLine( e.ToString() );
			}
			return connectedList;
		}

		public static void AcceptCallback( IAsyncResult ar )
		{
			// Get the socket that handles the client request.  
			Socket listener = (Socket) ar.AsyncState;
			Socket handler = listener.EndAccept(ar);

			// Signal the main thread to continue.  
			allDone.Set();

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
					state.Result = ( content == "ACK" );
					//if (content == "ACK") {
					//	var ep = state.workSocket.RemoteEndPoint as IPEndPoint;
					//	state.ConnectedList.Add( ep.Address.ToString() );
					//}
				}
				handler.Shutdown( SocketShutdown.Both );
				handler.Close();
			}
		}
	}
}
