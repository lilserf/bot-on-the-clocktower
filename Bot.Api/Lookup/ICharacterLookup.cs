using System.Threading.Tasks;

namespace Bot.Api.Lookup
{
    public interface ICharacterLookup
    {
        Task<LookupCharacterResult?> LookupCharacterAsync(ulong guildId, string charString);
    }
}
