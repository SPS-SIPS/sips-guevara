namespace SPSS.App.Features.Participants.Commands;
[Authorize(Roles = CreateParticipant)]
public sealed record EditParticipantCommand(string Id, string Name, int UserId, string BicNo, bool? Active, string? Email) : IRequest<Response<string>>;

public sealed class EditParticipantCommandValidator : AbstractValidator<EditParticipantCommand>
{
    public EditParticipantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BicNo).NotEmpty();
    }
}

public sealed class EditParticipantCommandHandler(IStorageBroker storage) : IRequestHandler<EditParticipantCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<string>> Handle(EditParticipantCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Participants
            .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            return Response<string>.NotFound("Participant not found");
        }

        if (entity.UserId != request.UserId && await _storage.Participants.AnyAsync(x => x.UserId.Equals(request.UserId), cancellationToken: cancellationToken))
        {
            return Response<string>.BadRequest("User already has a participant");
        }

        entity.Name = request.Name;
        entity.UserId = request.UserId;
        entity.Active = request.Active ?? entity.Active;
        entity.BicNo = request.BicNo;
        entity.Email = request.Email ?? string.Empty;

        await _storage.SaveChangesAsync(cancellationToken);
        return new Response<string>(entity.Id!);
    }
}