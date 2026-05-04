namespace SPSS.App.Features.Roles.Commands;

using SPSS.Domain.Models.Identity;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = UpdateRole)]
public sealed record EditRoleCommand(Guid Id, string Name, string[] Claims) : IRequest<Response<Guid>>;

public sealed class EditRoleCommandValidator : AbstractValidator<EditRoleCommand>
{
    public EditRoleCommandValidator()
    {
        RuleFor(x => x.Id).NotNull();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Claims).NotEmpty();
    }
}

public sealed class EditRoleCommandHandler(IStorageBroker storage, IRolesCache rolesCache, AppSettings appSettings) : IRequestHandler<EditRoleCommand, Response<Guid>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IRolesCache _rolesCache = rolesCache;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<Guid>> Handle(EditRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _storage.Roles.Include(x => x.RoleClaims).SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken) ?? throw new NotFoundException("Can't find the selected role!");

        if (_appSettings.RestrictedRoles.Contains(role.Name))
        {
            throw new BadRequestException("You can't modify this role!");
        }

        role.Name = request.Name;
        role.NormalizedName = request.Name.ToUpperInvariant();

        var dbClaims = role.RoleClaims.Select(x => x.Type).ToArray();
        var newClaims = request.Claims.Except(dbClaims).ToArray();
        var removedClaims = dbClaims.Except(request.Claims).ToArray();

        foreach (var claim in newClaims)
        {
            role.RoleClaims.Add(new RoleClaim
            {
                Type = claim,
            });
        }

        foreach (var claim in removedClaims)
        {
            var roleClaim = role.RoleClaims.SingleOrDefault(x => x.Type.Equals(claim));
            if (roleClaim is not null)
            {
                role.RoleClaims.Remove(roleClaim);
            }
        }

        await _storage.SaveChangesAsync(cancellationToken);
        _rolesCache.Invalidate();
        return new Response<Guid>(role.Id);
    }
}