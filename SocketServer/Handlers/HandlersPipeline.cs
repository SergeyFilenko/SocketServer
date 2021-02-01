using System.Collections.Generic;
using SocketServer.Models;

namespace SocketServer.Handlers
{
	internal class HandlersPipeline
	{
		private readonly List<IInputHandler> _handlers = new List<IInputHandler>();

		public HandlersPipeline Register(IInputHandler handler)
		{
			_handlers.Add(handler);
			return this;
		}

		/// <summary>
		/// Runs command through the pipeline of handlers
		/// </summary>
		public void Handle(ConnectionData connectionData, byte[] commandBytes)
		{
			foreach (var handler in _handlers)
			{
				if (handler.Handle(connectionData, commandBytes))
					break;
			}
		}
	}
}
