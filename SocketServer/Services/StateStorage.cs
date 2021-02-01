using System.Collections.Concurrent;
using SocketServer.Models;

namespace SocketServer.Services
{
	public class StateStorage
	{
		public ConcurrentDictionary<string, ClientObject> Clients { get; } =
			new ConcurrentDictionary<string, ClientObject>();
	}
}
