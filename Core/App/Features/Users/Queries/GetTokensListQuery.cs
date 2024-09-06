using SPSS.App.Common;

namespace SPSS.App.Features.Users.Queries;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryUser)]
public sealed record GetTokensListQuery() : PaginationFilter, IRequest<PagedResponse<TokenDto>>;

public sealed class GetTokensListQueryHandler(IStorageBroker storage, IMapper mapper, IIdentityService identityService) : IRequestHandler<GetTokensListQuery, PagedResponse<TokenDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly IIdentityService _identityService = identityService;

    public async ValueTask<PagedResponse<TokenDto>> Handle(GetTokensListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Tokens.AsNoTracking().Where(x => !x.Used && !x.Invalidated && x.TokenType.Equals(TokenType.Refresh)).Distinct();

        query = request.Sort switch
        {
            "createdOn_ascend" => query.OrderBy(x => x.CreatedOn),
            "createdOn_descend" => query.OrderByDescending(x => x.CreatedOn),
            _ => query.OrderByDescending(x => x.CreatedOn)
        };

        return await PaginationHelper<TokenDto>.CreateAsync(query.ProjectTo<TokenDto>(_mapper.ConfigurationProvider), request, cancellationToken);
    }
}
