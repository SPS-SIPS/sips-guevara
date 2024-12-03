namespace SPSS.App.Features.Certificates.Commands;
[Authorize(Roles = DownloadCertificates)]
public sealed record DownloadCertificateCommand(string SerialNumber, string IssuerDN) : IRequest<Response<CertificateDownloadDto>>;
public sealed class DownloadCertificateCommandValidator : AbstractValidator<DownloadCertificateCommand>
{
    public DownloadCertificateCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotNull().NotEqual(default(string));
        RuleFor(x => x.IssuerDN).NotNull().NotEqual(default(string));
    }
}

public sealed class DownloadCertificateCommandHandler(IStorageBroker storage, IIdentityService identity, AppSettings settings) : IRequestHandler<DownloadCertificateCommand, Response<CertificateDownloadDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly IIdentityService _identity = identity;
    private readonly AppSettings _settings = settings;


    public async ValueTask<Response<CertificateDownloadDto>> Handle(DownloadCertificateCommand request, CancellationToken cancellationToken)
    {
        var cacheInMins = _settings.CacheInMins ?? 20;
        var certResponse = new CertificateDownloadDto { Content = string.Empty };

        var certificate = await _storage.Certificates
        .Where(x => x.Id!.Equals(request.SerialNumber))
        .SingleOrDefaultAsync(cancellationToken);

        var result = $"User {_identity.Username} requested certificate with serial number {request.SerialNumber} and issuer {request.IssuerDN}";

        if (certificate is null)
        {
            return Response<CertificateDownloadDto>.NotFound($"Not Found: {result}", null);
        }

        if (certificate.Active == false)
        {
            return Response<CertificateDownloadDto>.NotFound($"Not Found: {result}", null);
        }

        if (certificate.IsRevoked)
        {
            certificate.Active = false;
            await _storage.SaveChangesAsync(cancellationToken);
            return Response<CertificateDownloadDto>.NotFound($"Not Found: {result}", null);
        }

        if (certificate.ExpireDate < DateTimeOffset.UtcNow)
        {
            certificate.Active = false;
            await _storage.SaveChangesAsync(cancellationToken);
            return Response<CertificateDownloadDto>.NotFound($"Not Found: {result}", null);
        }
        certResponse.Content = certificate.Name;
        certResponse.Owner = certificate.ParticipantId;

        if (_identity.Username != "admin")
        {
            certificate.Downloads.Add(new Download
            {
                Date = DateTime.UtcNow.ToUniversalTime(),
                ParticipantId = certificate.ParticipantId,
                Username = _identity.Username,
            });

            await _storage.SaveChangesAsync(cancellationToken);
        }
        return Response<CertificateDownloadDto>.Success(certResponse, cacheInMins);
    }
}