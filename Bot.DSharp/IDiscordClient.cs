using Bot.Api;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Emzi0767.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public interface IDiscordClient
    {
        SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config);

        event AsyncEventHandler<IDiscordClient, ReadyEventArgs> Ready;
        event AsyncEventHandler<IDiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated;
        event AsyncEventHandler<IDiscordClient, ModalSubmitEventArgs> ModalSubmitted;

        Task<IChannel?> GetChannelAsync(ulong id);
        Task<IChannelCategory?> GetChannelCategoryAsync(ulong id);
        Task<IGuild?> GetGuildAsync(ulong id);
        Task ConnectAsync();
    }

    public class DiscordClientWrapper : DiscordWrapper<DiscordClient>, IDiscordClient
    {
        private readonly AsyncEventWrapper<ReadyEventArgs> mReadyWrapper;
        private readonly AsyncEventWrapper<ComponentInteractionCreateEventArgs> mComponentInteractionCreatedWrapper;
        private readonly AsyncEventWrapper<ModalSubmitEventArgs> mModalSubmitWrapper;

        public DiscordClientWrapper(DiscordClient wrapped)
            : base(wrapped)
        {
            mReadyWrapper = new AsyncEventWrapper<ReadyEventArgs>(this, f => Wrapped.Ready += f, f => Wrapped.Ready -= f);
            mComponentInteractionCreatedWrapper = new AsyncEventWrapper<ComponentInteractionCreateEventArgs>(this, f => Wrapped.ComponentInteractionCreated += f, f => Wrapped.ComponentInteractionCreated -= f);
            mModalSubmitWrapper = new AsyncEventWrapper<ModalSubmitEventArgs>(this, f => Wrapped.ModalSubmitted += f, f => Wrapped.ModalSubmitted -= f);
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

        public event AsyncEventHandler<IDiscordClient, ModalSubmitEventArgs> ModalSubmitted
        {
            add { mModalSubmitWrapper.Event += value; }
            remove {  mModalSubmitWrapper.Event -= value;}
        }

        public SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config) => Wrapped.UseSlashCommands(config);
        public async Task<IChannel?> GetChannelAsync(ulong id) => new DSharpChannel(await Wrapped.GetChannelAsync(id));
        public async Task<IChannelCategory?> GetChannelCategoryAsync(ulong id) => new DSharpChannelCategory(await Wrapped.GetChannelAsync(id));
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
}
