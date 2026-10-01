using Disciples.Core.Cities;
using Disciples.Core.Items;
using Disciples.Core.Map;
using Disciples.Core.Squads;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    public enum GameEventKind
    {
        TurnStarted,
        BattleWon,
        CityCaptured,
        EnemyAttacks,
        CityFell,
        CityHeld,
        EnemyMoved,
        EnemyAppeared,
        TreasureFound,
        ItemFound,
        TreasureLost,
        /// <summary>A mine or a mana source became the player's; see <see cref="GameEvent.Site"/>.</summary>
        MineCaptured,
        MineLost,
        UnitLeveledUp,
        UnitUpgraded,
        UnitAwaitsBuilding,
        PartyLost,
        GameWon,
        GameLost
    }

    /// <summary>
    /// Something that happened in the session, for the front-end to report or animate. Subject and Detail are display names;
    /// the objects and positions involved are set where the event has them.
    /// </summary>
    public sealed class GameEvent
    {
        public GameEvent(GameEventKind kind, string subject = "", string detail = "", int amount = 0)
        {
            Kind = kind;
            Subject = subject;
            Detail = detail;
            Amount = amount;
        }

        public GameEventKind Kind { get; }
        public string Subject { get; }
        public string Detail { get; }
        public int Amount { get; }

        /// <summary>The party that acted: a player's party or an enemy leader's.</summary>
        public Party? Party { get; internal set; }

        public City? City { get; internal set; }
        public Site? Site { get; internal set; }
        public Unit? Unit { get; internal set; }
        public ItemDefinition? Item { get; internal set; }

        /// <summary>Where a moving party came from.</summary>
        public Position? From { get; internal set; }

        /// <summary>Where it happened.</summary>
        public Position? At { get; internal set; }
    }
}
