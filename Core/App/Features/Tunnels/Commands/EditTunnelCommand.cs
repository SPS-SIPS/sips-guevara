namespace SPSS.App.Features.Tunnels.Commands;
[Authorize(Roles = CreateTunnel)]
public sealed record EditTunnelCommand(int Id, int VPNId, string Name, string Endpoint, bool? Active) : IRequest<Response<int>>;

public sealed class EditTunnelCommandValidator : AbstractValidator<EditTunnelCommand>
{
    public EditTunnelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.VPNId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(50);
    }
}

public sealed class EditTunnelCommandHandler(IStorageBroker storage) : IRequestHandler<EditTunnelCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(EditTunnelCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Tunnels
            .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            return Response<int>.NotFound("Tunnel not found");
        }

        if (entity.VPNId != request.VPNId)
        {
            if (!await _storage.VPNs.AnyAsync(x => x.Id.Equals(request.VPNId), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("VNP does not exist");
            }
        }

        entity.VPNId = request.VPNId;
        entity.Name = request.Name;
        entity.Endpoint = request.Endpoint;
        if (request.Active.HasValue)
        {
            entity.Active = request.Active.Value;
        }

        await _storage.SaveChangesAsync(cancellationToken);
        return new Response<int>(entity.Id!);
    }
}