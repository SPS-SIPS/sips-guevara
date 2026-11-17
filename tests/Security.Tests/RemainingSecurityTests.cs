using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Mediator;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPSS.App.Common;
using SPSS.App.Features.Auth.Commands;
using SPSS.App.Features.Balance.Queries;
using SPSS.App.Features.NSI.Queries;
using SPSS.App.Features.Thresholds.Queries;
using SPSS.Domain.Exceptions;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Identity;
using SPSS.Infrastructure.Identity.Services;
using Xunit;
using static SPSS.Domain.Common.KnownClaims;
using Claim = System.Security.Claims.Claim;

namespace Security.Tests;

public sealed class RemainingApiTests(SecurityApi factory) : IClassFixture<SecurityApi>
{
    [Fact]
    public async Task GlobalFallbackRejectsAnUnattributedControllerAction()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/api/v1/Enumerations/fileTypes");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void OpenApiGenerationStillWorksOnTheSupportedRuntime()
    {
        using var scope = factory.Services.CreateScope();
        var swagger = scope.ServiceProvider.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>().GetSwagger("v1");
        Assert.Contains("/api/v1/Auth/login", swagger.Paths.Keys);
        Assert.Equal(2, swagger.Components.Schemas["LoginCommand"].Properties.Count);
        Assert.Equal(4, swagger.Components.Schemas["LoginDto"].Properties.Count);
    }

    [Fact]
    public async Task PublicLoginRemainsReachableWithOnlyUsernameAndPassword()
    {
        using var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var response = await client.PostAsJsonAsync("/api/v1/Auth/login", new { Username = "missing", Password = "missing" });
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class AuthorizationSecurityTests
{
    [Fact]
    public async Task MissingPolicyIsDeniedEvenForAuthenticatedStaff()
    {
        var pipeline = new AuthorizationBehavior<UnmarkedRequest, int>(new TestIdentity("", QueryTransactions));
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await pipeline.Handle(new(), (_, _) => new(1), default));
    }

    [Fact]
    public async Task BankRoleAssignmentsCannotGrantSupervisorQueriesOrDownloads()
    {
        var identity = new TestIdentity("BANKSOSS", QueryTransactions, QueryParticipantThreshold, DownloadNSI, QueryParticipantNSI);
        var thresholds = new AuthorizationBehavior<GetParticipantThresholdQuery, SPSS.Domain.Models.DTOs.Response<ParticipantThresholdDto>>(identity);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await thresholds.Handle(new(42), (_, _) => throw new Exception("Handler must not run"), default));
        var download = new AuthorizationBehavior<DownloadSettlementFileQuery, Response<(MemoryStream,string)>>(identity);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await download.Handle(new(42), (_, _) => throw new Exception("Handler must not run"), default));
    }

    [Fact]
    public async Task ParticipantListOnlyContainsHeadersInvolvingThatBank()
    {
        await using var db = SecurityUnitTests.Database();
        db.NetSettlementHeaders.AddRange(
            new() { Id = 1, MsgId = "one", CredDtTm = DateTimeOffset.UtcNow.Date, NetSettlementDetails = [new() { DebtorBic = "BANKSOSS", CreditorBic = "OTHESOSS" }] },
            new() { Id = 2, MsgId = "two", CredDtTm = DateTimeOffset.UtcNow.Date, NetSettlementDetails = [new() { DebtorBic = "EVILSOSS", CreditorBic = "OTHESOSS" }] });
        await db.SaveChangesAsync();
        var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap<SPSS.Domain.Models.BL.NetSettlementHeader, NetSettlementHeaderSimpleDto>(), NullLoggerFactory.Instance);
        var handler = new GetSettlementInfoQueryHandler(db, mapperConfig.CreateMapper(), new TestIdentity("BANKSOSS", QueryParticipantNSI));
        var result = await handler.Handle(new(DateTimeOffset.UtcNow.ToString("yyyy-MM-dd")), default);
        Assert.Single(result.Data); Assert.Equal(1, result.Data[0].Id);
    }

    private sealed record UnmarkedRequest : IRequest<int>;
}

internal sealed class TestIdentity(string tenant, params string[] permissions) : IIdentityService
{
    public int Id => 1;
    public string TenantId => tenant;
    public bool IsAuthenticated => true;
    public string Name => "test";
    public string Username => "test";
    public bool IsInRole(string role) => permissions.Contains(role);
    public bool HasClaim(string type) => IsInRole(type);
    public bool Authorize(string token) => true;
    public bool IsMaster() => false;
}

public sealed class LdapAndCryptoSecurityTests
{
    [Theory]
    [InlineData("Plain")]
    [InlineData("None")]
    public async Task LdapRejectsInsecureTransportBeforeBindingCredentials(string transport)
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["LDAP:Server"] = "unused.invalid", ["LDAP:Username"] = "test", ["LDAP:Password"] = "test-only",
            ["LDAP:BaseDN:0"] = "dc=test", ["LDAP:Transport"] = transport
        }).Build();
        var service = new SPSS.Infrastructure.Services.LDAPCertificateFetcher(configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SPSS.Infrastructure.Services.LDAPCertificateFetcher>.Instance, null!);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAllCertificates());
    }

    [Fact]
    public void OfficialBouncyCastlePackageParsesAndVerifiesCertificatesFromDotNet()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest("CN=interop-test", rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        var parsed = new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(certificate.RawData);
        parsed.Verify(parsed.GetPublicKey());
        Assert.Contains("CN=interop-test", parsed.SubjectDN.ToString());
    }
}
