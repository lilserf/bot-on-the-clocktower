using Bot.Api;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    class RoleHelper
    {
        public static async Task<IRole?> GetOrCreateRole(IGuild guild, string name, Color color)
        {
            var role = guild.Roles.Where(x => x.Value.Name == name).Select(x => x.Value).FirstOrDefault();

            if(role == null)
            {
                role = await guild.CreateRoleAsync(name, color);
            }

            return role;
        }
    }
}
