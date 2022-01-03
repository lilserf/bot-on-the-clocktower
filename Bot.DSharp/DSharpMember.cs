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

		public async Task<bool> MoveToChannelAsync(IChannel c, IProcessLogger logger)
		{
			if (c is DSharpChannel chan)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.PlaceInAsync(chan.Wrapped));
					return true;
				}
				catch (Exception ex)
				{
					logger.LogException(ex, $"move {DisplayName} to channel {chan.Name}");
					return false;
				}
			}

			throw new InvalidOperationException("Passed an incorrect IChannel type");
		}

		public async Task<bool> GrantRoleAsync(IRole r, IProcessLogger logger)
		{
			if (r is DSharpRole role)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.GrantRoleAsync(role.Wrapped, AUDIT_REASON));
					return true;
				}
				catch (Exception ex)
				{
					logger.LogException(ex, $"grant role '{role.Name}' to {DisplayName}");
					return false;
				}
			}

			throw new InvalidOperationException("Passed an incorrect IRole type");
		}

		public async Task<bool> RevokeRoleAsync(IRole r, IProcessLogger logger)
		{
			if (r is DSharpRole role)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.GrantRoleAsync(role.Wrapped, AUDIT_REASON));
					return true;
				}
				catch(Exception ex)
				{
					logger.LogException(ex, $"revoke role '{role.Name}' from {DisplayName}");
					return false;
				}
			}
			throw new InvalidOperationException("Passed an incorrect IRole type");
		}

		public async Task<IMessage> SendMessageAsync(string content)
		{
			var messageRet = await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.SendMessageAsync(content));
			return new DSharpMessage(messageRet);
		}
    }
}
