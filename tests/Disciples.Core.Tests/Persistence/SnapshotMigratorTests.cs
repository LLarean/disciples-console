using Disciples.Core.Content;
using Disciples.Core.Persistence;

namespace Disciples.Core.Tests.Persistence;

public class SnapshotMigratorTests
{
    private static readonly SnapshotMigrator Migrator = new(
        s => s.Gold += 1,
        s => s.Gold *= 10);

    [Fact]
    public void Upgrade_RunsEveryStepFromTheSnapshotVersion()
    {
        var oldest = new GameSnapshot { Version = 1, Gold = 1 };
        var newer = new GameSnapshot { Version = 2, Gold = 1 };

        Migrator.Upgrade(oldest);
        Migrator.Upgrade(newer);

        Assert.Equal((3, 20), (oldest.Version, oldest.Gold));
        Assert.Equal((3, 10), (newer.Version, newer.Gold));
    }

    [Fact]
    public void Upgrade_CurrentVersion_ChangesNothing()
    {
        var snapshot = new GameSnapshot { Version = 3, Gold = 1 };

        Migrator.Upgrade(snapshot);

        Assert.Equal((3, 1), (snapshot.Version, snapshot.Gold));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Upgrade_UnknownVersion_Throws(int version)
    {
        Assert.Throws<ContentException>(() => Migrator.Upgrade(new GameSnapshot { Version = version }));
    }

    [Fact]
    public void NewSnapshot_HasTheCurrentVersion()
    {
        Assert.Equal(SnapshotMigrator.Default.CurrentVersion, new GameSnapshot().Version);
    }
}
