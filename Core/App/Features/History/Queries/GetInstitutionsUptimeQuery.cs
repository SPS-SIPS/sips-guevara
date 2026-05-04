namespace SPSS.App.Features.History.Queries;

[Authorize]
public sealed record GetInstitutionsUptimeQuery(string? InstitutionBic, DateTime? FromDate, DateTime? ToDate) : IRequest<List<InstitutionUptimeDto>>;

public sealed class GetInstitutionsUptimeQueryHandler(IStorageBroker storage) : IRequestHandler<GetInstitutionsUptimeQuery, List<InstitutionUptimeDto>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<List<InstitutionUptimeDto>> Handle(GetInstitutionsUptimeQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.MonitHistories.AsNoTracking();

        if (!string.IsNullOrEmpty(request.InstitutionBic))
        {
            query = query.Where(x => x.InstitutionBic == request.InstitutionBic);
        }
        if (request.FromDate is not null && request.ToDate is not null)
        {
            query = query.Where(x => x.CheckedAt >= request.FromDate && x.CheckedAt <= request.ToDate);
        }

        var result = await Task.Run(() =>
            query.GroupBy(x => x.InstitutionBic)
                .Select(g => new InstitutionUptimeDto
                {
                    InstitutionBic = g.Key,
                    TotalChecks = g.Count(),
                    SuccessChecks = g.Count(x => x.Success),
                    Uptime = g.Count() == 0 ? 0 : g.Count(x => x.Success) * 100.0 / g.Count()
                })
                .ToList(), cancellationToken);

        return result;
    }
}
