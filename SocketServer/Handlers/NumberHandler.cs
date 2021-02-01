using System.Text;
using SocketServer.Models;
using SocketServer.Services;

namespace SocketServer.Handlers
{
	internal class NumberHandler : IInputHandler
	{
		private readonly StateStorage _stateStorage;

		public NumberHandler(StateStorage stateStorage)
		{
			_stateStorage = stateStorage;
		}

		/// <summary>
		/// Handles any 'number' command, adds passed number to client storage
		/// </summary>
		/// <returns>True if command is a valid number</returns>
		public bool Handle(ConnectionData connectionData, byte[] commandBytes)
		{
			var command = Encoding.ASCII.GetString(commandBytes);
			var isNumber = long.TryParse(command, out var result);

			if (isNumber)
			{
				_stateStorage.Clients.TryGetValue(connectionData.ClientIp.ToString(), out var client);
				client.UserData.Sum += result;
			}

			return isNumber;
		}
	}
}
