using System;

namespace SerousBot.Utility {
	public static class Logging {
		public const ConsoleColor COLOR_WARN = ConsoleColor.Yellow;
		public const ConsoleColor COLOR_INFO = ConsoleColor.White;
		public const ConsoleColor COLOR_ERROR = ConsoleColor.Red;
		public const ConsoleColor COLOR_SUCCESS = ConsoleColor.Green;

		private static readonly object _consoleLock = new();

		public static void WriteLine(string message, ConsoleColor forceColor) {
			lock (_consoleLock) {
				var fg = Console.ForegroundColor;
				var bg = Console.BackgroundColor;

				Console.ForegroundColor = forceColor;
				Console.BackgroundColor = ConsoleColor.Black;

				Console.WriteLine(message);

				Console.ForegroundColor = fg;
				Console.BackgroundColor = bg;
			}
		}

		public static void WriteLine(object message, ConsoleColor forceColor) => WriteLine(message.ToString(), forceColor);

		public static void Warn(string category, string message) => WriteLine($"[WARN/{category}]: {message}", COLOR_WARN);

		public static void Warn(object category, string message) => Warn(category.ToString(), message);

		public static void Info(string category, string message) => WriteLine($"[INFO/{category}]: {message}", COLOR_INFO);

		public static void Info(object category, string message) => Info(category.ToString(), message);

		public static void Error(string category, string message) => WriteLine($"[ERROR/{category}]: {message}", COLOR_ERROR);

		public static void Error(object category, string message) => Error(category.ToString(), message);

		public static void Success(string category, string message) => WriteLine($"[{category}]: {message}", COLOR_SUCCESS);

		public static void Success(object category, string message) => Success(category.ToString(), message);
	}
}
