using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface ICharacterLookup
    {
        Task<LookupCharacterResult> LookupCharacterAsync(ulong guildId, string charString);
    }
}
