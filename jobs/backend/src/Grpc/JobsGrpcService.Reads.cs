using Grpc.Core;
using HotelOS.Jobs.Application.Queries;
using HotelOS.Jobs.Contracts.V1;
using HotelOS.Platform;

namespace HotelOS.Jobs.Grpc;

/// <summary>The reads — the board, one job, presence, the catalogue.</summary>
public partial class JobsGrpcService
{
    public override async Task<ListJobsResponse> ListJobs(ListJobsRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        // CORE-Q13: the platform's paged pair, and the reply echoes the page and
        // the size the service actually applied.
        var asked = r.Paging ?? new HotelOS.Contracts.Common.V1.PagedRequest();
        var (rows, total, applied) = await queries.ListAsync(scope, new JobFilter(
            Blank(r.DepartmentCode)?.ToUpperInvariant(), r.Statuses.ToList(), r.ScheduledOnly,
            ParseOptionalId(r.AssigneeUserId, "assignee_user_id"), asked.PageSize, asked.Page), context.CancellationToken);
        var response = new ListJobsResponse
        {
            Paging = new HotelOS.Contracts.Common.V1.PagedResponse
            {
                Page = Math.Max(0, asked.Page), PageSize = applied, Total = total,
            },
        };
        response.Jobs.AddRange(rows.Select(row => Views.Job(row, scope.UserId)));
        return response;
    }

    public override async Task<JobDetail> GetJob(GetJobRequest r, ServerCallContext context)
    {
        var scope = r.Context.ToScope(CallerContext.Get(context));
        return Views.Detail(await queries.DetailAsync(scope, ParseId(r.Id, "id"), context.CancellationToken), queries.Now, scope.UserId);
    }

    public override async Task<ListPresenceResponse> ListPresence(ListPresenceRequest r, ServerCallContext context)
    {
        var rows = await queries.PresenceAsync(r.Context.ToScope(CallerContext.Get(context)), context.CancellationToken);
        var response = new ListPresenceResponse();
        response.Departments.AddRange(rows.Select(Views.Presence));
        return response;
    }

    public override async Task<ListCatalogueResponse> ListCatalogue(ListCatalogueRequest r, ServerCallContext context)
    {
        var rows = await queries.CatalogueAsync(r.Context.ToScope(CallerContext.Get(context)), context.CancellationToken);
        var aliases = rows.Aliases.ToLookup(a => a.ItemId, a => a.Alias);
        var response = new ListCatalogueResponse();
        response.Categories.AddRange(rows.Categories.Select(Views.Category));
        response.Items.AddRange(rows.Items.Select(i => Views.Item(i, aliases[i.Id])));
        response.Resolutions.AddRange(rows.Resolutions.Select(Views.Resolution));
        return response;
    }
}
