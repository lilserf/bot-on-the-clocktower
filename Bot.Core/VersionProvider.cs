using Bot.Api;
using System;
using System.Collections.Generic;

namespace Bot.Core
{
    public class VersionProvider : IVersionProvider
    {
        private Dictionary<Version, IMessageBuilder> m_versions = new();
        public Dictionary<Version, IMessageBuilder> Versions => m_versions;

        private IBotSystem m_botSystem;
        private IColorBuilder m_colorBuilder;

        public VersionProvider(IServiceProvider sp)
        {
            sp.Inject(out m_botSystem);
            sp.Inject(out m_colorBuilder);
        }

        public void InitializeVersions()
        {
            m_versions.Clear();

            // VERSION 3.0.0
            {
                IMessageBuilder msg = m_botSystem.CreateMessageBuilder();
                string title = @"**New version of Bot on the Clocktower!**
Due to upcoming changes to Discord (and our own areas of expertise) this bot has been completely rewritten.
Major things to be aware of are called out below.";
                msg.WithContent(title);

                {
                    IEmbedBuilder eb = m_botSystem.CreateEmbedBuilder();
                    eb.WithColor(m_colorBuilder.Blue);
                    string message =
                        @"This bot now has slash command equivalents for all the old !commands.
These are extremely powerful, with nifty autocompletion, documentation, and interactive buttons.
**TIP**: Try the `/game` command for a super-easy game management flow!";
                    eb.AddField("Slash Commands", message);
                    msg.AddEmbed(eb.Build());
                }
                {
                    IEmbedBuilder eb = m_botSystem.CreateEmbedBuilder();
                    eb.WithColor(m_colorBuilder.DarkBlue);
                    string message =
                        @"In the future (currently slated for August), Discord will _require_ the use of Slash Commands to interact with bots.
In most cases the slash commands are actually more cool and powerful than the old commands, so you're highly encouraged to learn them sooner rather than later.";
                    eb.AddField("Old !commands will go away eventually", message);
                    msg.AddEmbed(eb.Build());
                }

                m_versions.Add(new Version(3, 0, 0), msg);
            }

        }
    }
}
