using Disciples.Core.Content;
using Disciples.Core.Map;
using Disciples.Core.Units;

namespace Disciples.Core.Tests;

internal static class TestUnits
{
    public static readonly UnitDefinition Knight = new("knight", "Knight", 150, 0, 50, 50, 80, AttackType.Melee, UnitSize.Small, 0, leadership: 4);
    public static readonly UnitDefinition Squire = new("squire", "Squire", 100, 0, 50, 25, 80, AttackType.Melee, UnitSize.Small, 50);
    public static readonly UnitDefinition Archer = new("archer", "Archer", 45, 0, 60, 25, 80, AttackType.Ranged, UnitSize.Small, 40);
    public static readonly UnitDefinition Mage = new("mage", "Mage", 35, 0, 40, 15, 100, AttackType.AllEnemies, UnitSize.Small, 60);
    public static readonly UnitDefinition Acolyte = new("acolyte", "Acolyte", 50, 0, 10, 20, 100, AttackType.Heal, UnitSize.Small, 50);
    public static readonly UnitDefinition Ogre = new("ogre", "Ogre", 300, 0, 20, 65, 80, AttackType.Melee, UnitSize.Large, 200);

    public static readonly UnitDefinition Recruit = new("recruit", "Recruit", 50, 0, 50, 20, 80, AttackType.Melee, UnitSize.Small, 30,
        experienceToLevel: 50, experienceValue: 25, levelGrowthPercent: 10, upgradesTo: "veteran", upgradeBuilding: "barracks");
    public static readonly UnitDefinition Veteran = new("veteran", "Veteran", 80, 0, 50, 30, 80, AttackType.Melee, UnitSize.Small, 0,
        experienceToLevel: 100, levelGrowthPercent: 10);

    public static readonly Terrain Plains = new("plains", "Plains", 2);
    public static readonly Terrain Road = new("road", "Road", 1);
    public static readonly Terrain Water = new("water", "Water", null);

    public static GameContent TestContent { get; } = new(
        [Knight, Squire, Archer, Mage, Acolyte, Ogre, Recruit, Veteran],
        [Plains, Road, Water],
        [],
        new GameRules());
}
