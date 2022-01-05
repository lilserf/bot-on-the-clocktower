using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Core
{
	public interface IShuffleService
	{
		IEnumerable<T> Shuffle<T>(IEnumerable<T> input);
	}
}
