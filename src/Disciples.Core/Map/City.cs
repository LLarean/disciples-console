namespace Disciples.Core.Map
{
    public sealed class City
    {
        public City(string name, Position position, bool isCapital)
        {
            Name = name;
            Position = position;
            IsCapital = isCapital;
        }

        public string Name { get; }
        public Position Position { get; }
        public bool IsCapital { get; }
    }
}
