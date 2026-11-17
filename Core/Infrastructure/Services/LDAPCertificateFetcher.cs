using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger, IStorageBroker storage) : ICertificateRetriever
{
    private readonly string[] _baseDn = configuration.GetSection("LDAP:BaseDN").Get<string[]>() ?? throw new ArgumentNullException("LDAP:BaseDN");
    private readonly ILogger<LDAPCertificateFetcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
    private readonly string _ldapUser = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
    private readonly string _ldapPassword = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");
    private readonly IStorageBroker _storage = storage;
    public async Task GetAllCertificates()
    {
        foreach (var dn in _baseDn)
        {
            await GetCertificateByDN(dn);
        }
    }

    private async Task GetCertificateByDN(string dn)
    {
        // TLS is mandatory; never bind credentials before the secure channel exists.
        var mode = configuration["LDAP:Transport"] ?? "Ldaps";
        if (mode is not ("Ldaps" or "StartTls"))
            throw new InvalidOperationException("LDAP:Transport must be Ldaps or StartTls.");
        var port = configuration.GetValue<int?>("LDAP:Port") ?? (mode == "Ldaps" ? 636 : 389);
        using var connection = new LdapConnection(new LdapDirectoryIdentifier(_ldapServer, port),
            new NetworkCredential(_ldapUser, _ldapPassword), AuthType.Basic);
        connection.AutoBind = false;
        connection.Timeout = TimeSpan.FromSeconds(30);
        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;
        // Platform trust and hostname validation remain enabled. There is no certificate bypass.
        if (mode == "Ldaps") connection.SessionOptions.SecureSocketLayer = true;
        else connection.SessionOptions.StartTransportLayerSecurity(null);
        connection.Bind();

        // Enable paging by setting up PageResultRequestControl
        const int pageSize = 1000;  // Set page size (adjust based on your server's limits)
        PageResultRequestControl pageRequestControl = new(pageSize);

        // Define the search request
        SearchRequest searchRequest = new(dn, "(userCertificate=*)", SearchScope.Subtree, "userCertificate");
        searchRequest.Controls.Add(pageRequestControl);

        SearchResponse? searchResponse;
        do
        {
            try
            {
                // Send the search request
                searchResponse = (SearchResponse)connection.SendRequest(searchRequest);
                _logger.LogWarning("DN: {DN}", searchRequest.DistinguishedName);
                _logger.LogWarning("Filter: {Filter}", searchRequest.Filter);
                _logger.LogWarning("Result Code: {Code}", searchResponse.ResultCode);
                _logger.LogWarning("Error Message: {MSG}", searchResponse.ErrorMessage);
                _logger.LogWarning("Found {Count} certificates", searchResponse.Entries.Count);
                // Iterate over search results
                foreach (SearchResultEntry entry in searchResponse.Entries)
                {
                    if (entry.Attributes.Contains("userCertificate"))
                    {
                        foreach (var attr in entry.Attributes["userCertificate"])
                        {
                            try
                            {
                                byte[] certificateBytes = (byte[])attr;
                                X509Certificate certificate = new(certificateBytes);
                                _logger.LogInformation("Subject: {Subject}, Issuer: {Issuer}, Serial Number: {SerialNumber}",
                                    certificate.SubjectDN, certificate.IssuerDN, certificate.SerialNumber);
                                await SaveCertificate(certificate);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed processing certificate from LDAP entry {DN}", entry.DistinguishedName);
                                // continue with the next certificate
                            }
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
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while fetching certificates from LDAP server {Exception}", ex.Message);
                break;
            }
        } while (searchResponse.Controls?.OfType<PageResultResponseControl>().Any() ?? false);

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

        var commonName = GetCommonName(certificate.SubjectDN.ToString()).Trim();
        var participant = await _storage.Participants
            .FirstOrDefaultAsync(p => p.Id == commonName, cancellationToken: ct)
            ?? await _storage.Participants
                .FirstOrDefaultAsync(p => p.Id != null && EF.Functions.ILike(p.Id, commonName), cancellationToken: ct);

        if (participant == null)
        {
            // Ensure a User exists for this CN
            var user = await _storage.Users
                .FirstOrDefaultAsync(u => u.Username == commonName, cancellationToken: ct)
                ?? await _storage.Users
                    .FirstOrDefaultAsync(u => u.Username != null && EF.Functions.ILike(u.Username, commonName), cancellationToken: ct);

            if (user == null)
            {
                user = new User
                {
                    Username = commonName,
                    Phone = commonName,
                };
                await _storage.Users.AddAsync(user, ct);
            }

            var newParticipant = new Domain.Models.BL.Participant
            {
                Id = commonName,
                Name = commonName,
                BicNo = commonName,
                User = user,
                Active = true,
                Email = string.Empty,
            };

            try
            {
                await _storage.Participants.AddAsync(newParticipant, ct);
                await _storage.SaveChangesAsync(ct);
                participant = newParticipant;
            }
            catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("duplicate key") ?? false) || (ex.InnerException?.Message?.Contains("23505") ?? false))
            {
                // Another process may have created the user/participant concurrently; fetch the participant now
                participant = await _storage.Participants
                    .FirstOrDefaultAsync(p => p.Id == commonName, cancellationToken: ct)
                    ?? await _storage.Participants
                        .FirstOrDefaultAsync(p => p.Id != null && EF.Functions.ILike(p.Id, commonName), cancellationToken: ct);
                if (participant == null)
                {
                    _logger.LogError(ex, "Failed to create participant for CN {CommonName} due to unique constraint, and it could not be re-fetched.", commonName);
                    return;
                }
            }
        }

        var cert = new Domain.Models.BL.Certificate
        {
            Id = serialNumber.ToString(),
            Name = pemCertificate,
            Subject = GetCommonName(certificate.SubjectDN.ToString()),
            Issuer = certificate.IssuerDN.ToString(),
            ExpireDate = certificate.NotAfter.ToUniversalTime(),
            IssuedAt = certificate.NotBefore.ToUniversalTime(),
            CachedOn = DateTimeOffset.Now.ToUniversalTime(),
            Active = certificate.NotAfter.ToUniversalTime() > DateTimeOffset.Now.ToUniversalTime(),
            ParticipantId = participant.Id!,
            IsRevoked = false,
        };

        await _storage.Certificates.AddAsync(cert, ct);
        try
        {
            await _storage.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when ((ex.InnerException?.Message?.Contains("duplicate key") ?? false) || (ex.InnerException?.Message?.Contains("23505") ?? false))
        {
            _logger.LogWarning(ex, "Duplicate certificate insert detected for SerialNumber {SerialNumber}; skipping", serialNumber);
            return;
        }
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