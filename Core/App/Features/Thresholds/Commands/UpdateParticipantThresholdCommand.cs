using SPSS.Domain.Models.DTOs;

namespace SPSS.App.Features.Thresholds.Commands;

public sealed record UpdateParticipantThresholdCommand(UpdateParticipantThresholdDto Data) : IRequest<Response<int>>;

public sealed class UpdateParticipantThresholdCommandHandler(
    IStorageBroker storage,
    ILogger<UpdateParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<UpdateParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly ILogger<UpdateParticipantThresholdCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<Response<int>> Handle(
        UpdateParticipantThresholdCommand request, 
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        if (data.GoldenAmount <= 0)
            throw new ValidationException("GoldenAmount must be greater than zero");

        if (data.AmberPercentage < 0 || data.AmberPercentage > 100)
            throw new ValidationException("AmberPercentage must be between 0 and 100");

        if (data.RedPercentage < 0 || data.RedPercentage > 100)
            throw new ValidationException("RedPercentage must be between 0 and 100");

        if (data.RedPercentage >= data.AmberPercentage)
            throw new ValidationException("RedPercentage must be less than AmberPercentage");

        var threshold = await _storage.ParticipantThresholds
            .FirstOrDefaultAsync(x => x.Id == data.Id && x.Active, cancellationToken);

        if (threshold == null)
            throw new NotFoundException("Threshold configuration not found");

        threshold.GoldenAmount = data.GoldenAmount;
        threshold.AmberPercentage = data.AmberPercentage;
        threshold.RedPercentage = data.RedPercentage;
        threshold.AlertRecipients = data.AlertRecipients;

        _storage.ParticipantThresholds.Update(threshold);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated threshold configuration for BIC {Bic}. GoldenAmount: {GoldenAmount}, Amber: {AmberPercentage}%, Red: {RedPercentage}%",
            threshold.Bic,
            threshold.GoldenAmount,
            threshold.AmberPercentage,
            threshold.RedPercentage);

        return new Response<int>(threshold.Id);
    }
}
