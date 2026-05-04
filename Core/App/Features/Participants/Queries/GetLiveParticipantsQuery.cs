namespace SPSS.App.Features.Participants.Queries;

using static SPSS.Domain.Common.KnownClaims;

[Authorize(Roles = QueryParticipants)]
public sealed record GetLiveParticipantsQuery(bool? IsLive) : IRequest<Response<List<LiveParticipantDto>>>;

public sealed class GetLiveParticipantsQueryHandler(IStorageBroker storage, IIdentityService identity) : IRequestHandler<GetLiveParticipantsQuery, Response<List<LiveParticipantDto>>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;

    public async ValueTask<Response<List<LiveParticipantDto>>> Handle(GetLiveParticipantsQuery request, CancellationToken cancellationToken)
    {
        var query = _storage.ParticipantStatuses.AsNoTracking();

        if (request.IsLive.HasValue)
        {
            query = query.Where(x => x.IsLive == request.IsLive.Value);
        }

        var myBic = _identity.TenantId;

        var statuses = await query
            .GroupJoin(
                _storage.ParticipantThresholds.AsNoTracking().Where(t => t.Active),
                status => status.InstitutionBic,
                threshold => threshold.Bic,
                (status, thresholds) => new { status, threshold = thresholds.FirstOrDefault() })
            .OrderBy(x => x.status.InstitutionName)
            .Select(x => new LiveParticipantDto
            {
                InstitutionBic = x.status.InstitutionBic,
                InstitutionName = x.status.InstitutionName,
                IsLive = x.status.IsLive,
                LastCheckedAt = x.status.LastCheckedAt,
                LastStatusChangeAt = x.status.LastStatusChangeAt,
                ConsecutiveFailures = x.status.ConsecutiveFailures,
                ConsecutiveSuccesses = x.status.ConsecutiveSuccesses,
                LastError = x.status.LastError,
                CurrentBalance = x.threshold != null && x.status.InstitutionBic == myBic ? x.threshold.LastKnownBalance : null,
                CanSend = x.threshold != null ? x.threshold.CanSend : true,
                ThresholdZone = x.threshold != null ? x.threshold.LastKnownZone.ToString() : null
            })
            .ToListAsync(cancellationToken);

        return new Response<List<LiveParticipantDto>>(statuses);
    }
}
