using System;
using System.Collections.Generic;
using Disciples.Core.Content;

namespace Disciples.Core.Persistence
{
    /// <summary>
    /// Upgrades older snapshots step by step; step N turns format version N into N + 1.
    /// Renamed content ids are not a format change: they go to <see cref="ContentAliases"/>.
    /// </summary>
    public sealed class SnapshotMigrator
    {
        public static readonly SnapshotMigrator Default = new SnapshotMigrator(SinglePartyToParties);

        private readonly IReadOnlyList<Action<GameSnapshot>> _steps;

        public SnapshotMigrator(params Action<GameSnapshot>[] steps)
        {
            _steps = steps;
        }

        public int CurrentVersion => _steps.Count + 1;

        public void Upgrade(GameSnapshot snapshot)
        {
            if (snapshot.Version < 1 || snapshot.Version > CurrentVersion)
                throw new ContentException($"Unsupported save version {snapshot.Version}.");

            while (snapshot.Version < CurrentVersion)
            {
                _steps[snapshot.Version - 1](snapshot);
                snapshot.Version++;
            }
        }

        private static void SinglePartyToParties(GameSnapshot snapshot)
        {
            if (snapshot.Party == null)
                return;

            snapshot.Parties.Insert(0, snapshot.Party);
            snapshot.Party = null;
        }
    }
}
