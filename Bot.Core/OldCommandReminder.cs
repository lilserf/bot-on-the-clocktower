using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    internal class OldCommandReminder : IOldCommandReminder
    {
        Dictionary<ulong, DateTime> m_lastReminder = new Dictionary<ulong, DateTime>();

        private IDateTime m_dateTime;

        private static string s_prefix = "!";

        private static List<string> s_commands = new List<string>()
        {
            "announce",
            "noannounce",
            "townInfo",
            "addTown",
            "removeTown",
            "setChatChannel",
            "createTown",
            "destroyTown",
            "endGame",
            "setStorytellers",
            "storytellers",
            "sts",
            "setsts",
            "setst",
            "currGame",
            "curGame",
            "lunatic",
            "evil",
            "night",
            "day",
            "vote",
            "voteTimer",
            "vt",
            "stopVoteTimer",
            "svt",
            "character",
            "role",
            "char",
            "addScript",
            "removeScript",
            "refreshScripts",
            "listScripts",

        };

        public OldCommandReminder(IServiceProvider sp)
        {
            sp.Inject(out m_dateTime);
        }

        private void Cleanup()
        {
            var filtered = m_lastReminder.Where(x => x.Value.AddHours(1) > m_dateTime.Now);
            m_lastReminder = new(filtered);
        }

        private string GetMessage(string input)
        {
            // TODO: custom messages
            return "Discord is moving to require Slash Commands instead of messages to control bots. Please try using the corresponding slash command instead, as these `!` commands will eventually stop working.";
        }

        public async Task UserMessageCreated(string message, IChannel channel)
        {
            var first = message.Split(" ").FirstOrDefault() ?? "";

            if (first.StartsWith(s_prefix))
            {
                string keyword = first.Substring(s_prefix.Length);
                if (s_commands.Contains(keyword))
                {
                    bool nag = true;
                    if (m_lastReminder.ContainsKey(channel.Id))
                    {
                        var lastReminder = m_lastReminder[channel.Id];
                        if (lastReminder.AddHours(1) > m_dateTime.Now)
                        {
                            nag = false;
                        }
                    }

                    if (nag)
                    {
                        m_lastReminder[channel.Id] = m_dateTime.Now;
                        await channel.SendMessageAsync(GetMessage(keyword));
                    }
                    Cleanup();
                }
            }
        }
    }
}
