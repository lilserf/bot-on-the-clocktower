using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Emzi0767.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    public interface IDiscordClient
    {
        SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config);

        event AsyncEventHandler<IDiscordClient, ReadyEventArgs> Ready;
        event AsyncEventHandler<IDiscordClient, ComponentInteractionCreateEventArgs> ComponentInteractionCreated;

        Task<DiscordChannel> GetChannelAsync(ulong id);
        Task<DiscordGuild> GetGuildAsync(ulong id);
        Task ConnectAsync();
    }

    public class DiscordClientWrapper : IDiscordClient
    {
        private readonly DiscordClient mWrapped;

        private readonly AsyncEventWrapper<ReadyEventArgs> mReadyWrapper;
        private readonly AsyncEventWrapper<ComponentInteractionCreateEventArgs> mComponentInteractionCreatedWrapper;

        public DiscordClientWrapper(DiscordClient wrapped)
        {
            mWrapped = wrapped;

            mReadyWrapper = new AsyncEventWrapper<ReadyEventArgs>(this, f => mWrapped.Ready += f, f => mWrapped.Ready -= f);
            mComponentInteractionCreatedWrapper = new AsyncEventWrapper<ComponentInteractionCreateEventArgs>(this, f => mWrapped.ComponentInteractionCreated += f, f => mWrapped.ComponentInteractionCreated -= f);
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

        public SlashCommandsExtension UseSlashCommands(SlashCommandsConfiguration config) => mWrapped.UseSlashCommands(config);
        public Task<DiscordChannel> GetChannelAsync(ulong id) => mWrapped.GetChannelAsync(id);
        public Task<DiscordGuild> GetGuildAsync(ulong id) => mWrapped.GetGuildAsync(id);
        public Task ConnectAsync() => mWrapped.ConnectAsync();



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
                if (!Equals(sender, mClientWrapper.mWrapped)) throw new InvalidCastException("Unexpected DiscordClient returned from event handler");

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
