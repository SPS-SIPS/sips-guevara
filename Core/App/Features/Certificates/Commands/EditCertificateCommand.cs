namespace SPSS.App.Features.Certificates.Commands;
[Authorize(Roles = UpdateCertificate)]
public sealed record EditCertificateCommand(string Id, bool Active) : IRequest<Response<string>>;

public sealed class EditCertificateCommandValidator : AbstractValidator<EditCertificateCommand>
{
    public EditCertificateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Active).NotEmpty();
    }
}

public sealed class EditCertificateCommandHandler(IStorageBroker storage) : IRequestHandler<EditCertificateCommand, Response<string>>
{
    private readonly IStorageBroker _storage = storage;

    public async ValueTask<Response<string>> Handle(EditCertificateCommand request, CancellationToken cancellationToken)
    {
        var entity = await _storage.Certificates
            .SingleOrDefaultAsync(x => x.Id!.Equals(request.Id), cancellationToken);

        if (entity == null)
        {
            return Response<string>.NotFound("Certificate not found");
        }

        entity.Active = request.Active;

        await _storage.SaveChangesAsync(cancellationToken);
        return new Response<string>(entity.Id!);
    }
}