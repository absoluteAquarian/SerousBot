using Discord.WebSocket;
using System.Collections.Generic;

namespace SerousBot.DataStructures {
	public class TagGuildDictionary : Dictionary<string, TagUserDictionary> {
		public TagUserDictionary this[SocketGuild guild] {
			get => this[guild.Id.ToString()];
			set {
				this[guild.Id.ToString()] = value;
				value.Parent = this;
			}
		}

		public TagUserDictionary this[ulong guildID] {
			get => this[guildID.ToString()];
			set {
				this[guildID.ToString()] = value;
				value.Parent = this;
			}
		}

		public bool HasTag(ulong contextGuild, ulong contextUser, string name, out TagEntry tag, bool ignoreGlobalCheck = false) {
			foreach (var userDict in this[contextGuild]) {
				foreach (var entry in userDict.Value.Values) {
					if (!ignoreGlobalCheck && !entry.isGlobal && entry.ownerID != contextUser)
						continue;

					//Tag found.  Use it
					if (entry.MatchesName(name)) {
						tag = entry;
						return true;
					}
				}
			}

			//Could not find a tag
			tag = null;
			return false;
		}
	}
}
