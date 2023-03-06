using Discord.WebSocket;
using System.Collections.Generic;

namespace SerousBot.DataStructures {
	public class TagUserDictionary : Dictionary<string, TagNameDictionary> {
		public TagGuildDictionary Parent;

		public TagNameDictionary this[SocketUser user] {
			get => this[user.Id.ToString()];
			set {
				this[user.Id.ToString()] = value;
				value.Parent = this;
			}
		}

		public TagNameDictionary this[ulong userID] {
			get => this[userID.ToString()];
			set {
				this[userID.ToString()] = value;
				value.Parent = this;
			}
		}
	}
}
