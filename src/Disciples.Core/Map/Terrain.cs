namespace Disciples.Core.Map
{
    public sealed class Terrain
    {
        public Terrain(string id, string name, int? moveCost, char? symbol = null)
        {
            Id = id;
            Name = name;
            MoveCost = moveCost;
            Symbol = symbol;
        }

        public string Id { get; }
        public string Name { get; }
        public int? MoveCost { get; }

        /// <summary>Preferred tile character in saved map rows.</summary>
        public char? Symbol { get; }

        public bool IsPassable => MoveCost.HasValue;
    }
}
