using System.Net.Sockets;

namespace SocketServer.Models
{
	public class ClientObject
	{
		public ClientObject(Socket socket)
		{
			ConnectionData = new ConnectionData(socket);
		}

		public ConnectionData ConnectionData { get; }

		public UserData UserData { get; } = new UserData();
	}
}
