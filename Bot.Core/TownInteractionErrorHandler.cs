namespace Bot.Core
{
    public class TownInteractionErrorHandler : BaseInteractionErrorHandler<ulong>, ITownInteractionErrorHandler
    {
        protected override string GetFriendlyStringForKey(ulong key) => $"Guild: `{key}`";
    }
}
