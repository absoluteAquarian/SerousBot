using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Utility;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SerousBot.Services.Instances {
	public class BotChannelService : SerousComponent {
		private static readonly ConcurrentDictionary<ulong, SocketTextChannel> _channels = new();
		public static IReadOnlyCollection<SocketTextChannel> Channels => new ReadOnlyCollectionBuilder<SocketTextChannel>(_channels.Values).ToReadOnlyCollection();

		private const string FILE = "botchannels.txt";

		public override async Task InstallAsync(DiscordSocketClient client, CommandService command) {
			if (File.Exists(FILE)) {
				// File exists, attempt to parse the guild-channel IDs
				bool mismatch = await ParseFile(client);

				if (mismatch) {
					// Update the file with the valid guild-channel IDs
					await UpdateFileAsync();
				}
			}

			if (!Program.QuietStartup) {
				foreach (var channel in Channels)
					await channel.SendMessageAsync("Bot has successfully started up.");
			}

			// Successive restarts should be quiet
			Program.QuietStartup = true;
		}

		private static async Task<bool> ParseFile(DiscordSocketClient client) {
			bool mismatch = false;

			int lineNum = 0;
			foreach (string line in await File.ReadAllLinesAsync(FILE)) {
				lineNum++;

				if (!line.Contains('-')) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} had an invalid format.");
					mismatch = true;
					continue;
				}

				string[] parts = line.Split('-');
				if (parts.Length != 2) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} had an invalid format.");
					mismatch = true;
					continue;
				}

				if (!ulong.TryParse(parts[0], out ulong guildId)) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} had an invalid guild ID.");
					mismatch = true;
					continue;
				}

				SocketGuild guild = client.GetGuild(guildId);
				if (guild is null) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} referenced a guild that does not exist.");
					mismatch = true;
					continue;
				}

				if (!ulong.TryParse(parts[1], out ulong channelId)) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} had an invalid channel ID.");
					mismatch = true;
					continue;
				}

				SocketTextChannel channel = guild.GetTextChannel(channelId);
				if (channel is null) {
					Logging.Error("BotChannelService", $"Line {lineNum} in {FILE} referenced a channel that does not exist.");
					mismatch = true;
					continue;
				}

				_channels[guildId] = channel;
			}

			return mismatch;
		}

		public static async Task SetChannelAsync(SocketTextChannel channel) {
			_channels[channel.Guild.Id] = channel;

			await UpdateFileAsync();
		}

		private static async Task UpdateFileAsync() => await File.WriteAllLinesAsync(FILE, Channels.Select(channel => $"{channel.Guild.Id}-{channel.Id}"));
	}
}
