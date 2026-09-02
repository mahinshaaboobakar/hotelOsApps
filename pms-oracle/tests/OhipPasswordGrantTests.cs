using System.Text;

using PmsOracle.Authentication;
using Xunit;

namespace PmsOracle.Tests;

/// <summary>
/// The token request and the per-call headers, built without a network.
/// </summary>
/// <remarks>
/// Construction is separated from sending precisely so this suite can be
/// exhaustive: the transport (<c>IOhipQueue</c>) is unimplemented, and none of
/// what a tenancy actually refuses a request for is a transport concern.
/// </remarks>
public sealed class OhipPasswordGrantTests
{
    private static OhipCredentials Credentials() => new()
    {
        Endpoint = "https://ohip.example.com",
        HotelCode = "KOCHI01",
        ExternalSystemCode = "HOTELOS",
        ClientId = "hotelos_client",
        PmsUsername = "hotelos_kochi",
        ApplicationKey = "the-app-key",
        ClientSecret = "the-client-secret",
        PmsPassword = "the-password",
    };

    [Fact]
    public void The_body_carries_the_user_and_the_grant_type()
    {
        var form = OhipPasswordGrant.Form(Credentials());

        Assert.Equal(
            [
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", "hotelos_kochi"),
                new KeyValuePair<string, string>("password", "the-password"),
            ],
            form);
    }

    [Fact]
    public void The_client_pair_travels_in_the_header_not_the_body()
    {
        var credentials = Credentials();
        var header = OhipPasswordGrant.BasicAuthorization(credentials);

        Assert.StartsWith("Basic ", header, StringComparison.Ordinal);
        Assert.Equal(
            "hotelos_client:the-client-secret",
            Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..])));

        // OHIP's shape, and the reason the client secret is not simply another
        // form field: the pair authenticates the request, the user's
        // credentials authenticate the person the poll runs as.
        Assert.DoesNotContain(
            OhipPasswordGrant.Form(credentials),
            field => field.Value == "the-client-secret");
    }

    [Fact]
    public void Every_call_carries_the_tenancy_the_property_and_the_integration()
    {
        Assert.Equal(
            [
                new KeyValuePair<string, string>("x-app-key", "the-app-key"),
                new KeyValuePair<string, string>("x-hotelid", "KOCHI01"),
                new KeyValuePair<string, string>("x-externalsystem", "HOTELOS"),
            ],
            OhipRequestHeaders.ForEveryCall(Credentials()));
    }

    [Fact]
    public void The_headers_carry_no_bearer_token()
    {
        // A helper returning both would invite a caller to cache the pair and
        // keep sending an expired token — the reference system's defect that
        // TokenLifetime exists to prevent.
        Assert.DoesNotContain(
            OhipRequestHeaders.ForEveryCall(Credentials()),
            header => header.Key.Equals("authorization", StringComparison.OrdinalIgnoreCase));
    }
}
