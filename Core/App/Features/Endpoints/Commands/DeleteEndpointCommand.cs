namespace SPSS.App.Features.Endpoints.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveEndpoint)]
public sealed record DeleteEndpointCommand(int Id) : IRequest<Response<int>>;

public sealed class DeleteEndpointCommandValidator : AbstractValidator<DeleteEndpointCommand>
{
    public DeleteEndpointCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(int));
    }
}

public sealed class DeleteEndpointCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteEndpointCommand, Response<int>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<int>> Handle(DeleteEndpointCommand request, CancellationToken cancellationToken)
    {
        var Endpoint = await _storage.Endpoints
        .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (Endpoint == null)
        {
            return Response<int>.NotFound("Endpoint not found");
        }

        _storage.Endpoints.Remove(Endpoint);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<int>(Endpoint.Id!);
    }
}