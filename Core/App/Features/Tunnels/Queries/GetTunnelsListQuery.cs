using SPSS.App.Common;
namespace SPSS.App.Features.Tunnels.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryTunnels)]
public sealed record GetTunnelsListQuery(string? Endpoint, bool? Active, int? VPNId, string? ParticipantId) : PaginationFilter, IRequest<PagedResponse<TunnelDto>>;

public sealed class GetTunnelsListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetTunnelsListQuery, PagedResponse<TunnelDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    public async ValueTask<PagedResponse<TunnelDto>> Handle(GetTunnelsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Tunnels.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.Endpoint))
        {
            query = query.Where(x => x.Endpoint.Contains(request.Endpoint));
        }

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active.Equals(request.Active.Value));
        }

        if (request.VPNId.HasValue)
        {
            query = query.Where(x => x.VPNId.Equals(request.VPNId.Value));
        }

        if (!string.IsNullOrEmpty(request.ParticipantId))
        {
            query = query.Where(x => x.VPN.ParticipantId.Contains(request.ParticipantId));
        }

        query = request.Sort switch
        {
            "endpoint_ascend" => query.OrderBy(x => x.Endpoint),
            "endpoint_descend" => query.OrderByDescending(x => x.Endpoint),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<TunnelDto>.CreateAsync(query.ProjectTo<TunnelDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
