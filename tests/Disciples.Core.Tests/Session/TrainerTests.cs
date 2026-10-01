using Disciples.Core.Cities;
using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Squads;
using Disciples.Core.Units;
using static Disciples.Core.Tests.TestUnits;

namespace Disciples.Core.Tests.Session;

public class TrainerTests
{
    private static readonly Position Yard = new(1, 0);
    private static readonly Building Barracks = new("barracks", "Barracks", "Fighter", 0, "", null);
    private static readonly GameContent Content = new(
        [Knight, Squire, Recruit, Veteran], [Plains], [Barracks], new GameRules { TrainerGoldPerExperience = 2 });

    private static (GameSession Session, Site Trainer, Unit Pupil) SessionAtTheYard(
        UnitDefinition pupil, int gold = 500, bool barracks = true, Position? at = null)
    {
        var trainer = new Site("Yard", SiteKind.Trainer, Yard);
        var capital = new City("Capital", new Position(2, 0), true, 0, buildings: [Barracks], built: barracks ? [Barracks.Id] : []);
        var map = new WorldMap("Test", new[,] { { Plains }, { Plains }, { Plains } }, [capital], sites: [trainer]);
        var party = new Party(new Unit(Knight), at ?? Yard);
        var unit = new Unit(pupil);
        party.Squad.TryAdd(unit);
        return (new GameSession(Content, map, party, gold, new FixedRandom()), trainer, unit);
    }

    [Fact]
    public void Train_PaysForTheMissingExperienceAndUpgrades()
    {
        var (session, trainer, recruit) = SessionAtTheYard(Recruit);

        Assert.Equal(100, session.TrainingCost(recruit));
        Assert.Equal(TrainResult.Trained, session.Train(trainer, recruit));

        Assert.Same(Veteran, recruit.Definition);
        Assert.Equal(400, session.Gold);
        Assert.Contains(session.TakeEvents(), e => e.Kind == GameEventKind.UnitUpgraded);
    }

    [Fact]
    public void Train_TopTierUnit_GainsALevel()
    {
        var (session, trainer, veteran) = SessionAtTheYard(Veteran);

        session.Train(trainer, veteran);

        Assert.Equal(2, veteran.Level);
        Assert.Equal(300, session.Gold);
    }

    [Fact]
    public void Train_WithoutTheUpgradeBuilding_HasNothingToTeach()
    {
        var (session, trainer, recruit) = SessionAtTheYard(Recruit, barracks: false);

        Assert.False(session.CanTrain(recruit));
        Assert.Equal(TrainResult.NothingToLearn, session.Train(trainer, recruit));
        Assert.Equal(500, session.Gold);
    }

    [Fact]
    public void Train_WithoutGold_Fails()
    {
        var (session, trainer, recruit) = SessionAtTheYard(Recruit, gold: 99);

        Assert.Equal(TrainResult.NotEnoughGold, session.Train(trainer, recruit));
        Assert.Equal(0, recruit.Experience);
    }

    [Fact]
    public void Train_AwayFromTheTrainerOrAStranger_IsUnavailable()
    {
        var (away, trainer, recruit) = SessionAtTheYard(Recruit, at: new Position(0, 0));
        var (near, yard, _) = SessionAtTheYard(Recruit);

        Assert.Equal(TrainResult.Unavailable, away.Train(trainer, recruit));
        Assert.Equal(TrainResult.Unavailable, near.Train(yard, new Unit(Squire)));
    }
}
