namespace SerousBot.DataStructures {
	public readonly struct CommandMessageReactionInfo {
		public readonly ulong user;
		public readonly ulong userMessage;

		public CommandMessageReactionInfo(ulong user, ulong userMessage) {
			this.user = user;
			this.userMessage = userMessage;
		}
	}
}
