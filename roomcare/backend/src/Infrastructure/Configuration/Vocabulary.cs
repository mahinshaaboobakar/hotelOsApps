using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.RoomCare.Infrastructure.Configuration;

/// <summary>How a closed vocabulary becomes a CHECK constraint, and a typed record becomes jsonb.</summary>
internal static class Vocabulary
{
    /// <summary><c>"column" IN ('A', 'B')</c> — the database refuses a word the domain does not have.</summary>
    /// <remarks>
    /// The column is always quoted: <c>window</c> is a reserved word in
    /// PostgreSQL, and the first migration run refused the constraint that named
    /// it bare. Quoting every one means the next reserved name cannot do it again.
    /// </remarks>
    public static string OneOf(string column, IReadOnlyList<string> values) =>
        $"\"{column}\" IN ({Words(values)})";

    /// <summary>Null or one of the words.</summary>
    public static string NullOrOneOf(string column, IReadOnlyList<string> values) =>
        $"\"{column}\" IS NULL OR \"{column}\" IN ({Words(values)})";

    /// <summary>Every element of a text array is one of the words.</summary>
    public static string EachOneOf(string column, IReadOnlyList<string> values) =>
        $"\"{column}\" IS NULL OR \"{column}\" <@ ARRAY[{Words(values)}]::text[]";

    /// <summary>Store a typed record as jsonb, compared by its serialised form.</summary>
    public static PropertyBuilder<T> AsJson<T>(this PropertyBuilder<T> property)
        where T : class, new()
    {
        property.HasColumnType("jsonb").HasConversion(
            value => JsonSerializer.Serialize(value, Options),
            text => JsonSerializer.Deserialize<T>(text, Options) ?? new T(),
            new ValueComparer<T>(
                (a, b) => JsonSerializer.Serialize(a, Options) == JsonSerializer.Serialize(b, Options),
                value => JsonSerializer.Serialize(value, Options).GetHashCode(),
                value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!));
        return property;
    }

    /// <summary>The same names on disk as on the wire.</summary>
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static string Words(IReadOnlyList<string> values) => string.Join(", ", values.Select(v => $"'{v}'"));
}
