using SPSS.App.Features.Balance.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public sealed class TransactionAnalyticsController : BaseController
{
    [HttpGet("runs")]
    public async Task<PagedResponse<OracleFailedTransactionAnalyticsRunDto>> GetRuns(
        [FromQuery] GetOracleFailedAnalyticsRunsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("summary")]
    public async Task<Response<OracleFailedTransactionAnalyticsSummaryDto>> GetSummary(
        [FromQuery] GetOracleFailedAnalyticsSummaryQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("institutions")]
    public async Task<PagedResponse<OracleFailedInstitutionAnalyticDto>> GetInstitutions(
        [FromQuery] GetOracleFailedInstitutionAnalyticsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("accounts")]
    public async Task<PagedResponse<OracleFailedAccountAnalyticDto>> GetAccounts(
        [FromQuery] GetOracleFailedAccountAnalyticsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("rejection-types")]
    public async Task<PagedResponse<OracleFailedRejectionTypeAnalyticDto>> GetRejectionTypes(
        [FromQuery] GetOracleFailedRejectionTypeAnalyticsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);

    [HttpGet("trends")]
    public async Task<PagedResponse<OracleFailedTrendAnalyticDto>> GetTrends(
        [FromQuery] GetOracleFailedTrendAnalyticsQuery query,
        CancellationToken ct)
        => await Mediator.Send(query, ct);
}
