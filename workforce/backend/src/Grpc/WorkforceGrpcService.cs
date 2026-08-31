using HotelOS.Workforce.Application.Postings;
using HotelOS.Workforce.Contracts.V1;

namespace HotelOS.Workforce.Grpc;

/// <summary>
/// The composition root of this application's gRPC surface.
/// </summary>
/// <remarks>
/// <para>
/// <b>It holds no subject</b> — ADR 0042: a module's composition root composes
/// and nothing else. The RPCs live in one partial per subject beside this file,
/// which is what stops the first topic implemented from also becoming the home
/// for everything shared. Master Data's own root is 75 code lines with zero
/// RPCs for the same reason, and it got there by having a topic removed.
/// </para>
/// <para>
/// Slice 1 has one subject, and this file is still here rather than being
/// merged into it. The alternative — putting postings in the root because they
/// are the only thing so far — is exactly how the pattern erodes: the second
/// subject arrives, the first is already in the wrong place, and moving it is
/// then a refactor nobody schedules.
/// </para>
/// <para>
/// <b>Every RPC is authorized by the Kernel</b>, inside the application service
/// rather than here. This layer maps wire messages to commands and back;
/// business decisions, including who may make them, belong one layer in —
/// CLAUDE.md §"No business logic in API routes".
/// </para>
/// </remarks>
public partial class WorkforceGrpcService(PostingService postings)
    : WorkforceService.WorkforceServiceBase
{
    private readonly PostingService postings = postings;
}
