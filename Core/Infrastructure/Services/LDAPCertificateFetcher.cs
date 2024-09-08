using System.DirectoryServices.Protocols;
using System.Globalization;
using System.Net;
using System.Text;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger, IStorageBroker storage) : ICertificateRetriever
{
    private readonly string _baseDn = configuration["LDAP:BaseDN"] ?? throw new ArgumentNullException("LDAP:BaseDN");
    private readonly ILogger<LDAPCertificateFetcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
    private readonly string _ldapUser = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
    private readonly string _ldapPassword = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");
    private readonly IStorageBroker _storage = storage;
    public async Task GetAllCertificates()
    {
        try
        {
            // Establish LDAP connection
            LdapDirectoryIdentifier identifier = new(_ldapServer, 389, false, false);  // For plain LDAP

            NetworkCredential credential = new(_ldapUser, _ldapPassword);
            LdapConnection connection = new(identifier, credential, AuthType.Basic);

            // Specify that we only need to retrieve the 'userCertificate' attribute
            var searchFilter = "(userCertificate=*)";

            // Set LDAP Protocol Version
            connection.SessionOptions.ProtocolVersion = 3;

            // Define the search request
            SearchRequest searchRequest = new(_baseDn, searchFilter, SearchScope.Subtree, "userCertificate");

            // Send the search request and get the response
            SearchResponse searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
            // Iterate over search results
            foreach (SearchResultEntry entry in searchResponse.Entries)
            {
                foreach (var attr in entry.Attributes["userCertificate"])
                {
                    byte[] certificateBytes = (byte[])attr;
                    X509Certificate certificate = new(certificateBytes);
                    _logger.LogInformation("Subject: {Subject}, Issuer: {Issuer}, Serial Number: {SerialNumber}", certificate.SubjectDN, certificate.IssuerDN, certificate.SerialNumber);
                    await SaveCertificate(certificate);
                }
            }
        }
        catch (LdapException ldapEx)
        {
            Console.WriteLine($"LDAP Error: {ldapEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    private async Task SaveCertificate(X509Certificate certificate, CancellationToken ct = default)
    {
        string pemCertificate = ConvertCertificateToPem(certificate);
        var serialNumber = ConvertHexToDecimal(certificate.SerialNumber);

        if (await _storage.Certificates.AnyAsync(c => c.Id == serialNumber.ToString(), cancellationToken: ct))
        {
            _logger.LogInformation("Certificate with serial number {SerialNumber} already exists in the database", serialNumber);
            return;
        }

        var participant = await _storage.Participants.FirstOrDefaultAsync(p => p.Id == certificate.SubjectDN.ToString(), cancellationToken: ct) ?? new Domain.Models.BL.Participant
        {
            Id = certificate.SubjectDN.ToString(),
            Name = certificate.SubjectDN.ToString(),
            BicNo = certificate.SubjectDN.ToString(),
        };
        var cert = new Domain.Models.BL.Certificate
        {
            Id = serialNumber.ToString(),
            Name = certificate.SubjectDN.ToString(),
            Subject = certificate.SubjectDN.ToString(),
            ExpireDate = certificate.NotAfter,
            IssuedAt = certificate.NotBefore,
            CachedOn = DateTimeOffset.Now,
            // check if certificate is not expired
            Active = certificate.NotAfter > DateTimeOffset.Now
        };

        participant.Certificates.Add(cert);
        await _storage.Participants.AddAsync(participant, ct);
        await _storage.SaveChangesAsync(ct);
    }
    private static X509Certificate ParseCertificate(byte[] certData)
    {
        var parser = new X509CertificateParser();
        return parser.ReadCertificate(certData);
    }

    public string ConvertCertificateToPem(X509Certificate certificate)
    {
        StringBuilder pemStringBuilder = new();

        using (TextWriter textWriter = new StringWriter(pemStringBuilder))
        {
            PemWriter pemWriter = new(textWriter);
            pemWriter.WriteObject(certificate);
            pemWriter.Writer.Flush();
        }

        return pemStringBuilder.ToString();
    }

    private static long ConvertHexToDecimal(BigInteger serialNumber)
    {
        return long.Parse(serialNumber.ToString(), NumberStyles.HexNumber);
    }
}