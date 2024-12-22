namespace SPSS.App.Features.Tunnels.Commands;
[Authorize(Roles = CreateTunnel)]
public sealed record AddTunnelCommand(int VPNId, string Name, string Endpoint) : IRequest<Response<int>>;
public sealed class AddTunnelCommandValidator : AbstractValidator<AddTunnelCommand>
{
    public AddTunnelCommandValidator()
    {
        RuleFor(x => x.VPNId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(50);
    }
}

public sealed class AddTunnelCommandHandler(IStorageBroker storage) : IRequestHandler<AddTunnelCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(AddTunnelCommand request, CancellationToken cancellationToken)
    {

        if (!await _storage.VPNs.AnyAsync(x => x.Id.Equals(request.VPNId), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("VPN does not exist");
        }

        Tunnel entity = new()
        {
            VPNId = request.VPNId,
            Name = request.Name,
            Endpoint = request.Endpoint,
            Date = DateTime.UtcNow.ToUniversalTime(),
            Active = true,
        };

        await _storage.Tunnels.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entity.Id!);
    }
}