using Discord;

namespace SerousBot.Utility {
	public static class BotUtils {
		public static string FullName(this IUser user) => user == null ? "Unknown User" : $"{user.Username}#{user.Discriminator}";
	}
}
