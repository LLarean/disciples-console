using System.Collections.Generic;
using System.Linq;
using Disciples.Core.Magic;

namespace Disciples.Core.Content
{
    /// <summary>A playable race: its leader classes, the units its capital offers, the capital's guardian, name and mana.</summary>
    public sealed class Race
    {
        public Race(
            string id, string name, IEnumerable<string>? leaders = null, IEnumerable<string>? recruits = null, string? guardian = null,
            Mana? mana = null, string? capitalName = null)
        {
            Id = id;
            Name = name;
            Leaders = (leaders ?? Enumerable.Empty<string>()).ToList();
            Recruits = (recruits ?? Enumerable.Empty<string>()).ToList();
            Guardian = guardian;
            Mana = mana ?? Mana.None;
            CapitalName = capitalName;
        }

        public string Id { get; }
        public string Name { get; }

        /// <summary>Leader unit ids offered when starting a new game and for hire in the capital.</summary>
        public IReadOnlyList<string> Leaders { get; }

        /// <summary>Tier-1 unit ids for hire in the capital.</summary>
        public IReadOnlyList<string> Recruits { get; }

        public string? Guardian { get; }

        /// <summary>Mana the capital yields per turn.</summary>
        public Mana Mana { get; }

        public string? CapitalName { get; }

        /// <summary>Content without a race belongs to every race.</summary>
        public bool Has(string? race) => race == null || race == Id;
    }
}
