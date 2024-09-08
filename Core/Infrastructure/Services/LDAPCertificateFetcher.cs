using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger) : ICertificateRetriever
{
    private readonly string _baseDn = configuration["LDAP:BaseDN"] ?? throw new ArgumentNullException("LDAP:BaseDN");
    private readonly ILogger<LDAPCertificateFetcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly string _ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
    private readonly string _ldapUser = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
    private readonly string _ldapPassword = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");

    public void GetAllCertificates()
    {
        try
        {
            _logger.LogInformation("Getting Certificate");
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
                    // Convert the userCertificate binary data to an X509Certificate2 object
                    byte[] certificateBytes = (byte[])attr;
                    System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = new(certificateBytes);

                    _logger.LogInformation("Subject: {Subject}, Issuer: {Issuer}, Serial Number: {SerialNumber}", certificate.Subject, certificate.Issuer, certificate.SerialNumber);
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
}