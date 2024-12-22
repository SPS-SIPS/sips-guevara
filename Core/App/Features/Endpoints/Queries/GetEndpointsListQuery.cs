using SPSS.App.Common;
namespace SPSS.App.Features.Endpoints.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryEndpoints)]
public sealed record GetEndpointsListQuery(string? Address, bool? Active, string? ParticipantId) : PaginationFilter, IRequest<PagedResponse<EndpointDto>>;

public sealed class GetEndpointsListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetEndpointsListQuery, PagedResponse<EndpointDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    public async ValueTask<PagedResponse<EndpointDto>> Handle(GetEndpointsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Endpoints.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.Address))
        {
            query = query.Where(x => x.Address.Contains(request.Address));
        }

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active.Equals(request.Active.Value));
        }

        if (!string.IsNullOrEmpty(request.ParticipantId))
        {
            query = query.Where(x => x.ParticipantId.Equals(request.ParticipantId));
        }

        query = request.Sort switch
        {
            "address_ascend" => query.OrderBy(x => x.Address),
            "address_descend" => query.OrderByDescending(x => x.Address),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<EndpointDto>.CreateAsync(query.ProjectTo<EndpointDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
