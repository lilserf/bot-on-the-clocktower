using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface IOfficialCharacterCache
    {
        Task<GetOfficialCharactersResult> GetOfficialCharactersAsync();
        void InvalidateCache();
    }
}
