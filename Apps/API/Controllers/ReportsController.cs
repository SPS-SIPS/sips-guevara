using SPSS.App.Features.Reports.Queries;

namespace SPSS.API.Controllers;

[ApiController]
[Produces("application/json")]
[Route("api/v1/[controller]")]
public class ReportsController : BaseController
{
    [HttpGet("SettlementStatistics/{Id:long}")]
    public async Task<Response<List<BicStatisticsDto>>> SettlementStatistics([FromRoute] GetSettlementBICStatisticsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);
    [HttpGet("FinancialReports")]
    public async Task<Response<CombinedReportDto>> FinancialReports([FromQuery] FinancialReportsQuery query, CancellationToken ct)
        => await Mediator.Send(query, ct);
}
