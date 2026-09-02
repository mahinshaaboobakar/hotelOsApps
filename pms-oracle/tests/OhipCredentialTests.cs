using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The credential set frame 3 draws, read from a stored configuration.
/// </summary>
/// <remarks>
/// The values are spelled out here rather than read from
/// <see cref="OhipCredentials"/>'s own constants. A test asserting on the
/// constant the code under test imports is a tautology: it would keep passing
/// through a rename that broke every stored configuration in the field.
/// </remarks>
public sealed class OhipCredentialTests
{
    private static Dictionary<string, string> Settings() => new()
    {
        ["endpoint"] = "https://ohip.example.com/",
        ["hotelCode"] = "KOCHI01",
        ["externalSystemCode"] = "HOTELOS",
        ["clientId"] = "hotelos_client",
        ["pmsUsername"] = "hotelos_kochi",
    };

    private static Dictionary<string, string> Secrets() => new()
    {
        ["application-key"] = "the-app-key",
        ["client-secret"] = "the-client-secret",
        ["pms-password"] = "the-password",
    };

    [Fact]
    public void A_complete_configuration_reads_as_a_credential_set()
    {
        var reading = OhipCredentials.Read(Settings(), Secrets());

        Assert.True(reading.Complete);
        Assert.True(reading.TryGet(out var credentials));

        Assert.Equal("KOCHI01", credentials.HotelCode);
        Assert.Equal("HOTELOS", credentials.ExternalSystemCode);
        Assert.Equal("hotelos_client", credentials.ClientId);
        Assert.Equal("hotelos_kochi", credentials.PmsUsername);
        Assert.Equal("the-app-key", credentials.ApplicationKey);
    }

    [Fact]
    public void The_endpoint_loses_its_trailing_slash()
    {
        Assert.True(OhipCredentials.Read(Settings(), Secrets()).TryGet(out var credentials));

        // Every path is appended to this, so a stored trailing slash would
        // produce `//oauth/v1/tokens` — which some gateways route and some
        // reject, making it the kind of defect that works in one tenancy.
        Assert.Equal("https://ohip.example.com", credentials.Endpoint);
        Assert.Equal("https://ohip.example.com/oauth/v1/tokens",
            OhipPasswordGrant.TokenEndpoint(credentials));
    }

    [Theory]
    [InlineData("externalSystemCode")]
    [InlineData("clientId")]
    [InlineData("pmsUsername")]
    public void An_absent_setting_is_named_rather_than_defaulted(string absent)
    {
        var settings = Settings();
        settings.Remove(absent);

        var reading = OhipCredentials.Read(settings, Secrets());

        Assert.False(reading.Complete);
        Assert.Equal([absent], reading.Missing);
        Assert.False(reading.TryGet(out _));
    }

    [Theory]
    [InlineData("application-key")]
    [InlineData("pms-password")]
    [InlineData("client-secret")]
    public void An_absent_secret_is_named_too(string absent)
    {
        var secrets = Secrets();
        secrets.Remove(absent);

        Assert.Equal([absent], OhipCredentials.Read(Settings(), secrets).Missing);
    }

    [Fact]
    public void A_blank_value_is_absent()
    {
        var settings = Settings();
        settings["pmsUsername"] = "   ";

        // Present-but-empty would produce a 401 at poll time, half an hour
        // after the person who could have fixed it left the screen.
        Assert.Equal(["pmsUsername"], OhipCredentials.Read(settings, Secrets()).Missing);
    }

    [Fact]
    public void Everything_absent_is_named_settings_first_in_the_drawn_order()
    {
        var reading = OhipCredentials.Read(
            new Dictionary<string, string>(), new Dictionary<string, string>());

        // The operator works down the screen rather than hunting, so the order
        // is the form's: settings in drawn order, then secrets.
        Assert.Equal(
            [
                "endpoint", "hotelCode", "externalSystemCode", "clientId", "pmsUsername",
                "application-key", "pms-password", "client-secret",
            ],
            reading.Missing);
    }

    [Fact]
    public void An_incomplete_reading_must_name_something()
    {
        // Otherwise a complete configuration could be reported broken with no
        // way to see why.
        Assert.Throws<ArgumentException>(() => OhipCredentialReading.Incomplete([]));
    }
}
