namespace SPSS.App.Features.Participants.Queries;

public sealed record GetLiveParticipantsQuery(bool? IsLive) : IRequest<Response<List<LiveParticipantDto>>>;

public sealed class GetLiveParticipantsQueryHandler(IStorageBroker storage) : IRequestHandler<GetLiveParticipantsQuery, Response<List<LiveParticipantDto>>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<List<LiveParticipantDto>>> Handle(GetLiveParticipantsQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.ParticipantStatuses.AsNoTracking();

        if (request.IsLive.HasValue)
        {
            query = query.Where(x => x.IsLive == request.IsLive.Value);
        }

        var statuses = await query
            .OrderBy(x => x.InstitutionName)
            .Select(x => new LiveParticipantDto
            {
                InstitutionBic = x.InstitutionBic,
                InstitutionName = x.InstitutionName,
                IsLive = x.IsLive,
                LastCheckedAt = x.LastCheckedAt,
                LastStatusChangeAt = x.LastStatusChangeAt,
                ConsecutiveFailures = x.ConsecutiveFailures,
                ConsecutiveSuccesses = x.ConsecutiveSuccesses,
                LastError = x.LastError
            })
            .ToListAsync(cancellationToken);

        return new Response<List<LiveParticipantDto>>(statuses);
    }
}
