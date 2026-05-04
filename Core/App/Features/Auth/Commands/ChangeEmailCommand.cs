namespace SPSS.App.Features.Auth.Commands;

[Authorize]
public sealed record ChangeEmailCommand(string Email, string Password) : IRequest<Response<string>>;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(x => x.Email).EmailAddress();
    }
}

public sealed class ChangeEmailCommandHandler(IStorageBroker storage, IIdentityService identity, IPasswordManager passwordManager) : IRequestHandler<ChangeEmailCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly IPasswordManager _passwordManager = passwordManager;

    public async ValueTask<Response<string>> Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = _identity.Id;
        var user = await _storage.Users.FindAsync([userId], cancellationToken) ?? throw new NotFoundException("User", request.Email);
        if (!_passwordManager.CheckPassword(user, request.Password))
        {
            throw new BadRequestException("Invalid Password");
        }

        if (user.Email.ToUpperInvariant().Equals(request.Email.ToUpperInvariant()))
        {
            throw new BadRequestException($"{request.Email}, is already registered in the system!");
        }

        if (await _storage.Users.AnyAsync(x => x.Email.Equals(request.Email.ToUpperInvariant()), cancellationToken: cancellationToken))
        {
            throw new BadRequestException($"{request.Email}, is already registered in the system!");
        }

        user.Email = request.Email;

        await _storage.SaveChangesAsync(cancellationToken);
        await _identity.InvalidateRefreshTokens(_storage, cancellationToken);

        return new Response<string>("logout");
    }
}