namespace SPSS.App.Features.Endpoints.Commands;
[Authorize(Roles = CreateEndpoint)]
public sealed record AddEndpointCommand(string ParticipantId, string Name, string Address, bool Active) : IRequest<Response<int>>;
public sealed class AddEndpointCommandValidator : AbstractValidator<AddEndpointCommand>
{
    public AddEndpointCommandValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(100);
    }
}

public sealed class AddEndpointCommandHandler(IStorageBroker storage) : IRequestHandler<AddEndpointCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(AddEndpointCommand request, CancellationToken cancellationToken)
    {

        if (!await _storage.Participants.AnyAsync(x => x.Id.Equals(request.ParticipantId), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("Participant does not exist");
        }

        Endpoint entity = new()
        {
            ParticipantId = request.ParticipantId,
            Name = request.Name,
            Address = request.Address,
            Date = DateTime.UtcNow.ToUniversalTime(),
            Active = request.Active,
        };

        await _storage.Endpoints.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entity.Id!);
    }
}