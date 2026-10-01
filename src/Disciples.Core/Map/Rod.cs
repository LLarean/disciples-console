using Disciples.Core.Cities;

namespace Disciples.Core.Map
{
    /// <summary>Planted by a rod-bearing leader; claims the land around it for its owner.</summary>
    public sealed class Rod
    {
        public Rod(Position position, Owner owner = Owner.Player)
        {
            Position = position;
            Owner = owner;
        }

        public Position Position { get; }
        public Owner Owner { get; }
    }
}
