using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public class DSharpMember : DiscordWrapper<DiscordMember>, IMember
	{
		private const string AUDIT_REASON = "Playing Blood on the Clocktower";

		public string DisplayName => Wrapped.DisplayName;
		public bool IsBot => Wrapped.IsBot;

		public DSharpMember(DiscordMember wrapped)
			: base(wrapped)
		{}

		public async Task MoveToChannelAsync(IChannel c)
		{
			if (c is not DSharpChannel chan)
				throw new InvalidOperationException("Passed an incorrect IChannel type");
			
			await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.PlaceInAsync(chan.Wrapped));

		}

		public async Task GrantRoleAsync(IRole r)
		{
			if (r is not DSharpRole role)
				throw new InvalidOperationException("Passed an incorrect IRole type");
			
			await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.GrantRoleAsync(role.Wrapped, AUDIT_REASON));
		}

		public async Task RevokeRoleAsync(IRole r)
		{
			if (r is not DSharpRole role)
				throw new InvalidOperationException("Passed an incorrect IRole type");
			
			await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.RevokeRoleAsync(role.Wrapped, AUDIT_REASON));
		}

		public async Task<IMessage> SendMessageAsync(string content)
		{
			var messageRet = await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.SendMessageAsync(content));
			return new DSharpMessage(messageRet);
		}
    }
}
