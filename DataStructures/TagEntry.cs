using Discord.WebSocket;
using System;
using System.Runtime.Serialization;

namespace SerousBot.DataStructures {
	[DataContract]
	public class TagEntry {
		[DataMember(Name = "ownerID")]
		public ulong ownerID;
		[DataMember(Name = "name")]
		public string name;
		[DataMember(Name = "text")]
		public string text;
		[DataMember(Name = "global")]
		public bool isGlobal;

		public TagNameDictionary Parent;

		public TagEntry() { }

		public TagEntry(ulong ownerID, string name, string text) {
			this.ownerID = ownerID;
			this.name = name;
			this.text = text;
		}

		public TagEntry(SocketUser owner, string name, string text) : this(owner.Id, name, text) { }

		public bool IsOwner(ulong id) => ownerID == id;

		public bool MatchesName(string name) => this.name.Equals(name, StringComparison.InvariantCultureIgnoreCase);
	}
}
