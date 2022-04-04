using Bot.Api;
using Bot.Api.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public class BotLookupService : IBotLookupService
    {
        private readonly IGuildInteractionQueue m_interactionQueue;
        private readonly IGuildInteractionErrorHandler m_errorHandler;
        private readonly ILookupRoleDatabase m_lookupDb;
        private readonly ICharacterLookup m_characterLookup;
        private readonly ILookupMessageSender m_messageSender;

        public BotLookupService(IServiceProvider serviceProvider)
        {
            serviceProvider.Inject(out m_interactionQueue);
            serviceProvider.Inject(out m_errorHandler);
            serviceProvider.Inject(out m_lookupDb);
            serviceProvider.Inject(out m_characterLookup);
            serviceProvider.Inject(out m_messageSender);
        }

        public Task LookupAsync(IBotInteractionContext ctx, string lookupString) => m_interactionQueue.QueueInteractionAsync($"Looking up \"{lookupString}\"...", ctx, () => PerformLookupAsync(ctx, lookupString));
        public Task AddScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl) => m_interactionQueue.QueueInteractionAsync($"Adding script at \"{scriptJsonUrl}\"...", ctx, () => PerformAddScriptAsync(ctx, scriptJsonUrl));
        public Task RemoveScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl) => m_interactionQueue.QueueInteractionAsync($"Removing script at \"{scriptJsonUrl}\"...", ctx, () => PerformRemoveScriptAsync(ctx, scriptJsonUrl));
        public Task ListScriptsAsync(IBotInteractionContext ctx) => m_interactionQueue.QueueInteractionAsync($"Finding registered scripts...", ctx, () => PerformListScriptsAsync(ctx));

        private async Task<InteractionResult> PerformLookupAsync(IBotInteractionContext ctx, string lookupString)
        {
            var result = await m_errorHandler.TryProcessReportingErrorsAsync(ctx.Guild.Id, ctx.Member, async l =>
            {
                var lookupResult = await m_characterLookup.LookupCharacterAsync(ctx.Guild.Id, lookupString);

                if (lookupResult.Items.Count == 0)
                    return $"Found no results for \"{lookupString}\"";
                else
                {
                    var prefix = $"Found {lookupResult.Items.Count} result{(lookupResult.Items.Count > 1 ? "s" : "")} for \"{lookupString}\"";
                    bool success = await TrySendLookupMessagesToChannel(l, ctx.Channel, lookupResult.Items);
                    if (success)
                        return $"{prefix}.";
                    return $"{prefix}, but am unable to post about them!";
                }
            });
            return result;
        }

        private async Task<bool> TrySendLookupMessagesToChannel(IProcessLogger logger, IChannel channel, IEnumerable<LookupCharacterItem> items)
        {
            bool success = false;
            try
            {
                await SendLookupMessagesToChannel(channel, items);
                success = true;
            }
            catch (UnauthorizedException unauthorizedEx)
            {
                logger.LogException(unauthorizedEx, $"send messages to \"{channel.Name}\"");
            }
            catch (Exception)
            {
                throw;
            }
            return success;
        }

        private async Task SendLookupMessagesToChannel(IChannel channel, IEnumerable<LookupCharacterItem> items)
        {
            foreach (var i in items)
                await m_messageSender.SendLookupMessageAsync(channel, i);
        }

        private async Task<InteractionResult> PerformAddScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl)
        {
            var result = await m_errorHandler.TryProcessReportingErrorsAsync(ctx.Guild.Id, ctx.Member, async l =>
            {
                await m_lookupDb.AddScriptUrlAsync(ctx.Guild.Id, scriptJsonUrl);
                return $"Script \"{scriptJsonUrl}\" added to lookups for this server.";
            });
            return result;
        }

        private async Task<InteractionResult> PerformRemoveScriptAsync(IBotInteractionContext ctx, string scriptJsonUrl)
        {
            var result = await m_errorHandler.TryProcessReportingErrorsAsync(ctx.Guild.Id, ctx.Member, async l =>
            {
                await m_lookupDb.RemoveScriptUrlAsync(ctx.Guild.Id, scriptJsonUrl);
                return $"Script \"{scriptJsonUrl}\" removed from lookups for this server.";
            });
            return result;
        }

        private async Task<InteractionResult> PerformListScriptsAsync(IBotInteractionContext ctx)
        {
            var result = await m_errorHandler.TryProcessReportingErrorsAsync(ctx.Guild.Id, ctx.Member, async l =>
            {
                var scripts = await m_lookupDb.GetScriptUrlsAsync(ctx.Guild.Id);
                if (scripts.Count == 0)
                    return "No custom scripts have been added to this server.";

                var sb = new StringBuilder();
                sb.AppendLine("The following custom scripts were found for this server:");
                foreach (var script in scripts)
                    sb.AppendLine($"⦁ {script}");
                return sb.ToString();
            });
            return result;
        }
    }
}
