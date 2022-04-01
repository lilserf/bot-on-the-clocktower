using Bot.Api;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace Bot.DSharp
{
    class DSharpLookupSlashCommands : ApplicationCommandModule
    {
        public IBotLookupService? BotLookup { get; set; }

        [SlashCommand("lookup", "Look up a character")]
        public Task LookupCommand(InteractionContext ctx,
            [Option("lookupString", "String you want to look up")]string lookupString)
        {
            return BotLookup!.LookupAsync(new DSharpInteractionContext(ctx), lookupString);
        }

        [SlashCommand("addScript", "Add a script json URL for later lookup")]
        public Task AddScriptCommand(InteractionContext ctx,
            [Option("scriptJsonUrl", "URL pointing at a json file for the script")]string scriptJsonUrl)
        {
            return BotLookup!.AddScriptAsync(new DSharpInteractionContext(ctx), scriptJsonUrl);
        }

        [SlashCommand("removeScript", "Remove script json URL previously added")]
        public Task RemoveScriptCommand(InteractionContext ctx,
            [Option("scriptJsonUrl", "URL pointing at a json file for the script")]string scriptJsonUrl)
        {
            return BotLookup!.RemoveScriptAsync(new DSharpInteractionContext(ctx), scriptJsonUrl);
        }

        [SlashCommand("listScripts", "List script json URLs added to this server")]
        public Task ListScriptsCommand(InteractionContext ctx)
        {
            return BotLookup!.ListScriptsAsync(new DSharpInteractionContext(ctx));
        }
    }
}
