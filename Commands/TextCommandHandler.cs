using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Utility;
using System.Reflection;
using System.Threading.Tasks;

namespace SerousBot.Commands {
	public class TextCommandHandler {
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;

		// Retrieve client and CommandService instance via ctor
		public TextCommandHandler(DiscordSocketClient client, CommandService commands) {
			_commands = commands;
			_client = client;
		}

		public async Task InstallCommandsAsync() {
			// Hook the MessageReceived event into our command handler
			_client.MessageReceived += HandleCommandAsync;
			_commands.CommandExecuted += HandleExecutedAsync;

			// Here we discover all of the command modules in the entry 
			// assembly and load them. Starting from Discord.NET 2.0, a
			// service provider is required to be passed into the
			// module registration method to inject the 
			// required dependencies.
			//
			// If you do not use Dependency Injection, pass null.
			// See Dependency Injection guide for more information.
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam) {
			// Don't process the command if it was a system message
			if (messageParam is not SocketUserMessage message || message.Channel is not SocketTextChannel)
				return;

			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;

			// Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (!message.HasMentionPrefix(_client.CurrentUser, ref argPos))
				return;

			// Ignore messages from bots and webhooks
			if (message.Author.IsBot || message.Author.IsWebhook)
				return;

			// Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(_client, message);

			// Execute the command with the command context we just
			// created, along with the service provider for precondition checks.
			await _commands.ExecuteAsync(context: context, argPos: argPos, services: null);
		}

		private async Task HandleExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {
			if (!command.IsSpecified) {
				// Command doesn't exist
				await context.Channel.SendMessageAsync($"Unknown command.");
			} else if (!result.IsSuccess) {
				// Something went wrong.  Let the user know
				await context.Channel.SendMessageAsync($"Something went wrong when executing the \"{command.Value.Name}\" command:\n{result}");
			} else
				Logging.Info("TextCommandHandler", $"Command \"{command.Value.Name}\" executed successfully by user \"{context.User.FullName()}\"");
		}
	}
}
