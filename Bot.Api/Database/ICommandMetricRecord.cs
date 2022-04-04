using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface ICommandMetricRecord
    {
        DateTime Day { get; }

        Dictionary<string, int> Commands { get; }
    }
}
