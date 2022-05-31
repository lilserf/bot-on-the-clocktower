using Bot.Api;
using Bot.Api.Database;
using Bot.Core.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bot.Core
{
    public class BotGameplay : BotTownLookupHelper, IVoteHandler
    {
        private readonly IBotClient m_client;
        private readonly IShuffleService m_shuffle;
        private readonly ITownCleanup m_townCleanup;
        private readonly IGameMetricDatabase m_gameMetricsDatabase;
        private readonly ICommandMetricDatabase m_commandMetricsDatabase;
        private readonly IDateTime m_dateTime;

        public const string NoGameInProgressMessage = "Could not start a game. This might mean there was a permission issue, or it could mean nobody is currently in the Town channels. Are you ready to play?";

        public BotGameplay(IServiceProvider services)
            : base(services)
		{
            services.Inject(out m_client);
            services.Inject(out m_shuffle);
            services.Inject(out m_townCleanup);
            services.Inject(out m_gameMetricsDatabase);
            services.Inject(out m_commandMetricsDatabase);
            services.Inject(out m_dateTime);

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
            EndGameUnsafeAsync(e.TownKey, logger).ConfigureAwait(continueOnCapturedContext: true);
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
                await MemberHelper.RemoveStorytellerTagAsync(u, logger);
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
            
            var activityRecordTask = m_townCleanup.RecordActivityAsync(townKey);
            var gameTask = CreateGameFromDiscordState(townKey, requester, logger);

            await Task.WhenAll(gameTask, activityRecordTask);
            return gameTask.Result;
        }

        // Create a new IGame with state matching what's going on in Discord
        public async Task<IGame?> CreateGameFromDiscordState(TownKey townKey, IMember? commandAuthor, IProcessLogger logger)
        {
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);
            if (town == null)
                return null;

            // Find all participants in the game
            var allUsers = new List<IMember>();

            foreach (var c in town.DayCategory!.Channels.Where(c => c.IsVoice))
            {
                allUsers.AddRange(c.Users.ToList());
            }

            allUsers.AddRange(GetMembersInNightCategory(town));
            // Sanity check for bots
            allUsers = allUsers.Where(u => !u.IsBot).ToList();

            HashSet<IMember> storytellers = new();
            HashSet<IMember> villagers = new();
            // Now find everybody who currently has the storyteller role
            foreach (var user in allUsers)
            {
                if(user.Roles.Contains(town.StorytellerRole))
                    storytellers.Add(user);
            }

            if (commandAuthor != null)
            {
                // Next, resolve whether this command produces a change in Storytellers
                if (!storytellers.Contains(commandAuthor))
                {
                    foreach (var user in storytellers)
                    {
                        villagers.Add(user);
                        storytellers.Remove(user);
                    }
                    villagers.Remove(commandAuthor);
                    storytellers.Add(commandAuthor);
                }
            }

            bool commandAuthorInChannels = true;
            
            if(commandAuthor != null)
                commandAuthorInChannels = allUsers.Remove(commandAuthor);
            
            // If the author isn't in one of the channels
            // or nobody else is around, we can't play a game
            if (!commandAuthorInChannels && allUsers.Count == 0)
                return null;

            // Last, populate the Villagers by putting all non-Storytellers as Villagers
            foreach (var p in allUsers)
            {
                if(!storytellers.Contains(p))
                    villagers.Add(p);
            }

            IGame? game = new Game(townKey, storytellers, villagers);
            // Finally grant the proper roles where needed
            await GrantAndRevokeRoles(game, town, logger);
            // And tag the new storytellers where needed
            await TagStorytellers(game, logger);

            return game;
        }

        private static IEnumerable<IMember> GetMembersInNightCategory(ITown town)
        {
            if (town.NightCategory != null)
                foreach (var c in town.NightCategory.Channels.Where(c => c.IsVoice))
                    foreach (var u in c.Users)
                        yield return u;
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
                await MoveStorytellersToCottages(processLog, town, m_shuffle.Shuffle(stPairs));

                // Finally give members permission to see their cottages so they can move back if need be, or see 
                foreach (var (cottage, user) in villagerPairs)
                {
                    await cottage.ClearOverwrites();
                    await cottage.AddOverwriteAsync(user, IBaseChannel.Permissions.AccessChannels);
                }

                await m_gameMetricsDatabase.RecordNightAsync(game.TownKey, m_dateTime.Now);
                await m_commandMetricsDatabase.RecordCommand("night", m_dateTime.Now);

                return $"Moved all players from {town.TownSquare?.Name ?? "Town Square"} to nighttime!";
            }
            else
            {
                return "No Night Category for this town!";
            }
        }

        private static async Task MoveStorytellersToCottages(IProcessLogger processLog, ITown town, IEnumerable<Tuple<IChannel, IMember>> sts)
        {
            HashSet<IMember>? nightCatMembers = null;
            foreach (var (c, st) in sts)
            {
                if (nightCatMembers == null)
                    nightCatMembers = GetMembersInNightCategory(town).ToHashSet();

                if (nightCatMembers.Contains(st))
                    continue;

                await MemberHelper.MoveToChannelLoggingErrorsAsync(st, c, processLog);
                nightCatMembers = null; // We just waited for a move, so the members list may have changed and need refreshing
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
            if (town.NightCategory != null)
            {
                foreach (var cottage in town.NightCategory.Channels)
                {
                    foreach (var user in cottage.Users)
                    {
                        await cottage.RemoveOverwriteAsync(user);
                    }
                }
            }
            await MoveActivePlayersToTownSquare(game, town, processLog);

            await m_gameMetricsDatabase.RecordDayAsync(game.TownKey, m_dateTime.Now);
            await m_commandMetricsDatabase.RecordCommand("day", m_dateTime.Now);

            return "Moved all players from Cottages back to Town Square!";
        }

        public async Task<string> PhaseVoteUnsafe(IGame game, IProcessLogger processLog)
        {
            var town = await GetValidTownOrLogErrorAsync(game.TownKey, processLog);
            if (town == null)
                return "Failed to find a valid town!";

            await MoveActivePlayersToTownSquare(game, town, processLog);

            await m_gameMetricsDatabase.RecordVoteAsync(game.TownKey, m_dateTime.Now);
            await m_commandMetricsDatabase.RecordCommand("vote", m_dateTime.Now);

            return "Moved all players to Town Square for voting!";
        }

        public async Task PerformVoteAsync(TownKey townKey)
        {
            var logger = new ProcessLogger();

            var game = await CreateGameFromDiscordState(townKey, null, logger);
            
            if (game != null)
            {
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

        public async Task<InteractionResult> EndGameUnsafeAsync(TownKey townKey, IProcessLogger logger)
        {
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);
            if (town == null)
                return "Failed to find a valid town for this command!";

            await m_gameMetricsDatabase.RecordEndGameAsync(townKey, m_dateTime.Now);
            await m_commandMetricsDatabase.RecordCommand("endgame", m_dateTime.Now);

            // We don't care if there are currently people in town doing stuff. All we care about is the current Discord role state
            var guild = await m_client.GetGuildAsync(townKey.GuildId);
            if (guild != null)
                await EndGameForTownAsync(guild, town.StorytellerRole, town.VillagerRole, logger);
            return "Thank you for playing Blood on the Clocktower!";
        }

        private static async Task EndGameForTownAsync(IGuild guild, IRole? storytellerRole, IRole? villagerRole, IProcessLogger logger)
        {
            if (storytellerRole == null && villagerRole == null)
                return;

            var allMembers = guild.Members.Values.ToList();
            foreach (var member in allMembers)
            {
                var roles = member.Roles.ToHashSet();
                if (storytellerRole != null && roles.Contains(storytellerRole))
                {
                    await member.RevokeRoleAsync(storytellerRole);
                    await MemberHelper.RemoveStorytellerTagAsync(member, logger);
                }
                if (villagerRole != null && roles.Contains(villagerRole))
                {
                    await member.RevokeRoleAsync(villagerRole);
                }
            }
        }

        public async Task<InteractionResult> SetStorytellersUnsafe(TownKey townKey, IMember requester, IEnumerable<IMember> users, IProcessLogger logger)
        {
            IGame? game = await CurrentGameAsync(townKey, requester, logger);
            var town = await GetValidTownOrLogErrorAsync(townKey, logger);

            if (game == null)
            {
                // TODO: more error reporting here?
                return NoGameInProgressMessage;
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

            await m_commandMetricsDatabase.RecordCommand("setstorytellers", m_dateTime.Now);

            var verbage = foundUsers.Count > 1 ? "New Storytellers are" : "New Storyteller is";
            returnMsg += $"{verbage} {string.Join(", ", foundUsers.Select(x => MemberHelper.DisplayName(x)))}";
            return returnMsg;
        }
    }
}
