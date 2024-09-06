namespace SPSS.App.Features.Auth.Commands;

[Authorize]
public sealed record ChangePhoneCommand(string Phone, string Password) : IRequest<Response<string>>;

public sealed class ChangePhoneCommandValidator : AbstractValidator<ChangePhoneCommand>
{
    public ChangePhoneCommandValidator()
    {
        RuleFor(x => x.Phone).EmailAddress();
    }
}

public sealed class ChangePhoneCommandHandler(IStorageBroker storage, IIdentityService identity, IPasswordManager passwordManager) : IRequestHandler<ChangePhoneCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly IPasswordManager _passwordManager = passwordManager;

    public async ValueTask<Response<string>> Handle(ChangePhoneCommand request, CancellationToken cancellationToken)
    {
        var userId = _identity.Id;
        var user = await _storage.Users.FindAsync([userId], cancellationToken) ?? throw new NotFoundException("User", request.Phone);
        if (!_passwordManager.CheckPassword(user, request.Password))
        {
            throw new BadRequestException("Invalid Password");
        }

        if (user.Phone.Equals(request.Phone))
        {
            throw new BadRequestException($"{request.Phone}, is already registered in the system!");
        }

        if (await _storage.Users.AnyAsync(x => x.Phone.Equals(request.Phone), cancellationToken: cancellationToken))
        {
            throw new BadRequestException($"{request.Phone}, is already registered in the system!");
        }

        user.Phone = request.Phone;

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<string>("logout");
    }
}