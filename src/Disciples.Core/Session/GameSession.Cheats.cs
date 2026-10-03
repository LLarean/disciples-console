using System.Linq;
using Disciples.Core.Magic;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    /// <summary>Shortcuts for manual testing: they skip prices, limits and the passing of turns.</summary>
    public sealed partial class GameSession
    {
        public void CheatGold(int amount) => Gold += amount;

        public void CheatMana(int amount) => Mana = Mana.Plus(new Mana(amount, amount, amount, amount));

        /// <summary>Every unit of the active party gets the experience it lacks to its next level or upgrade.</summary>
        public void CheatExperience()
        {
            foreach (var unit in Party.Squad.AliveUnits.ToList())
                Report(Progression.Gain(unit, unit.Definition.ExperienceToLevel - unit.Experience, Content.Unit, HasCapitalBuilding));
        }

        /// <summary>Heals every player party and gives it its movement back.</summary>
        public void CheatRestore()
        {
            foreach (var party in _parties)
            {
                HealSquad(party.Squad, 100);
                party.RestoreMovement();
            }
        }

        public void CheatRevealMap() => Fog.RevealAll();

        /// <summary>Builds everything in the capital, whatever the requirements.</summary>
        public void CheatBuildings()
        {
            if (!(Capital is { } capital))
                return;

            foreach (var building in capital.Buildings.Where(b => !capital.HasBuilt(b.Id)))
                capital.MarkBuilt(building);
        }

        /// <summary>Learns every spell of the player's race, with or without a tower.</summary>
        public void CheatSpells()
        {
            foreach (var spell in Spells.Where(s => !Spellbook.Knows(s)).ToList())
                Spellbook.Learn(spell);
        }

        /// <summary>Puts one of each item into the active party's bag.</summary>
        public void CheatItems()
        {
            foreach (var item in Content.Items)
                Party.Give(item);
        }
    }
}
