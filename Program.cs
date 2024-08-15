using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands;
using SerousBot.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SerousBot {
	public class Program {
		public static bool QuietStartup { get; set; }

		public static void Main(string[] args) {
			foreach (string arg in args) {
				if (arg == "--quiet" || arg == "-q") {
					QuietStartup = true;
					break;
				}
			}

			while (true) {
				try {
					Run().GetAwaiter().GetResult();
				} catch (Exception ex) {
					Logging.WriteLine(ex.ToString(), ConsoleColor.Red);

					// Forcibly stop the client if it's still running
					client?.StopAsync().GetAwaiter().GetResult();
					client = null;

					// Restarting the client should not spam the bot channel with startup messages
					QuietStartup = true;
				}
			}
		}

		private static DiscordSocketClient client;
		private static TextCommandHandler textCommands;
		private static SlashCommandHandler slashCommands;
		private static ComponentInstaller componentInstaller;

		public static async Task Run() {
			client = new DiscordSocketClient(new DiscordSocketConfig() {
				GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
			});

			// Initialize the commands handler
			CommandService commandService = new(new CommandServiceConfig() {
				CaseSensitiveCommands = true,
				DefaultRunMode = RunMode.Async
			});

			textCommands = new TextCommandHandler(client, commandService);
			await textCommands.InstallCommandsAsync();

			slashCommands = new SlashCommandHandler(client);
			await slashCommands.InstallAsync();

			// Initialize the services
			componentInstaller = new(client, commandService);
			await componentInstaller.InstallStartupServicesAsync();

			client.Ready += async () => await componentInstaller.InstallClientReadyServicesAsync();

			client.Ready += async () => {
				Logging.Success("STARTUP", $"Successfully connected to Discord as \"{client.CurrentUser.FullName()}\"");
				await Task.CompletedTask;
			};

			client.JoinedGuild += async guild => {
				Logging.Success("GUILD_JOIN", $"Joined guild \"{guild.Name}\" [ID: {guild.Id}]");
				await Task.CompletedTask;
			};

			client.GuildAvailable += async guild => {
				Logging.Success("GUILD_AVAILABLE", $"Successfully connected to guild \"{guild.Name}\" [ID: {guild.Id}]");
				await Task.CompletedTask;
			};

			// Start the client session
			await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("SerousBotToken"));
			await client.StartAsync();

			// Loop infinitely until an unhandled exception has occurred OR the console was prompted with "exit"
			new Task(ReadInputAsync, TaskCreationOptions.LongRunning).Start();

			while (true) {
				while (!_inputAvailable)
					Thread.Yield();

				if (_input == "exit") {
					Environment.Exit(0);
					return;
				}

				_input = null;
				_inputAvailable = false;
			}
		}

		private static bool _inputAvailable;
		private static string _input;

		private static void ReadInputAsync() {
			while (true) {
				while (_inputAvailable)
					Thread.Yield();

				_input = Console.ReadLine();
				_inputAvailable = true;
			}
		}
	}
}
