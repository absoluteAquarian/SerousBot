using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using SerousBot.DataStructures.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerousBot.Commands.Instances {
	public class ModCommand : SerousSlashCommand {
		private class LazyModInfo {
			public readonly string mod;

			public bool Ready { get; private set; }

			public LazyModInfo(string mod) {
				this.mod = mod;
			}

			private ModInfoJson _cache;
			public async Task<ModInfoJson> GetMod() {
				if (!Ready) {
					_cache = await ParseModFile(mod);
					Ready = true;
				}

				return _cache;
			}
		}

		private static class ID {
			public const long MagicStorage = 1;
			public const long SerousCommonLib = 2;
		}

		protected override IEnumerable<SlashCommandBuilder> GetGuildCommandBuilders() {
			yield return new SlashCommandBuilder()
				.WithName("mod")
				.WithDescription("Displays information about one of absoluteAquarian's mods.")
				.WithDefaultMemberPermissions(GuildPermission.SendMessages)
				.AddOption(new SlashCommandOptionBuilder()
					.WithName("name")
					.WithDescription("The name of the mod to display information about.")
					.WithType(ApplicationCommandOptionType.Integer)
					.WithRequired(true)
					.AddChoice("Magic Storage", ID.MagicStorage)
					.AddChoice("absoluteAquarian Utilities", ID.SerousCommonLib));
		}

		private static readonly Dictionary<long, LazyModInfo> _mods = new();
		private static readonly Dictionary<string, long> _inverseNameToIDLookup = new();

		protected override async Task PostCommandInstall(DiscordSocketClient client) {
			RegisterMod("MagicStorage", ID.MagicStorage);
			RegisterMod("SerousCommonLib", ID.SerousCommonLib);

			await Task.CompletedTask;
		}

		private static void RegisterMod(string mod, long id) {
			_mods[id] = new LazyModInfo(mod);
			_inverseNameToIDLookup[mod] = id;
		}

		private static async Task<ModInfoJson> ParseModFile(string mod) {
			string file = Path.Combine("meta", "mods", $"{mod}.json");
			if (!File.Exists(file))
				return null;

			string json = await File.ReadAllTextAsync(file);
			return JsonConvert.DeserializeObject<ModInfoJson>(json);
		}

		public override async Task HandleCommand(SocketSlashCommand command) {
			try {
				await command.DeferAsync();

				long id = (long)command.Data.Options.First().Value;

				if (!_mods.TryGetValue(id, out var lazy) || await lazy.GetMod() is not ModInfoJson info) {
					await command.ModifyOriginalResponseAsync(static msg => msg.Embed = CreateBadModEmbed());
					return;
				}

				// Respond with an embed
				var embed = new EmbedBuilder()
					.WithAuthor(command.User)
					.WithTitle($"Requested mod: {info.displayName}")
					.WithDescription(await BuildEmbedDescription(lazy.mod, info))
					.WithColor(Color.Green)
					.WithCurrentTimestamp();

				await command.ModifyOriginalResponseAsync(msg => msg.Embed = embed.Build());
			} catch (Exception ex) {
				// Tell the user that the command failed
				await command.ModifyOriginalResponseAsync(msg => msg.Embed = CreateCommandFailEmbed(ex));
				throw;
			}
		}

		private static Embed CreateBadModEmbed() {
			return new EmbedBuilder()
				.WithTitle("Failed mod request.")
				.WithDescription("The mod you requested could not be found.")
				.WithColor(Color.Red)
				.WithCurrentTimestamp()
				.Build();
		}

		private static Embed CreateCommandFailEmbed(Exception ex) {
			return new EmbedBuilder()
				.WithTitle("Failed to execute command.")
				.WithDescription(ex.Message)
				.WithColor(Color.Red)
				.WithCurrentTimestamp()
				.Build();
		}

		private static async Task<string> BuildEmbedDescription(string modFile, ModInfoJson info) {
			StringBuilder sb = new StringBuilder()
				.Append("*Internal name:* `")
				.Append(modFile)
				.AppendLine("`")
				.Append("*Authors:* ")
				.AppendLine(string.Join(", ", info.authors.Select(static author => $"`{author}`")))
				.Append("*Workshop link:* <https://steamcommunity.com/sharedfiles/filedetails/?id=")
				.Append(info.steamID)
				.AppendLine(">");

			if (info.dependencies.Length > 0) {
				sb.Append("*Dependencies:* ")
					.AppendLine(await BuildDependencyList(info));
			}

			return sb.AppendLine("*Description:* ")
				.AppendLine(info.description)
				.ToString();
		}

		private static async Task<string> BuildDependencyList(ModInfoJson info) {
			if (info.dependencies.Length == 0)
				return "None";

			StringBuilder sb = new();
			for (int i = 0; i < info.dependencies.Length; i++) {
				sb.Append('`')
					.Append(await GetModName(info.dependencies[i]))
					.Append('`');

				if (i < info.dependencies.Length - 1)
					sb.Append(", ");
			}

			return sb.ToString();
		}

		private static async Task<string> GetModName(string dependency) {
			if (!_inverseNameToIDLookup.TryGetValue(dependency, out long id))
				return "<UNKNOWN>";

			if (await _mods[id].GetMod() is not ModInfoJson info)
				return "<UNKNOWN>";

			return info.displayName;
		}
	}
}
