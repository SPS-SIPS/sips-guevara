namespace SPSS.App.Features.Endpoints.Queries;
[Authorize]
public sealed record GetEndpointsLookupListQuery() : IRequest<Response<List<Lookup<int>>>>;

public sealed class GetEndpointLookupListQueryHandler(IStorageBroker storage, IMapper mapper, AppSettings appSettings) : IRequestHandler<GetEndpointsLookupListQuery, Response<List<Lookup<int>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<List<Lookup<int>>>> Handle(GetEndpointsLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Endpoints.AsNoTracking()
            .Where(x => x.Active);

        var response = await query
        .ProjectTo<Lookup<int>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return Response<List<Lookup<int>>>.Success(response, _appSettings.CacheInMins ?? 0);
    }
}
