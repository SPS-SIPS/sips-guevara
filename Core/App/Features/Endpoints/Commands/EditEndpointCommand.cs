namespace SPSS.App.Features.Endpoints.Commands;
[Authorize(Roles = CreateEndpoint)]
public sealed record EditEndpointCommand(int Id, string ParticipantId, string Name, string Address, bool? Active) : IRequest<Response<int>>;

public sealed class EditEndpointCommandValidator : AbstractValidator<EditEndpointCommand>
{
    public EditEndpointCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(100);
    }
}

public sealed class EditEndpointCommandHandler(IStorageBroker storage) : IRequestHandler<EditEndpointCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(EditEndpointCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Endpoints
            .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            return Response<int>.NotFound("Endpoint not found");
        }

        // if participantId changed check if new participant exists
        if (entity.ParticipantId != request.ParticipantId)
        {
            if (!await _storage.Participants.AnyAsync(x => x.Id.Equals(request.ParticipantId), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Participant does not exist");
            }
        }

        entity.ParticipantId = request.ParticipantId;
        entity.Name = request.Name;
        entity.Address = request.Address;
        if (request.Active.HasValue)
        {
            entity.Active = request.Active.Value;
        }

        await _storage.SaveChangesAsync(cancellationToken);
        return new Response<int>(entity.Id!);
    }
}