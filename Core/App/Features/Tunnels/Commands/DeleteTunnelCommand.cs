namespace SPSS.App.Features.Tunnels.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveTunnel)]
public sealed record DeleteTunnelCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteTunnelCommandValidator : AbstractValidator<DeleteTunnelCommand>
{
    public DeleteTunnelCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(int));
    }
}

public sealed class DeleteTunnelCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteTunnelCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(DeleteTunnelCommand request, CancellationToken cancellationToken)
    {
        var Tunnel = await _storage.Tunnels
        .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (Tunnel == null)
        {
            return Response<int>.NotFound("Tunnel not found");
        }

        _storage.Tunnels.Remove(Tunnel);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(Tunnel.Id!);
    }
}