using SPSS.App.Common;

namespace SPSS.App.Features.History.Queries;
[Authorize]
public sealed record GetHistoryQuery(string? Institution, int? Type, int? Status, DateTime? FromDate, DateTime? ToDate, bool? Sent) : PaginationFilter, IRequest<PagedResponse<HistoryDto>>;

public sealed class GetHistoryQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetHistoryQuery, PagedResponse<HistoryDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<HistoryDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.MonitHistories.AsNoTracking();

        if (request.Institution is not null)
        {
            query = query.Where(x => x.InstitutionBic == request.Institution);
        }

        if (request.FromDate is not null && request.ToDate is not null)
        {
            query = query.Where(x => x.CheckedAt >= request.FromDate && x.CheckedAt <= request.ToDate);
        }

        return await PaginationHelper<HistoryDto>.CreateAsync(query.ProjectTo<HistoryDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
