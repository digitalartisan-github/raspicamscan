using System;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp1
{
	class Program
	{
		private static readonly int Port = 27781;
		private static readonly string MulticastAddress = "239.2.1.1";

		static void Main( string[] args )
		{
			using (UdpClient multicast_client = new UdpClient()) {
				IPEndPoint multicast_point = new IPEndPoint( IPAddress.Parse( MulticastAddress ), Port );
				multicast_client.Client.Bind( new IPEndPoint( IPAddress.Any, Port ) );
				multicast_client.JoinMulticastGroup( multicast_point.Address );

				string sendMsg = "INQ";
				byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes( sendMsg );
				multicast_client.Send( sendBytes, sendBytes.Length, multicast_point );

				IPEndPoint rcv_point = null;
				while (true) {
					byte[] packet = multicast_client.Receive( ref rcv_point );
					string sRcv = System.Text.Encoding.UTF8.GetString( packet, 0, packet.Length );
					Console.WriteLine( sRcv );
					multicast_client.Send( sendBytes, sendBytes.Length, multicast_point );
				}
			}
		}
	}
}
