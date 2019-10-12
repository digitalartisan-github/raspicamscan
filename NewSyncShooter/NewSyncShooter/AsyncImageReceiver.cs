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
    public class AsyncImageReceiver
    {
        // State object for receiving data from remote device.  
        public class ReceivingImageStateObject
        {
            // Client socket.  
            public Socket workSocket = null;
            // Size of receive buffer.  
            public const int BufferSize = 65536;
            // image data size.
            public int ImageDataSize = 0;
            // Receive buffer.
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public MemoryStream ms = new MemoryStream();
            //public StringBuilder sb = new StringBuilder();
            public byte[] Result = null;
        }

        // ManualResetEvent instances signal completion.  
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);

        public byte[] ReceiveImage( string ipAddress, int portNo )
        {
            IPAddress ipAdrs = IPAddress.Parse( ipAddress );
            IPEndPoint remoteEP = new IPEndPoint( ipAdrs, portNo );

            Socket socket = new Socket( ipAdrs.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
            connectDone.Reset();
            socket.BeginConnect( remoteEP, new AsyncCallback( ConnectCallback ), socket );
            connectDone.WaitOne();

            // Send command string to the remote device. 
            sendDone.Reset();
            Send( socket, "IMG" );
            sendDone.WaitOne();

            // Receive the response from the remote device.  
            receiveDone.Reset();
            var result = ReceiveImage( socket );
            receiveDone.WaitOne();

            socket.Shutdown( SocketShutdown.Both );
            socket.Close();
            return result;
        }

        private void ConnectCallback( IAsyncResult ar )
        {
            try {
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;
                // Complete the connection.  
                client.EndConnect( ar );
                //Console.WriteLine( "Socket connected to {0}", client.RemoteEndPoint.ToString() );
                // Signal that the connection has been made.  
                connectDone.Set();
            } catch ( Exception e ) {
                Console.WriteLine( e.ToString() );
            }
        }

        private byte[] ReceiveImage( Socket client )
        {
            try {
                // Create the state object.  
                ReceivingImageStateObject state = new ReceivingImageStateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive( state.buffer, 0, ReceivingImageStateObject.BufferSize, 0,
                    new AsyncCallback( ReceiveImageCallback ), state );
                receiveDone.WaitOne();

                return state.Result;

            } catch ( Exception e ) {
                Console.WriteLine( e.ToString() );
                return null;
            }
        }

        private void ReceiveImageCallback( IAsyncResult ar )
        {
            try {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                var state = (ReceivingImageStateObject) ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive( ar );

                if ( bytesRead > 0 ) {
                    // There might be more data, so store the data received so far.  
                    //state.sb.Append( Encoding.UTF8.GetString( state.buffer, 0, bytesRead ) );
                    state.ms.Write( state.buffer, 0, bytesRead );

                    if ( ( state.ImageDataSize == 0 ) && ( state.ms.Length >= 4 ) ) {
                        // 先頭の4バイトには、次に続く画像データのサイズが書かれている
                        byte[] buffer = state.ms.GetBuffer();
                        state.ImageDataSize = (int) ( ( (ulong) buffer[0] ) | ( (ulong) buffer[1] << 8 ) | ( (ulong) buffer[2] << 16 ) | ( (ulong) buffer[3] << 24 ) );
                    }
                    // Get the rest of the data.  
                    client.BeginReceive( state.buffer, 0, ReceivingImageStateObject.BufferSize, 0,
                        new AsyncCallback( ReceiveImageCallback ), state );
                } else {
                    // All the data has arrived; put it in response.  
                    //if ( state.sb.Length > 1 ) {
                    //	response = state.sb.ToString();
                    //}
                    // MemoryStrem の最初の4バイトは画像データサイズを表すので飛ばす
                    if ( state.ms.Length == state.ImageDataSize + 4 ) {
                        state.Result = state.ms.GetBuffer().Skip( 4 ).ToArray();
                        // Signal that all bytes have been received.  
                        receiveDone.Set();
                    } else {
                        state.Result = null;
                        receiveDone.Set();
                        //// Get the rest of the data.  
                        //client.BeginReceive( state.buffer, 0, ReceivingImageStateObject.BufferSize, 0,
                        //	new AsyncCallback( ReceiveImageCallback ), state );
                    }
                }
            } catch ( Exception e ) {
                Console.WriteLine( e.ToString() );
            }
        }

        private void Send( Socket client, String data )
        {
            // Convert the string data to byte data using UTF8 encoding.  
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes( data );

            // Begin sending the data to the remote device.  
            client.BeginSend( byteData, 0, byteData.Length, 0,
                new AsyncCallback( SendCallback ), client );
        }

        private void SendCallback( IAsyncResult ar )
        {
            try {
                // Retrieve the socket from the state object.  
                Socket client = (Socket) ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                //Console.WriteLine( "Sent {0} bytes to server.", bytesSent );

                // Signal that all bytes have been sent.  
                sendDone.Set();
            } catch ( Exception e ) {
                Console.WriteLine( e.ToString() );
            }
        }
    }
}
