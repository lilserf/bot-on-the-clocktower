using Bot.Api;
using System;
using System.Collections.Generic;

namespace Bot.Core
{
    public class VersionProvider : IVersionProvider
    {
        private Dictionary<Version, IEmbed> m_versions = new();
        public Dictionary<Version, IEmbed> Versions => m_versions;

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
                IEmbedBuilder eb = m_botSystem.CreateEmbedBuilder();
                eb.WithColor(m_colorBuilder.DarkRed);
                eb.AddField("Slash Commands", "All major features are now accessible via Slash Commands!");
                m_versions.Add(new Version(3, 0, 0), eb.Build());
            }
        }
    }
}
