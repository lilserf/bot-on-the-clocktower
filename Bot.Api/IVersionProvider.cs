using System;
using System.Collections.Generic;

namespace Bot.Api
{
    public interface IVersionProvider
    {
        Dictionary<Version, IEmbed> Versions { get; }
        void InitializeVersions();
    }
}
