using Bot.Api;
using System.Diagnostics.CodeAnalysis;

namespace Bot.Core
{
    public interface IActiveGameService
	{
		bool TryGetGame(TownKey townKey, [MaybeNullWhen(false)] out IGame game);
		bool RegisterGame(ITown town, IGame game);
		bool EndGame(ITown town);
	}

	public static class IActiveGameServiceExtensions
	{
		public static bool TryGetGame(this IActiveGameService @this, IBotInteractionContext context, [MaybeNullWhen(false)] out IGame game) => @this.TryGetGame(new TownKey(context.Guild.Id, context.Channel.Id), out game);
		public static bool TryGetGame(this IActiveGameService @this, ulong guildId, ulong channelId, [MaybeNullWhen(false)] out IGame game) => @this.TryGetGame(new TownKey(guildId, channelId), out game);

	}
}
