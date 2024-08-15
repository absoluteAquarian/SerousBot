using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Utility;
using System.Threading.Tasks;

namespace SerousBot.Services.Instances {
	public class LoggingService : SerousComponent {
		public override async Task InstallAsync(DiscordSocketClient client, CommandService commands) {
			client.Log += LogAsync;
			commands.Log += LogAsync;

			await Task.CompletedTask;
		}

		private static Task LogAsync(LogMessage message) {
			if (message.Exception is CommandException cmdException) {
				Logging.Error(message.Severity, $"{cmdException.Command.Aliases[0]} failed to execute in {cmdException.Context.Channel}.");
				Logging.WriteLine(cmdException.ToString(), Logging.COLOR_ERROR);
			} else
				Logging.Info(message.Severity, $"{message}");

			return Task.CompletedTask;
		}
	}
}
