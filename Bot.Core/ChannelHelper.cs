using Bot.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
    class ChannelHelper
    {

        public static async Task<IChannelCategory?> GetOrCreateCategory(IGuild guild, string name)
        {
            var cat = guild.ChannelCategories.Where(x => x.Name == name).FirstOrDefault();

            if(cat == null)
            {
                cat = await guild.CreateCategoryAsync(name);
            }

            return cat;
        }

        public static async Task<IChannel?> GetOrCreateVoiceChannel(IGuild guild, IChannelCategory parent, string name)
        {
            var chan = parent.Channels.Where(x => x.Name == name).FirstOrDefault();

            if(chan == null)
            {
                chan = await guild.CreateVoiceChannelAsync(name, parent);
            }

            return chan;
        }

        static string MakeTextChannelName(string inName)
        {
            return inName.ToLower().Replace(' ', '-');
        }

        public static async Task<IChannel?> GetOrCreateTextChannel(IGuild guild, IChannelCategory parent, string name)
        {
            var textName = MakeTextChannelName(name);
            var chan = parent.Channels.Where(x => x.Name == textName).FirstOrDefault();

            if(chan == null)
            {
                chan = await guild.CreateTextChannelAsync(name, parent);
            }

            return chan;
        }
    }
}
