namespace SPSS.App.Features.NSI.Queries;

using SPSS.App.Common;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryNSI)]
public sealed record GetSettlementFileQuery(bool? Processed, DateTimeOffset? CredDtTm) : PaginationFilter, IRequest<PagedResponse<NetSettlementHeaderDto>>;
public sealed class GetSettlementFileQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetSettlementFileQuery, PagedResponse<NetSettlementHeaderDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<NetSettlementHeaderDto>> Handle(GetSettlementFileQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.NetSettlementHeaders.AsNoTracking().AsQueryable();

        if (request.Processed.HasValue)
        {
            query = query.Where(x => x.Processed == request.Processed.Value);
        }

        if (request.CredDtTm.HasValue)
        {
            query = query.Where(x => x.CredDtTm == request.CredDtTm);
        }

        query = request.Sort switch
        {
            "cred_dt_tm_ascend" => query.OrderBy(x => x.CredDtTm),
            "cred_dt_tm_descend" => query.OrderByDescending(x => x.CredDtTm),
            "processed_ascend" => query.OrderBy(x => x.ProcessedOn),
            "processed_descend" => query.OrderByDescending(x => x.ProcessedOn),
            _ => query.OrderByDescending(x => x.CredDtTm)
        };

        return await PaginationHelper<NetSettlementHeaderDto>.CreateAsync(query.ProjectTo<NetSettlementHeaderDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
