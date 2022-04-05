using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core
{
    public class LegacyCommandReminder : ILegacyCommandReminder
    {
        private readonly IDateTime m_dateTime;
        private readonly IBotSystem m_botSystem;
        private readonly IBotClient m_botClient;

        private readonly Dictionary<ulong, DateTime> m_lastReminder = new();

        private readonly static string s_prefix = "!";

        private readonly static List<LegacyCommandUpdate> s_commands = new()
        {
            new("announce", "announce"),
            new("noAnnounce", "announce"),
            LegacyCommandUpdate.UnimplementedCommand("townInfo"),
            LegacyCommandUpdate.UnimplementedCommand("addTown"),
            LegacyCommandUpdate.UnimplementedCommand("removeTown"),
            LegacyCommandUpdate.UnimplementedCommand("setChatChannel"),
            "createTown",
            LegacyCommandUpdate.UnimplementedCommand("destroyTown"),
            LegacyCommandUpdate.GameCommand("endGame"),
            new("setStorytellers", "storytellers"),
            "storytellers",
            new("sts", "storytellers"),
            new("setsts", "storytellers"),
            new("setst", "storytellers"),
            new("currGame", "game"),
            new("curGame", "game"),
            "lunatic",
            "evil",
            LegacyCommandUpdate.GameCommand("night"),
            LegacyCommandUpdate.GameCommand("day"),
            LegacyCommandUpdate.GameCommand("vote"),
            LegacyCommandUpdate.GameCommand("voteTimer"),
            LegacyCommandUpdate.GameCommand("vt", "voteTimer"),
            "stopVoteTimer",
            new("svt", "stopVoteTimer"),
            new("character", "lookup"),
            new("role", "lookup"),
            new("char", "lookup"),
            "addScript",
            "removeScript",
            LegacyCommandUpdate.UnimplementedCommand("refreshScripts"),
            "listScripts",
        };

        private readonly static Dictionary<string, LegacyCommandUpdate> s_commandMap = new();

        static LegacyCommandReminder()
        {
            foreach(var command in s_commands)
                s_commandMap.Add(command.LegacyCommandString.ToLowerInvariant(), command);
        }

        public LegacyCommandReminder(IServiceProvider sp)
        {
            sp.Inject(out m_dateTime);
            sp.Inject(out m_botSystem);
            sp.Inject(out m_botClient);

            m_botClient.MessageCreated += ClientMessageCreated;
        }

        private void Cleanup()
        {
            var now = m_dateTime.Now;
            var filtered = m_lastReminder.Where(x => x.Value.AddHours(1) > now);
            m_lastReminder.Clear();
            foreach (var item in filtered)
                m_lastReminder.Add(item.Key, item.Value);
        }

        private static string GetMessage(LegacyCommandUpdate update)
        {
            if (!update.IsImplemented)
                return $"Discord will eventually require using `/` commands (such as `/{update.ModernCommandString}`).\n\nUnfortunately, Bot on the Clocktower has not yet implemented an equivalent `/` command for `!{update.LegacyCommandString}`. Stay tuned!";

            var gameSuffix = update.IsGameCommand ? "\n\nAlternatively, try out the new `/game` command for an updated experience!" : "";
            return $"Please use `/{update.ModernCommandString}` instead of `!{update.LegacyCommandString}`.{gameSuffix}\n\nDiscord will eventually require using `/` instead, and `!` commands will stop working.";
        }

        private void ClientMessageCreated(object? sender, MessageCreatedEventArgs e)
        {
            var first = e.Message.Split(" ").FirstOrDefault() ?? "";

            if (first.StartsWith(s_prefix))
            {
                string keyword = first.Substring(s_prefix.Length).ToLowerInvariant();
                if (s_commandMap.TryGetValue(keyword, out var update))
                {
                    bool nag = true;
                    var now = m_dateTime.Now;

                    if (m_lastReminder.ContainsKey(e.Channel.Id))
                    {
                        var lastReminder = m_lastReminder[e.Channel.Id];
                        if (lastReminder.AddHours(1) > now)
                        {
                            nag = false;
                        }
                    }

                    if (nag)
                    {
                        m_lastReminder[e.Channel.Id] = now;

                        var eb = m_botSystem.CreateEmbedBuilder();
                        eb.WithColor(m_botSystem.ColorBuilder.DarkRed);
                        eb.WithTitle("Warning!");
                        eb.WithDescription(GetMessage(update));
                        try
                        {
                            SendMessageToChannel(e.Channel, eb.Build());
                        }
                        catch (Exception)
                        { }
                    }
                    Cleanup();
                }
            }
        }

        private static void SendMessageToChannel(IChannel channel, IEmbed embed)
        {
            channel.SendMessageAsync(embed).ConfigureAwait(continueOnCapturedContext: true);
        }

        private class LegacyCommandUpdate
        {
            public string LegacyCommandString { get; }
            public string ModernCommandString { get; }
            public bool IsGameCommand { get; private set; } = false;
            public bool IsImplemented { get; private set; } = true;

            public LegacyCommandUpdate(string legacyCommandString, string? modernCommandString=null)
            {
                LegacyCommandString = legacyCommandString;
                ModernCommandString = modernCommandString ?? legacyCommandString;
            }

            public static implicit operator LegacyCommandUpdate(string legacyCommandString) => new(legacyCommandString);

            public static LegacyCommandUpdate GameCommand(string legacyCommandString, string? modernCommandString = null)
            {
                var ret = new LegacyCommandUpdate(legacyCommandString, modernCommandString);
                ret.IsGameCommand = true;
                return ret;
            }

            // Can't figure out a good way to do a warning if a method is called other than Obsolete
            // https://stackoverflow.com/questions/154109/custom-compiler-warnings
            [Obsolete("Anything calling this needs attention before release.")]
            public static LegacyCommandUpdate UnimplementedCommand(string legacyCommandString)
            {
                var ret = new LegacyCommandUpdate(legacyCommandString);
                ret.IsImplemented = false;
                return ret;
            }
        }
    }
}
