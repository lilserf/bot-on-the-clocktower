﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    internal interface IGameMetricRecord
    {
        ulong TownHash { get; }

        DateTime ActivityStart { get; }
        DateTime ActivityEnd { get; }

    }
}
