using Bot.Api;
using System;
using System.Threading.Tasks;

namespace Bot.Core
{
    public static class MemberHelper
    {
        public static async Task<bool> MoveToChannelLoggingErrorsAsync(IMember member, IChannel channel, IProcessLogger logger)
        {
            try
            {
                await member.MoveToChannelAsync(channel);
                return true;
            }
            catch (Exception ex)
            {
                if (!IsHandledException(ex))
                    throw;

                logger.LogException(ex, $"move {member.DisplayName} to channel {channel.Name}");
                return false;
            }
        }

        public static async Task<bool> GrantRoleLoggingErrorsAsync(IMember member, IRole role, IProcessLogger logger)
        {
            try
            {
                await member.GrantRoleAsync(role);
                return true;
            }
            catch (Exception ex)
            {
                if (!IsHandledException(ex))
                    throw;

                logger.LogException(ex, $"grant role '{role.Name}' to {member.DisplayName}");
                return false;
            }
        }

        public static async Task<bool> RevokeRoleLoggingErrorsAsync(IMember member, IRole role, IProcessLogger logger)
        {
            try
            {
                await member.RevokeRoleAsync(role);
                return true;
            }
            catch (Exception ex)
            {
                if (!IsHandledException(ex))
                    throw;

                logger.LogException(ex, $"revoke role '{role.Name}' from {member.DisplayName}");
                return false;
            }
        }

        private static bool IsHandledException(Exception ex)
        {
            return (ex is UnauthorizedException || ex is NotFoundException || ex is ServerErrorException);
        }
    }
}
