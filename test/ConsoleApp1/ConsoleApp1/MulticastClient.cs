using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp1
{
	public class MulticastClient
	{
		private Socket mySocket;
		private IPAddress localAddr;
		private IPAddress multicastAddr;
		private int multicastPort;
		private int _recievingPort;
		private IPEndPoint multicastEP;

		public MulticastClient( IPAddress localAddress )
		{
			localAddr = localAddress;

			mySocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
		}

		public void JoinGroup( IPAddress groupAddress, int groupPort, int recievingPort )
		{
			multicastAddr = groupAddress;
			multicastPort = groupPort;
			_recievingPort = recievingPort;

			#region Setting for Send

			byte[] multicastAddrBytes = multicastAddr.GetAddressBytes();
			byte[] ipAddrBytes = IPAddress.Any.GetAddressBytes();
			byte[] multicastOpt = new byte[]
			{
				multicastAddrBytes[0],multicastAddrBytes[1],multicastAddrBytes[2],multicastAddrBytes[3],
				ipAddrBytes[0],ipAddrBytes[1],ipAddrBytes[2],ipAddrBytes[3]
			};
			mySocket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt );
			multicastEP = new IPEndPoint( multicastAddr, multicastPort );

			#endregion

			var thread = new Thread( MCCommReceive );
			thread.Start();
		}

		public void SendMessage( string msg )
		{
			if (joinFlag) {
				byte[] msgBytes = System.Text.UTF8Encoding.UTF8.GetBytes( msg );
				int len = msgBytes.Length;
				if (len > RECEIVEBUFSIZE - 2) {
					throw new ArgumentOutOfRangeException( "Message length should be less than " + (RECEIVEBUFSIZE - 2) + " bytes" );
				}
				byte[] dataBytes = new byte[2 + len];
				dataBytes[0] = (byte)(len >> 8);
				dataBytes[1] = (byte)(len & 0xff);
				msgBytes.CopyTo( dataBytes, 2 );
				mySocket.SendTo( dataBytes, dataBytes.Length, SocketFlags.None, multicastEP );
			}
		}

		int RECEIVEBUFSIZE = 1024;

		bool joinFlag = false;
		void MCCommReceive()
		{
			var receiveSocket = new Socket( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );

			#region Setting for Receive

			receiveSocket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false );
			receiveSocket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
			IPEndPoint localEP = new IPEndPoint( IPAddress.Any, multicastPort );
			receiveSocket.Bind( localEP );
			byte[] multicastAddrBytes = multicastAddr.GetAddressBytes();
			byte[] ipAddrBytes = IPAddress.Any.GetAddressBytes();
			byte[] multicastOpt = new byte[]
			{
			   multicastAddrBytes[0], multicastAddrBytes[1], multicastAddrBytes[2], multicastAddrBytes[3],    // WsDiscovery Multicast Address: 239.255.255.250
                 ipAddrBytes       [0], ipAddrBytes       [1], ipAddrBytes       [2], ipAddrBytes       [3]
			};
			receiveSocket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOpt );
			EndPoint senderEP = new IPEndPoint( multicastAddr, multicastPort );

			#endregion

			int len = 0;
			byte[] dataBytes = new byte[RECEIVEBUFSIZE];
			bool mcgJoining = false;
			lock (this) {
				joinFlag = true;
				mcgJoining = joinFlag;
			}
			while (mcgJoining) {
				try {
					len = receiveSocket.Receive( dataBytes, dataBytes.Length, SocketFlags.None );
					len = (int)(dataBytes[0] << 8) + dataBytes[1];
					byte[] buf = new byte[len];
					for (int i = 0; i < len; i++) {
						buf[i] = dataBytes[i + 2];
					}
					if (OnMulticastMessageReceived != null) {
						OnMulticastMessageReceived( buf, len, (IPEndPoint)senderEP );
					}
				} catch (Exception ex) {
					System.Diagnostics.Debug.Print( ex.Message );
				}
				lock (this) {
					mcgJoining = joinFlag;
				}
			}

			receiveSocket.SetSocketOption( SocketOptionLevel.IP, SocketOptionName.DropMembership, multicastOpt );
		}

		public event OnMulticastMessageReceivedDelegate OnMulticastMessageReceived;
		public delegate void OnMulticastMessageReceivedDelegate( byte[] msg, int len, IPEndPoint sender );
	}
}
