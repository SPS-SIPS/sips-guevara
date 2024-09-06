using SPSS.App.Common;
namespace SPSS.App.Features.Users.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryUser)]
public sealed record GetUsersListQuery(string? Username, string? Email, string? Phone, bool? Active, bool? Deleted) : PaginationFilter, IRequest<PagedResponse<UserDto>>;

public sealed class GetUsersListQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identityService) : IRequestHandler<GetUsersListQuery, PagedResponse<UserDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identityService = identityService;

    public async ValueTask<PagedResponse<UserDto>> Handle(GetUsersListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Users.AsQueryable().AsNoTracking();

        if (!string.IsNullOrEmpty(request.Phone))
        {
            query = query.Where(x => x.Phone.ToLower().Contains(request.Phone.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            query = query.Where(x => x.Username.ToLower().Contains(request.Username.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            query = query.Where(x => x.Email.ToLower().Contains(request.Email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
        }


        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active.Equals(request.Active.Value));
        }

        if (request.Deleted.HasValue)
        {
            query = query.Where(x => x.DeletedOn.HasValue == request.Deleted.Value);
        }

        query = request.Sort switch
        {
            "username_ascend" => query.OrderBy(x => x.Username),
            "username_descend" => query.OrderByDescending(x => x.Username),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<UserDto>.CreateAsync(query.ProjectTo<UserDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
