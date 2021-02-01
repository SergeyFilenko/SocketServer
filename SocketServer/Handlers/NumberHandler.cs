using System;
using System.Linq;
using System.Net.Sockets;
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
		/// Handles any 'number' command, adds passed number to client storage, and prints current sum for client
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
				Response(connectionData.Socket, $"Sum: {client.UserData.Sum}{Environment.NewLine}");
			}

			return isNumber;
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
