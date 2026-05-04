namespace SPSS.App.Features.Auth.Commands;

public sealed record RefreshCommand(string AccessToken, Guid RefreshToken) : IRequest<Response<LoginDto>>;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshCommandHandler(IStorageBroker storage, ITokenService tokenService) : IRequestHandler<RefreshCommand, Response<LoginDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ITokenService _tokenService = tokenService;

    public async ValueTask<Response<LoginDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var (jti, userId, _) = _tokenService.ParseJwt(request.AccessToken);

        if (jti == Guid.Empty || userId <= -1)
        {
            throw new InvalidTokenException("Can't parse token");
        }
        var storedRefreshToken = await _storage.Tokens
        .Where(x => x.Id.Equals(request.RefreshToken))
        .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidTokenException("Can't find refresh token");

        if (DateTimeOffset.UtcNow > storedRefreshToken.ExpiresOn)
        {
            throw new InvalidTokenException("Token Expired");
        }
        if (storedRefreshToken.Invalidated || storedRefreshToken.Used)
        {
            throw new InvalidTokenException("Token Invalidated or Used");
        }

        if (storedRefreshToken.Jti != jti)
        {
            throw new InvalidTokenException("Token Jti doesn't match");
        }

        storedRefreshToken.Used = true;
        _storage.Tokens.Update(storedRefreshToken);
        await _storage.SaveChangesAsync(cancellationToken);

        var user = await _storage.Users
        .Where(x => x.Id.Equals(userId))
        .Include(x => x.Participant)
        .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidTokenException("Can't find user");
        if (!user.Active)
        {
            throw new InvalidTokenException("User is not active");
        }

        var result = await _tokenService.GenerateToken(user, _storage, cancellationToken);

        return new Response<LoginDto>(result);
    }
}