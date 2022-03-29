using System.Threading.Tasks;

namespace Bot.Core.Lookup
{
    public interface IOfficialCharacterCache
    {
        public Task<GetOfficialCharactersResult> GetOfficialCharactersAsync();
    }
}
