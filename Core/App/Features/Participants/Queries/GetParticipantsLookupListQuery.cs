namespace SPSS.App.Features.Participants.Queries;
[Authorize]
public sealed record GetParticipantsLookupListQuery() : IRequest<Response<List<Lookup<string>>>>;

public sealed class GetParticipantLookupListQueryHandler(IStorageBroker storage, IMapper mapper, AppSettings appSettings) : IRequestHandler<GetParticipantsLookupListQuery, Response<List<Lookup<string>>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IMapper _mapper = mapper;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<List<Lookup<string>>>> Handle(GetParticipantsLookupListQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.Participants.AsNoTracking()
            .Where(x => !x.DeletedOn.HasValue && x.Active);

        var response = await query
        .ProjectTo<Lookup<string>>(_mapper.ConfigurationProvider)
        .ToListAsync(cancellationToken);

        return Response<List<Lookup<string>>>.Success(response, _appSettings.CacheInMins ?? 0);
    }
}
