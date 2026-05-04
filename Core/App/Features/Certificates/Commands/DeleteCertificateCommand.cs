namespace SPSS.App.Features.Certificates.Commands;

using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = RemoveCertificate)]
public sealed record DeleteCertificateCommand(string Id) : IRequest<Response<string>>;

public sealed class DeleteCertificateCommandValidator : AbstractValidator<DeleteCertificateCommand>
{
    public DeleteCertificateCommandValidator()
    {
        RuleFor(x => x.Id).NotNull().NotEqual(default(string));
    }
}

public sealed class DeleteCertificateCommandHandler(IStorageBroker storage) : IRequestHandler<DeleteCertificateCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<string>> Handle(DeleteCertificateCommand request, CancellationToken cancellationToken)
    {
        var Certificate = await _storage.Certificates
        .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (Certificate == null)
        {
            return Response<string>.NotFound("Certificate not found");
        }

        _storage.Certificates.Remove(Certificate);

        await _storage.SaveChangesAsync(cancellationToken);

        return new Response<string>(Certificate.Id!);
    }
}