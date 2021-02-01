using System;
using System.Net.Sockets;
using System.Text;
using SocketServer.Models;

namespace SocketServer.Handlers
{
	public class UnknownCommandHandler : IInputHandler
	{
		/// <summary>
		/// Must be registered last in the pipeline. Handles any input, return an error message
		/// </summary>
		/// <returns>Always returns true</returns>
		public bool Handle(ConnectionData connectionData, byte[] commandBytes)
		{
			var command = Encoding.ASCII.GetString(commandBytes).Trim();
			var message = $"'{command}' is unknown command{Environment.NewLine}";
			var bytes = Encoding.ASCII.GetBytes(message);
			connectionData.Socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, connectionData.Socket);

			return true;
		}

		private void SendCallback(IAsyncResult asyncResult)
		{
			var socket = (Socket) asyncResult.AsyncState;
			socket.EndSend(asyncResult);
		}
	}
}
