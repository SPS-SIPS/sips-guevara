using System.Text.Json;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetLatestFailedTransactionComplianceReportQuery() : IRequest<Response<FailedTransactionComplianceReportDto>>;

public sealed class GetLatestFailedTransactionComplianceReportQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetLatestFailedTransactionComplianceReportQuery, Response<FailedTransactionComplianceReportDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<FailedTransactionComplianceReportDto>> Handle(
        GetLatestFailedTransactionComplianceReportQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _storage.FailedTransactionComplianceReports
            .AsNoTracking()
            .OrderByDescending(x => x.WindowStart)
            .Select(x => new
            {
                x.Id,
                x.ReportJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return Response<FailedTransactionComplianceReportDto>.NotFound("No failed transaction compliance report has been generated yet");
        }

        var dto = JsonSerializer.Deserialize<FailedTransactionComplianceReportDto>(report.ReportJson, JsonOptions)
                  ?? new FailedTransactionComplianceReportDto();
        dto.Id = report.Id;

        return new Response<FailedTransactionComplianceReportDto>(dto);
    }
}
