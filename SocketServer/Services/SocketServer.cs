using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Serilog;
using SocketServer.Handlers;
using SocketServer.Models;

namespace SocketServer.Services
{
	internal class SocketServer : IDisposable
	{
		private readonly ILogger _logger;
		private readonly StateStorage _stateStorage;
		private readonly HandlersPipeline _pipeline;
		private readonly ManualResetEventSlim _connectionWaiter = new ManualResetEventSlim(false);

		public SocketServer(ILogger logger, StateStorage stateStorage, HandlersPipeline pipeline)
		{
			_logger = logger;
			_stateStorage = stateStorage;
			_pipeline = pipeline;
		}

		/// <summary>
		/// Spins an infinity loop and waits for a	 connection
		/// </summary>
		/// <param name="port">Port to listen</param>
		public void Start(int port)
		{
			try
			{
				var endPoint = new IPEndPoint(IPAddress.Any, port);
				var listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				listener.Bind(endPoint);
				listener.Listen(64);

				_logger.Information($"Listening on {endPoint}");
				while (true)
				{
					_connectionWaiter.Reset();
					listener.BeginAccept(AcceptCallback, listener);
					_connectionWaiter.Wait();
				}
			}
			catch (Exception exception)
			{
				_logger.Error(exception, "Unhandled exception");
			}
		}

		private void AcceptCallback(IAsyncResult asyncResult)
		{
			_connectionWaiter.Set();

			var listener = (Socket)asyncResult.AsyncState;
			var socket = listener.EndAccept(asyncResult);

			_logger.Verbose($"{socket.RemoteEndPoint} connected");

			var state = new ClientObject(socket);
			_stateStorage.Clients.TryAdd(socket.RemoteEndPoint.ToString(), state);
			SendWelcomeMessage(state.ConnectionData);
			socket.BeginReceive(state.ConnectionData.Buffer, 0, state.ConnectionData.Buffer.Length, SocketFlags.None, ReceiveCallback, state.ConnectionData);
		}

		private void ReceiveCallback(IAsyncResult asyncResult)
		{
			var connectionData = (ConnectionData)asyncResult.AsyncState;
			var socket = connectionData.Socket;
			connectionData.BytesReceived = socket.EndReceive(asyncResult);
			if (connectionData.BytesReceived > 0)
			{
				var commands = GetCommands(connectionData);

				// Try handle commands
				foreach (var command in commands)
					_pipeline.Handle(connectionData, command);

				// Socket can be closed if the 'exit' command was handled.
				if (socket.Connected)
					socket.BeginReceive(connectionData.Buffer, 0, connectionData.Buffer.Length, SocketFlags.None, ReceiveCallback, connectionData);
			}
		}

		private void SendWelcomeMessage(ConnectionData connectionData)
		{
			var message = $"Welcome to simple socket server. Available command: 'any integer', 'list', 'exit'{Environment.NewLine}";
			var bytes = Encoding.ASCII.GetBytes(message);
			connectionData.Socket.BeginSend(
				bytes,
				0,
				bytes.Length,
				SocketFlags.None,
				ar =>
				{
					var socket = (Socket) ar.AsyncState;
					socket.EndSend(ar);
				},
				connectionData.Socket);
		}

		/// <summary>
		/// Searches for any 'command' in input buffer. The 'command' is a sequence of bytes that ends with CRLF
		/// IAC commands will be ignored.
		/// </summary>
		/// <returns>List of commands in bytes</returns>
		private IEnumerable<byte[]> GetCommands(ConnectionData connectionData)
		{
			var inputBuffer = connectionData.InputBuffer;

			// Assumes that connection was made from telnet in a character mode (windows terminal)
			// The client will send instantly any pressed character so we have to store it until we recognize a full command
			// Also this perfectly handles CRLF behaviour in PuTTy, considering PuTTy telnet by default in a line mode 
			// (buffer characters on the client and then sending them as one package) but sometimes CRLF sent in a separate package.
			inputBuffer.AddRange(connectionData.Buffer.Take(connectionData.BytesReceived));

			var commands = new List<byte[]>();
			var indexOfLineFeed = -1;

			// Check if any command can be parsed from an input buffer
			do
			{
				RemoveIacPackagesFromInputBuffer(connectionData);
				indexOfLineFeed = inputBuffer.IndexOf((byte)'\n');

				if (indexOfLineFeed > -1)
				{
					// TODO: it would be better to rewrite the input buffer with another collection to reduce memory allocations,
					// but it requires researching telnet RFC to avoid buffer overflow
					var bytes = inputBuffer.GetRange(0, indexOfLineFeed + 1);
					inputBuffer.RemoveRange(0, indexOfLineFeed + 1);
					commands.Add(bytes.ToArray());
				}
			} while (indexOfLineFeed > -1);

			return commands.ToArray();
		}

		/// <summary>
		/// PuTTy has an active negotiation mode by default. Try remove IAC packages from input buffer because they will break command handlers
		/// </summary>
		/// <param name="connectionData">Warning, This method modifies input buffer</param>
		private void RemoveIacPackagesFromInputBuffer(ConnectionData connectionData)
		{
			var inputBuffer = connectionData.InputBuffer;
			var nonAicIndex = 0;
			int possibleIacIndex;
			do
			{
				possibleIacIndex = inputBuffer.IndexOf(0xFF, nonAicIndex);
				// If the next byte after iac also 0xFF this is a data byte. Has to be treated as a single value
				var isIacCommand = possibleIacIndex > -1 && inputBuffer[possibleIacIndex + 1] != 0xFF;
				if (isIacCommand)
				{
					_logger.Warning($"IAC package spotted from {connectionData.ClientIp} - {BitConverter.ToString(inputBuffer.GetRange(0, 3).ToArray())} " +
					                $"Server won't reply with a proper answer.");
					// The next 2 bytes after IAC has to be service packages Ex. AIC WILL ECHO
					inputBuffer.RemoveRange(possibleIacIndex, 3);
				}
				else
				{
					nonAicIndex = possibleIacIndex;
				}
			} while (possibleIacIndex != -1);
		}

		public void Dispose()
		{
			_connectionWaiter?.Dispose();

			foreach (var (_, value) in _stateStorage.Clients)
			{
				var socket = value.ConnectionData.Socket;
				socket.Shutdown(SocketShutdown.Both);
				socket.Close(60);
			}
		}
	}
}
