using Bot.Api.Database;

namespace Bot.Api
{
    public class TownKey
    {
        public ulong GuildId { get; }
        public ulong ControlChannelId { get; }

        public TownKey(ulong guildId, ulong channelId)
        {
            GuildId = guildId;
            ControlChannelId = channelId;
        }

        public static TownKey FromTown(ITown town) => FromTownRecord(town.TownRecord);
        public static TownKey FromTownRecord(ITownRecord townRecord) => new TownKey(townRecord.GuildId, townRecord.ControlChannelId);

        public bool Equals(TownKey other)
        {
            return
                GuildId == other.GuildId &&
                ControlChannelId == other.ControlChannelId;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TownKey tk)
                return false;
            return Equals(tk);
        }

        public override int GetHashCode()
        {
            return GuildId.GetHashCode() | ControlChannelId.GetHashCode();
        }

        public override string ToString() => $"[TownKey GuildId: {GuildId}, ControlChannelId: {ControlChannelId}]";

        public static bool operator ==(TownKey lhs, TownKey rhs) => lhs.Equals(rhs);
        public static bool operator !=(TownKey lhs, TownKey rhs) => !lhs.Equals(rhs);
    }
}
