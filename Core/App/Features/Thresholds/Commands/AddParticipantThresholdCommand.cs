using SPSS.Domain.Models.BL;
using SPSS.Domain.Models.Enums;

namespace SPSS.App.Features.Thresholds.Commands;
[Authorize(Roles = CreateParticipantThreshold)]
public sealed record AddParticipantThresholdCommand(
    string Bic, 
    decimal GoldenAmount, 
    decimal AmberPercentage, 
    decimal RedPercentage, 
    string? AlertRecipients,
    bool EnforceSendRestriction = false,
    decimal MinimumSendBalance = 0) : IRequest<Response<int>>;

public sealed class AddParticipantThresholdCommandValidator : AbstractValidator<AddParticipantThresholdCommand>
{
    public AddParticipantThresholdCommandValidator()
    {
        RuleFor(x => x.Bic)
            .NotEmpty();

        RuleFor(x => x.GoldenAmount)
            .GreaterThan(0)
            .WithMessage("GoldenAmount must be greater than zero");

        RuleFor(x => x.AmberPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("AmberPercentage must be between 0 and 100");

        RuleFor(x => x.RedPercentage)
            .InclusiveBetween(0, 100)
            .WithMessage("RedPercentage must be between 0 and 100")
            .LessThan(x => x.AmberPercentage)
            .WithMessage("RedPercentage must be less than AmberPercentage");

        RuleFor(x => x.MinimumSendBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("MinimumSendBalance must be greater than or equal to zero");

        RuleFor(x => x.AlertRecipients)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.AlertRecipients));
    }
}

public sealed class AddParticipantThresholdCommandHandler(IStorageBroker storage, ILogger<AddParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<AddParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<AddParticipantThresholdCommandHandler> _logger = logger;

    public async ValueTask<Response<int>> Handle(AddParticipantThresholdCommand request, CancellationToken cancellationToken)
    {
        var exists = await _storage.ParticipantThresholds
            .AnyAsync(x => x.Bic == request.Bic && x.Active, cancellationToken);

        if (exists)
            throw new ValidationException($"Threshold configuration already exists for BIC {request.Bic}");

        var threshold = new ParticipantThreshold
        {
            Bic = request.Bic.ToUpperInvariant(),
            GoldenAmount = request.GoldenAmount,
            AmberPercentage = request.AmberPercentage,
            RedPercentage = request.RedPercentage,
            LastKnownZone = ThresholdZone.Green,
            AlertRecipients = request.AlertRecipients,
            EnforceSendRestriction = request.EnforceSendRestriction,
            MinimumSendBalance = request.MinimumSendBalance,
            CanSend = true
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
