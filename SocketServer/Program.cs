using System.Linq;
using Serilog;
using SocketServer.Handlers;
using SocketServer.Services;

namespace SocketServer
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			using var logger = new LoggerConfiguration()
				.WriteTo.Console().MinimumLevel.Verbose()
				.WriteTo.RollingFile("Logs\\SocketServer-{Date}.log")
				.CreateLogger();

			if (!args.Any())
			{
				logger.Fatal("Port is not specified. Supply a valid port via command line args. Ex: dotnet SocketServer.dll 23");
				return;
			}

			var isValidPort = int.TryParse(args[0], out var port);
			if (!isValidPort)
			{
				logger.Fatal($"Failed to parse port from '{args[0]}'");
				return;
			}

			var stateStorage = new StateStorage();
			var pipeline = new HandlersPipeline()
				.Register(new NumberHandler(stateStorage))
				.Register(new ListCommandHandler(logger, stateStorage))
				.Register(new ExitCommandHandler(logger, stateStorage))
				.Register(new UnknownCommandHandler()); ;


			using var socketServer = new Services.SocketServer(logger, stateStorage, pipeline);
			socketServer.Start(port);
		}
	}
}
