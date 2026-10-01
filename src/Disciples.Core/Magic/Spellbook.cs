using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Magic
{
    /// <summary>The player's researched spells and what was done with them this turn.</summary>
    public sealed class Spellbook
    {
        private readonly List<SpellDefinition> _known;
        private readonly List<SpellDefinition> _cast;

        public Spellbook(IEnumerable<SpellDefinition>? known = null, bool researchedThisTurn = false, IEnumerable<SpellDefinition>? castThisTurn = null)
        {
            _known = known?.ToList() ?? new List<SpellDefinition>();
            _cast = castThisTurn?.ToList() ?? new List<SpellDefinition>();
            ResearchedThisTurn = researchedThisTurn;
        }

        public IReadOnlyList<SpellDefinition> Known => _known;
        public IReadOnlyList<SpellDefinition> CastThisTurn => _cast;
        public bool ResearchedThisTurn { get; private set; }

        public bool Knows(SpellDefinition spell) => _known.Contains(spell);

        public bool WasCast(SpellDefinition spell) => _cast.Contains(spell);

        internal void Learn(SpellDefinition spell)
        {
            _known.Add(spell);
            ResearchedThisTurn = true;
        }

        internal void MarkCast(SpellDefinition spell) => _cast.Add(spell);

        internal void StartTurn()
        {
            _cast.Clear();
            ResearchedThisTurn = false;
        }
    }
}
