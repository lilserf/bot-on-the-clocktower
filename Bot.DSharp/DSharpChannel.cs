using Bot.Api;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Bot.Api.IBaseChannel;

namespace Bot.DSharp
{
    public class DSharpChannel : DiscordWrapper<DiscordChannel>, IChannel
	{
		public DSharpChannel(DiscordChannel wrapped)
			: base(wrapped)
		{}

		public ulong Id => Wrapped.Id;

		public IReadOnlyCollection<IMember> Users => Wrapped.Users.Select(x => new DSharpMember(x)).ToList();

		public int Position => Wrapped.Position;

		public bool IsVoice => Wrapped.Type == DSharpPlus.ChannelType.Voice;
		public bool IsText => Wrapped.Type == DSharpPlus.ChannelType.Text;

		public string Name => Wrapped.Name;

        public async Task AddOverwriteAsync(IMember m, Permissions allow, Permissions deny = Permissions.None)
        {
			if (m is DSharpMember member)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.AddOverwriteAsync(member.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny));
				}
				catch (Bot.Api.UnauthorizedException)
				{ }
			}
        }

		public async Task AddOverwriteAsync(IRole r, IChannel.Permissions allow, Permissions deny = Permissions.None)
		{
			if (r is DSharpRole role)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.AddOverwriteAsync(role.Wrapped, (DSharpPlus.Permissions)allow, (DSharpPlus.Permissions)deny));
				}
				catch (Bot.Api.UnauthorizedException)
				{ }

			}
		}

        public async Task DeleteAsync(string? reason = null)
        {
			await Wrapped.DeleteAsync(reason);
        }

        public async Task RemoveOverwriteAsync(IMember m)
        {
			if (m is DSharpMember member)
            {
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.DeleteOverwriteAsync(member.Wrapped));
				}
				catch (Bot.Api.UnauthorizedException)
				{ }

			}
		}

		public async Task RemoveOverwriteAsync(IRole m)
		{
			if (m is DSharpRole role)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.DeleteOverwriteAsync(role.Wrapped));
				}
				catch (Bot.Api.UnauthorizedException)
				{ }

			}
		}
		
		public async Task<IMessage> SendMessageAsync(string msg)
		{
			var messageRet = await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.SendMessageAsync(msg));
			return new DSharpMessage(messageRet);
		}

		public async Task<IMessage> SendMessageAsync(IEmbed e)
        {
			if(e is not DSharpEmbed emb) throw new InvalidOperationException("Expected an embed that works with DSharp");            
			var messageRet = await Wrapped.SendMessageAsync(emb.Wrapped);
			return new DSharpMessage(messageRet);
        }

        public async Task<IMessage> SendMessageAsync(IMessageBuilder b)
        {
			if(b is not DSharpMessageBuilder builder) throw new InvalidOperationException("Expected a MessageBuilder that works with DSharp");
			var messageRet = await Wrapped.SendMessageAsync(builder.Wrapped);
			return new DSharpMessage(messageRet);
        }
    }
}
