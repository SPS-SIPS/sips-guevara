namespace SPSS.App.Features.Auth.Commands;

public record ResendOTPCommand(string Username, Guid Connection) : IRequest<Response<Guid>>;

public sealed class ResendOTPCommandValidator : AbstractValidator<ResendOTPCommand>
{
    public ResendOTPCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Connection).NotEmpty();
    }
}

public sealed class ResendOTPCommandHandler(IStorageBroker storage, ITokenService tokenService) : IRequestHandler<ResendOTPCommand, Response<Guid>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ITokenService _tokenService = tokenService;

    public async ValueTask<Response<Guid>> Handle(ResendOTPCommand request, CancellationToken cancellationToken)
    {
        var otp = await _storage.Tokens
        .Where(x => x.Id.Equals(request.Connection) && x.TokenType == TokenType.OTP)
        .Include(x => x.User)
        .SingleOrDefaultAsync(cancellationToken) ?? throw new BadRequestException("Invalid OTP!");

        if (otp.User.Phone != request.Username)
        {
            throw new BadRequestException("Invalid OTP!");
        }

        if (DateTimeOffset.UtcNow > otp.ExpiresOn)
        {
            throw new BadRequestException("Your session has expired!, please login again.");
        }

        if (otp.Invalidated || otp.Used)
        {
            throw new BadRequestException("Invalid OTP!");
        }

        otp.Invalidated = true;
        _storage.Tokens.Update(otp);

        await _storage.SaveChangesAsync(cancellationToken);

        var (otpNumber, refresh) = await _tokenService.GenerateOTP(otp, _storage, cancellationToken);

        return new Response<Guid>(refresh);
    }
}