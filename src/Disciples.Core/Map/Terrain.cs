namespace Disciples.Core.Map
{
    public sealed class Terrain
    {
        public Terrain(string id, string name, int? moveCost)
        {
            Id = id;
            Name = name;
            MoveCost = moveCost;
        }

        public string Id { get; }
        public string Name { get; }
        public int? MoveCost { get; }
        public bool IsPassable => MoveCost.HasValue;
    }
}
