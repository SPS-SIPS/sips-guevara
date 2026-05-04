using SPSS.App.Common;

namespace SPSS.App.Features.Roles.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryRoles)]
public sealed record GetRoleUsersListQuery(Guid Id, string Username, string Phone) : PaginationFilter, IRequest<PagedResponse<UserDto>>;

public sealed class GetRoleUsersListQueryValidator : AbstractValidator<GetRoleUsersListQuery>
{
    public GetRoleUsersListQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}
public sealed class GetRoleUsersListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetRoleUsersListQuery, PagedResponse<UserDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<PagedResponse<UserDto>> Handle(GetRoleUsersListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.UserRoles.AsNoTracking()
        .Where(x => x.RoleId.Equals(request.Id));

        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(x => x.User.Username.Contains(request.Username));
        }

        if (!string.IsNullOrEmpty(request.Phone))
        {
            query = query.Where(x => x.User.Phone.Contains(request.Phone));
        }

        query = request.Sort switch
        {
            "username_ascend" => query.OrderBy(x => x.User.Username),
            "username_descend" => query.OrderByDescending(x => x.User.Username),
            _ => query.OrderBy(x => x.User.Id)
        };
        return await PaginationHelper<UserDto>.CreateAsync(query.ProjectTo<UserDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
