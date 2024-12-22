namespace SPSS.App.Features.Tunnels.Queries;
[Authorize]
public sealed record GetTunnelsLookupListQuery() : IRequest<Response<List<Lookup<int>>>>;

public sealed class GetTunnelLookupListQueryHandler(IStorageBroker storage, IMapper mapper, AppSettings appSettings) : IRequestHandler<GetTunnelsLookupListQuery, Response<List<Lookup<int>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<List<Lookup<int>>>> Handle(GetTunnelsLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Tunnels.AsNoTracking()
            .Where(x => x.Active);

        var response = await query
        .ProjectTo<Lookup<int>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return Response<List<Lookup<int>>>.Success(response, _appSettings.CacheInMins ?? 0);
    }
}
