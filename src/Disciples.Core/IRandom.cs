using System;

namespace Disciples.Core
{
    public interface IRandom
    {
        /// <returns>Value in [minInclusive, maxExclusive).</returns>
        int Next(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRandom : IRandom
    {
        private readonly Random _random;

        public SystemRandom(int? seed = null)
        {
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
