using SPSS.App.Common;

namespace SPSS.App.Features.History.Queries;
[Authorize]
public sealed record GetHistoryQuery(string? ParticipantId, int? Type, int? Status, DateTime? FromDate, DateTime? ToDate, bool? Sent) : PaginationFilter, IRequest<PagedResponse<HistoryDto>>;

public sealed class GetHistoryQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetHistoryQuery, PagedResponse<HistoryDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<HistoryDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.MonitHistories.AsNoTracking();

        if (request.ParticipantId is not null)
        {
            query = query.Where(x => x.ParticipantId == request.ParticipantId);
        }

        if (request.Type.HasValue)
        {
            var type = Enumeration<int>.FromId<ServiceType>(request.Type.Value);
            query = query.Where(x => x.Type == type);
        }

        if (request.Status.HasValue)
        {
            var status = Enumeration<int>.FromId<ServiceStatus>(request.Status.Value);
            query = query.Where(x => x.Status == status);
        }

        if (request.FromDate is not null && request.ToDate is not null)
        {
            query = query.Where(x => x.Date >= request.FromDate && x.Date <= request.ToDate);
        }

        if (request.Sent is not null)
        {
            query = query.Where(x => x.Sent == request.Sent);
        }

        // order by
        query = query.OrderByDescending(x => x.Date);

        return await PaginationHelper<HistoryDto>.CreateAsync(query.ProjectTo<HistoryDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
