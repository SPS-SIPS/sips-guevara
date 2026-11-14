
using SPSS.Domain.Models.Identity;

namespace NTran.App.Features.Users.Commands;
[Authorize(Roles = UpdateUser)]
public sealed record EditUserCommand(int Id, string Phone, string Email, string Username, string? Password, Guid[] Roles) : IRequest<Response<int>>;

public sealed class EditUserCommandValidator : AbstractValidator<EditUserCommand>
{
    public EditUserCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().GreaterThan(0);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(50);
        RuleFor(x => x.Username).MaximumLength(50);
        RuleFor(x => x.Roles).NotEmpty();
        RuleFor(x => x.Password!).Password().MaximumLength(1024).When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public sealed class EditUserCommandHandler(IStorageBroker storage, IPasswordHash password, AppSettings appSettings) : IRequestHandler<EditUserCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IPasswordHash _password = password;
    private readonly AppSettings _appSettings = appSettings;
    public async ValueTask<Response<int>> Handle(EditUserCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Users
        .Include(x => x.Roles).ThenInclude(x => x.Role)
        .SingleOrDefaultAsync(x => x.Id.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(entity), request.Id);
        }

        if (entity.Roles.Any(x => x.Role.Super || x.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            _appSettings.RestrictedRoles.Contains(x.Role.Name, StringComparer.OrdinalIgnoreCase)))
            throw new ForbiddenAccessException("Protected accounts cannot be edited through user administration.");

        if (entity.DeletedOn != null) throw new NotFoundException("User", request.Id);

        if (entity.Phone != request.Phone)
        {
            if (await _storage.Users.AnyAsync(x => x.Phone.Equals(request.Phone), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Phone number already exists");
            }
        }

        if (!string.IsNullOrEmpty(request.Username) && entity.Username != request.Username)
        {
            if (await _storage.Users.AnyAsync(x => x.Username.Equals(request.Username), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Username already exists");
            }
        }

        if (entity.Email != request.Email)
        {
            if (await _storage.Users.AnyAsync(x => x.Email.Equals(request.Email), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Email already exists");
            }
        }

        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Photo = "";
        entity.Username = string.IsNullOrEmpty(request.Username) ? request.Phone[^9..] : request.Username.ToLower();

        if (!string.IsNullOrEmpty(request.Password))
        {
            entity.PasswordHash = _password.HashPassword(entity, request.Password);
        }

        var dbUserRoles = entity.Roles.Select(x => x.RoleId).ToArray();
        var newRoles = request.Roles.Except(dbUserRoles).ToArray();
        var removeRoles = dbUserRoles.Except(request.Roles).ToArray();

        foreach (Guid roleId in removeRoles)
        {
            var role = entity.Roles.SingleOrDefault(x => x.RoleId == roleId);
            if (role != null)
            {
                _storage.UserRoles.Remove(role);
            }
        }

        foreach (var roleId in newRoles)
        {
            var role = await _storage.Roles.Where(x => x.Id.Equals(roleId)).Include(x => x.RoleClaims).FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new NotFoundException("Role", roleId);

            if (role.Super || role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase) || _appSettings.RestrictedRoles.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
            {
                throw new BadRequestException("You can't add Users to the admin role");
            }

            entity.Roles.Add(new UserRole
            {
                RoleId = roleId,
                UserId = entity.Id
            });
        }

        entity.SecurityStamp = Guid.NewGuid().ToString();
        var sessions = await _storage.Tokens.Where(x => x.UserId == entity.Id && !x.Invalidated).ToListAsync(cancellationToken);
        foreach (var session in sessions) session.Invalidated = true;
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entity.Id);
    }
}