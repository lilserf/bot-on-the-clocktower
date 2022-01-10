using Bot.Api;
using Bot.Core.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : BotTownLookupHelper, IVoteHandler
    {
        private readonly IActiveGameService m_activeGameService;
        private readonly IShuffleService m_shuffle;
        private readonly ICallbackScheduler<TownKey> m_callbackScheduler;
        private readonly IDateTime m_dateTime;

		public BotGameplay(IServiceProvider services)
            : base(services)
		{
            m_activeGameService = services.GetService<IActiveGameService>();
            m_shuffle = services.GetService<IShuffleService>();

            var callbackFactory = services.GetService<ICallbackSchedulerFactory>();
            m_callbackScheduler = callbackFactory.CreateScheduler<TownKey>(CheckForTownCleanup, TimeSpan.FromHours(1));

            m_dateTime = services.GetService<IDateTime>();
        }

        private Task CheckForTownCleanup(TownKey arg)
        {
            // TODO: get the database entry for this town and see if it's ready for cleanup
            return Task.CompletedTask;
        }

        public static bool CheckIsTownViable(ITown? town, IProcessLogger logger)
        {
            // Without these two, there's not much else to do
            if (town == null)
            {
                logger.LogMessage(InvalidTownMessage);
                return false;
            }
            if (town.TownRecord == null)
            {
                logger.LogMessage(InvalidTownMessage);
                return false;
            }

            // Run all these even if one fails
            bool success = true;
            if (town.DayCategory == null)
            {
                if (town.TownRecord.DayCategory != null)
                    logger.LogMessage($"Couldn't find Day Category '{town.TownRecord.DayCategory}'");
                else
                    logger.LogMessage($"Couldn't find a registered Day Category for this town! Consider re-creating the town with /createTown or /addTown.");
                success = false;
            }
            if(town.TownSquare == null)
            {
                if(town.TownRecord.TownSquare != null)
                    logger.LogMessage($"Couldn't find Town Square channel '{town.TownRecord.TownSquare}'");
                else
                    logger.LogMessage($"Couldn't find a registered Town Square for this town! Consider re-creating the town with /createTown or /addTown.");
                success = false;
            }
            if(town.StorytellerRole == null)
            {
                if(town.TownRecord.StorytellerRole != null)
                    logger.LogMessage($"Couldn't find Storyteller role '{town.TownRecord.StorytellerRole}'");
                else
                    logger.LogMessage($"Couldn't find a registered Storyteller role for this town! Consider re-creating the town with /createTown or /addTown.");
            }
            if (town.VillagerRole == null)
            {
                if (town.TownRecord.VillagerRole != null)
                    logger.LogMessage($"Couldn't find Storyteller role '{town.TownRecord.VillagerRole}'");
                else
                    logger.LogMessage($"Couldn't find a registered Villager role for this town! Consider re-creating the town with /createTown or /addTown.");
            }

            return success;
        }

        private async Task TagStorytellers(IGame game, IProcessLogger logger)
        {
            foreach (var u in game.Storytellers)
            {
                await MemberHelper.AddStorytellerTag(u, logger);
            }

            foreach(var u in game.Villagers)
            {
                await MemberHelper.RemoveStorytellerTag(u, logger);
            }
        }

        private async Task GrantAndRevokeRoles(IGame game, IProcessLogger logger)
        {
            IRole storytellerRole = game!.Town!.StorytellerRole!;
            IRole villagerRole = game!.Town!.VillagerRole!;

            foreach(var u in game.Storytellers)
            {
                if(!u.Roles.Contains(game.Town.StorytellerRole))
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, storytellerRole, logger);
                }
                if (!u.Roles.Contains(game.Town.VillagerRole))
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, villagerRole, logger);
                }
            }

            foreach (var u in game.Villagers)
            {
                if(u.Roles.Contains(game.Town.StorytellerRole))
                {
                    await MemberHelper.RevokeRoleLoggingErrorsAsync(u, storytellerRole, logger);
                }
                if(!u.Roles.Contains(game.Town.VillagerRole))
                {
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, villagerRole, logger);
                }
            }
        }

        // Schedule this town for a cleanup after a long time period
        public void ScheduleCleanup(TownKey townKey)
        {
            // TODO: store a record for this town for when to clean it up

            var time = m_dateTime.Now + TimeSpan.FromHours(5);
            m_callbackScheduler.ScheduleCallback(townKey, time);
        }

        // TODO: better name for this method, probably
        public async Task<IGame?> CurrentGameAsync(IBotInteractionContext context, IProcessLogger logger)
        {
            if (m_activeGameService.TryGetGame(context, out IGame? game))
            {
                if(!CheckIsTownViable(game.Town, logger))
                {
                    return null;
                }

                ScheduleCleanup(TownKey.FromTown(game.Town));

                //Resolve a change in Storytellers
                if (!game.Storytellers.Contains(context.Member))
                {
                    foreach (var user in game.Storytellers.ToList())
                    {
                        game.AddVillager(user);
                        game.RemoveStoryteller(user);
                    }
                    game.RemoveVillager(context.Member);
                    game.AddStoryteller(context.Member);
                }

                await TagStorytellers(game, logger);

                var foundUsers = game.Town!.TownSquare!.Users.ToList();

                foreach (var c in game.Town!.DayCategory!.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }

                if (game.Town.NightCategory != null)
                {
                    foreach (var c in game.Town.NightCategory.Channels.Where(c => c.IsVoice))
                    {
                        foundUsers.AddRange(c.Users.ToList());
                    }
                }

                // Sanity check for bots
                foundUsers = foundUsers.Where(u => !u.IsBot).ToList();

                var newPlayers = foundUsers.Except(game.AllPlayers);
                var oldPlayers = game.AllPlayers.Except(foundUsers);

                foreach (var p in newPlayers)
                {
                    game.AddVillager(p);
                }
                foreach (var p in oldPlayers)
                {
                    game.RemoveVillager(p);
                }

                await GrantAndRevokeRoles(game, logger);
            }
            else
            {
                var town = await GetValidTownOrLogErrorAsync(context, logger);
                if (town == null)
                    return null;

                if (!CheckIsTownViable(town, logger))
                    return null;

                ScheduleCleanup(TownKey.FromTown(town));

                // No record, so create one
                game = new Game(town!);

                // Assume the author of the command is the Storyteller
                var storyteller = context.Member;
                game.AddStoryteller(storyteller);

                var allUsers = new List<IMember>();

                foreach (var c in town.DayCategory!.Channels.Where(c => c.IsVoice))
                {
                    allUsers.AddRange(c.Users);
                }

                if(town.NightCategory != null)
                {
                    foreach(var c in town.NightCategory.Channels.Where(c => c.IsVoice))
                    {
                        allUsers.AddRange(c.Users);
                    }
                }

                // Sanity check for bots
                allUsers = allUsers.Where(u => !u.IsBot).ToList();

                allUsers.Remove(storyteller);

                // Make everyone else a villager
                foreach (var v in allUsers)
                {
                    game.AddVillager(v);
                }

                await GrantAndRevokeRoles (game, logger);
                await TagStorytellers (game, logger);

                m_activeGameService.RegisterGame(town, game);
            }

            return game;
        }

        public async Task<string> PhaseNightUnsafe(IGame game, IProcessLogger processLog)
		{
            if (game.Town.NightCategory != null)
            {
                // First, put storytellers into the top cottages
                var cottages = game.Town.NightCategory.Channels.OrderBy(c => c.Position).ToList();
                int numStCottages = game.Storytellers.Count > 0 ? 1 : 0;
                if (cottages.Count < numStCottages + game.Villagers.Count)
                    return "Not enough night channels to move all players"; // TODO: Test

                var stPairs = game.Storytellers.Select(m => Tuple.Create(cottages.First(), m)).ToList();

                // Put all the villagers into cottages first so the STs are the last to be auto-moved
                var villagerPairs = cottages.Skip(numStCottages).Zip(game.Villagers.OrderBy(u => u.DisplayName), (c, u) => Tuple.Create(c, u)).ToList();

                foreach (var (cottage, user) in m_shuffle.Shuffle(villagerPairs))
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(user, cottage, processLog);

                // Now move STs
                foreach (var (c, st) in m_shuffle.Shuffle(stPairs))
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(st, c, processLog);

                // Finally give members permission to see their cottages so they can move back if need be, or see 
                foreach (var (cottage, user) in villagerPairs)
                    await MemberHelper.AddPermissionsAsync(user, cottage, processLog);

                return "Moved all players from Town Square to Cottages!";
            }
            else
            {
                return "No Night Category for this town!";
            }
        }

        // Clear all permissions for cottages based on who's in which one
        private async Task ClearCottagePermissions(IGame game, IProcessLogger logger)
        {
            if (game.Town.NightCategory != null)
            {
                foreach (var chan in game.Town.NightCategory.Channels)
                {
                    foreach (var mem in chan.Users)
                    {
                        await MemberHelper.RemovePermissionsAsync(mem, chan, logger);
                    }
                }
            }
        }

        // TODO: should this be a method on Game itself? :thinking:
        // Helper for moving all players to Town Square (used by Day and Vote commands)
        private async Task MoveActivePlayersToTownSquare(IGame game, IProcessLogger logger)
        {
            foreach (var member in m_shuffle.Shuffle(game.AllPlayers))
            {
                await MemberHelper.MoveToChannelLoggingErrorsAsync(member, game.Town.TownSquare!, logger);
            }
        }

        public async Task<string> PhaseDayUnsafe(IGame game, IProcessLogger processLog)
		{
            await ClearCottagePermissions(game, processLog);
            await MoveActivePlayersToTownSquare(game, processLog);
            return "Moved all players from Cottages back to Town Square!";
        }

        public async Task<string> PhaseVoteUnsafe(IGame game, IProcessLogger processLog)
        { 
            await MoveActivePlayersToTownSquare(game, processLog);
            return "Moved all players to Town Square for voting!";
        }

        public async Task PerformVoteAsync(TownKey townRecord)
        {
            IGame? game;
            if(m_activeGameService.TryGetGame(townRecord, out game))
            {
                var logger = new ProcessLogger();
                try
                {
                    await PhaseVoteUnsafe(game, logger);
                }
                catch(Exception ex)
                {
                    logger.LogException(ex, "trying to run a vote");
                }
                // TODO: what to do with this logger?
            }
        }

        public async Task<string> EndGameUnsafe(IGame game, IProcessLogger logger)
        {
            m_activeGameService.EndGame(game.Town);

            foreach (var user in game.Storytellers)
            {
                await MemberHelper.RemoveStorytellerTag(user, logger);
                await user.RevokeRoleAsync(game.Town!.StorytellerRole!);
                await user.RevokeRoleAsync(game.Town!.VillagerRole!);
            }

            foreach(var user in game.Villagers)
            {
                await user.RevokeRoleAsync(game.Town!.VillagerRole!);
            }

            return "Thank you for playing Blood on the Clocktower!";
        }

        public async Task<string> SetStorytellersUnsafe(IBotInteractionContext context, IEnumerable<IMember> users, IProcessLogger logger)
        {
            IGame? game = await CurrentGameAsync(context, logger);
            if (game == null)
            {
                // TODO: more error reporting here?
                return "Couldn't find an active game record for this town!";
            }

            List<IMember> foundUsers = new();

            List<string> notFoundNames = new();
            foreach(var u in users)
            {
                if(game.AllPlayers.Contains(u))
                {
                    foundUsers.Add(u);
                }
                else
                {
                    notFoundNames.Add(MemberHelper.DisplayName(u));
                }
            }

            string returnMsg = "";
            if (notFoundNames.Count > 0)
            {
                returnMsg += $"These users don't seem to be playing the game: {string.Join(", ", notFoundNames.Select(x => "'" + x + "'"))}\n";
            }
            if (foundUsers.Count == 0)
            {
                return returnMsg + "Nothing to do!";
            }

            var revoke = game.Storytellers.Where(s => !foundUsers.Contains(s)).ToList();
            var grant = foundUsers.Where(s => !game.Storytellers.Contains(s)).ToList();

            foreach(var user in revoke)
            {
                game.RemoveStoryteller(user);
                game.AddVillager(user);
            }
            foreach (var user in grant)
            {
                game.RemoveVillager(user);
                game.AddStoryteller(user);
            }
            await GrantAndRevokeRoles(game, logger);
            await TagStorytellers (game, logger);

            var verbage = foundUsers.Count() > 1 ? "New Storytellers are" : "New Storyteller is";
            returnMsg += $"{verbage} {string.Join(", ", foundUsers.Select(x => MemberHelper.DisplayName(x)))}";
            return returnMsg;
        }
    }
}
