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

public sealed class DeleteUserCommandHandler(IStorageBroker storage, AppSettings settings) : IRequestHandler<DeleteUserCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var User = await _storage.Users
        .Include(x => x.Roles).ThenInclude(x => x.Role)
        .SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken);

        if (User == null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }
        if (User.Roles.Any(x => x.Role.Super || x.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) || settings.RestrictedRoles.Contains(x.Role.Name, StringComparer.OrdinalIgnoreCase)))
            throw new ForbiddenAccessException("Protected accounts cannot be deleted through user administration.");

        User.Active = false;
        User.SecurityStamp = Guid.NewGuid().ToString();
        var sessions = await _storage.Tokens.Where(x => x.UserId == User.Id && !x.Invalidated).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.Invalidated = true;
        _storage.Users.Remove(User);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(User.Id);
    }
}