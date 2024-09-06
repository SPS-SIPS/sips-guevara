namespace SPSS.App.Features.Users.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryUser)]
public sealed record GetUserQuery(int Id) : IRequest<Response<UserDto>>;

public sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetUserQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetUserQuery, Response<UserDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Users.AsNoTracking().ProjectTo<UserDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected User!");
        return new Response<UserDto>(entity);
    }
}
