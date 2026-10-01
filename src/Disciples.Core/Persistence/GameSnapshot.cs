using System.Collections.Generic;
using Disciples.Core.Cities;
using Disciples.Core.Magic;
using Disciples.Core.Map;
using Disciples.Core.Squads;

namespace Disciples.Core.Persistence
{
    /// <summary>
    /// Serializable game state. Scenario files use the same format, so a new game is a load of a pristine snapshot.
    /// Units, terrains and buildings are referenced by content id.
    /// </summary>
    public sealed class GameSnapshot
    {
        public int Version { get; set; } = SnapshotMigrator.Default.CurrentVersion;
        public MapSnapshot Map { get; set; } = new MapSnapshot();

        /// <summary>Id of the player's race; scenarios and older saves name none and get the first race.</summary>
        public string? Race { get; set; }

        public int Turn { get; set; } = 1;
        public int Gold { get; set; }

        /// <summary>The enemy's treasury: it pays for recruits and new leaders.</summary>
        public int EnemyGold { get; set; }

        public Mana? Mana { get; set; }

        /// <summary>Ids of the researched spells.</summary>
        public List<string> Spells { get; set; } = new List<string>();

        public bool ResearchedThisTurn { get; set; }

        /// <summary>Ids of the spells already cast this turn.</summary>
        public List<string> CastThisTurn { get; set; } = new List<string>();

        /// <summary>The player's parties.</summary>
        public List<PartySnapshot> Parties { get; set; } = new List<PartySnapshot>();

        /// <summary>Index of the active party in <see cref="Parties"/>.</summary>
        public int Active { get; set; }

        /// <summary>Version 1 only: the single player party, moved into <see cref="Parties"/> by <see cref="SnapshotMigrator"/>.</summary>
        public PartySnapshot? Party { get; set; }

        public List<CitySnapshot> Cities { get; set; } = new List<CitySnapshot>();
        public List<NeutralSnapshot> Neutrals { get; set; } = new List<NeutralSnapshot>();

        /// <summary>Enemy leaders' parties, moved by the AI.</summary>
        public List<PartySnapshot> Enemies { get; set; } = new List<PartySnapshot>();

        public List<SiteSnapshot> Sites { get; set; } = new List<SiteSnapshot>();
        public List<RodSnapshot> Rods { get; set; } = new List<RodSnapshot>();

        /// <summary>Fog of war rows, '#' for explored tiles. Empty in scenarios: the start is revealed around the party and cities.</summary>
        public List<string> Explored { get; set; } = new List<string>();
    }

    public sealed class MapSnapshot
    {
        public string Name { get; set; } = "";

        /// <summary>Tile character to terrain id.</summary>
        public Dictionary<string, string> Legend { get; set; } = new Dictionary<string, string>();

        public List<string> Rows { get; set; } = new List<string>();
    }

    public sealed class PartySnapshot
    {
        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>Remaining points; null means full.</summary>
        public int? MovementPoints { get; set; }

        /// <summary>Squad units, the leader included.</summary>
        public List<UnitSnapshot> Units { get; set; } = new List<UnitSnapshot>();

        public List<LeaderPerk> Perks { get; set; } = new List<LeaderPerk>();

        /// <summary>Ids of items in the bag.</summary>
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>Ids of the worn artifact and banner.</summary>
        public List<string> Equipped { get; set; } = new List<string>();
    }

    public sealed class CitySnapshot
    {
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public bool Capital { get; set; }
        public Owner Owner { get; set; }
        public int Income { get; set; }
        public int Tier { get; set; } = 1;

        /// <summary>Mana per turn; set on capitals.</summary>
        public Mana? Mana { get; set; }

        public List<string> Recruits { get; set; } = new List<string>();

        /// <summary>Whether the city offers the building tree.</summary>
        public bool Buildings { get; set; }

        public List<string> Built { get; set; } = new List<string>();
        public List<UnitSnapshot> Garrison { get; set; } = new List<UnitSnapshot>();
    }

    public sealed class SiteSnapshot
    {
        public string Name { get; set; } = "";
        public SiteKind Kind { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Gold { get; set; }

        /// <summary>Mana a source yields per turn.</summary>
        public Mana? Mana { get; set; }

        public Owner Owner { get; set; }
        public List<string> Mercenaries { get; set; } = new List<string>();
        public List<string> Items { get; set; } = new List<string>();
    }

    public sealed class RodSnapshot
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Owner Owner { get; set; } = Owner.Player;
    }

    public sealed class NeutralSnapshot
    {
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Reward { get; set; }
        public List<UnitSnapshot> Units { get; set; } = new List<UnitSnapshot>();
    }

    public sealed class UnitSnapshot
    {
        public string Id { get; set; } = "";
        public SquadLine Line { get; set; }
        public int Column { get; set; }
        public int Level { get; set; } = 1;
        public int Experience { get; set; }

        /// <summary>Current HP; null means full.</summary>
        public int? Hp { get; set; }
    }
}
