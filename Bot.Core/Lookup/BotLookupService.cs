using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class BotLookupService : IBotLookupService
    {
        private readonly IGuildInteractionWrapper m_interactionWrapper;
        private readonly ILookupRoleDatabase m_lookupDb;
        private readonly ICharacterLookup m_characterLookup;
        private readonly ILookupEmbedBuilder m_lookupEmbedBuilder;

        public BotLookupService(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_interactionWrapper);
            serviceProvider.Inject(out m_lookupDb);
            serviceProvider.Inject(out m_characterLookup);
            serviceProvider.Inject(out m_lookupEmbedBuilder);
        }

        public Task LookupAsync(IBotInteractionContext ctx, string lookupString) => m_interactionWrapper.WrapInteractionAsync($"Looking up \"{lookupString}\"...", ctx, l => PerformLookupAsync(l, ctx, lookupString));
        public Task AddScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl) => m_interactionWrapper.WrapInteractionAsync($"Adding script at \"{scriptJsonUrl}\"...", ctx, l => PerformAddScriptAsync(l, ctx, scriptJsonUrl));
        public Task RemoveScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl) => m_interactionWrapper.WrapInteractionAsync($"Removing script at \"{scriptJsonUrl}\"...", ctx, l => PerformRemoveScriptAsync(l, ctx, scriptJsonUrl));
        public Task ListScriptsAsync(IBotInteractionContext ctx) => m_interactionWrapper.WrapInteractionAsync($"Finding registered scripts...", ctx, l => PerformListScriptsAsync(l, ctx));
        public Task RefreshScriptsAsync(IBotInteractionContext ctx) => m_interactionWrapper.WrapInteractionAsync($"Refreshing scripts...", ctx, l => PerformRefreshScriptsAsync(l, ctx));

        private async Task<InteractionResult> PerformLookupAsync(IProcessLogger _, IBotInteractionContext ctx, string lookupString)
        {
            var lookupResult = await m_characterLookup.LookupCharacterAsync(ctx.Guild.Id, lookupString);

            if (lookupResult.Items.Count == 0)
                return $"Found no results for \"{lookupString}\"";
            else
            {
                string message = $"Found {lookupResult.Items.Count} result{(lookupResult.Items.Count > 1 ? "s" : "")} for \"{lookupString}\"";
                var embeds = lookupResult.Items.Select(m_lookupEmbedBuilder.BuildLookupEmbed).ToArray();
                return InteractionResult.FromMessageAndEmbeds(message, embeds);
            }
        }

        private async Task<InteractionResult> PerformAddScriptAsync(IProcessLogger _, IBotInteractionContext ctx, string scriptJsonUrl)
        {
            await m_lookupDb.AddScriptUrlAsync(ctx.Guild.Id, scriptJsonUrl);
            return $"Script \"{scriptJsonUrl}\" added to lookups for this server.";
        }

        private async Task<InteractionResult> PerformRemoveScriptAsync(IProcessLogger _, IBotInteractionContext ctx, string scriptJsonUrl)
        {
            await m_lookupDb.RemoveScriptUrlAsync(ctx.Guild.Id, scriptJsonUrl);
            return $"Script \"{scriptJsonUrl}\" removed from lookups for this server.";
        }

        private async Task<InteractionResult> PerformListScriptsAsync(IProcessLogger _, IBotInteractionContext ctx)
        {
            var scripts = await m_lookupDb.GetScriptUrlsAsync(ctx.Guild.Id);
            if (scripts.Count == 0)
                return "No custom scripts have been added to this server.";

            var sb = new StringBuilder();
            sb.AppendLine("The following custom scripts were found for this server:");
            foreach (var script in scripts)
                sb.AppendLine($"⦁ {script}");
            return sb.ToString();
        }

        private async Task<InteractionResult> PerformRefreshScriptsAsync(IProcessLogger _, IBotInteractionContext ctx)
        {
            await m_characterLookup.RefreshCharactersAsync(ctx.Guild.Id);
            return "Scripts have been refreshed.";
        }
    }
}
