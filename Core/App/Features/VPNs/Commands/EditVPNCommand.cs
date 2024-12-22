namespace SPSS.App.Features.VPNs.Commands;
[Authorize(Roles = CreateVPN)]
public sealed record EditVPNCommand(int Id, string ParticipantId, string Gateway, bool? Active, List<EditVPNTunnelCommand> Tunnels) : IRequest<Response<int>>;
public sealed record EditVPNTunnelCommand(int Id, string Endpoint, string Name) : IRequest<Response<int>>;

public sealed class EditVPNCommandValidator : AbstractValidator<EditVPNCommand>
{
    public EditVPNCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ParticipantId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Gateway).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.Tunnels).SetValidator(new EditVPNTunnelCommandValidator());
    }
}

public sealed class EditVPNTunnelCommandValidator : AbstractValidator<EditVPNTunnelCommand>
{
    public EditVPNTunnelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
public sealed class EditVPNCommandHandler(IStorageBroker storage) : IRequestHandler<EditVPNCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(EditVPNCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.VPNs
            .Include(x => x.Tunnels)
            .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            return Response<int>.NotFound("VPN not found");
        }

        // if participantId changed check if new participant exists
        if (entity.ParticipantId != request.ParticipantId)
        {
            if (!await _storage.Participants.AnyAsync(x => x.Id.Equals(request.ParticipantId), cancellationToken: cancellationToken))
            {
                throw new BadRequestException("Participant does not exist");
            }
        }

        entity.ParticipantId = request.ParticipantId;
        entity.Gateway = request.Gateway;
        entity.Date = DateTimeOffset.UtcNow.ToUniversalTime();
        if (request.Active.HasValue)
        {
            entity.Active = request.Active.Value;
        }

        var oldTunnels = entity.Tunnels.ToList();

        // we need to remove the tunnels that are not in the request but are in the database
        foreach (var oldTunnel in oldTunnels)
        {
            if (!request.Tunnels.Any(x => x.Id == oldTunnel.Id))
            {
                entity.Tunnels.Remove(oldTunnel);
            }
        }

        foreach (var tunnel in request.Tunnels)
        {
            var oldTunnel = entity.Tunnels.FirstOrDefault(x => x.Id == tunnel.Id);
            if (oldTunnel == null)
            {
                entity.Tunnels.Add(new Tunnel
                {
                    Endpoint = tunnel.Endpoint,
                    Name = tunnel.Name
                });
            }
            else
            {
                oldTunnel.Endpoint = tunnel.Endpoint;
                oldTunnel.Name = tunnel.Name;
            }
        }

        await _storage.SaveChangesAsync(cancellationToken);
        return new Response<int>(entity.Id!);
    }
}