using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Squads;

namespace Disciples.Core.Session
{
    public sealed partial class GameSession
    {
        public bool CanPlantRods => Party.Leader.Definition.PlantsRods;

        /// <summary>The active party's leader plants a rod on its tile; mines and mana sources on the claimed land follow at turn end.</summary>
        public RodResult PlantRod()
        {
            if (!CanPlantRods)
                return RodResult.NotARodBearer;

            var at = Party.Position;
            if (Map.CityAt(at) != null || Map.SiteAt(at) != null || Map.RodAt(at) != null)
                return RodResult.Occupied;

            if (Gold < Rules.RodCost)
                return RodResult.NotEnoughGold;

            Gold -= Rules.RodCost;
            Map.AddRod(new Rod(at));
            _events.Add(new GameEvent(GameEventKind.RodPlanted, Party.Name, amount: Rules.RodCost) { Party = Party, At = at });
            return RodResult.Planted;
        }

        /// <summary>A party stepping on a rod of the other side breaks it.</summary>
        private void BreakRod(Party party, Owner side)
        {
            if (Map.RodAt(party.Position) is not { } rod || rod.Owner == side)
                return;

            Map.RemoveRod(rod);
            var kind = side == Owner.Player ? GameEventKind.RodDestroyed : GameEventKind.RodLost;
            _events.Add(new GameEvent(kind, party.Name) { Party = party, At = rod.Position });
        }
    }
}
