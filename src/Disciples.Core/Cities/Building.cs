namespace Disciples.Core.Cities
{
    public sealed class Building
    {
        public Building(string id, string name, string branch, int cost, string description, string? requires)
        {
            Id = id;
            Name = name;
            Branch = branch;
            Cost = cost;
            Description = description;
            Requires = requires;
        }

        public string Id { get; }
        public string Name { get; }
        public string Branch { get; }
        public int Cost { get; }
        public string Description { get; }

        /// <summary>Id of the building that must be built first.</summary>
        public string? Requires { get; }
    }
}
