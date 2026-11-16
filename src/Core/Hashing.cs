using System.Text;

namespace NightCero.Core;

/// <summary>
/// FNV-1a 64-bit hashing, shared by the state hash and the world-generation hash.
/// </summary>
/// <remarks>
/// The algorithm is fixed by specification, so the same bytes hash to the same value on every
/// platform and runtime version. It is not cryptographic: it detects change, it does not resist
/// tampering (see <c>docs/SECURITY.md</c>).
/// </remarks>
public static class Hashing
{
    private const ulong OffsetBasis = 0xCBF29CE484222325UL;
    private const ulong Prime = 0x100000001B3UL;

    /// <summary>Hashes a byte sequence.</summary>
    public static ulong Fnv1a64(ReadOnlySpan<byte> bytes)
    {
        var hash = OffsetBasis;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= Prime;
        }

        return hash;
    }

    /// <summary>Hashes the UTF-8 encoding of a string.</summary>
    public static ulong Fnv1a64(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Fnv1a64(Encoding.UTF8.GetBytes(text));
    }
}
