﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Api
{
	public interface IGame
	{
		ITown Town { get; }

		IList<IMember> StoryTellers { get; }

		IList<IMember> Villagers { get; }

	}
}