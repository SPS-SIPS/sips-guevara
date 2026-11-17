using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Enums;

namespace SPSS.App.Features.Participants.Queries;

[Authorize]
public sealed record GetParticipantBalanceStatusQuery() : IRequest<Response<ParticipantBalanceStatusDto>>;

public sealed class GetParticipantBalanceStatusQueryHandler(IStorageBroker storage, IIdentityService identity, ILogger<GetParticipantBalanceStatusQueryHandler> logger) 
    : IRequestHandler<GetParticipantBalanceStatusQuery, Response<ParticipantBalanceStatusDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly ILogger<GetParticipantBalanceStatusQueryHandler> _logger = logger;

    public async ValueTask<Response<ParticipantBalanceStatusDto>> Handle(
        GetParticipantBalanceStatusQuery request, 
        CancellationToken cancellationToken)
    {
        var myBic = _identity.TenantId;

        _logger.LogInformation(
            "Balance status inquiry received from BIC: {Bic}, Username: {Username}, Name: {Name}, UserId: {UserId}",
            myBic ?? "(not authenticated)",
            _identity.Username ?? "(anonymous)",
            _identity.Name ?? "(anonymous)",
            _identity.Id);

        if (string.IsNullOrWhiteSpace(myBic))
            throw new ForbiddenAccessException("Authentication required. Please provide a valid JWT token with tenant information.");

        var threshold = await _storage.ParticipantThresholds
            .AsNoTracking()
            .Where(x => x.Bic == myBic && x.Active)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException($"No threshold configuration found for BIC {myBic}");
        var participant = await _storage.Participants
            .AsNoTracking()
            .Where(x => x.BicNo == myBic && x.Active)
            .FirstOrDefaultAsync(cancellationToken);

        var result = new ParticipantBalanceStatusDto
        {
            Bic = myBic,
            ParticipantName = participant?.Name ?? "Unknown",
            LastKnownBalance = threshold.LastKnownBalance,
            CurrentZone = threshold.LastKnownZone.ToString(),
            LastZoneChangeAt = threshold.LastZoneChangeAt,
            LastAlertSentAt = threshold.LastAlertSentAt,
            LastUpdatedAt = threshold.LastKnownBalance.HasValue ? DateTimeOffset.UtcNow : null,
            Currency = "USD"
        };

        return new Response<ParticipantBalanceStatusDto>(result);
    }
}
