using System.Collections.Generic;
using System.Linq;

namespace Disciples.Core.Map
{
    /// <summary>Tiles the player has seen; once explored, a tile stays visible.</summary>
    public sealed class FogOfWar
    {
        private const char ExploredMark = '#';
        private const char HiddenMark = '.';

        private readonly bool[,] _explored;

        public FogOfWar(int width, int height)
        {
            _explored = new bool[width, height];
        }

        public bool IsExplored(Position position) => _explored[position.X, position.Y];

        internal void Reveal(Position center, int radius)
        {
            for (var y = center.Y - radius; y <= center.Y + radius; y++)
            for (var x = center.X - radius; x <= center.X + radius; x++)
            {
                var dx = x - center.X;
                var dy = y - center.Y;
                if (x >= 0 && y >= 0 && x < _explored.GetLength(0) && y < _explored.GetLength(1) && dx * dx + dy * dy <= radius * (radius + 1))
                    _explored[x, y] = true;
            }
        }

        internal List<string> ToRows() =>
            Enumerable.Range(0, _explored.GetLength(1))
                .Select(y => new string(Enumerable.Range(0, _explored.GetLength(0)).Select(x => _explored[x, y] ? ExploredMark : HiddenMark).ToArray()))
                .ToList();

        internal static FogOfWar FromRows(int width, int height, IReadOnlyList<string> rows)
        {
            var fog = new FogOfWar(width, height);
            for (var y = 0; y < height && y < rows.Count; y++)
            for (var x = 0; x < width && x < rows[y].Length; x++)
                fog._explored[x, y] = rows[y][x] == ExploredMark;
            return fog;
        }
    }
}
