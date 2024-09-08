using System.DirectoryServices;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

namespace SPSS.Infrastructure.Services;
public sealed class LDAPCertificateFetcher : IDisposable, ICertificateRetriever
{
    private readonly string _baseDn;
    private readonly LdapConnection _ldapConnection;
    private readonly ILogger<LDAPCertificateFetcher> _logger;
    private readonly string _ldapServer;
    public LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger)
    {
        _ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
        var ldapUser = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
        var ldapPassword = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseDn = configuration["LDAP:BaseDN"] ?? throw new ArgumentNullException("LDAP:BaseDN");
        _ldapConnection = new LdapConnection(_ldapServer)
        {
            Credential = new NetworkCredential(ldapUser, ldapPassword)
        };
        _ldapConnection.Bind();
    }


    public X509Certificate? GetCertificateBySerialNumber(string serialNumber)
    {
        var searchFilter = $"(serialNumber={serialNumber})";
        var searchRequest = new SearchRequest(_baseDn, searchFilter, System.DirectoryServices.Protocols.SearchScope.Subtree, "userCertificate");

        var searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);

        foreach (SearchResultEntry entry in searchResponse.Entries)
        {
            if (entry.Attributes["userCertificate"]?[0] is byte[] certData)
            {
                return ParseCertificate(certData);
            }
        }
        return null;
    }

    public void GetAllCertificates()
    {
        try
        {
            LdapDirectoryIdentifier identifier = new(_ldapServer, true, false);
            // Specify that we only need to retrieve the 'userCertificate' attribute
            var searchFilter = "(userCertificate=*)";
            // Define the search request
            SearchRequest searchRequest = new(_baseDn, searchFilter, SearchScope.Subtree, "userCertificate");

            // Send the search request and get the response
            SearchResponse searchResponse = (SearchResponse)_ldapConnection.SendRequest(searchRequest);

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

    public void Dispose()
    {
        _ldapConnection?.Dispose();
    }
}