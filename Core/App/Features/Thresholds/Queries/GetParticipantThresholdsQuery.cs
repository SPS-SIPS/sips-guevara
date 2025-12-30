using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Thresholds.Queries;

[Authorize(Roles = QueryParticipantThreshold)]
public sealed record GetParticipantThresholdsQuery : IRequest<Response<List<ParticipantThresholdDto>>>;

public sealed class GetParticipantThresholdsQueryHandler(IStorageBroker storage) 
    : IRequestHandler<GetParticipantThresholdsQuery, Response<List<ParticipantThresholdDto>>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<List<ParticipantThresholdDto>>> Handle(
        GetParticipantThresholdsQuery request, 
        CancellationToken cancellationToken)
    {
        var thresholds = await _storage.ParticipantThresholds
            .AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.Bic)
            .Select(x => new ParticipantThresholdDto
            {
                Id = x.Id,
                Bic = x.Bic,
                GoldenAmount = x.GoldenAmount,
                AmberPercentage = x.AmberPercentage,
                RedPercentage = x.RedPercentage,
                LastKnownZone = x.LastKnownZone,
                LastKnownBalance = x.LastKnownBalance,
                LastZoneChangeAt = x.LastZoneChangeAt,
                LastAlertSentAt = x.LastAlertSentAt,
                AlertRecipients = x.AlertRecipients
            })
            .ToListAsync(cancellationToken);

        return new Response<List<ParticipantThresholdDto>>(thresholds);
    }
}
