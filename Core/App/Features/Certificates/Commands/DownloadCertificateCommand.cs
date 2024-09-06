namespace SPSS.App.Features.Certificates.Commands;

using Org.BouncyCastle.X509;
using static SPSS.Domain.Common.KnownClaims;
[Authorize(Roles = DownloadCertificates)]
public sealed record DownloadCertificateCommand(string SerialNumber) : IRequest<Response<CertificateDownloadDto>>;
public sealed class DownloadCertificateCommandValidator : AbstractValidator<DownloadCertificateCommand>
{
    public DownloadCertificateCommandValidator()
    {
        RuleFor(x => x.SerialNumber).NotNull().NotEqual(default(string));
    }
}

public sealed class DownloadCertificateCommandHandler(IStorageBroker storage, ICertificateRetriever retriever, IIdentityService identity, AppSettings settings) : IRequestHandler<DownloadCertificateCommand, Response<CertificateDownloadDto>>
{
    private readonly IStorageBroker _storage = storage;
    private readonly ICertificateRetriever _retriever = retriever;
    private readonly IIdentityService _identity = identity;
    private readonly AppSettings _settings = settings;


    public async ValueTask<Response<CertificateDownloadDto>> Handle(DownloadCertificateCommand request, CancellationToken cancellationToken)
    {
        var certResponse = new CertificateDownloadDto { Content = string.Empty, CacheInMins = _settings.CacheInMins ?? 20 };
        var certificate = await _storage.Certificates
        .Where(x => x.Id!.Equals(request.SerialNumber))
        .SingleOrDefaultAsync(cancellationToken);

        if (certificate is not null)
        {
            certResponse.Content = certificate.Name;

            if (certificate.Active == false)
            {
                return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
            }

            if (certificate.ExpireDate < DateTimeOffset.UtcNow)
            {
                certificate.Active = false;
                await _storage.SaveChangesAsync(cancellationToken);
                return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
            }

            return new Response<CertificateDownloadDto>(certResponse);
        }

        var response = RetrieverFromLdap(request.SerialNumber);
        if (response.IsSuccess == false)
        {
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
        }
        var certPem = _retriever.ConvertCertificateToPem(response.Data!);
        var participantId = response.Data!.SubjectDN.GetValueList().FirstOrDefault();

        if (string.IsNullOrWhiteSpace(participantId))
        {
            return Response<CertificateDownloadDto>.NotFound("Certificate not found", certResponse);
        }

        var participant = await _storage.Participants.Where(x => x.Name.Equals(participantId)).SingleOrDefaultAsync(cancellationToken);

        if (participant == null)
        {
            return Response<CertificateDownloadDto>.NotFound("Participant not found", certResponse);
        }

        var entity = new Certificate
        {
            Id = request.SerialNumber,
            Name = certPem,
            Active = true,
            CachedOn = DateTimeOffset.UtcNow,
            ExpireDate = response.Data!.NotAfter,
            ParticipantId = participant.Id!,
        };

        entity.Downloads.Add(new Download
        {
            ParticipantId = participant.Id!,
            Date = DateTimeOffset.UtcNow,
            Username = _identity.Username,
        });

        await _storage.Certificates.AddAsync(entity, cancellationToken);
        await _storage.SaveChangesAsync(cancellationToken);

        certResponse.Content = certPem;

        return new Response<CertificateDownloadDto>(certResponse);
    }
    private Response<X509Certificate?> RetrieverFromLdap(string serialNumber)
    {
        var cert = _retriever.GetCertificateBySerialNumber(serialNumber);
        if (cert == null)
        {
            return Response<X509Certificate?>.NotFound("Certificate not found");
        }
        return new Response<X509Certificate?>(cert);
    }
}