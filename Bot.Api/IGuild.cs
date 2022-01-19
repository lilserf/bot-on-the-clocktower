using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IGuild
	{
		public ulong Id { get; }

		public IReadOnlyDictionary<ulong, IRole> Roles { get; }

		public IReadOnlyDictionary<ulong, IMember> Members { get; }

		public Task<IChannel> CreateVoiceChannelAsync(string name, IChannel? parent = null);
		public Task<IChannel> CreateTextChannelAsync(string name, IChannel? parent = null);
		public Task<IChannel> CreateCategoryAsync(string name);

		public Task<IRole> CreateRoleAsync(string name, Color color);
	}
}
