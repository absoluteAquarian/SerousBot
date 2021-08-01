using Discord;
using System;
using System.Threading.Tasks;

namespace SerousBot.Utility{
	public static class Logging{
		public static Task Log(LogMessage message){
			Console.WriteLine(message.ToString());
			return Task.CompletedTask;
		}

		public static void WriteLine(string value, ConsoleColor? forceColor = null, bool overrideColor = false){
			var old = Console.ForegroundColor;

			if(forceColor is ConsoleColor color)
				Console.ForegroundColor = color;

			Console.WriteLine(value);

			if(!overrideColor)
				Console.ForegroundColor = old;
		}
	}
}
