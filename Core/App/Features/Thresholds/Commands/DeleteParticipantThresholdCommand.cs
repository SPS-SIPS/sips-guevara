namespace SPSS.App.Features.Thresholds.Commands;

public sealed record DeleteParticipantThresholdCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteParticipantThresholdCommandHandler(
    IStorageBroker storage,
    ILogger<DeleteParticipantThresholdCommandHandler> logger) 
    : IRequestHandler<DeleteParticipantThresholdCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    private readonly ILogger<DeleteParticipantThresholdCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask<Response<int>> Handle(
        DeleteParticipantThresholdCommand request, 
        CancellationToken cancellationToken)
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
