using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IMember
	{
		public Task PlaceInAsync(IChannel c);
	}
}
