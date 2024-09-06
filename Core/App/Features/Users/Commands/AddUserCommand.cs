namespace SPSS.App.Features.Users.Queries;

using SPSS.Domain.Models.Identity;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = CreateUser)]
public sealed record AddUserCommand(string Phone, string Email, string Username, string Password, Guid[] Roles) : IRequest<Response<int>>;

public sealed class AddUserCommandValidator : AbstractValidator<AddUserCommand>
{
    public AddUserCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().Password();
        RuleFor(x => x.Username).MaximumLength(50);
        RuleFor(x => x.Roles).NotEmpty();
    }
}

public sealed class AddUserCommandHandler(IStorageBroker storage, IPasswordHash password, AppSettings appSettings) : IRequestHandler<AddUserCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IPasswordHash _password = password;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<int>> Handle(AddUserCommand request, CancellationToken cancellationToken)
    {

        if (await _storage.Users.AnyAsync(x => x.Phone.Equals(request.Phone), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("Phone number already exists");
        }

        if (!string.IsNullOrEmpty(request.Username))
        {
            if (await _storage.Users.AnyAsync(x => x.Username.Equals(request.Username), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Username already exists");
            }
        }

        if (await _storage.Users.AnyAsync(x => x.Email.Equals(request.Email), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("Email already exists");
        }

        User entity = new()
        {
            Phone = request.Phone,
            Email = request.Email,
            Username = string.IsNullOrEmpty(request.Username) ? request.Phone[^9..] : request.Username.ToLower(),
            Active = true,
        };

        entity.PasswordHash = _password.HashPassword(entity, request.Password);

        foreach (var item in request.Roles)
        {
            var role = await _storage.Roles.Where(x => x.Id.Equals(item)).Include(x => x.RoleClaims).FirstOrDefaultAsync(cancellationToken: cancellationToken) ?? throw new NotFoundException("Role", request.Roles);

            if (_appSettings.RestrictedRoles.Contains(role.Name))
            {
                throw new BadRequestException("You can't add Users to the admin role");
            }

            entity.Roles.Add(new UserRole
            {
                RoleId = role.Id,
                UserId = entity.Id
            });
        }

        await _storage.Users.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entity.Id);
    }
}