namespace SPSS.App.Features.Auth.Commands;

[Authorize]
public sealed record RevokeAccessTokenCommand(Guid Id) : IRequest<Response<string>>;

public sealed class RevokeAccessTokenCommandValidator : AbstractValidator<RevokeAccessTokenCommand>
{
    public RevokeAccessTokenCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class RevokeAccessTokenCommandHandler(IStorageBroker storage, IIdentityService identity) : IRequestHandler<RevokeAccessTokenCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;

    public async ValueTask<Response<string>> Handle(RevokeAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _identity.Id;
        var token = await _storage.Tokens.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("Invalid Token");
        if (token.UserId != userId)
        {
            throw new NotFoundException("Invalid Token");
        }

        if (token.Used || token.Invalidated)
        {
            throw new BadRequestException("Used or Invalidated Token!");
        }

        token.Invalidated = true;
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<string>("logout");
    }
}