using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    class DSharpMember : IMember
	{
		public DiscordMember Wrapped { get; }

		public string DisplayName => Wrapped.DisplayName;
		public bool IsBot => Wrapped.IsBot;

		public DSharpMember(DiscordMember wrapped)
		{
			Wrapped = wrapped;
		}

		public Task PlaceInAsync(IChannel c)
		{
			if (c is DSharpChannel chan)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.PlaceInAsync(chan.Wrapped));

			throw new InvalidOperationException("Passed an incorrect IChannel type");
		}

		public Task GrantRoleAsync(IRole r, string? reason = null)
		{
			if (r is DSharpRole role)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.GrantRoleAsync(role.Wrapped, reason));

			throw new InvalidOperationException("Passed an incorrect IRole type");
		}

		public Task RevokeRoleAsync(IRole r, string? reason = null)
		{
			if (r is DSharpRole role)
				return ExceptionWrap.WrapExceptionsAsync(() => Wrapped.GrantRoleAsync(role.Wrapped, reason));

			throw new InvalidOperationException("Passed an incorrect IRole type");
		}

		public async Task<IMessage> SendMessageAsync(string content)
		{
			var messageRet = await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.SendMessageAsync(content));
			return new DSharpMessage(messageRet);
		}

		public override bool Equals(object? other)
		{
			if (other is DSharpMember d)
			{
				return Wrapped.Equals(d?.Wrapped);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Wrapped.GetHashCode();
		}
    }
}
