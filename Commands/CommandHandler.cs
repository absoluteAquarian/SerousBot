using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands.Modules;
using SerousBot.Utility;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SerousBot.Commands {
	public class CommandHandler {
		private readonly DiscordSocketClient client;
		private readonly CommandService commands;

		//Retrieve client and CommandService instance via ctor
		public CommandHandler(DiscordSocketClient client, CommandService commands) {
			this.commands = commands;
			this.client = client;
		}

		public async Task InstallCommandsAsync() {
			//Hook the MessageReceived event into our command handler
			client.MessageReceived += HandleCommandAsync;
			commands.CommandExecuted += HandleExecutedAsync;

			//Here we discover all of the command modules in the entry 
			//assembly and load them. Starting from Discord.NET 2.0, a
			//service provider is required to be passed into the
			//module registration method to inject the 
			//required dependencies.
			//
			//If you do not use Dependency Injection, pass null.
			//See Dependency Injection guide for more information.
			await commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
		}

		private async Task HandleCommandAsync(SocketMessage messageParam) {
			//Don't process the command if it was a system message
			if (!(messageParam is SocketUserMessage message))
				return;

			//Create a number to track where the prefix ends and the command begins
			int argPos = 0;

			//Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (!(message.HasCharPrefix(Program.Prefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)) || message.Author.IsBot)
				return;

			//Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(client, message);

			//Execute the command with the command context we just
			//created, along with the service provider for precondition checks.
			var result = await commands.ExecuteAsync(context: context, argPos: argPos, services: null);

			if (!result.IsSuccess) {
				//Might be a tag... Check if it exists
				string text = message.Content.Substring(1);

				// TODO: finish
			}
		}

		private async Task HandleExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result) {
			//Command doesn't exist
			if (!command.IsSpecified) {
				Logging.WriteLine($"Command failed to execute for user \"{context.User.Username}\": {result.ErrorReason}", ConsoleColor.Red);
				return;
			}

			//Command was successful
			if (result.IsSuccess) {
				Console.WriteLine($"User \"{context.User.Username}#{context.User.DiscriminatorValue}\" executed command \"{command.Value.Name}\"");
				return;
			}

			//Something went wrong.  Let the user know
			await context.Channel.SendMessageAsync($"Something went wrong when executing the \"{command.Value.Name}\" command:\n{result}");
		}
	}
}
