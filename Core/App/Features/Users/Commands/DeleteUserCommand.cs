namespace SPSS.App.Features.Users.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = DeleteUser)]
public sealed record DeleteUserCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(int));
    }
}

public sealed class DeleteUserCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteUserCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var User = await _storage.Users
        .SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken);

        if (User == null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }
        _storage.Users.Remove(User);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(User.Id);
    }
}