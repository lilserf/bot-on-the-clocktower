using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class VersionProvider : IVersionProvider
    {
        private Dictionary<Version, IEmbed> m_versions;
        public Dictionary<Version, IEmbed> Versions => m_versions;

        private IBotSystem m_botSystem;
        private IColorBuilder m_colorBuilder;

        public VersionProvider(IServiceProvider sp)
        {
            sp.Inject(out m_botSystem);
            sp.Inject(out m_colorBuilder);

            m_versions = new Dictionary<Version, IEmbed>();

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
