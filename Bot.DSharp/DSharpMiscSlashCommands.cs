using Bot.Api;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    internal class DSharpMiscSlashCommands : ApplicationCommandModule
    {
        public IAnnouncer? Announcer { get; set; }

        [SlashCommand("announce", "Set whether this server wants to hear new version announcements")]
        public async Task AnnounceCommand(InteractionContext ctx, 
            [Option("hearAnnouncements", "If true, this server will hear new version announcements")] bool hear)
        {
            await Announcer!.CommandSetGuildAnnounce(new DSharpInteractionContext(ctx), hear);
        }
    }
}
