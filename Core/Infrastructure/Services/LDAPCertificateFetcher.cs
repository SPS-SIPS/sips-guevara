using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
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

            // Set LDAP Protocol Version
            connection.SessionOptions.ProtocolVersion = 3;

            // Enable paging by setting up PageResultRequestControl
            const int pageSize = 1000;  // Set page size (adjust based on your server's limits)
            PageResultRequestControl pageRequestControl = new(pageSize);

            // Define the search request
            SearchRequest searchRequest = new(_baseDn, "(userCertificate=*)", SearchScope.Subtree, "userCertificate");
            searchRequest.Controls.Add(pageRequestControl);

            SearchResponse searchResponse;
            do
            {
                // Send the search request
                searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                _logger.LogInformation("Found {Count} certificates", searchResponse.Entries.Count);
                // Iterate over search results
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    _logger.LogInformation("DN: {DistinguishedName}", entry.DistinguishedName);
                    if (entry.Attributes.Contains("userCertificate"))
                    {
                        foreach (var attr in entry.Attributes["userCertificate"])
                        {
                            byte[] certificateBytes = (byte[])attr;
                            X509Certificate certificate = new(certificateBytes);
                            _logger.LogInformation("Subject: {Subject}, Issuer: {Issuer}, Serial Number: {SerialNumber}",
                            certificate.SubjectDN, certificate.IssuerDN, certificate.SerialNumber);
                            await SaveCertificate(certificate);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No userCertificate found for DN: {DistinguishedName}", entry.DistinguishedName);
                    }
                }
                // Check if more results are available
                PageResultResponseControl? pageResponseControl = searchResponse.Controls?
                    .OfType<PageResultResponseControl>()
                    .FirstOrDefault();

                // If there is a cookie (meaning more results), continue paging
                if (pageResponseControl != null && pageResponseControl.Cookie.Length > 0)
                {
                    pageRequestControl.Cookie = pageResponseControl.Cookie;
                }
                else
                {
                    break;  // No more pages
                }
            } while (searchResponse.Controls.OfType<PageResultResponseControl>().Any());
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

        var participant = await _storage.Participants.FirstOrDefaultAsync(p => p.Id == GetCommonName(certificate.SubjectDN.ToString()), cancellationToken: ct) ?? new Domain.Models.BL.Participant
        {
            Id = GetCommonName(certificate.SubjectDN.ToString()),
            Name = GetCommonName(certificate.SubjectDN.ToString()),
            BicNo = GetCommonName(certificate.SubjectDN.ToString()),
            User = new User
            {
                Username = GetCommonName(certificate.SubjectDN.ToString()),
                Phone = GetCommonName(certificate.SubjectDN.ToString()),
            }
        };
        var cert = new Domain.Models.BL.Certificate
        {
            Id = serialNumber.ToString(),
            Name = ConvertCertificateToPem(certificate),
            Subject = GetCommonName(certificate.SubjectDN.ToString()),
            Issuer = certificate.IssuerDN.ToString(),
            ExpireDate = certificate.NotAfter,
            IssuedAt = certificate.NotBefore,
            CachedOn = DateTimeOffset.Now,
            // check if certificate is not expired
            Active = certificate.NotAfter > DateTimeOffset.Now,
            Participant = participant,
            IsRevoked = false,
        };

        await _storage.Certificates.AddAsync(cert, ct);
        await _storage.SaveChangesAsync(ct);
    }
    private static string GetCommonName(string name)
    {
        var cn = name.Split(',').FirstOrDefault(n => n.Contains("CN="));
        return cn?.Split('=')[1] ?? name;
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

    private static string ConvertHexToDecimal(BigInteger serialNumber)
    {
        return serialNumber.ToString();
    }
}