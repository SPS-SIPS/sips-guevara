using SPSS.App.Security;
namespace SPSS.App.Features.Roles.Commands;

using SPSS.Domain.Models.Identity;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = CreateRole)]
public sealed record AddRoleCommand(string Name, string[] Claims) : IRequest<Response<Guid>>;

public sealed class AddRoleCommandValidator : AbstractValidator<AddRoleCommand>
{
    public AddRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Claims).NotEmpty();
    }
}

public sealed class AddRoleCommandHandler(IStorageBroker storage, IRolesCache rolesCache, IIdentityService identity) : IRequestHandler<AddRoleCommand, Response<Guid>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IRolesCache _rolesCache = rolesCache;

    public async ValueTask<Response<Guid>> Handle(AddRoleCommand request, CancellationToken cancellationToken)
    {

        PermissionDelegation.EnsureWithinAuthority(identity, request.Claims);
        if (await _storage.Roles.AnyAsync(x => x.NormalizedName.Equals(request.Name.ToUpperInvariant()), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("Role already exists");
        }

        Role role = new()
        {
            Name = request.Name,
            Super = false,
            Active = true,
            NormalizedName = request.Name.ToUpperInvariant()
        };

        foreach (string permission in request.Claims)
        {
            role.RoleClaims.Add(new RoleClaim
            {
                Type = permission,
            });
        }

        await _storage.Roles.AddAsync(role, cancellationToken);

        await _storage.SaveChangesAsync(cancellationToken);
        _rolesCache.Invalidate();
        return new Response<Guid>(role.Id);
    }
}