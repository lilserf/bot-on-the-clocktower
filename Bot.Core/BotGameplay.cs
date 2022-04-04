using Bot.Api;
using Bot.Core.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : BotTownLookupHelper, IVoteHandler
    {
        private readonly IActiveGameService m_activeGameService;
        private readonly IBotClient m_client;
        private readonly IShuffleService m_shuffle;
        private readonly ITownCleanup m_townCleanup;

		public BotGameplay(IServiceProvider services)
            : base(services)
		{
            services.Inject(out m_activeGameService);
            services.Inject(out m_client);
            services.Inject(out m_shuffle);
            services.Inject(out m_townCleanup);

            m_townCleanup.CleanupRequested += TownCleanup_CleanupRequested;
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

        private void TownCleanup_CleanupRequested(object? sender, TownCleanupRequestedArgs e)
        {
            var logger = new ProcessLogger();
            EndGameUnsafe(e.TownKey, logger).ConfigureAwait(continueOnCapturedContext: true);
        }

        private static async Task TagStorytellers(IGame game, IProcessLogger logger)
        {
            Serilog.Log.Debug("TagStorytellers in game {@game}", game);
            foreach (var u in game.Storytellers)
            {
                await MemberHelper.AddStorytellerTag(u, logger);
            }

            foreach(var u in game.Villagers)
            {
                await MemberHelper.RemoveStorytellerTag(u, logger);
            }
        }

        private static async Task GrantAndRevokeRoles(IGame game, ITown town, IProcessLogger logger)
        {
            Serilog.Log.Debug("GrantAndRevokeRoles in game {@game}", game);

            IRole? storytellerRole = town.StorytellerRole;
            IRole? villagerRole = town.VillagerRole;

            foreach(var u in game.Storytellers)
            {
                if (storytellerRole != null && !u.Roles.Contains(storytellerRole))
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, storytellerRole, logger);
                if (villagerRole != null && !u.Roles.Contains(villagerRole))
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, villagerRole, logger);
            }

            foreach (var u in game.Villagers)
            {
                if (storytellerRole != null && u.Roles.Contains(storytellerRole))
                    await MemberHelper.RevokeRoleLoggingErrorsAsync(u, storytellerRole, logger);
                if (villagerRole != null && !u.Roles.Contains(villagerRole))
                    await MemberHelper.GrantRoleLoggingErrorsAsync(u, villagerRole, logger);
            }
        }

        public async Task<IGame?> CurrentGameAsync(TownKey townKey, IMember requester, IProcessLogger logger)
        {
            Serilog.Log.Debug("CurrentGameAsync from town {@townKey}", townKey);
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);
            if (town == null)
                return null;
            if (!CheckIsTownViable(town, logger))
                return null;
            
            Task activityRecordTask = m_townCleanup.RecordActivityAsync(townKey);
            if (m_activeGameService.TryGetGame(townKey, out IGame? game))
            {
                Serilog.Log.Debug("CurrentGameAsync found viable game in progress: {@game}", game);

                //Resolve a change in Storytellers
                if (!game.Storytellers.Contains(requester))
                {
                    foreach (var user in game.Storytellers.ToList())
                    {
                        game.AddVillager(user);
                        game.RemoveStoryteller(user);
                    }
                    game.RemoveVillager(requester);
                    game.AddStoryteller(requester);
                }

                await TagStorytellers(game, logger);

                var foundUsers = new List<IMember>();

                foreach (var c in town.DayCategory!.Channels.Where(c => c.IsVoice))
                {
                    foundUsers.AddRange(c.Users.ToList());
                }

                if (town.NightCategory != null)
                {
                    foreach (var c in town.NightCategory.Channels.Where(c => c.IsVoice))
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

                await GrantAndRevokeRoles(game, town, logger);
            }
            else
            {
                // No record, so create one
                game = new Game(townKey);
                Serilog.Log.Debug("CurrentGameAsync created new game {@game} from town {@town}", game, town);

                // Assume the author of the command is the Storyteller
                var storyteller = requester;
                game.AddStoryteller(storyteller);
                Serilog.Log.Debug("CurrentGameAsync: Storyteller is {@storyteller}", storyteller);

                var allUsers = new List<IMember>();

                foreach (var c in town.DayCategory!.Channels.Where(c => c.IsVoice))
                {
                    allUsers.AddRange(c.Users);
                }

                if (town.NightCategory != null)
                {
                    foreach (var c in town.NightCategory.Channels.Where(c => c.IsVoice))
                    {
                        allUsers.AddRange(c.Users);
                    }
                }

                // Sanity check for bots
                allUsers = allUsers.Where(u => !u.IsBot).ToList();

                bool storytellerInChannels = allUsers.Remove(storyteller);

                // Make everyone else a villager
                foreach (var v in allUsers)
                {
                    game.AddVillager(v);
                    Serilog.Log.Debug("CurrentGameAsync: Added villager {@villager}", v);
                }

                // Check that the storyteller is actually in one of the channels
                if (!storytellerInChannels)
                    return null;
                // Check that the players of the game are actually in channels?
                foreach (var user in game.Villagers)
                {
                    if (!allUsers.Contains(user))
                        return null;
                }

                await GrantAndRevokeRoles(game, town, logger);
                await TagStorytellers(game, logger);

                m_activeGameService.RegisterGame(town, game);
            }

            await activityRecordTask;

            return game;
        }

        public async Task<string> PhaseNightUnsafe(IGame game, IProcessLogger processLog)
		{
            var town = await GetValidTownOrLogErrorAsync(game.TownKey, processLog);
            if(town == null)
            {
                return "Failed to find a valid town!";
            }
            if (town.NightCategory != null)
            {
                // First, put storytellers into the top cottages
                var cottages = town.NightCategory.Channels.OrderBy(c => c.Position).ToList();
                int numStCottages = game.Storytellers.Count > 0 ? 1 : 0;
                if (cottages.Count < numStCottages + game.Villagers.Count)
                    return "Not enough night channels to move all players"; // TODO: Test

                var stPairs = game.Storytellers.Select(m => Tuple.Create(cottages.First(), m)).ToList();

                // Put all the villagers into cottages first so the STs are the last to be auto-moved
                var villagerPairs = cottages.Skip(numStCottages).Zip(game.Villagers.OrderBy(u => MemberHelper.DisplayName(u)), (c, u) => Tuple.Create(c, u)).ToList();

                foreach (var (cottage, user) in m_shuffle.Shuffle(villagerPairs))
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(user, cottage, processLog);

                // Now move STs
                foreach (var (c, st) in m_shuffle.Shuffle(stPairs))
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(st, c, processLog);

                // Finally give members permission to see their cottages so they can move back if need be, or see 
                // This currently throws UnauthorizedException :/
                //foreach (var (cottage, user) in villagerPairs)
                //    await MemberHelper.AddPermissionsAsync(user, cottage, processLog);

                return "Moved all players from Town Square to Cottages!";
            }
            else
            {
                return "No Night Category for this town!";
            }
        }

        // TODO: should this be a method on Game itself? :thinking:
        // Helper for moving all players to Town Square (used by Day and Vote commands)
        private async Task MoveActivePlayersToTownSquare(IGame game, ITown town, IProcessLogger logger)
        {
            if (town.TownSquare != null)
                foreach (var member in m_shuffle.Shuffle(game.AllPlayers))
                    await MemberHelper.MoveToChannelLoggingErrorsAsync(member, town.TownSquare, logger);
        }

        public async Task<string> PhaseDayUnsafe(IGame game, IProcessLogger processLog)
		{
            var town = await GetValidTownOrLogErrorAsync(game.TownKey, processLog);
            if (town == null)
                return "Failed to find a valid town!";
            // Doesn't currently work :(
            //await ClearCottagePermissions(game, processLog);
            await MoveActivePlayersToTownSquare(game, town, processLog);
            return "Moved all players from Cottages back to Town Square!";
        }

        public async Task<string> PhaseVoteUnsafe(IGame game, IProcessLogger processLog)
        {
            var town = await GetValidTownOrLogErrorAsync(game.TownKey, processLog);
            if (town == null)
                return "Failed to find a valid town!";

            await MoveActivePlayersToTownSquare(game, town, processLog);
            return "Moved all players to Town Square for voting!";
        }

        public async Task PerformVoteAsync(TownKey townRecord)
        {
            if (m_activeGameService.TryGetGame(townRecord, out IGame? game))
            {
                var logger = new ProcessLogger();
             
                try
                {
                    await PhaseVoteUnsafe(game, logger);
                }
                catch (Exception ex)
                {
                    logger.LogException(ex, "trying to run a vote");
                }
                // TODO: what to do with this logger?
            }
        }

        private static async Task EndGameForUser(IMember user, ITown town, IProcessLogger logger)
        {
            await MemberHelper.RemoveStorytellerTag(user, logger);
            if (town.StorytellerRole != null)
                await user.RevokeRoleAsync(town.StorytellerRole);
            if (town.VillagerRole != null)
                await user.RevokeRoleAsync(town.VillagerRole);
        }

        public async Task<string> EndGameUnsafe(TownKey townKey, IProcessLogger logger)
        {
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);
            if (town == null)
                return "Failed to find a valid town for this command!";

            if (m_activeGameService.TryGetGame(townKey, out IGame? game))
            {
                m_activeGameService.EndGame(town);

                foreach (var user in game.AllPlayers)
                    await EndGameForUser(user, town, logger);
                return "Thank you for playing Blood on the Clocktower!";
            }
            else
            {
                var guild = await m_client.GetGuildAsync(townKey.GuildId);
                if (guild != null)
                    foreach (var (_, user) in guild.Members)
                        await EndGameForUser(user, town, logger);
                return "Cleanup of inactive game complete";
            }

        }

        public async Task<InteractionResult> SetStorytellersUnsafe(TownKey townKey, IMember requester, IEnumerable<IMember> users, IProcessLogger logger)
        {
            IGame? game = await CurrentGameAsync(townKey, requester, logger);
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);

            if (game == null)
            {
                // TODO: more error reporting here?
                return "Couldn't find an active game record for this town!";
            }
            if (town == null)
            {
                return "Couldn't find a valid town for this command!";
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
            await GrantAndRevokeRoles(game, town, logger);
            await TagStorytellers(game, logger);

            var verbage = foundUsers.Count > 1 ? "New Storytellers are" : "New Storyteller is";
            returnMsg += $"{verbage} {string.Join(", ", foundUsers.Select(x => MemberHelper.DisplayName(x)))}";
            return returnMsg;
        }
    }
}
