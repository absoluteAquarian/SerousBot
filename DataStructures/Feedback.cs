using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace SerousBot.DataStructures {
	// Non-generic variant needed to allow for more flexibility
	public abstract class Feedback {
		public readonly SocketUser user;

		public Feedback(SocketUser user) {
			this.user = user;
		}

		public virtual async Task Reply(string message) => await Task.CompletedTask;

		public virtual async Task Defer() => await Task.CompletedTask;

		public virtual async Task CancelDefer(string reason) => await Task.CompletedTask;

		public virtual async Task Modify(string newMessage) => await Task.CompletedTask;
	}

	public sealed class Feedback<T> : Feedback {
		public readonly T value;

		public event Func<T, string, Task> ReplyMessage;
		public event Func<T, Task> DeferMessage;
		public event Func<T, string, Task> CancelDeferMessage;
		public event Func<T, string, Task> ModifyMessage;

		public Feedback(SocketUser user, T value) : base(user) {
			this.value = value;
		}

		public override async Task Reply(string message) => await (ReplyMessage?.Invoke(value, message) ?? Task.CompletedTask);

		public override async Task Defer() => await (DeferMessage?.Invoke(value) ?? Task.CompletedTask);

		public override async Task CancelDefer(string reason) => await (CancelDeferMessage?.Invoke(value, reason) ?? Task.CompletedTask);

		public override async Task Modify(string newMessage) => await (ModifyMessage?.Invoke(value, newMessage) ?? Task.CompletedTask);
	}
}
