namespace SPSS.App.Features.Participants.Commands;
[Authorize(Roles = CreateParticipant)]
public sealed record AddParticipantCommand(string Id, string Name, string BicNo, int UserId) : IRequest<Response<string>>;

public sealed class AddParticipantCommandValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BicNo).NotEmpty();
    }
}

public sealed class AddParticipantCommandHandler(IStorageBroker storage) : IRequestHandler<AddParticipantCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<string>> Handle(AddParticipantCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.ToUpper();
        if (await _storage.Participants.AnyAsync(x => x.Id!.Equals(normalizedName), cancellationToken: cancellationToken))
        {
            return Response<string>.BadRequest("Participant with the same name already exists");
        }

        if (await _storage.Participants.AnyAsync(x => x.UserId.Equals(request.UserId), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("User already has a participant");
        }

        Participant entity = new()
        {
            Id = request.Id,
            Name = request.Name,
            UserId = request.UserId,
            BicNo = request.BicNo,
            Active = true,
        };

        await _storage.Participants.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<string>(entity.Id!);
    }
}