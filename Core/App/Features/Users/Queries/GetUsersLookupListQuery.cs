namespace SPSS.App.Features.Users.Queries;
[Authorize]
public sealed record GetUsersLookupListQuery() : IRequest<Response<List<Lookup<int>>>>;

public sealed class GetUserLookupListQueryHandler(IStorageBroker storage, IMapper mapper) : IRequestHandler<GetUsersLookupListQuery, Response<List<Lookup<int>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;

    public async ValueTask<Response<List<Lookup<int>>>> Handle(GetUsersLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Users.AsNoTracking().Where(x => !x.DeletedOn.HasValue);

        var response = await query
        .ProjectTo<Lookup<int>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return new Response<List<Lookup<int>>>(response);
    }
}
