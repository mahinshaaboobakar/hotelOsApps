using System.Text.Json;
using HotelOS.GuestOps.Domain;
using HotelOS.GuestOps.Module;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelOS.GuestOps.Tests;

/// <summary>
/// The door onto this application's own configuration — ADR 0356's `GUEST-Q15`.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written because the write had no module door at all.</b> It was served
/// over gRPC and by no method the pane could call, so no property could store a
/// settings row through the desk — and <c>MintCardNumber</c> refuses to number a
/// registration card until one exists. <c>SettingsServiceTests</c> prove the
/// service; these prove a caller can reach it.
/// </para>
/// <para>
/// <b>The fixture sends a configuration the DEFAULTS would not produce</b>, so a
/// door that ignored the body and stored defaults would fail rather than pass:
/// <c>AE</c> rather than the declared home country, <c>every_guest</c> rather
/// than from-outside, and a prefix of its own.
/// </para>
/// </remarks>
public sealed class SettingsCommandTests
{
    [Fact]
    public async Task A_first_save_stores_the_row_and_the_values_sent()
    {
        await using var harness = await InboundHarness.CreateAsync();

        var answer = await Save(harness, version: 0);

        Assert.Equal("1", Read(answer, "version"));
        Assert.Equal("True", Read(answer, "stored"));
        Assert.Equal("AE", Read(answer, "homeCountry"));
        Assert.Equal("every_guest", Read(answer, "reportingAppliesTo"));

        harness.Db.ChangeTracker.Clear();
        var row = await harness.Db.Settings.SingleAsync();
        Assert.Equal("AE", row.HomeCountry);
        Assert.Equal(ReportingScope.EveryGuest, row.ReportingAppliesTo);
        Assert.Equal("DXB/2026/", row.CardNumberPrefix);
    }

    /// <summary>The version the caller read is what decides the write.</summary>
    /// <remarks>
    /// <c>SaveAsync</c> owns the rule; this proves the door passes the caller's
    /// version through rather than reading the row's own, which would make every
    /// save succeed and lose whichever edit arrived second.
    /// </remarks>
    [Fact]
    public async Task A_stale_version_is_refused_rather_than_overwriting()
    {
        await using var harness = await InboundHarness.CreateAsync();
        await Save(harness, version: 0);

        await Assert.ThrowsAsync<HotelOS.Platform.ConcurrencyException>(
            () => Save(harness, version: 0));
    }

    [Fact]
    public async Task An_absent_version_is_refused_by_name()
    {
        // Defaulting it to 0 would refuse an existing row with a concurrency
        // error naming a version nobody sent — three layers from the omission.
        await using var harness = await InboundHarness.CreateAsync();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Run(harness, """{"homeCountry":"AE"}"""));

        Assert.Contains("the version the configuration was read at", refused.Message);
    }

    [Fact]
    public async Task A_reporting_scope_this_application_does_not_have_is_refused_by_name()
    {
        // Defaulting it would quietly narrow a property's statutory obligation,
        // and the property would not find out until an inspection.
        await using var harness = await InboundHarness.CreateAsync();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Save(harness, version: 0, appliesTo: "everyone"));

        Assert.Contains("from_outside or every_guest", refused.Message);
        Assert.Empty(await harness.Db.Settings.ToListAsync());
    }

    [Fact]
    public async Task An_absent_flag_is_refused_rather_than_read_as_false()
    {
        // `signatureRequired` missing and read as false would silently drop a
        // requirement the property had set, with nothing on screen saying so.
        await using var harness = await InboundHarness.CreateAsync();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Run(harness, """
                {"version":0,"homeCountry":"AE","requiredForHomeCountry":[],
                 "requiredForVisitors":[],"acceptedIdTypes":[],
                 "printOnCheckIn":true,"cardNumberPrefix":"DXB/2026/",
                 "reportingRequired":true,"reportingAppliesTo":"every_guest",
                 "reportingDueHours":24}
                """));

        Assert.Contains("signatureRequired", refused.Message);
    }

    [Fact]
    public async Task An_absent_list_is_refused_rather_than_read_as_empty()
    {
        // An EMPTY required-set means "this property demands nothing", which is a
        // decision. An absent one means the caller did not say. Two values.
        await using var harness = await InboundHarness.CreateAsync();

        var refused = await Assert.ThrowsAsync<HotelOS.Platform.InvalidRequestException>(
            () => Run(harness, """
                {"version":0,"homeCountry":"AE","requiredForVisitors":[],
                 "acceptedIdTypes":[],"signatureRequired":true,"printOnCheckIn":true,
                 "cardNumberPrefix":"DXB/2026/","reportingRequired":true,
                 "reportingAppliesTo":"every_guest","reportingDueHours":24}
                """));

        Assert.Contains("requiredForHomeCountry as a list", refused.Message);
    }

    /// <summary>A configuration the declared defaults would not produce.</summary>
    private static Task<object?> Save(
        InboundHarness harness, long version, string appliesTo = "every_guest")
        => Run(harness, $$"""
            {"version":{{version}},"homeCountry":"AE",
             "requiredForHomeCountry":["name","address"],
             "requiredForVisitors":["name","address","passport"],
             "acceptedIdTypes":["Passport","Emirates ID"],
             "signatureRequired":true,"printOnCheckIn":false,
             "cardNumberPrefix":"DXB/2026/","reportingRequired":true,
             "reportingAppliesTo":"{{appliesTo}}","reportingDueHours":12}
            """);

    private static Task<object?> Run(InboundHarness harness, string body)
        => new SettingsCommand(harness.Settings).RunAsync(
            harness.Scope(), JsonDocument.Parse(body).RootElement, CancellationToken.None);

    private static string? Read(object? answer, string name)
        => answer?.GetType().GetProperty(name)?.GetValue(answer)?.ToString();
}
