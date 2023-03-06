using System.Collections.Generic;

namespace SerousBot.DataStructures {
	public class TagNameDictionary : Dictionary<string, TagEntry> {
		public TagUserDictionary Parent;

		public new TagEntry this[string name] {
			get => (this as Dictionary<string, TagEntry>)[name];
			set {
				(this as Dictionary<string, TagEntry>)[name] = value;
				value.Parent = this;
			}
		}
	}
}
