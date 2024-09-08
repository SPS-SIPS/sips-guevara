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
    private readonly string ldapPath;
    private readonly string ldapUser;
    private readonly string ldapPassword;
    public LDAPCertificateFetcher(IConfiguration configuration, ILogger<LDAPCertificateFetcher> logger)
    {
        var ldapServer = configuration["LDAP:Server"] ?? throw new ArgumentNullException("LDAP:Server");
        ldapUser = configuration["LDAP:Username"] ?? throw new ArgumentNullException("LDAP:Username");
        ldapPassword = configuration["LDAP:Password"] ?? throw new ArgumentNullException("LDAP:Password");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseDn = configuration["LDAP:BaseDN"] ?? throw new ArgumentNullException("LDAP:BaseDN");
        ldapPath = $"{ldapServer}/{_baseDn}";
        _ldapConnection = new LdapConnection(ldapServer)
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
        DirectoryEntry ldapConnection = new(ldapPath, ldapUser, ldapPassword);

        DirectorySearcher searcher = new(ldapConnection)
        {
            Filter = "(userCertificate=*)",  // Filter for entries with a userCertificate
            PageSize = 1000  // Fetch 1000 results per page (adjust as needed)
        };

        // Specify that we only need to retrieve the 'userCertificate' attribute
        searcher.PropertiesToLoad.Add("userCertificate");

        // Perform the search and iterate over the results
        SearchResultCollection results = searcher.FindAll();

        foreach (SearchResult result in results)
        {
            if (result.Properties.Contains("userCertificate"))
            {
                // Iterate over each userCertificate (in case there are multiple certificates)
                foreach (var cert in result.Properties["userCertificate"])
                {
                    byte[] certificateBytes = (byte[])cert;

                    // Convert the binary certificate to an X509Certificate2 object
                    _logger.LogInformation("Certificate found, Length: {content}", Convert.ToBase64String(certificateBytes));
                    System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = new(certificateBytes);
                    // Print certificate details
                    _logger.LogInformation("Certificate found, Subject: {Subject}, Issuer: {Issuer}, Serial Number: {SerialNumber}", certificate.Subject, certificate.Issuer, certificate.SerialNumber);
                }
            }
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