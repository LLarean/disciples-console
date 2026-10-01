using Disciples.Core.Map;
using Disciples.Core.Session;
using Disciples.Core.Units;
using Disciples.Tui.Widgets;

namespace Disciples.Tui.Screens;

public sealed class TrainerScreen(GameSession session, Site trainer) : MenuScreen
{
    protected override IReadOnlyList<string> Heading =>
    [
        trainer.Name,
        $"{session.Gold} gold — a lesson gives a unit the experience it lacks, {session.Rules.TrainerGoldPerExperience}g per point"
    ];

    protected override IReadOnlyList<MenuItem> Items =>
        session.Party.Squad.AliveUnits
            .Select(u => new MenuItem(
                $"{u.Name} L{u.Level} — {session.TrainingCost(u)}g", () => Train(u), CanPay(u), Describe(u)))
            .Append(new MenuItem("Leave", Back))
            .ToList();

    protected override string BackLabel => "leave";

    protected override void Back() => Shell.Pop();

    private bool CanPay(Unit unit) => session.CanTrain(unit) && session.Gold >= session.TrainingCost(unit);

    private string Describe(Unit unit) => session.CanTrain(unit)
        ? $"exp {unit.Experience}/{unit.Definition.ExperienceToLevel}"
        : $"needs {session.Content.BuildingName(unit.Definition.UpgradeBuilding ?? "")} in the capital";

    private void Train(Unit unit)
    {
        var name = unit.Name;
        if (session.Train(trainer, unit) != TrainResult.Trained)
            Show("Not enough gold.", Palette.Bad);
        else if (unit.Name != name)
            Show($"{name} became {unit.Name}.", Palette.Good);
        else
            Show($"{name} reached level {unit.Level}.", Palette.Good);
    }
}
