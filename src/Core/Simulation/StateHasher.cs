using System.Globalization;
using System.Reflection;
using System.Text;
using NightCero.Core.Ecs;

namespace NightCero.Core.Simulation;

/// <summary>
/// Produces a stable hash and canonical text of a world's state.
/// </summary>
/// <remarks>
/// <para>
/// The hash covers the seed, the tick, entity liveness and every component value. Floats are
/// quantized to <see cref="Precision"/> decimal places before hashing, so tiny differences in the
/// last bits of a float (different platforms, different math libraries) do not change a replay's
/// hash (ADR 0002); the canonical text is also what the save checksum is computed over.
/// </para>
/// <para>
/// Order is fixed: components are sorted by type name, rows by entity id, properties by field
/// name. Nothing here depends on hash-map order or on the clock.
/// </para>
/// </remarks>
public static class StateHasher
{
    /// <summary>Decimal places kept when hashing floats.</summary>
    public const int Precision = 4;

    /// <summary>
    /// FNV-1a 64-bit hash of the canonical state text plus the liveness of every entity id, so a
    /// despawned entity never hashes the same as a spawned one that lost its components.
    /// </summary>
    public static ulong Hash(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var bytes = Encoding.UTF8.GetBytes(Canonical(world));
        var hash = Hashing.Fnv1a64(bytes);
        for (uint id = 1; id <= world.HighestEntityId; id++)
        {
            hash = Hashing.Fnv1a64(BitConverter.GetBytes(unchecked(hash ^ (world.IsAlive(new Entity(id)) ? 1UL : 0UL))));
        }

        return hash;
    }

    /// <summary>The canonical, human-diffable text of a world's state.</summary>
    public static string Canonical(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var text = new StringBuilder();
        text.Append("seed=").Append(world.Seed.ToString(CultureInfo.InvariantCulture)).Append('\n');
        text.Append("tick=").Append(world.TickCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (var (type, rows) in world.Columns())
        {
            text.Append("component=").Append(type.FullName).Append('\n');
            foreach (var (entity, value) in rows)
            {
                text.Append("e").Append(entity.Value.ToString(CultureInfo.InvariantCulture)).Append(' ');
                WriteValue(text, value);
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    private static void WriteValue(StringBuilder text, object value)
    {
        switch (value)
        {
            case float f:
                text.Append(Quantize(f).ToString(CultureInfo.InvariantCulture));
                return;
            case double d:
                text.Append(Quantize(d).ToString(CultureInfo.InvariantCulture));
                return;
            case Entity entity:
                text.Append(entity.Value.ToString(CultureInfo.InvariantCulture));
                return;
            case bool flag:
                text.Append(flag ? '1' : '0');
                return;
            case string text2:
                text.Append(text2);
                return;
            case Enum enumValue:
                text.Append(Convert.ToInt64(enumValue, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
                return;
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                text.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
        }

        // Value-type records: write the readable value, then the public fields in name order.
        text.Append('|');
        foreach (var property in value.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            text.Append(property.Name).Append('=');
            WriteValue(text, property.GetValue(value)!);
            text.Append(';');
        }
    }

    private static decimal Quantize(float value) =>
        System.Math.Round((decimal)value, Precision, MidpointRounding.ToEven) + 0m;

    private static decimal Quantize(double value) =>
        System.Math.Round((decimal)value, Precision, MidpointRounding.ToEven) + 0m;
}
