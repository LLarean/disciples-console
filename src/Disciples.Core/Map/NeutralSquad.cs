using Disciples.Core.Squads;

namespace Disciples.Core.Map
{
    public sealed class NeutralSquad
    {
        public NeutralSquad(string name, Position position, Squad squad, int reward)
        {
            Name = name;
            Position = position;
            Squad = squad;
            Reward = reward;
        }

        public string Name { get; }
        public Position Position { get; }
        public Squad Squad { get; }

        /// <summary>Gold granted on victory.</summary>
        public int Reward { get; }
    }
}
