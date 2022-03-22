namespace Bot.Api.Lookup
{
    public enum CharacterTeam
    {
        Townsfolk,
        Outsider,
        Minion,
        Demon,
        Traveler,
        Fabled,
    }

    public class CharacterData
    {
        public string Name { get; }
        public string Ability { get; }
        public CharacterTeam Team { get; }
        public bool IsOfficial { get; }
        public string? FlavorText { get; set; }
        public string? ImageUrl { get; set; }

        public CharacterData(string name, string ability, CharacterTeam team, bool isOfficial)
        {
            Name = name;
            Ability = ability;
            Team = team;
            IsOfficial = isOfficial;
        }
    }
}
