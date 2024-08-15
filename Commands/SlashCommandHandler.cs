using Discord;
using Discord.WebSocket;
using SerousBot.Commands.Instances;
using SerousBot.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SerousBot.Commands {
	public class SlashCommandHandler {
		private readonly DiscordSocketClient _client;

		private static readonly Dictionary<string, SerousSlashCommand> _handlers = new();

		public SlashCommandHandler(DiscordSocketClient client) {
			_client = client;
		}

		public async Task InstallAsync() {
			_client.SlashCommandExecuted += async command => await HandleCommand(command);

			await Task.CompletedTask;
		}

		internal static void RegisterAutomaticHandling(SlashCommandBuilder builder, SerousSlashCommand source) {
			if (_handlers.ContainsKey(builder.Name))
				throw new ArgumentException($"A command with the name \"{builder.Name}\" is already registered.");

			_handlers[builder.Name] = source;
		}

		private static async Task HandleCommand(SocketSlashCommand command) {
			Logging.Info("SlashCommandHandler", $"Received command \"{command.Data.Name}\" from user \"{command.User.FullName()}\"");

			if (!_handlers.TryGetValue(command.Data.Name, out var handler)) {
				await command.RespondAsync("This command is not yet implemented.");
				return;
			}

			await handler.HandleCommand(command);
		}
	}
}
