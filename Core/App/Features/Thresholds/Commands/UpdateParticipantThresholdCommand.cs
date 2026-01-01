namespace SPSS.App.Features.Thresholds.Commands;
[Authorize(Roles = UpdateParticipantThreshold)]
public sealed record UpdateParticipantThresholdCommand(
    int Id,
    decimal GoldenAmount, 
    decimal AmberPercentage, 
    decimal RedPercentage, 
    string? AlertRecipients,
    bool EnforceSendRestriction = false,
    decimal MinimumSendBalance = 0) : IRequest<Response<int>>;

public sealed class UpdateParticipantThresholdCommandValidator : AbstractValidator<UpdateParticipantThresholdCommand>
{
    public UpdateParticipantThresholdCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

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

public sealed class UpdateParticipantThresholdCommandHandler(IStorageBroker storage, ILogger<UpdateParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<UpdateParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<UpdateParticipantThresholdCommandHandler> _logger = logger;

    public async ValueTask<Response<int>> Handle(UpdateParticipantThresholdCommand request, CancellationToken cancellationToken)
    {
        var threshold = await _storage.ParticipantThresholds
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Active, cancellationToken);

        if (threshold == null)
            throw new NotFoundException("Threshold configuration not found");

        threshold.GoldenAmount = request.GoldenAmount;
        threshold.AmberPercentage = request.AmberPercentage;
        threshold.RedPercentage = request.RedPercentage;
        threshold.AlertRecipients = request.AlertRecipients;
        threshold.EnforceSendRestriction = request.EnforceSendRestriction;
        threshold.MinimumSendBalance = request.MinimumSendBalance;

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
