using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Thresholds.Queries;

public sealed record GetParticipantThresholdQuery(int Id) : IRequest<Response<ParticipantThresholdDto>>;

public sealed class GetParticipantThresholdQueryHandler(IStorageBroker storage) 
    : IRequestHandler<GetParticipantThresholdQuery, Response<ParticipantThresholdDto>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));

    public async ValueTask<Response<ParticipantThresholdDto>> Handle(
        GetParticipantThresholdQuery request, 
        CancellationToken cancellationToken)
    {
        var threshold = await _storage.ParticipantThresholds
            .AsNoTracking()
            .Where(x => x.Id == request.Id && x.Active)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (threshold == null)
        {
            throw new NotFoundException("Threshold configuration not found");
        }

        return new Response<ParticipantThresholdDto>(threshold);
    }
}
