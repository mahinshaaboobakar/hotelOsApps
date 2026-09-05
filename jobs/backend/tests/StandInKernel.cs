using Grpc.Core;
using HotelOS.Contracts.Kernel.V1;

namespace HotelOS.Jobs.Tests;

/// <summary>
/// A Kernel that answers <c>Authorize</c>, for the rounds where the real one is
/// not running.
/// </summary>
/// <remarks>
/// <para>
/// <b>The client, not the decision logic.</b> The platform's own
/// <c>KernelAuthorizer</c> asks the question with the envelope ADR 0014
/// requires — this service as the caller, the person as the subject — reads the
/// answer, and fails closed when the call throws. All of that is real. What
/// this supplies is the reply, from a set the test states.
/// </para>
/// <para>
/// <b>Why the client rather than a server on a socket.</b> The platform SDK
/// compiles <c>kernel.proto</c> with <c>GrpcServices="Client"</c>, so no server
/// base exists to implement — and rightly: the Kernel is Rust, and nothing in
/// .NET serves this contract on a property. Compiling the proto again in this
/// suite to obtain one would put the same messages in two assemblies, which is
/// the ambiguous-reference error the SDK's own csproj warns about. So the seam
/// is the generated client's virtual methods, which is where a stopped Kernel
/// can be stood in for without inventing a second wire.
/// </para>
/// <para>
/// It answers a <b>permission</b> and never an object: the property-scoped
/// guard is what the envelope asks, and a stand-in that pretended to know about
/// rooms would be a second authorization model beside the real one.
/// </para>
/// </remarks>
public sealed class StandInKernel(Func<string, bool> allows) : KernelService.KernelServiceClient
{
    /// <summary>Every question this stand-in was asked — a test reads it back.</summary>
    public List<string> Asked { get; } = [];

    public override AsyncUnaryCall<AuthorizeResponse> AuthorizeAsync(
        AuthorizeRequest request,
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        Asked.Add(request.Permission);
        return Answer(Decide(request.Permission));
    }

    public override AsyncUnaryCall<AuthorizeBatchResponse> AuthorizeBatchAsync(
        AuthorizeBatchRequest request,
        Metadata? headers = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default)
    {
        var response = new AuthorizeBatchResponse();
        foreach (var check in request.Checks)
        {
            Asked.Add(check.Permission);
            response.Results.Add(Decide(check.Permission));
        }

        return Answer(response);
    }

    private AuthorizeResponse Decide(string permission) => new()
    {
        Allowed = allows(permission),
        Reason = allows(permission) ? string.Empty : $"{permission} is not granted at this property",
    };

    private static AsyncUnaryCall<T> Answer<T>(T value) => new(
        Task.FromResult(value),
        Task.FromResult(new Metadata()),
        () => Status.DefaultSuccess,
        () => [],
        () => { });
}
