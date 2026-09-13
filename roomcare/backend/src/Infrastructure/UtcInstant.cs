using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HotelOS.RoomCare.Infrastructure;

/// <summary>An instant, normalised to UTC on the way into the database; the same instant comes back.</summary>
public sealed class UtcInstant() : ValueConverter<DateTimeOffset, DateTimeOffset>(
    instant => instant.ToUniversalTime(),
    stored => stored);
