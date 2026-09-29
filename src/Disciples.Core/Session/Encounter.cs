using Disciples.Core.Cities;
using Disciples.Core.Map;
using Disciples.Core.Squads;

namespace Disciples.Core.Session
{
    /// <summary>A hostile squad the party is about to fight: a neutral squad or a city garrison.</summary>
    public sealed class Encounter
    {
        private Encounter(string name, Squad defenders, int reward, NeutralSquad? neutral, City? city)
        {
            Name = name;
            Defenders = defenders;
            Reward = reward;
            Neutral = neutral;
            City = city;
        }

        public string Name { get; }
        public Squad Defenders { get; }
        public int Reward { get; }
        public NeutralSquad? Neutral { get; }
        public City? City { get; }

        public static Encounter With(NeutralSquad neutral) => new Encounter(neutral.Name, neutral.Squad, neutral.Reward, neutral, null);

        public static Encounter With(City city) => new Encounter(city.Name, city.Garrison, 0, null, city);
    }
}
