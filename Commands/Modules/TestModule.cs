using Discord.Commands;
using System.Threading.Tasks;

namespace SerousBot.Commands.Modules {
	public class TestModule : ModuleBase<SocketCommandContext> {
		[Command("hello")]
		public async Task SayHello()
			=> await ReplyAsync("Hello world!");
	}
}
