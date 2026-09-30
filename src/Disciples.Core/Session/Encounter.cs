using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Squads;

namespace Disciples.Core.Session
{
    /// <summary>A hostile squad the party is about to fight: a neutral squad, an enemy leader's squad or a city garrison.</summary>
    public sealed class Encounter
    {
        private Encounter(string name, Squad defenders, int reward, NeutralSquad? neutral, Party? enemy, City? city)
        {
            Name = name;
            Defenders = defenders;
            Reward = reward;
            Neutral = neutral;
            Enemy = enemy;
            City = city;
        }

        public string Name { get; }
        public Squad Defenders { get; }
        public int Reward { get; }
        public NeutralSquad? Neutral { get; }
        public Party? Enemy { get; }
        public City? City { get; }

        public static Encounter With(NeutralSquad neutral) => new Encounter(neutral.Name, neutral.Squad, neutral.Reward, neutral, null, null);

        public static Encounter With(Party enemy) => new Encounter(enemy.Name, enemy.Squad, 0, null, enemy, null);

        public static Encounter With(City city) => new Encounter(city.Name, city.Garrison, 0, null, null, city);
    }
}
