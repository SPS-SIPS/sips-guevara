namespace SPSS.App.Features.Auth.Queries;
[Authorize]
public sealed record GetMeQuery() : IRequest<Response<UserDto>>;

public sealed class GetMeQueryHandler(IStorageBroker storage, IIdentityService identity, IMapper mapper) : IRequestHandler<GetMeQuery, Response<UserDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<UserDto>> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        var id = _identity.Id;
        var Employee = await _storage.Users.Where(x => x.Id.Equals(id))
        .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
        .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Can't find your account info");
        return new Response<UserDto>(Employee);
    }
}