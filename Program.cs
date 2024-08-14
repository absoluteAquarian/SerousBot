using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands;
using SerousBot.Utility;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SerousBot {
	public class Program {
		public static void Main() {
			while (true) {
				try {
					Run().GetAwaiter().GetResult();
				} catch (Exception ex) {
					Logging.WriteLine(ex.ToString(), ConsoleColor.Red);

					// Forcibly stop the client if it's still running
					client?.StopAsync().GetAwaiter().GetResult();
					client = null;
				}
			}
		}

		private static DiscordSocketClient client;
		private static CommandHandler commands;

		// TODO: configuration for the token and prefix perhaps?
		public static char Prefix = '?';

		public static async Task Run() {
			client = new DiscordSocketClient();

			client.Log += Logging.Log;
			client.Ready += async () => {
				Logging.WriteLine($"Successfully connected as \"{client.CurrentUser}\"", ConsoleColor.Green);

				string pfxFile = "prefix.txt";
				if (File.Exists(pfxFile))
					Prefix = File.ReadAllText(pfxFile)[0];

				Logging.WriteLine($"Prefix initialized as \"{Prefix}\"");

				await Task.CompletedTask;
			};

			//Initialize the commands handler
			commands = new CommandHandler(client, new CommandService(new CommandServiceConfig() {
				CaseSensitiveCommands = true,
				DefaultRunMode = RunMode.Async
			}));
			await commands.InstallCommandsAsync();

			//Start the client session
			await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("SerousBotToken"));
			await client.StartAsync();

			//Loop infinitely until an unhandled exception has occurred OR the console was prompted with "exit"
			while (true) {
				//"request" will be a non-null value if and only if the awaited task completed
				string request = null;
				await Task.Run(() => request = Console.ReadLine());

				if (request == "exit") {
					Environment.Exit(0);
					return;
				}
			}
		}
	}
}
