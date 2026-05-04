namespace SPSS.App.Features.Roles.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveRole)]
public sealed record RemoveRoleCommand(Guid Id) : IRequest<Response<Guid>>;

public sealed class RemoveRoleCommandValidator : AbstractValidator<RemoveRoleCommand>
{
    public RemoveRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull();
    }
}

public sealed class RemoveRoleCommandHandler(IStorageBroker storage, IRolesCache rolesCache) : IRequestHandler<RemoveRoleCommand, Response<Guid>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IRolesCache _rolesCache = rolesCache;

    public async ValueTask<Response<Guid>> Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _storage.Roles.SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected role!");

        _storage.Roles.Remove(role);

        await _storage.SaveChangesAsync(cancellationToken);
        _rolesCache.Invalidate();
        return new Response<Guid>(role.Id);
    }
}