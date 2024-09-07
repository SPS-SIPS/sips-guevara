namespace SPSS.App.Features.Auth.Commands;

public record LoginCommand(string Username, string Password, string? OTP) : IRequest<Response<LoginDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(IStorageBroker storage, ITokenService tokenService, IPasswordManager passwordManager) : IRequestHandler<LoginCommand, Response<LoginDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IPasswordManager _passwordManager = passwordManager;

    public async ValueTask<Response<LoginDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _storage.Users
        .Where(x => x.Phone.Equals(request.Username) || x.Username.Equals(request.Username.ToLower()))
        .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Response<LoginDto>.BadRequest("Invalid username or password!");
        }

        if (!_passwordManager.CheckPassword(user, request.Password))
        {
            return Response<LoginDto>.BadRequest("Invalid username or password!");
        }

        if (!user.Active)
        {
            return Response<LoginDto>.BadRequest("Your account is locked or not allowed to login!");
        }

        await _storage.SaveChangesAsync(cancellationToken);

        var result = await _tokenService.GenerateToken(user, _storage, cancellationToken);

        return new Response<LoginDto>(result);
    }
}