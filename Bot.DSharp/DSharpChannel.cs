using Bot.Api;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

		public bool IsVoice => Wrapped.Type == ChannelType.Voice;
		public bool IsText => Wrapped.Type == ChannelType.Text;

		public string Name => Wrapped.Name;

        public async Task AddOverwriteAsync(IMember m, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
        {
			if (m is DSharpMember member)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.AddOverwriteAsync(member.Wrapped, DSharpPermissionHelper.DSharpPermissionsFromBasePermissions(allow), DSharpPermissionHelper.DSharpPermissionsFromBasePermissions(deny)));
				}
				catch (Bot.Api.UnauthorizedException)
				{ }
			}
        }

		public async Task AddOverwriteAsync(IRole r, IBaseChannel.Permissions allow, IBaseChannel.Permissions deny = IBaseChannel.Permissions.None)
		{
			if (r is DSharpRole role)
			{
				try
				{
					await ExceptionWrap.WrapExceptionsAsync(() => Wrapped.AddOverwriteAsync(role.Wrapped, DSharpPermissionHelper.DSharpPermissionsFromBasePermissions(allow), DSharpPermissionHelper.DSharpPermissionsFromBasePermissions(deny)));
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

		public Task RestrictOverwriteToMembersAsync(IReadOnlyCollection<IMember> memberPool, IBaseChannel.Permissions permission, params IMember[] allowedMembers)
		{
			var dSharpPerm = DSharpPermissionHelper.DSharpPermissionsFromBasePermissions(permission);

			var membersNeedGranting = allowedMembers.ToDictionary(m => m.Id, m => m);
			var overwritesToRemove = new HashSet<DiscordOverwrite>();
			var allIds = memberPool.Select(m => m.Id).ToHashSet();

			var relevantOverwrites = Wrapped.PermissionOverwrites.Where(o => o.Type == OverwriteType.Member && o.Allowed.HasFlag(dSharpPerm));

			foreach (var o in relevantOverwrites)
			{
				if (membersNeedGranting.ContainsKey(o.Id))
					membersNeedGranting.Remove(o.Id);
				else if (allIds.Contains(o.Id))
					overwritesToRemove.Add(o);
			}

			List<Task> tasks = new();
			foreach (var o in overwritesToRemove)
				tasks.Add(DeletePermissionOverwriteAsync(o));
			foreach (var m in membersNeedGranting.Values)
				tasks.Add(AddOverwriteAsync(m, permission));
			return Task.WhenAll(tasks);
		}

		private static async Task DeletePermissionOverwriteAsync(DiscordOverwrite overwrite)
		{
			try
			{
				await ExceptionWrap.WrapExceptionsAsync(() => overwrite.DeleteAsync());
			}
			catch (Bot.Api.UnauthorizedException)
			{ }
		}
	}
}
