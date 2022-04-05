namespace Bot.Core.Interaction
{
    public class GuildInteractionErrorHandler : BaseInteractionErrorHandler<ulong>, IGuildInteractionErrorHandler
    {
        protected override string GetFriendlyStringForKey(ulong key) => $"Guild: `{key}`";
    }
}
