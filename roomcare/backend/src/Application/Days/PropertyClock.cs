using HotelOS.Platform;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Domain;

namespace HotelOS.RoomCare.Application.Days;

/// <summary>Now, at a property: the instant, the business date and the property-local time.</summary>
public sealed class PropertyClock(IHouse house, TimeProvider clock)
{
    /// <summary>The instant, from the injected clock so a test can hold it still.</summary>
    public DateTimeOffset Now => clock.GetUtcNow();

    /// <summary>The property's day at this moment — refused when Master Data cannot say.</summary>
    public async Task<PropertyNow> AtAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await AtAsync(propertyId, Now, cancellationToken);

    /// <summary>The property's day at an instant.</summary>
    public async Task<PropertyNow> AtAsync(Guid propertyId, DateTimeOffset instant, CancellationToken cancellationToken)
    {
        var settings = await house.DaySettingsAsync(propertyId, cancellationToken)
            ?? throw new InvalidRequestException(
                "this property has no time zone and day boundary set up, so its day cannot be worked out");
        var (date, local) = OperatingDay.At(instant, settings.Timezone, settings.Boundary);
        return new PropertyNow(instant, date, TimeOnly.FromDateTime(local), settings);
    }
}

/// <summary>A property's present: the instant, its business date, its local time of day and its settings.</summary>
public sealed record PropertyNow(DateTimeOffset Instant, DateOnly Day, TimeOnly LocalTime, PropertyDaySettings Settings)
{
    /// <summary>The instant a local time falls on, on a given local calendar date.</summary>
    public DateTimeOffset InstantOf(DateOnly date, TimeOnly time) => OperatingDay.Instant(date, time, Settings.Timezone);

    /// <summary>The property-local calendar date right now — not the business date.</summary>
    public DateOnly LocalDate => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Instant, OperatingDay.Zone(Settings.Timezone)).DateTime);
}
