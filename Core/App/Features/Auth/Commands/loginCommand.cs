namespace SPSS.App.Features.Auth.Commands;

[SPSS.Domain.Security.PublicRequest]
public record LoginCommand(string Username, string Password) : IRequest<Response<LoginDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
    }
}

public sealed class LoginCommandHandler(IStorageBroker storage, ITokenService tokenService, IPasswordManager passwordManager, SPSS.App.Features.Auth.LoginAttemptLimiter attempts) : IRequestHandler<LoginCommand, Response<LoginDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IPasswordManager _passwordManager = passwordManager;

    public async ValueTask<Response<LoginDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (attempts.IsBlocked(request.Username))
            return new Response<LoginDto>(default!) { IsSuccess = false, StatusCode = 429, Message = "Too many login attempts. Try again later." };
        var user = await _storage.Users
        .Where(x => x.DeletedOn == null && (x.Phone.Equals(request.Username) || x.Username.Equals(request.Username.ToLower())))
        .Include(x => x.Participant)
        .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Response<LoginDto>.BadRequest("Invalid username or password!");
        }

        // Reserve an attempt atomically in the shared database before checking the password.
        if (!await _storage.TryReserveLoginAttemptAsync(user.Id, cancellationToken))
            return new Response<LoginDto>(default!) { IsSuccess = false, StatusCode = 429, Message = "Too many login attempts. Try again later." };

        if (!_passwordManager.CheckPassword(user, request.Password))
        {
            return Response<LoginDto>.BadRequest("Invalid username or password!");
        }

        if (!user.Active)
        {
            return Response<LoginDto>.BadRequest("Your account is locked or not allowed to login!");
        }

        if (string.IsNullOrEmpty(user.SecurityStamp)) user.SecurityStamp = Guid.NewGuid().ToString();
        await _storage.SaveChangesAsync(cancellationToken);

        var result = await _tokenService.GenerateToken(user, _storage, cancellationToken);

        return new Response<LoginDto>(result);
    }
}