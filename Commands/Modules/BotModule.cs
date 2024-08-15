using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SerousBot.Services.Instances;
using System.Threading.Tasks;

namespace SerousBot.Commands.Modules {
	[Group("bot")]
	public class BotModule : ModuleBase<SocketCommandContext> {
		[Group("channel")]
		public class ChannelModule : ModuleBase<SocketCommandContext> {
			[Command("set")]
			[RequireUserPermission(GuildPermission.Administrator)]
			[Summary("Sets the channel the bot will use for startup messages.")]
			public async Task SetChannelAsync(IChannel channel) {
				if (channel is not SocketTextChannel textChannel) {
					await ReplyAsync("The channel must be a text channel.");
					return;
				}

				await BotChannelService.SetChannelAsync(textChannel);

				await ReplyAsync($"The bot channel has been set to {textChannel.Mention}.");
			}
		}
	}
}
