using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using Serilog;
using SocketServer.Models;
using SocketServer.Services;

namespace SocketServer.Handlers
{
	public class ListCommandHandler : IInputHandler
	{
		private readonly ILogger _logger;
		private readonly StateStorage _stateStorage;

		public ListCommandHandler(ILogger logger, StateStorage stateStorage)
		{
			_logger = logger;
			_stateStorage = stateStorage;
		}


		/// <summary>
		/// Handles 'list' command and returns connected clients
		/// </summary>
		/// <returns>True if 'list' command was found</returns>
		public bool Handle(ConnectionData connectionData, byte[] commandBytes)
		{
			var command = Encoding.ASCII.GetString(commandBytes);
			if (command.Trim() == "list")
			{
				Response(connectionData.Socket, BuildResponse());
				_logger.Verbose($"'list' command from client {connectionData.Socket.RemoteEndPoint}");
				return true;
			}

			return false;
		}

		private string BuildResponse()
		{
			var builder = new StringBuilder();
			var fancyLine = string.Join(string.Empty, Enumerable.Repeat('=', 50));
			builder.AppendLine("Connected users:");
			builder.AppendLine(fancyLine);
			foreach (var (key, value) in _stateStorage.Clients)
				builder.AppendLine($"Ip: {key}\t Sum: {value.UserData.Sum}");
			builder.AppendLine(fancyLine);

			return builder.ToString();
		}

		private void Response(Socket socket, string response)
		{
			var bytes = Encoding.ASCII.GetBytes(response);
			socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, socket);
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			var socket = (Socket) asyncResult.AsyncState;
			socket.EndSend(asyncResult);
		}
	}
}
