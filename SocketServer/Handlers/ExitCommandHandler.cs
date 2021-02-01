using System.Net.Sockets;
using System.Text;
using Serilog;
using SocketServer.Models;
using SocketServer.Services;

namespace SocketServer.Handlers
{
	public class ExitCommandHandler : IInputHandler
	{
		private readonly ILogger _logger;
		private readonly StateStorage _stateStorage;

		public ExitCommandHandler(ILogger logger, StateStorage stateStorage)
		{
			_logger = logger;
			_stateStorage = stateStorage;
		}

		/// <summary>
		/// Handles an exit command. Closing socket and remove client from <see cref="StateStorage"/>
		/// </summary>
		public bool Handle(ConnectionData connectionData, byte[] commandBytes)
		{
			var command = Encoding.ASCII.GetString(commandBytes).Trim();
			if (command == "exit")
			{
				var ip = connectionData.ClientIp.ToString();
				_logger.Verbose($"'exit' command from client {ip}");
				
				connectionData.Socket.Shutdown(SocketShutdown.Both);
				connectionData.Socket.Close();
				RemoveFromClientFromStateStorage(ip);
				_logger.Verbose($"{ip} disconnected");

				return true;
			}

			return false;
		}

		private void RemoveFromClientFromStateStorage(string ip) => _stateStorage.Clients.TryRemove(ip, out _);
	}
}
