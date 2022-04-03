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
        private Dictionary<Version, string> m_versions = new Dictionary<Version, string>
        {
            { new Version(3,0,0), "Version 3!" },
        };

        public Dictionary<Version, string> Versions => m_versions;

    }
}
