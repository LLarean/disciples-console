using System.Collections.Generic;

namespace Disciples.Core.Content
{
    /// <summary>Retired content ids mapped to their current ids, so old saves and scenarios keep loading after a rename.</summary>
    public sealed class ContentAliases
    {
        public Dictionary<string, string> Units { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Terrains { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Buildings { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Items { get; set; } = new Dictionary<string, string>();
    }
}
