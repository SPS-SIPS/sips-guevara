namespace SPSS.App.Features.VPNs.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveVPN)]
public sealed record DeleteVPNCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteVPNCommandValidator : AbstractValidator<DeleteVPNCommand>
{
    public DeleteVPNCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(int));
    }
}

public sealed class DeleteVPNCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteVPNCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(DeleteVPNCommand request, CancellationToken cancellationToken)
    {
        var VPN = await _storage.VPNs
        .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (VPN == null)
        {
            return Response<int>.NotFound("VPN not found");
        }

        _storage.VPNs.Remove(VPN);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(VPN.Id!);
    }
}