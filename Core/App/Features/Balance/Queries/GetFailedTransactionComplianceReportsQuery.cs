using System.Text.Json;
using SPSS.App.Common;

namespace SPSS.App.Features.Balance.Queries;

[Authorize(Roles = QueryTransactions)]
public sealed record GetFailedTransactionComplianceReportsQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Severity) : PaginationFilter, IRequest<PagedResponse<FailedTransactionComplianceReportDto>>;

public sealed class GetFailedTransactionComplianceReportsQueryHandler(IStorageBroker storage)
    : IRequestHandler<GetFailedTransactionComplianceReportsQuery, PagedResponse<FailedTransactionComplianceReportDto>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<PagedResponse<FailedTransactionComplianceReportDto>> Handle(
        GetFailedTransactionComplianceReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _storage.FailedTransactionComplianceReports.AsNoTracking();

        if (request.From.HasValue)
        {
            query = query.Where(x => x.WindowEnd > request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(x => x.WindowStart < request.To.Value);
        }

        if (string.Equals(request.Severity, "HIGH", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.HighRiskFindings > 0);
        }
        else if (string.Equals(request.Severity, "MEDIUM", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.MediumRiskFindings > 0);
        }

        var projected = query
            .OrderByDescending(x => x.WindowStart)
            .Select(x => new ComplianceReportProjection
            {
                Id = x.Id,
                ReportJson = x.ReportJson
            });

        var page = await PaginationHelper<ComplianceReportProjection>.CreateAsync(projected, request, cancellationToken);

        return new PagedResponse<FailedTransactionComplianceReportDto>
        {
            Data = page.Data?.Select(ToDto).ToList() ?? [],
            Index = page.Index,
            Size = page.Size,
            Count = page.Count
        };
    }

    private static FailedTransactionComplianceReportDto ToDto(ComplianceReportProjection projection)
    {
        var dto = JsonSerializer.Deserialize<FailedTransactionComplianceReportDto>(projection.ReportJson, JsonOptions)
                  ?? new FailedTransactionComplianceReportDto();
        dto.Id = projection.Id;
        return dto;
    }

    private sealed class ComplianceReportProjection
    {
        public long Id { get; set; }
        public string ReportJson { get; set; } = "{}";
    }
}
