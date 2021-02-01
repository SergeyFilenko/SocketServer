using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace SocketServer.Models
{
	public class ConnectionData
	{
		private const int BufferSize = 1024;

		public ConnectionData(Socket socket)
		{
			Socket = socket;
		}

		public Socket Socket { get; }

		public EndPoint ClientIp => Socket.RemoteEndPoint;

		public byte[] Buffer { get; } = new byte[BufferSize];

		public List<byte> InputBuffer { get; } = new List<byte>(BufferSize);

		public int BytesReceived { get; set; }

	}
}
