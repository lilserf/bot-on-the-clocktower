using Bot.Api;

namespace Bot.DSharp
{
    public static class DSharpPermissionHelper
    {
        public static IBaseChannel.Permissions BasePermissionsFromDSharpPermissions(DSharpPlus.Permissions dSharpPermissions)
        {
            // TODO: Could be better than just a cast, we should probably seaprate these.
            // But note that flag status needs to be preserved
            return (IBaseChannel.Permissions)dSharpPermissions;
        }

        public static DSharpPlus.Permissions DSharpPermissionsFromBasePermissions(IBaseChannel.Permissions dSharpPermissions)
        {
            // TODO: Could be better than just a cast, we should probably seaprate these.
            // But note that flag status needs to be preserved
            return (DSharpPlus.Permissions)dSharpPermissions;
        }
    }
}
