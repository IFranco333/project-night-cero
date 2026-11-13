using System.Buffers.Binary;
using System.Text;

namespace NightCero.Core.Rng;

/// <summary>
/// A deterministic random number stream derived from a world seed and a name.
/// </summary>
/// <remarks>
/// Every system draws from its own named stream (<c>"loot"</c>, <c>"zombie-ai"</c>, …), so adding or
/// removing a system never shifts another system's sequence (ADR 0002). The generator is SplitMix64:
/// a 64-bit counter mixed with a seed; it has no hidden state beyond that counter, which makes the
/// sequence trivial to reproduce on every platform.
/// </remarks>
public sealed class RngStream
{
    private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;

    private readonly ulong _counterStart;

    /// <param name="seed">The world seed.</param>
    /// <param name="name">A stable stream name; must not be blank.</param>
    public RngStream(ulong seed, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A random stream needs a non-blank name.", nameof(name));
        }

        Seed = seed;
        Name = name;
        _counterStart = Hashing.InitialCounter(seed, name);
        _counter = 0;
    }

    private ulong _counter;

    /// <summary>The world seed this stream was created from.</summary>
    public ulong Seed { get; }

    /// <summary>The stream name (for example <c>loot</c>).</summary>
    public string Name { get; }

    /// <summary>Derives a child stream. Does not advance this stream.</summary>
    /// <remarks>
    /// The child depends on the parent stream's identity (seed and name), not on how far the parent
    /// has been consumed, so forking never reorders the parent's sequence.
    /// </remarks>
    public RngStream Fork(string name)
    {
        var childSeed = Hashing.Fnv1a64(Encoding.UTF8.GetBytes($"{Seed}:{Name}/{name}"));
        return new RngStream(childSeed, name);
    }

    /// <summary>Returns the next 64 raw bits of the stream.</summary>
    public ulong NextULong()
    {
        unchecked
        {
            _counter += GoldenGamma;
            return Hashing.Mix(_counterStart + _counter);
        }
    }

    /// <summary>Returns an integer in <c>[minInclusive, maxInclusive]</c>.</summary>
    public int NextInt(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            throw new ArgumentException(
                $"maxInclusive ({maxInclusive}) must be greater than or equal to minInclusive ({minInclusive}).",
                nameof(maxInclusive));
        }

        var range = (ulong)((long)maxInclusive - minInclusive + 1);
        var scaled = range == 0 ? NextULong() : NextULong() % range;
        return (int)(minInclusive + (long)scaled);
    }

    /// <summary>Returns a double in <c>[0, 1)</c>.</summary>
    public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

    /// <summary>Fisher–Yates shuffle driven by this stream.</summary>
    public void Shuffle<T>(T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (var i = items.Length - 1; i > 0; i--)
        {
            var j = NextInt(0, i);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static class Hashing
    {
        /// <summary>Initial counter for a stream: the hash of the seed and the stream name.</summary>
        internal static ulong InitialCounter(ulong seed, string name)
        {
            var span = new byte[8 + name.Length * 4];
            BinaryPrimitives.WriteUInt64LittleEndian(span, seed);
            var written = 8 + Encoding.UTF8.GetBytes(name, span.AsSpan(8));
            return Fnv1a64(span.AsSpan(0, written));
        }

        /// <summary>SplitMix64 finalizer: a bijective avalanche of a 64-bit counter.</summary>
        internal static ulong Mix(ulong z)
        {
            unchecked
            {
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        internal static ulong Fnv1a64(ReadOnlySpan<byte> bytes)
        {
            const ulong offsetBasis = 0xCBF29CE484222325UL;
            const ulong prime = 0x100000001B3UL;

            var hash = offsetBasis;
            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= prime;
            }

            return hash;
        }
    }
}
