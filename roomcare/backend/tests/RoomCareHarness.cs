using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.RoomCare.Application;
using HotelOS.RoomCare.Application.Abstractions;
using HotelOS.RoomCare.Domain;
using HotelOS.RoomCare.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace HotelOS.RoomCare.Tests;

/// <summary>Every Room Care service wired as the host wires it, over the scratch database, a held clock and a stand-in house.</summary>
/// <remarks>
/// The appender is the SDK's real one, so every event lands in the platform's
/// real store — the unique (aggregate, version) constraint and the snake_case
/// payload are both exercised, not assumed. Each harness gets a property of its
/// own; tests share a database, never a property.
/// </remarks>
public sealed class RoomCareHarness
{
    private readonly ServiceProvider _root;

    public RoomCareHarness(RoomCareFixture fixture, DateTimeOffset? now = null)
    {
        Fixture = fixture;
        Clock = new FrozenClock(now ?? Saturday(9, 12));
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<RoomCareDbContext>(options => options
            .UseSnakeCaseNamingConvention()
            .UseNpgsql(fixture.ApplicationConnection, npgsql => npgsql.MigrationsHistoryTable("__migrations", RoomCareDbContext.Schema)));
        services.AddRoomCareApplication();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IHouse>(House);
        services.AddSingleton<IKernelAuthorizer>(Authorizer);
        services.AddSingleton(new ServiceIdentity("roomcare"));
        services.AddScoped<IEventAppender>(p => new EventAppender(
            p.GetRequiredService<RoomCareDbContext>(), p.GetRequiredService<TimeProvider>(), p.GetRequiredService<ServiceIdentity>()));
        Module.RoomCareModule.AddRoomCareModule(services);
        _root = services.BuildServiceProvider();
    }

    public RoomCareFixture Fixture { get; }

    public FrozenClock Clock { get; }

    public HouseDouble House { get; } = new();

    public RecordingAuthorizer Authorizer { get; } = new();

    public Guid PropertyId { get; } = Guid.CreateVersion7();

    /// <summary>Saturday 5 September 2026 at a Kolkata local time — the frames' morning.</summary>
    public static DateTimeOffset Saturday(int hour, int minute) => new(2026, 9, 5, hour, minute, 0, TimeSpan.FromHours(5.5));

    /// <summary>A person's scope at this harness's property.</summary>
    public RequestScope As(Guid person) => new() { Caller = CallerKind.User, PropertyId = PropertyId, UserId = person };

    /// <summary>The tick's scope — Room Care's own identity, no user.</summary>
    public RequestScope Tick => RequestScope.ForBackgroundWork(new ServiceIdentity("roomcare"), PropertyId);

    /// <summary>A fresh call scope — its own context, as a module call or a consumer delivery has.</summary>
    public T Get<T>()
        where T : notnull => _root.CreateScope().ServiceProvider.GetRequiredService<T>();

    /// <summary>Run work in one call scope and hand back its result.</summary>
    public async Task<TResult> InScopeAsync<TResult>(Func<IServiceProvider, Task<TResult>> work)
    {
        await using var scope = _root.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public RoomCareDbContext Db() => Fixture.Context();

    /// <summary>Put a room's state straight into the table, as an earlier day would have left it.</summary>
    public async Task<RoomState> SeedStateAsync(Guid roomId, Action<RoomState> shape)
    {
        await using var db = Db();
        var at = Clock.GetUtcNow().AddHours(-6);
        var state = new RoomState
        {
            RoomId = roomId, PropertyId = PropertyId, ConditionSetAt = at, CreatedAt = at, UpdatedAt = at, Version = 1,
            Occupancy = Occupancy.Occupied, StayStatuses = [StayStatus.CheckedIn],
        };
        shape(state);
        db.RoomStates.Add(state);
        await db.SaveChangesAsync();
        return state;
    }

    /// <summary>A Workforce posting to Housekeeping, as <c>user.posted</c> would have left it.</summary>
    public async Task<Guid> PostAsync(string name)
    {
        var person = Guid.CreateVersion7();
        House.Names[person] = name;
        await using var db = Db();
        db.Postings.Add(new PostingSeen
        {
            PostingId = Guid.CreateVersion7(), PropertyId = PropertyId, UserId = person, StaffId = Guid.CreateVersion7(),
            DepartmentId = House.Housekeeping, DepartmentCode = "HK", PostedAt = Clock.GetUtcNow().AddDays(-30),
        });
        await db.SaveChangesAsync();
        return person;
    }

    /// <summary>What an aggregate announced, in the platform's store, in version order.</summary>
    public async Task<IReadOnlyList<Announced>> EventsAsync(Guid aggregateId)
    {
        await using var connection = new NpgsqlConnection(Fixture.ApplicationConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT event_type, aggregate_type, entity_version, payload::text, correlation_id FROM event_store.events WHERE aggregate_id = @id ORDER BY entity_version";
        command.Parameters.AddWithValue("id", aggregateId);
        var found = new List<Announced>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            found.Add(new Announced(reader.GetString(0), reader.GetString(1), reader.GetInt64(2),
                JsonDocument.Parse(reader.GetString(3)).RootElement.Clone(), reader.GetString(4)));
        }

        return found;
    }
}

/// <summary>One stored event, as a test reads it back.</summary>
public sealed record Announced(string Type, string Aggregate, long Version, JsonElement Payload, string CorrelationId);

/// <summary>A clock a test moves by hand.</summary>
public sealed class FrozenClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;

    public void Set(DateTimeOffset to) => _now = to;
}
