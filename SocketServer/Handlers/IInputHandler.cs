using SocketServer.Models;

namespace SocketServer.Handlers
{
	public interface IInputHandler
	{
		/// <summary>
		/// Handles command
		/// </summary>
		/// <returns>Return true if command was handled</returns>
		public bool Handle(ConnectionData connectionData, byte[] commandBytes);
	}
}
