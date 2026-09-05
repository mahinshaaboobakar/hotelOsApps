using System.Text.Json;
using HotelOS.Platform;
using HotelOS.Platform.TestSupport;
using HotelOS.Workforce.Application.Abstractions;
using HotelOS.Workforce.Application.Attendance;
using HotelOS.Workforce.Application.Capabilities;
using HotelOS.Workforce.Application.Duties;
using HotelOS.Workforce.Application.Leave;
using HotelOS.Workforce.Application.Periods;
using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Application.Rota;
using HotelOS.Workforce.Application.Shifts;
using HotelOS.Workforce.Application.Swaps;
using HotelOS.Workforce.Application.Teams;
using HotelOS.Workforce.Infrastructure;
using HotelOS.Workforce.Module;
using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.Workforce.Tests;

/// <summary>
/// A module call, served the way the envelope serves one.
/// </summary>
/// <remarks>
/// <para>
/// <b>What this covers, and what it cannot.</b> It builds the container an
/// application's handlers run in, hands a view the same
/// <see cref="ModuleCall"/> the composition root builds, and lets it write to
/// the real database on real migrations. So it covers the projection, the
/// method dispatch, the services and the schema — the whole application half of
/// the path.
/// </para>
/// <para>
/// It does <b>not</b> cover the envelope: the token validation, the capability
/// guard and the status mapping are the platform's and live in
/// <c>ModuleEnvelope</c>. Those cannot run in any application today —
/// <c>MapModuleCapability</c> resolves <c>JwtCallerAuthenticator</c> and the
/// concrete <c>KernelAuthorizer</c>, and an application's own registration
/// (<c>AddHotelOsPlatform</c>) provides neither. That is reported as a redline,
/// and this harness is deliberately not a way around it: it stops exactly where
/// the platform's half begins, so nothing here can be mistaken for evidence
/// about it.
/// </para>
/// </remarks>
public sealed class ModuleHarness
{
    private readonly ServiceProvider _provider;

    /// <summary>Build the container an application's handlers run in.</summary>
    /// <param name="fixture">The suite's database.</param>
    public ModuleHarness(WorkforceFixture fixture)
    {
        Directory = new StaffDirectoryDouble();
        Authorizer = new RecordingAuthorizer();
        Events = new RecordingEventAppender();

        var services = new ServiceCollection();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IStaffDirectory>(Directory);
        services.AddSingleton<IKernelAuthorizer>(Authorizer);
        services.AddSingleton<IEventAppender>(Events);

        // One context for the whole harness rather than one per call. A test
        // that wrote through one scope and read through another would be
        // exercising two connections and calling it a round trip.
        services.AddSingleton(fixture.Context());

        services.AddScoped<PostingService>();
        services.AddScoped<CapabilityService>();
        services.AddScoped<TeamService>();
        services.AddScoped<ShiftCatalogueService>();
        services.AddScoped<PolicyService>();
        services.AddScoped<RotaService>();
        services.AddScoped<OvertimeCheck>();
        services.AddScoped<LeaveService>();
        services.AddScoped<LeaveTypeService>();
        services.AddScoped<SwapProposalService>();
        services.AddScoped<ApproverResolver>();
        services.AddScoped<DutyService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<DayComparison>();
        services.AddScoped<PeriodService>();
        services.AddScoped<PostingAnnouncer>();

        _provider = services.BuildServiceProvider();
    }

    /// <summary>Master Data, as this harness answers for it.</summary>
    public StaffDirectoryDouble Directory { get; }

    /// <summary>Every authorization question the handlers asked.</summary>
    public RecordingAuthorizer Authorizer { get; }

    /// <summary>Every event the handlers appended.</summary>
    public RecordingEventAppender Events { get; }

    /// <summary>Serve one call, and give back the JSON a bundle would receive.</summary>
    /// <param name="answer">The view under test.</param>
    /// <param name="scope">Who is asking, and where.</param>
    /// <param name="method">The application's own verb.</param>
    /// <param name="body">What the bundle sent, as an anonymous object.</param>
    /// <returns>The handler's answer, re-read as JSON.</returns>
    /// <remarks>
    /// <b>Serialised and re-parsed rather than asserted on directly.</b> The
    /// handlers return anonymous objects, and what a bundle actually receives is
    /// their JSON — a field named with the wrong case, or one that serialises to
    /// nothing, is invisible to an assertion against the object and obvious
    /// against the text.
    /// </remarks>
    public async Task<JsonElement> CallAsync(
        Func<ModuleCall, CancellationToken, Task<object?>> answer,
        RequestScope scope,
        string method,
        object? body = null)
    {
        using var scoped = _provider.CreateScope();

        var call = new ModuleCall(
            method,
            body is null ? null : JsonSerializer.SerializeToElement(body),
            scope,
            scoped.ServiceProvider);

        var result = await answer(call, default);

        return JsonSerializer.SerializeToElement(result);
    }

    /// <summary>One of the application's own services, for a test's set-up.</summary>
    /// <typeparam name="T">The service.</typeparam>
    /// <returns>It, from a scope of its own.</returns>
    /// <remarks>
    /// For arranging and for reading back what a handler is not asked to
    /// return — a posting's version, say. Never for the act under test: a row
    /// that reached the database through a service rather than through the view
    /// would prove the service works and say nothing about the surface.
    /// </remarks>
    public T Service<T>() where T : notnull => _provider.GetRequiredService<T>();

    /// <summary>A property of this test's own, with nothing else in it.</summary>
    public static RequestScope Property() => new()
    {
        Caller = CallerKind.User,
        PropertyId = Guid.CreateVersion7(),
        UserId = Guid.CreateVersion7(),
    };
}
