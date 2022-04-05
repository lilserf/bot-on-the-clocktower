using System;
using System.Collections.Generic;

namespace Bot.Api
{
    public interface IVersionProvider
    {
        Dictionary<Version, IMessageBuilder> Versions { get; }
        void InitializeVersions();
    }
}
