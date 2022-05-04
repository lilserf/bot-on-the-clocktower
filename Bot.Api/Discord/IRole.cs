using System.Threading.Tasks;

namespace Bot.Api
{
    public interface IRole
	{
		public string Name { get; }
		public string Mention { get; }

		public ulong Id { get; }

		public bool IsThisBot { get; }

		public Task DeleteAsync();
	}
}
