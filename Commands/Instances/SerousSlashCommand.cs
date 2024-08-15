using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using SerousBot.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SerousBot.Commands.Instances {
	public abstract class SerousSlashCommand : SerousComponent {
		public sealed override async Task InstallAsync(DiscordSocketClient client, CommandService command) {
			try {
				foreach (var guild in client.Guilds) {
					foreach (var builder in GetGuildCommandBuilders()) {
						SlashCommandHandler.RegisterAutomaticHandling(builder, this);
						await client.Rest.CreateGuildCommand(builder.Build(), guild.Id);
					}
				}

				/*
				foreach (var builder in GetGlobalCommandBuilders())  {
					// Global commands should only be installed once since an installation sends a request to Discord.
					if (ComponentInstaller.IsGlobalCommandKnown(builder.Name))
						continue;

					ComponentInstaller.RememberGlobalCommand(builder.Name);
				//	SlashCommandHandler.RegisterAutomaticHandling(builder, this);
					await client.CreateGlobalApplicationCommandAsync(builder.Build());
				}
				*/
			} catch (HttpException commandEx) {
				var json = JsonConvert.SerializeObject(commandEx.Errors, Formatting.Indented);
				Logging.Error("ComponentInstaller", $"Failed to install service \"{GetType().Name}\"");
				Logging.WriteLine(json, Logging.COLOR_ERROR);
			}
		}

		protected virtual IEnumerable<SlashCommandBuilder> GetGuildCommandBuilders() {
			yield break;
		}

		public virtual async Task HandleCommand(SocketSlashCommand command) => await Task.CompletedTask;
	}
}
