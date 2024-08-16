using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Commands.Instances;
using SerousBot.Services.Instances;
using SerousBot.Utility;
using System;
using System.Threading.Tasks;

namespace SerousBot {
	public class ComponentInstaller {
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;

		public ComponentInstaller(DiscordSocketClient client, CommandService commands) {
			_client = client;
			_commands = commands;
		}

		public async Task InstallStartupServicesAsync() {
			await SafelyInstall<LoggingService>();
			await SafelyInstall<AutoPastebinService>();
		}

		public async Task InstallClientReadyServicesAsync() {
			// Guilds are not available until the client is ready
			await SafelyInstall<BotChannelService>();
			await SafelyInstall<PasteCommand>();
			await SafelyInstall<ModCommand>();
		}

		private async Task SafelyInstall<T>() where T : SerousComponent, new() {
			try {
				await new T().InstallAsync(_client, _commands);

				Logging.Success("ComponentInstaller", $"Successfully installed component \"{typeof(T).Name}\"");
			} catch (Exception ex) {
				Logging.Error("ComponentInstaller", $"Failed to install component \"{typeof(T).Name}\"");
				Logging.WriteLine(ex, Logging.COLOR_ERROR);
			}
		}
	}
}
