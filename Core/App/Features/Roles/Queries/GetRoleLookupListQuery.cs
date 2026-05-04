namespace SPSS.App.Features.Roles.Queries;
[Authorize]
public sealed record GetRoleLookupListQuery() : IRequest<Response<List<Lookup<Guid>>>>;

public sealed class GetRoleLookupListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetRoleLookupListQuery, Response<List<Lookup<Guid>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<List<Lookup<Guid>>>> Handle(GetRoleLookupListQuery request, CancellationToken cancellationToken)
    {
        var response = await _storage.Roles.AsNoTracking()
        .ProjectTo<Lookup<Guid>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return new Response<List<Lookup<Guid>>>(response);
    }
}
