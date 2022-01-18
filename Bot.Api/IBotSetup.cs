using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api.Database
{
    public interface IBotSetup
    {
        public Task AddTown(ITown town, IMember author);

        public Task CreateTown(TownDescription townDesc);
    }
}
