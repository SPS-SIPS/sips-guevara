using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Enums;

namespace SPSS.App.Features.Thresholds.Commands;

public sealed record AddParticipantThresholdCommand(CreateParticipantThresholdDto Data) : IRequest<Response<int>>;

public sealed class AddParticipantThresholdCommandHandler(
    IStorageBroker storage,
    ILogger<AddParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<AddParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly ILogger<AddParticipantThresholdCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<Response<int>> Handle(
        AddParticipantThresholdCommand request, 
        CancellationToken cancellationToken)
    {
        var data = request.Data;

        if (string.IsNullOrWhiteSpace(data.Bic))
            throw new ValidationException("BIC is required");

        if (data.Bic.Length != 11)
            throw new ValidationException("BIC must be exactly 11 characters");

        if (data.GoldenAmount <= 0)
            throw new ValidationException("GoldenAmount must be greater than zero");

        if (data.AmberPercentage < 0 || data.AmberPercentage > 100)
            throw new ValidationException("AmberPercentage must be between 0 and 100");

        if (data.RedPercentage < 0 || data.RedPercentage > 100)
            throw new ValidationException("RedPercentage must be between 0 and 100");

        if (data.RedPercentage >= data.AmberPercentage)
            throw new ValidationException("RedPercentage must be less than AmberPercentage");

        var exists = await _storage.ParticipantThresholds
            .AnyAsync(x => x.Bic == data.Bic && x.Active, cancellationToken);

        if (exists)
            throw new ValidationException($"Threshold configuration already exists for BIC {data.Bic}");

        var threshold = new ParticipantThreshold
        {
            Bic = data.Bic.ToUpperInvariant(),
            GoldenAmount = data.GoldenAmount,
            AmberPercentage = data.AmberPercentage,
            RedPercentage = data.RedPercentage,
            LastKnownZone = ThresholdZone.Green,
            AlertRecipients = data.AlertRecipients
        };

        await _storage.ParticipantThresholds.AddAsync(threshold, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created threshold configuration for BIC {Bic}. GoldenAmount: {GoldenAmount}, Amber: {AmberPercentage}%, Red: {RedPercentage}%",
            threshold.Bic,
            threshold.GoldenAmount,
            threshold.AmberPercentage,
            threshold.RedPercentage);

        return new Response<int>(threshold.Id);
    }
}
