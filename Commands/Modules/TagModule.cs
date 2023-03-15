using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using SerousBot.DataStructures;
using SerousBot.Utility;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SerousBot.Commands.Modules {
	[Group("tag")]
	public class TagModule : ModuleBase<SocketCommandContext> {
		public static TagGuildDictionary dict;

		public static IEnumerable<TagEntry> AllTags() {
			foreach (var user in dict.Values) {
				foreach (var nameDict in user.Values) {
					foreach (var tag in nameDict.Values)
						yield return tag;
				}
			}
		}

		public static IEnumerable<TagEntry> TagsOwnedBy(ulong id) {
			foreach (var user in dict.Values) {
				foreach (var (userID, nameDict) in user) {
					if (userID != id)
						continue;

					foreach (var tag in nameDict.Values)
						yield return tag;
				}
			}
		}

		public static IEnumerable<TagEntry> GetTags(string key, ulong? id = null, bool globalTags = false) {
			var tags = id is ulong u ? TagsOwnedBy(u) : AllTags();

			foreach (var tag in tags) {
				if (globalTags && !tag.isGlobal)
					continue;

				if (tag.MatchesName(key)) {
					yield return tag;
					yield break;
				}
			}

			foreach (var tag in tags) {
				if (globalTags && !tag.isGlobal)
					continue;

				if (tag.name.Contains(key))
					yield return tag;
			}
		}

		private static readonly object addLock = new object();

		internal static readonly Dictionary<ulong, (ulong, ulong)> DeleteableTags = new Dictionary<ulong, (ulong, ulong)>(); // bot message id, <requester user id, original request message>

		private static bool ValidTagName(string name) => Format.Sanitize(name) == name && !name.Contains(" ");

		[Command("add")]
		public async Task AddTag(string name, [Remainder] string tag) {
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag))
				return;

			//User had something like:  ?tag add "cool tag" stuff
			if (!ValidTagName(name)) {
				await ReplyAsync($"Command `{Program.Prefix}tag add` expects tag name to not contain spaces, symbols or control characters");
				return;
			}

			var guild = Context.Guild;
			var user = Context.User;

			Directory.CreateDirectory("Tags");
			string path = Path.Combine("Tags", "list.json");

			TagEntry entry = new TagEntry(user, name, tag);

			//File doesn't exist?  Create it, since no tags exist yet
			if (!File.Exists(path)) {
				lock (addLock) {
					// "guild id" -> "user id" -> "tag name" -> tag
					dict = new TagGuildDictionary() {
						[guild] = new TagUserDictionary() {
							[user] = new TagNameDictionary() {
								[name] = entry
							}
						}
					};
				}
			} else {
				lock (addLock) {
					if (dict is null)
						dict = JsonConvert.DeserializeObject<TagGuildDictionary>(File.ReadAllText(path));

					if (dict[guild] is null)
						dict[guild] = new TagUserDictionary();
					if (dict[guild][user] is null)
						dict[guild][user] = new TagNameDictionary();
				}

				if (!dict[guild][user].ContainsKey(name)) {
					lock (addLock) {
						dict[guild][user][name] = entry;
					}
				} else {
					await ReplyAsync($"Tag `{name}` was added by user **{user.Username}#{user.DiscriminatorValue}** already.  Did you mean to use `{Program.Prefix}tag edit {name}` instead?");
					return;
				}
			}

			lock (addLock) {
				File.WriteAllText(path, JsonConvert.SerializeObject(dict, Formatting.Indented));
			}

			await ReplyAsync($"Tag `{name}` was created successfully.");
		}

		[Command]
		public async Task Default(string key)
			=> await GetTagAsync(Context.User, key);

		[Command]
		public async Task Default(SocketUser user, string name)
			=> await GetTagAsync(user, name);

		[Command("get")]
		public async Task GetTagAsync(SocketUser user, string name) {
			var guildID = Context.Guild.Id;
			if (!dict.TryGetValue(guildID, out var userDict)) {
				await ReplyAsync($"Tag `{name}` could not be found.");
				return;
			}

			var userID = user.Id;
			if (!userDict.TryGetValue(userID, out var tagDict) || !tagDict.TryGetValue(name, out TagEntry tag)) {
				await ReplyAsync($"Tag `{name}` could not be found.");
				return;
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine(Format.Bold($"Tag: {name} (Owner: {Context.Guild.GetUser(tag.ownerID).FullName()})"));
			sb.Append(tag.text);

			var msg = await ReplyAsync(sb.ToString());

			await msg.AddReactionAsync(new Emoji("❌"));
			await Context.Message.DeleteAsync();
			DeleteableTags.Add(msg.Id, (Context.Message.Author.Id, Context.Message.Id));
		}

		[Command("edit")]
		public async Task EditTagAsync(string name, [Remainder] string tag)
			=> await EditTagAsync(Context.User, name, tag);

		[Command("edit")]
		public async Task EditTagAsync(SocketUser user, string name, [Remainder] string tag) {
			if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(tag))
				return;

			//User had something like:  ?tag edit "cool tag" stuff
			if (!ValidTagName(name)) {
				await ReplyAsync($"Command `{Program.Prefix}tag edit` expects tag name to not contain spaces, symbols or control characters");
				return;
			}

			Directory.CreateDirectory("Tags");
			string path = Path.Combine("Tags", "list.json");

			if (!File.Exists(path)) {
				await ReplyAsync("No tags have been registered.");
				return;
			}

			if (dict is null) {
				lock (addLock) {
					dict = JsonConvert.DeserializeObject<TagGuildDictionary>(File.ReadAllText(path));
				}
			}

			var guildID = Context.Guild.Id;
			if (!dict.TryGetValue(guildID, out var userDict)) {
				await ReplyAsync($"This server does not contain any tags.");
				return;
			}

			var userID = user.Id;
			if (!userDict.TryGetValue(userID, out var tagDict)) {
				await ReplyAsync($"User `{userID}` does not own any tags.");
				return;
			}

			if (user.Id != Context.User.Id) {
				await ReplyAsync("You can only edit tags that you have created.");
				return;
			}

			tagDict[name].text = tag;

			lock (addLock) {
				File.WriteAllText(path, JsonConvert.SerializeObject(dict, Formatting.Indented));
			}

			await ReplyAsync($"Tag `{name}` was updated.");
		}

		[Command("global")]
		public async Task GlobalAsync(string name, bool toggle)
			=> await GlobalAsync(Context.User.Id, name, toggle);

		[Command("global")]
		public async Task GlobalAsync(ulong userID, string name, bool toggle) {
			//Must be the server owner to use this command
			if (Context.Guild.OwnerId != userID)
				return;

			if (string.IsNullOrWhiteSpace(name))
				return;

			//User had something like:  ?tag global "cool tag" stuff
			if (!ValidTagName(name)) {
				await ReplyAsync($"Command `{Program.Prefix}tag global` expects tag name to not contain spaces, symbols or control characters");
				return;
			}

			Directory.CreateDirectory("Tags");
			string path = Path.Combine("Tags", "list.json");

			if (!File.Exists(path)) {
				await ReplyAsync("No tags have been registered.");
				return;
			}

			if (dict is null) {
				lock (addLock) {
					dict = JsonConvert.DeserializeObject<TagGuildDictionary>(File.ReadAllText(path));
				}
			}

			if (!dict.HasTag(Context.Guild.Id, userID, name, out TagEntry tag, ignoreGlobalCheck: true)) {
				await ReplyAsync("Tag could not be found.");
				return;
			}

			tag.isGlobal = toggle;

			lock (addLock) {
				File.WriteAllText(path, JsonConvert.SerializeObject(dict, Formatting.Indented));
			}

			await ReplyAsync($"Tag `{name}` had its global status updated.");
		}
	}
}
