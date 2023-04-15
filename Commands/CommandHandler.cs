using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands.Modules;
using SerousBot.Utility;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
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
			client.MessageReceived += HandleAutoPasteAsync;
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
			if (!(messageParam is SocketUserMessage message) || !(message.Channel is SocketTextChannel))
				return;

			//Create a number to track where the prefix ends and the command begins
			int argPos = 0;

			//Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (!(message.HasCharPrefix(Program.Prefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
				return;

			// Ignore messages from bots and webhooks
			if (message.Author.IsBot || message.Author.IsWebhook)
				return;

			//Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(client, message);

			//Execute the command with the command context we just
			//created, along with the service provider for precondition checks.
			var result = await commands.ExecuteAsync(context: context, argPos: argPos, services: null);

			if (!result.IsSuccess) {
				//Might be a tag... Check if it exists
				result = await GetTagAsync(context, result);

				if (!result.IsSuccess && !result.ErrorReason.Equals("Unknown command.", StringComparison.InvariantCultureIgnoreCase))
					await context.Channel.SendMessageAsync(result.ErrorReason);
			}
		}

		private static readonly Regex hasteRegex = new Regex(@"{""key"":""(?<key>[a-z].*)""}", RegexOptions.Compiled);

		private readonly string[] codeFiles = new string[] {
			"html",
			"css",
			"cs",
			"dns",
			"python",
			"lua",
			"http",
			"markdown",
		};

		private async Task HandleAutoPasteAsync(SocketMessage messageParam) {
			//Don't process the command if it was a system message
			if (!(messageParam is SocketUserMessage message) || !(message.Channel is SocketTextChannel))
				return;

			// Ignore messages from bots and webhooks
			if (message.Author.IsBot || message.Author.IsWebhook)
				return;

			//Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(client, message);

			string contents = message.Content;
			bool shouldHastebin = false;
			bool autoDeleteUserMessage = false;
			string extra = "";

			var attachents = message.Attachments;
			if (attachents.Count == 1 && attachents.ElementAt(0) is Attachment attachment) {
				Console.WriteLine($"Attempting auto-paste for attachment \"{attachment.Filename}\" from user \"{message.Author.Username}\"...");

				const int MAX_ATTACHMENT_SIZE = 400000;

				if (attachment.Filename.EndsWith(".log") || attachment.Filename.EndsWith(".cs") || attachment.Filename.EndsWith(".json") || attachment.Filename.EndsWith(".txt")) {
					if (attachment.Size < MAX_ATTACHMENT_SIZE) {
						using (var client = new HttpClient())
							contents = await client.GetStringAsync(attachment.Url);

						shouldHastebin = true;
						extra = $" `({attachment.Filename})`";
					} else
						Logging.WriteLine($"Could not auto-paste file \"{attachment.Filename}\", size exceeded maximum ({attachment.Size} > {MAX_ATTACHMENT_SIZE})", ConsoleColor.Red);
				}
			}

			if (string.IsNullOrWhiteSpace(contents))
				return;

			int count = 0;
			if (!shouldHastebin) {
				foreach (char c in contents) {
					if (c == '{' || c == '}' || c == '=' || c == ';')
						count++;
				}
				if (count > 1 && message.Content.Split('\n').Length > 16) {
					shouldHastebin = true;
					autoDeleteUserMessage = true;

					Console.WriteLine($"Large code block detected from user \"{message.Author.Username}\".  Attempting auto-paste...");
				}
			}

			if (shouldHastebin) {
				string hastebinContent = contents.Trim('`');
				for (int i = 0; i < codeFiles.Length; i++) {
					string keyword = codeFiles[i];
					if (hastebinContent.StartsWith(keyword + "\n")) {
						hastebinContent = hastebinContent.Substring(keyword.Length).TrimStart('\n');
						break;
					}
				}

				//var msg = await context.Channel.SendMessageAsync("Auto Hastebin in progress");
				using (var client = new HttpClient()) {
					HttpContent content = new StringContent(hastebinContent);

					var response = await client.PostAsync("https://hst.sh/documents", content);
					string resultContent = await response.Content.ReadAsStringAsync();

					var match = hasteRegex.Match(resultContent);

					if (!match.Success) {
						// hastebin down?
						Logging.WriteLine("Auto-paste failed", ConsoleColor.Red);
						return;
					}

					string hasteUrl = $"https://hst.sh/{match.Groups["key"]}.cs";
					await context.Channel.SendMessageAsync($"Automatic Hastebin for {message.Author.Username}{extra}: {hasteUrl}");
					if (autoDeleteUserMessage)
						await message.DeleteAsync();

					Logging.WriteLine("Auto-paste succeeded", ConsoleColor.Green);
				}
			}
		}

		private async Task<IResult> GetTagAsync(SocketCommandContext context, IResult result) {
			if (context.Channel != null) {
				// Skip prefix
				string text = context.Message.Content[1..];
				string check = Format.Sanitize(text);

				if (check == text) {
					if (TagModule.dict.HasTag(context.Guild.Id, context.User.Id, text, out var tag)) {
						var msg = await context.Channel.SendMessageAsync($"{Format.Bold($"Tag: {tag.name}")}" +
							$"\n{tag.text}");

						await msg.AddReactionAsync(new Emoji("❌"));
						TagModule.DeleteableTags.Add(msg.Id, (context.Message.Author.Id, context.Message.Id));
						await context.Message.DeleteAsync();
						return new ExecuteResult();
					} else {
						// Find a global tag
						var tags = TagModule.GetTags(text, globalTags: true).ToList();

						// One tag was found; use it
						if (tags.Count == 1) {
							return await commands.ExecuteAsync(context, $"tag -g {tag.ownerID} {tag.name}", services: null, MultiMatchHandling.Exception);
						}

						// TODO: "tag find" and "tag list"?
						if (tags.Count > 1) {
							await context.Channel.SendMessageAsync("Multiple tags with that name were found." +
								"\nTag listing has not been implemented yet.");
							return new ExecuteResult();
						}

						await context.Channel.SendMessageAsync("No tags with that name were found.");
						return new ExecuteResult();
					}
				}
			}

			return result;
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
