namespace SPSS.App.Features.VPNs.Queries;
[Authorize]
public sealed record GetVPNsLookupListQuery(string? ParticipantId) : IRequest<Response<List<Lookup<int>>>>;

public sealed class GetVPNLookupListQueryHandler(IStorageBroker storage, IMapper mapper, AppSettings appSettings) : IRequestHandler<GetVPNsLookupListQuery, Response<List<Lookup<int>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<List<Lookup<int>>>> Handle(GetVPNsLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.VPNs.AsNoTracking()
            .Where(x => x.Active);

        if (!string.IsNullOrWhiteSpace(request.ParticipantId))
        {
            query = query.Where(x => x.ParticipantId == request.ParticipantId);
        }

        var response = await query
        .ProjectTo<Lookup<int>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return Response<List<Lookup<int>>>.Success(response, _appSettings.CacheInMins ?? 0);
    }
}
