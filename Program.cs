using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands;
using SerousBot.Utility;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SerousBot {
	public class Program {
		public static void Main() {
			try {
				new Program().Run().GetAwaiter().GetResult();
			} catch (Exception ex) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex.ToString());
			}
		}

		private DiscordSocketClient client;
		private CommandHandler commands;

		// TODO: configuration for the token and prefix perhaps?
		public const char Prefix = '?';

		public async Task Run() {
			client = new DiscordSocketClient();

			client.Log += Logging.Log;
			client.Ready += () => {
				Logging.WriteLine($"Successfully connected as \"{client.CurrentUser}\"", ConsoleColor.Green);
				return Task.CompletedTask;
			};

			//Initialize the commands handler
			commands = new CommandHandler(client, new CommandService(new CommandServiceConfig() {
				CaseSensitiveCommands = true,
				DefaultRunMode = RunMode.Async
			}));
			await commands.InstallCommandsAsync();

			//Start the client session
			await client.LoginAsync(TokenType.Bot, File.ReadAllText("token.txt"));
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
