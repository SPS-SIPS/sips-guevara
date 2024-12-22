namespace SPSS.App.Features.VPNs.Commands;
[Authorize(Roles = CreateVPN)]
public sealed record AddVPNCommand(string ParticipantId, string Gateway, List<AddVPNTunnelCommand> Tunnels) : IRequest<Response<int>>;
public sealed record AddVPNTunnelCommand(string Endpoint, string Name) : IRequest<Response<int>>;
public sealed class AddVPNCommandValidator : AbstractValidator<AddVPNCommand>
{
    public AddVPNCommandValidator()
    {
        RuleFor(x => x.ParticipantId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Gateway).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.Tunnels).SetValidator(new AddVPNTunnelCommandValidator());
    }
}

public sealed class AddVPNTunnelCommandValidator : AbstractValidator<AddVPNTunnelCommand>
{
    public AddVPNTunnelCommandValidator()
    {
        RuleFor(x => x.Endpoint).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}

public sealed class AddVPNCommandHandler(IStorageBroker storage) : IRequestHandler<AddVPNCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(AddVPNCommand request, CancellationToken cancellationToken)
    {

        if (!await _storage.Participants.AnyAsync(x => x.Id.Equals(request.ParticipantId), cancellationToken: cancellationToken))
        {
            throw new BadRequestException("Participant does not exist");
        }

        VPN entity = new()
        {
            ParticipantId = request.ParticipantId,
            Gateway = request.Gateway,
            Date = DateTimeOffset.UtcNow.ToUniversalTime(),
            Active = true,
        };

        foreach (var tunnel in request.Tunnels)
        {
            entity.Tunnels.Add(new Tunnel
            {
                Endpoint = tunnel.Endpoint,
                Name = tunnel.Name,
                Date = DateTimeOffset.UtcNow.ToUniversalTime(),
                Active = true,
            });
        }

        await _storage.VPNs.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(entity.Id!);
    }
}