using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace SerousBot {
	public abstract class SerousComponent {
		public virtual async Task InstallAsync(DiscordSocketClient client, CommandService command) => await Task.CompletedTask;
	}
}
