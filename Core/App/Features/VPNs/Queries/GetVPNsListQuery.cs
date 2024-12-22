using SPSS.App.Common;
namespace SPSS.App.Features.VPNs.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryVPNs)]
public sealed record GetVPNsListQuery(string? Gateway, bool? Active, string? ParticipantId) : PaginationFilter, IRequest<PagedResponse<VPNDto>>;

public sealed class GetVPNsListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetVPNsListQuery, PagedResponse<VPNDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    public async ValueTask<PagedResponse<VPNDto>> Handle(GetVPNsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.VPNs.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.Gateway))
        {
            query = query.Where(x => x.Gateway.Contains(request.Gateway));
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
            "gateway_ascend" => query.OrderBy(x => x.Gateway),
            "gateway_descend" => query.OrderByDescending(x => x.Gateway),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<VPNDto>.CreateAsync(query.ProjectTo<VPNDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
