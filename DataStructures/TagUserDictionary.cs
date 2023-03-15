using Discord.WebSocket;
using System.Collections.Generic;

namespace SerousBot.DataStructures {
	public class TagUserDictionary : Dictionary<ulong, TagNameDictionary> {
		public TagGuildDictionary Parent;

		public TagNameDictionary this[SocketUser user] {
			get => this[user.Id];
			set {
				this[user.Id] = value;
				value.Parent = this;
			}
		}
	}
}
