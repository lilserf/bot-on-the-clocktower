﻿using Bot.Api;
using System.Diagnostics.CodeAnalysis;

namespace Bot.Core
{
    public interface IActiveGameService
	{
		bool TryGetGame(IBotInteractionContext context, [MaybeNullWhen(false)] out IGame game);
		bool RegisterGame(ITown town, IGame game);
	}
}