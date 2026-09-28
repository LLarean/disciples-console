namespace Disciples.Core.Tests;

/// <summary>Returns the given value clamped to the requested range: 0 means every attack hits with no spread.</summary>
internal sealed class FixedRandom(int value = 0) : IRandom
{
    public int Next(int minInclusive, int maxExclusive) => Math.Clamp(value, minInclusive, maxExclusive - 1);
}
