namespace Bot.Core.Lookup
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
        public string Id { get; }
        public string Name { get; }
        public string Ability { get; }
        public CharacterTeam Team { get; }
        public bool IsOfficial { get; }
        public string? FlavorText { get; set; }
        public string? ImageUrl { get; set; }

        public CharacterData(string id, string name, string ability, CharacterTeam team, bool isOfficial)
        {
            Id = id;
            Name = name;
            Ability = ability;
            Team = team;
            IsOfficial = isOfficial;
        }

        public override string ToString() => Name;
    }
}
