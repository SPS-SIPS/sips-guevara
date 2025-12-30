namespace SPSS.App.Features.Thresholds.Commands;

[Authorize(Roles = RemoveParticipantThreshold)]
public sealed record DeleteParticipantThresholdCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteParticipantThresholdCommandValidator : AbstractValidator<DeleteParticipantThresholdCommand>
{
    public DeleteParticipantThresholdCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class DeleteParticipantThresholdCommandHandler(IStorageBroker storage, ILogger<DeleteParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<DeleteParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ILogger<DeleteParticipantThresholdCommandHandler> _logger = logger;

    public async ValueTask<Response<int>> Handle(DeleteParticipantThresholdCommand request, CancellationToken cancellationToken)
    {
        var threshold = await _storage.ParticipantThresholds
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.Active, cancellationToken);

        if (threshold == null)
            throw new NotFoundException("Threshold configuration not found");

        threshold.Active = false;

        _storage.ParticipantThresholds.Update(threshold);
        await _storage.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted threshold configuration for BIC {Bic}",
            threshold.Bic);

        return new Response<int>(threshold.Id);
    }
}
