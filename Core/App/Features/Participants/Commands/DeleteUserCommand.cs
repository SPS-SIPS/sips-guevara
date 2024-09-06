namespace SPSS.App.Features.Participants.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveParticipant)]
public sealed record DeleteParticipantCommand(string Id) : IRequest<Response<string>>;

public sealed class DeleteParticipantCommandValidator : AbstractValidator<DeleteParticipantCommand>
{
    public DeleteParticipantCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(string));
    }
}

public sealed class DeleteParticipantCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteParticipantCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<string>> Handle(DeleteParticipantCommand request, CancellationToken cancellationToken)
    {
        var Participant = await _storage.Participants
        .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (Participant == null)
        {
            return Response<string>.NotFound("Participant not found");
        }

        _storage.Participants.Remove(Participant);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<string>(Participant.Id!);
    }
}