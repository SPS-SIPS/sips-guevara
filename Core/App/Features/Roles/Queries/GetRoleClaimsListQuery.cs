using SPSS.App.Common;

namespace SPSS.App.Features.Roles.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryRoles)]
public sealed record GetRoleClaimsListQuery(Guid Id) : PaginationFilter, IRequest<PagedResponse<ClaimDto>>;

public sealed class GetRoleClaimsListQueryValidator : AbstractValidator<GetRoleClaimsListQuery>
{
    public GetRoleClaimsListQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}
public sealed class GetRoleClaimsListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetRoleClaimsListQuery, PagedResponse<ClaimDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<ClaimDto>> Handle(GetRoleClaimsListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.RoleClaims.AsNoTracking()
        .Where(x => x.RoleId.Equals(request.Id));

        query = request.Sort switch
        {
            "type_ascend" => query.OrderBy(x => x.Claim),
            "type_descend" => query.OrderByDescending(x => x.Claim),
            _ => query.OrderBy(x => x.Claim)
        };
        return await PaginationHelper<ClaimDto>.CreateAsync(query.ProjectTo<ClaimDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
