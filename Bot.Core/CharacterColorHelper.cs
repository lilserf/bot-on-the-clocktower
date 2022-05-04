using Bot.Api;
using Bot.Core.Lookup;
using System;

namespace Bot.Core
{
    public static class CharacterColorHelper
    {
        public static IColor GetColorForTeam(IColorBuilder builder, CharacterTeam team)
        {
            switch (team)
            {
                case CharacterTeam.Townsfolk:   return builder.Build(32, 100, 252);
                case CharacterTeam.Minion:      return builder.Build(251, 102, 0);
                case CharacterTeam.Demon:       return builder.Build(203, 1, 0);
                case CharacterTeam.Outsider:    return builder.Build(69, 209, 251);
                case CharacterTeam.Traveler:    return builder.Purple;
                case CharacterTeam.Fabled:      return builder.Build(248, 227, 30);
            }
            throw new NotImplementedException();
        }
    }
}
