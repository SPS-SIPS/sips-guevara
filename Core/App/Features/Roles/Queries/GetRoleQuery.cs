namespace SPSS.App.Features.Roles.Queries;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = QueryRoles)]
public sealed record GetRoleQuery(Guid Id) : IRequest<Response<RoleDto>>;

public sealed class GetRoleQueryValidator : AbstractValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(e => e.Id).NotNull();
    }
}

public sealed class GetRoleQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetRoleQuery, Response<RoleDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<RoleDto>> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Roles.AsNoTracking().ProjectTo<RoleDto>(_mapper.ConfigurationProvider).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected role!");
        return new Response<RoleDto>(entity);
    }
}
