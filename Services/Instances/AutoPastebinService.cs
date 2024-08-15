using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands.Instances;
using SerousBot.DataStructures;
using SerousBot.Utility;
using System.Linq;
using System.Threading.Tasks;

namespace SerousBot.Services.Instances {
	public class AutoPastebinService : SerousComponent {
		public override async Task InstallAsync(DiscordSocketClient client, CommandService command) {
			client.MessageReceived += HandleAutopaste;
			await Task.CompletedTask;
		}

		private static async Task HandleAutopaste(SocketMessage socketMessage) {
			// Don't process the message if it was a system message
			if (socketMessage is not SocketUserMessage message || message.Channel is not SocketTextChannel)
				return;

			// Ignore messages from bots and webhooks
			if (message.Author.IsBot || message.Author.IsWebhook)
				return;

			// Check the message for any attachments
			var feedback = new Feedback<SocketUserMessage>(message.Author, message);
			feedback.ReplyMessage += async (socketMessage, message) => await HandleCommandResponse(socketMessage, message);
			feedback.CancelDeferMessage += async (socketMessage, message) => await HandleCommandResponse(socketMessage, message);
			feedback.ModifyMessage += async (socketMessage, message) => await HandleCommandResponse(socketMessage, message);

			if (message.Attachments.Count > 0) {
				// Attachments are present, so that logic takes priority even if a code block is present
				PasteContents paste = await PasteCommand.CheckMessageAttachments(feedback, message);

				if (paste.successful) {
					Logging.Info("AutoPastebinService", $"Detected text attachment from user \"{message.Author.FullName()}\", attempting to paste.");

					await PasteCommand.UploadPaste(feedback, paste, automaticPaste: true);
				}
			} else if (IsMessageCodeHeavy(message)) {
				// Prepare the message for pasting
				string content = CleanupCodeBlock(message);

				Logging.Info("AutoPastebinService", $"Detected code block from user \"{message.Author.FullName()}\", attempting to paste.");

				// Attempt to upload the paste
				if (await PasteCommand.UploadPaste(feedback, new PasteContents(null, content, true), automaticPaste: true)) {
					// Delete the original message so that it looks like the bot replaced it with the paste link
					await message.DeleteAsync();
				}
			}
		}

		private static async Task HandleCommandResponse(SocketUserMessage socketMessage, string message) => await socketMessage.Channel.SendMessageAsync(message);

		private static bool IsMessageCodeHeavy(SocketUserMessage message) {
			const int CODE_LINES_THRESHOLD = 15;

			bool mightBeCode = false;
			foreach (char c in message.Content) {
				if (c == '{' || c == '}' || c == '=' || c == ';') {
					mightBeCode = true;
					break;
				}
			}

			return mightBeCode && message.Content.Count(static c => c == '\n') > CODE_LINES_THRESHOLD;
		}

		private static readonly string[] languageIdentifiers = new[] { "html", "css", "cs", "dns", "python", "lua", "http", "markdown", "diff" };

		private static string CleanupCodeBlock(SocketUserMessage message) {
			string content = message.Content.Trim('`');

			foreach (string language in languageIdentifiers) {
				if (content.StartsWith(language + "\n"))
					return content[language.Length..].TrimStart('\n');
			}

			return content;
		}
	}
}
