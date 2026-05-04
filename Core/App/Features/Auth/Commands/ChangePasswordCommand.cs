namespace SPSS.App.Features.Auth.Commands;

[Authorize]
public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Response<string>>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword);
        RuleFor(x => x.NewPassword).Password()
            .NotEqual(e => e.CurrentPassword).WithMessage("New password must be different from current password");
    }
}

public sealed class ChangePasswordCommandHandler(IStorageBroker storage, IIdentityService identity, IPasswordManager passwordManager, AppSettings appSettings) : IRequestHandler<ChangePasswordCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly IPasswordManager _passwordManager = passwordManager;
    private readonly AppSettings _appSettings = appSettings;

    public async ValueTask<Response<string>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _identity.Id;
        var user = await _storage.Users.FindAsync([userId], cancellationToken) ?? throw new NotFoundException("User", userId);
        if (!_passwordManager.CheckPassword(user, request.CurrentPassword))
        {
            throw new BadRequestException("Invalid Password");
        }

        user.SecurityStamp = Guid.NewGuid().ToString();
        user.PasswordHash = _passwordManager.GeneratePasswordHash(user, request.NewPassword);

        await _storage.SaveChangesAsync(cancellationToken);
        await _identity.InvalidateRefreshTokens(_storage, cancellationToken);

        return new Response<string>("logout");
    }
}