using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IChannel
	{
		public ulong Id { get; }

		public IReadOnlyCollection<IMember> Users { get; }

		public IReadOnlyCollection<IChannel> Channels { get; }

		public int Position { get; }
		
		public bool IsVoice { get; }
	}
}
