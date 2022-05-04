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
                string title = @"**Version 3.0.0 of Bot on the Clocktower!**
Due to upcoming changes to Discord (and our own areas of expertise) this bot has been completely rewritten.
Major things to be aware of are called out below.";
                msg.WithContent(title);

                {
                    IEmbedBuilder eb = m_botSystem.CreateEmbedBuilder();
                    eb.WithColor(m_colorBuilder.Red);
                    string message =
                        @"You should re-invite this bot to your server to ensure all permissions are set correctly.
You can do this by going to Bot on the Clocktower's profile and choosing **Add to Server**.";
                    eb.AddField("Alert!", message);
                    msg.AddEmbed(eb.Build());
                }

                {
                    IEmbedBuilder eb = m_botSystem.CreateEmbedBuilder();
                    eb.WithColor(m_colorBuilder.Blue);
                    eb.WithImageUrl("https://user-images.githubusercontent.com/151635/162874601-a94936c7-de43-4c0b-ad08-6089f67f6dc3.png");
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
In most cases the slash commands are actually more cool and powerful than the old commands, so you're highly encouraged to learn them sooner rather than later.

You may have seen the bot telling you this for a few weeks, even though the slash commands weren't active yet. Sorry about that.";
                    eb.AddField("Old !commands will go away eventually", message);
                    msg.AddEmbed(eb.Build());
                }

                m_versions.Add(new Version(3, 0, 0), msg);
            }

        }
    }
}
