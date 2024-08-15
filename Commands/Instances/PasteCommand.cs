using Discord;
using Discord.WebSocket;
using SerousBot.DataStructures;
using SerousBot.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SerousBot.Commands.Instances {
	public class PasteCommand : SerousSlashCommand {
		protected override IEnumerable<SlashCommandBuilder> GetGuildCommandBuilders() {
			yield return new SlashCommandBuilder()
				.WithName("paste")
				.WithDescription("Attempts to paste a message's attachments to a pastebin.")
				.WithDefaultMemberPermissions(GuildPermission.SendMessages)
				.AddOption("channel", ApplicationCommandOptionType.Channel, "The channel which contains the message to paste from.", isRequired: false)
				.AddOption("message", ApplicationCommandOptionType.String, "The message ID to paste from.", isRequired: true);
		}

		public override async Task HandleCommand(SocketSlashCommand command) {
			if (await command.GetChannelAsync() is not SocketTextChannel channel) {
				await command.RespondAsync("This command can only be used in text channels.");
				return;
			}

			// Check the option
			string id = (string)command.Data.Options.First().Value;

			if (!ulong.TryParse(id, out ulong messageId)) {
				await command.RespondAsync("Invalid message ID.");
				return;
			}

			if (command.Data.Options.FirstOrDefault(option => option.Name == "channel") is SocketSlashCommandDataOption option) {
				if (option.Value is not SocketTextChannel textChannel) {
					await command.RespondAsync("Channel is not a text channel.");
					return;
				}

				channel = textChannel;
			}

			// Attempt to get the message from the channel
			IMessage message = await channel.GetMessageAsync(messageId);
			if (message is null) {
				await command.RespondAsync("Message not found.");
				return;
			}

			// Check the message
			var feedback = new Feedback<SocketSlashCommand>(command.User, command);
			feedback.ReplyMessage += async (command, message) => await command.RespondAsync(message);
			feedback.DeferMessage += async command => await command.DeferAsync();
			feedback.CancelDeferMessage += async (command, message) => await HandleFeedbackUpdate(command, message);
			feedback.ModifyMessage += async (command, message) => await HandleFeedbackUpdate(command, message);

			PasteContents paste = await CheckMessageAttachments(feedback, message);

			if (!paste.successful)
				return;

			// Upload the paste
			await UploadPaste(feedback, paste, automaticPaste: false);
		}

		private static async Task HandleFeedbackUpdate(SocketSlashCommand command, string message) {
			// Local capturing
			var m = message;
			await command.ModifyOriginalResponseAsync(msg => msg.Content = m);
		}

		public static async Task<PasteContents> CheckMessageAttachments(Feedback feedback, IMessage message) {
			// Check for attachments
			if (message.Attachments.Count == 0) {
				await feedback.Reply("No attachments found, could not paste.");
				return PasteContents.Failed;
			}

			// Only one attachment is supported
			if (message.Attachments.Count > 1) {
				await feedback.Reply("Message contained too many attachments, only one attachment per message is supported.");
				return PasteContents.Failed;
			}

			// Handle the attachment
			if (message.Attachments.First() is Attachment attachment)
				return await HandleAttachment(feedback, attachment);

			await feedback.Reply("Failed to handle attachment.");
			return PasteContents.Failed;
		}

		private static bool IsAttachmentSupported(Attachment attachment) {
			return attachment.Filename.EndsWith(".log") || attachment.Filename.EndsWith(".cs") || attachment.Filename.EndsWith(".json") || attachment.Filename.EndsWith(".txt");
		}

		private static async Task<PasteContents> HandleAttachment(Feedback feedback, Attachment attachment) {
			bool hasDeferred = false;

			try {
				if (IsAttachmentSupported(attachment)) {
					const int MAX_ATTACHMENT_SIZE = 400000;

					Logging.Info("PasteCommand", $"Attempting to paste attachment: {attachment.Filename}");

					if (attachment.Size >= MAX_ATTACHMENT_SIZE) {
						Logging.Error("PasteCommand", $"Attachment is too large to paste ({attachment.Size} > {MAX_ATTACHMENT_SIZE})");

						await feedback.Reply("Attachment is too large to paste.");
						return PasteContents.Failed;
					}

					// Inform the user that the command is being processed
					await feedback.Defer();
					hasDeferred = true;

					// Offload the download request to another thread to prevent gateway blocking
					string url = attachment.Url;
					string content = await Task.Run(async () => await DownloadContentsAsync(url));

					if (string.IsNullOrWhiteSpace(content)) {
						Logging.Error("PasteCommand", "Attachment download returned no content.");

						await feedback.CancelDefer("Attachment contained no visible text, could not paste.");
						return PasteContents.Failed;
					}

					return new PasteContents(attachment.Filename, content, true);
				}
			} catch (Exception ex) {
				Logging.Error("PasteCommand", "Attachment download threw an exception.");
				Logging.WriteLine(ex, ConsoleColor.Red);

				if (hasDeferred)
					await feedback.CancelDefer("Failed to handle attachment.");
				else
					await feedback.Reply("Failed to handle attachment.");

				return PasteContents.Failed;
			}

			// Attachment is not supported, but reporting this would cause spam, so we'll just silently ignore it
			return PasteContents.Failed;
		}

		private static async Task<string> DownloadContentsAsync(string url) {
			// Download the contents of the attachment
			using var client = new HttpClient();
			return await client.GetStringAsync(url);
		}

		private static readonly Regex hasteRegex = new(@"{""key"":""(?<key>[a-z].*)""}", RegexOptions.Compiled);

		public static async Task<bool> UploadPaste(Feedback feedback, PasteContents paste, bool automaticPaste) {
			if (!paste.successful)
				return false;

			string contents = paste.contents;
			string resultContent = await Task.Run(async () => await UploadContentsAndGetKeyAsync(contents));

			var match = hasteRegex.Match(resultContent);

			if (!match.Success) {
				// hastebin down?
				Logging.Error("PasteCommand", "Failed to upload paste.");
				await feedback.CancelDefer("Failed to upload paste.");
				return false;
			}

			string url = new StringBuilder("https://hst.sh/")
				.Append(match.Groups["key"].Value)
				.Append(paste.filename is not null ? Path.GetExtension(paste.filename) : ".cs")
				.ToString();

			Logging.Success("PasteCommand", $"Uploaded paste: {url}");

			StringBuilder message = new StringBuilder(automaticPaste ? "Automatic" : "Manual")
				.Append(" Hastebin for ")
				.Append(feedback.user.Username);

			if (paste.filename is not null)
				message.Append(" (`").Append(paste.filename).Append("`)");

			message.Append(": ").Append(url);

			await feedback.Modify(message.ToString());

			return true;
		}

		private static async Task<string> UploadContentsAndGetKeyAsync(string contents) {
			using var client = new HttpClient();
			HttpContent content = new StringContent(contents);

			var response = await client.PostAsync("https://hst.sh/documents", content);
			return await response.Content.ReadAsStringAsync();
		}
	}
}
