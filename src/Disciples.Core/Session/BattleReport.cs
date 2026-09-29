using System.Collections.Generic;
using Disciples.Core.Battles;
using Disciples.Core.Cities;
using Disciples.Core.Units;

namespace Disciples.Core.Session
{
    public sealed class BattleReport
    {
        public BattleReport(BattleOutcome outcome, int gold, int experience, IReadOnlyList<UnitProgress> progress, City? capturedCity)
        {
            Outcome = outcome;
            Gold = gold;
            Experience = experience;
            Progress = progress;
            CapturedCity = capturedCity;
        }

        public BattleOutcome Outcome { get; }
        public int Gold { get; }

        /// <summary>Total experience shared between the surviving units.</summary>
        public int Experience { get; }

        public IReadOnlyList<UnitProgress> Progress { get; }
        public City? CapturedCity { get; }
    }
}
