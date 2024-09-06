using SPSS.App.Common;

namespace SPSS.App.Features.Roles.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryRoles)]
public sealed record GetRolesListQuery(string? Name, int? UserId) : PaginationFilter, IRequest<PagedResponse<RoleDto>>;

public sealed class GetRolesListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetRolesListQuery, PagedResponse<RoleDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<RoleDto>> Handle(GetRolesListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Roles.AsQueryable().AsNoTracking();
        if (!string.IsNullOrEmpty(request.Name))
        {
            query = query.Where(x => x.NormalizedName.Contains(request.Name.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
        }
        if (request.UserId.HasValue)
        {
            query = query.Where(x => x.UserRoles.Any(y => y.UserId.Equals(request.UserId.Value)));
        }

        query = request.Sort switch
        {
            "name_ascend" => query.OrderBy(x => x.Name),
            "name_descend" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => x.Id)
        };

        return await PaginationHelper<RoleDto>.CreateAsync(query.ProjectTo<RoleDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
