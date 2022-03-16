using Bot.Api;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Emzi0767.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public enum ChannelType
    {
        Text,
        Voice,
    }

    public interface IDiscordClient
    {
        SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config);

        event AsyncEventHandler<IDiscordClient, ReadyEventArgs> Ready;
        event AsyncEventHandler<IDiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated;

        Task<GetChannelResult> GetChannelAsync(ulong id, string? name, ChannelType type);
        Task<GetChannelCategoryResult> GetChannelCategoryAsync(ulong id, string? name);
        Task<IGuild?> GetGuildAsync(ulong id);
        Task ConnectAsync();
    }

    public class DiscordClientWrapper : DiscordWrapper<DiscordClient>, IDiscordClient
    {
        private readonly AsyncEventWrapper<ReadyEventArgs> mReadyWrapper;
        private readonly AsyncEventWrapper<ComponentInteractionCreateEventArgs> mComponentInteractionCreatedWrapper;

        public DiscordClientWrapper(DiscordClient wrapped)
            : base(wrapped)
        {
            mReadyWrapper = new AsyncEventWrapper<ReadyEventArgs>(this, f => Wrapped.Ready += f, f => Wrapped.Ready -= f);
            mComponentInteractionCreatedWrapper = new AsyncEventWrapper<ComponentInteractionCreateEventArgs>(this, f => Wrapped.ComponentInteractionCreated += f, f => Wrapped.ComponentInteractionCreated -= f);
        }


        public event AsyncEventHandler<IDiscordClient, ReadyEventArgs> Ready
        {
            add { mReadyWrapper.Event += value; }
            remove { mReadyWrapper.Event -= value; }
        }

        public event AsyncEventHandler<IDiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated
        {
            add { mComponentInteractionCreatedWrapper.Event += value; }
            remove { mComponentInteractionCreatedWrapper.Event -= value; }
        }

        public SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config) => Wrapped.UseSlashCommands(config);

        public async Task<GetChannelResult> GetChannelAsync(ulong id, string? name, ChannelType type)
        {
            var channel = await Wrapped.GetChannelAsync(id);
            return new GetChannelResult(channel != null ? new DSharpChannel(channel) : null, ChannelUpdateRequired.None);
        }

        public async Task<GetChannelCategoryResult> GetChannelCategoryAsync(ulong id, string? name)
        {
            var channel = await Wrapped.GetChannelAsync(id);
            return new GetChannelCategoryResult(channel != null ? new DSharpChannelCategory(channel) : null, ChannelUpdateRequired.None);
        }

        public async Task<IGuild?> GetGuildAsync(ulong id) => new DSharpGuild(await Wrapped.GetGuildAsync(id));
        public Task ConnectAsync() => Wrapped.ConnectAsync();

        private class AsyncEventWrapper<T> where T : DiscordEventArgs
        {
            private readonly DiscordClientWrapper mClientWrapper;
            private readonly Action<AsyncEventHandler<DiscordClient, T>> mAttach;
            private readonly Action<AsyncEventHandler<DiscordClient, T>> mDetach;

            public AsyncEventWrapper(DiscordClientWrapper clientWrapper, Action<AsyncEventHandler<DiscordClient, T>> attach, Action<AsyncEventHandler<DiscordClient, T>> detach)
            {
                mClientWrapper = clientWrapper;
                mAttach = attach;
                mDetach = detach;
            }

            private readonly List<AsyncEventHandler<IDiscordClient, T>> mEventHandlers = new();

            public event AsyncEventHandler<IDiscordClient, T> Event
            {
                add
                {
                    lock (mEventHandlers)
                    {
                        mEventHandlers.Add(value);
                        if (mEventHandlers.Count > 0)
                        {
                            mAttach(Wrapped_Handler);
                        }
                    }
                }

                remove
                {
                    lock (mEventHandlers)
                    {
                        mEventHandlers.Remove(value);
                        if (mEventHandlers.Count == 0)
                        {
                            mDetach(Wrapped_Handler);
                        }
                    }
                }
            }


            private Task Wrapped_Handler(DiscordClient sender, T e)
            {
                if (!Equals(sender, mClientWrapper.Wrapped)) throw new InvalidCastException("Unexpected DiscordClient returned from event handler");

                AsyncEventHandler<IDiscordClient, T>[] ehCopy;
                lock (mEventHandlers)
                {
                    ehCopy = mEventHandlers.ToArray();
                }

                var tasks = new List<Task>(ehCopy.Length);
                foreach (var eh in ehCopy)
                    tasks.Add(eh.Invoke(mClientWrapper, e));
                return Task.WhenAll(tasks);
            }
        }
    }

    public enum ChannelUpdateRequired
    {
        None,
        Id,
        Name,
    }

    public class GetChannelResult : GetChannelResultBase<IChannel>
    {
        public GetChannelResult(IChannel? channel, ChannelUpdateRequired updateRequired)
            : base(channel, updateRequired)
        {}
    }
    public class GetChannelCategoryResult : GetChannelResultBase<IChannelCategory>
    {
        public GetChannelCategoryResult(IChannelCategory? channel, ChannelUpdateRequired updateRequired)
            : base(channel, updateRequired)
        {}
    }

    public class GetChannelResultBase<T> where T : class
    {
        public ChannelUpdateRequired UpdateRequired { get; }
        public T? Channel { get; }

        public GetChannelResultBase(T? channel, ChannelUpdateRequired updateRequired)
        {
            UpdateRequired = updateRequired;
            Channel = channel;
        }
    }
}
