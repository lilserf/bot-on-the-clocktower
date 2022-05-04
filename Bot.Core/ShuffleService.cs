using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Core
{
    public class ShuffleService : IShuffleService
	{
		private static Random rng = new Random();
		// Okay this should be a Fisher-Yates-Durstenfeld shuffle
		// from https://stackoverflow.com/questions/5807128/an-extension-method-on-ienumerable-needed-for-shuffling
		public IEnumerable<T> Shuffle<T>(IEnumerable<T> input)
		{
			var source = input.ToList();
			for(int i=0; i < source.Count; i++)
			{
				int j = rng.Next(i, source.Count);
				yield return source[j];
				source[j] = source[i];
			}
		}
	}
}
