using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NTran.App.Features.Users.Commands;
using SPSS.App.Features.Auth;
using SPSS.App.Features.Auth.Commands;
using SPSS.App.Features.Balance.Commands;
using SPSS.Domain.Exceptions;
using SPSS.Domain.Interfaces;
using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Identity;
using SPSS.Infrastructure.Identity.Services;
using SPSS.Infrastructure.Persistence;
using SPSS.Infrastructure.Persistence.Interceptors;
using SPSS.Infrastructure.Services;
using Xunit;
using Claim = System.Security.Claims.Claim;
using TokenType = SPSS.Domain.Enums.TokenType;

namespace Security.Tests;

public sealed class ApiSecurityTests : IClassFixture<SecurityApi>
{
    private readonly HttpClient client;
    public ApiSecurityTests(SecurityApi factory) => client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    [Fact]
    public async Task ProductionApiRejectsPlaintextRequests()
    {
        Assert.Equal(HttpStatusCode.UpgradeRequired, (await client.GetAsync("http://localhost/api/v1/Auth")).StatusCode);
    }

    [Theory]
    [InlineData("Balance/webhook")]
    [InlineData("Balance/failed-transactions")]
    [InlineData("Balance/processed-transactions")]
    [InlineData("Balance/transactions")]
    [InlineData("Balance/fraud-transactions")]
    [InlineData("Balance/transaction-summary")]
    [InlineData("Transactions/failed-notifications/manual")]
    public async Task AnonymousFinancialWritesAreDenied(string route)
    {
        var response = await client.PostAsJsonAsync("/api/v1/" + route, new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Theory]
    [InlineData("invalid", "invalid", HttpStatusCode.Unauthorized)]
    [InlineData("reader", "reader-secret", HttpStatusCode.Forbidden)]
    [InlineData("monitor", "monitor-secret", HttpStatusCode.OK)]
    public async Task MonitoringClientRequiresValidCredentialsAndScope(string key, string secret, HttpStatusCode status)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Balance/webhook");
        request.Headers.Add("X-API-Key", key); request.Headers.Add("X-API-Secret", secret);
        request.Content = JsonContent.Create(new { participants = Array.Empty<object>() });
        Assert.Equal(status, (await client.SendAsync(request)).StatusCode);
    }
}

public sealed class SecurityApi : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("OpenApiExport", "true");
        foreach (var setting in new Dictionary<string,string?>
        {
            ["OpenApiExport"] = "true", ["JwtSettings:Secret"] = new string('s',64),
            ["JwtSettings:Issuer"] = "security-test", ["JwtSettings:Audience"] = "security-test",
            ["JwtSettings:TokenLifetime"] = "00:15:00",
            ["ApiAuth:0:ClientId"] = "monitor", ["ApiAuth:0:ClientSecret"] = "monitor-secret", ["ApiAuth:0:Scopes:0"] = "CMH",
            ["ApiAuth:1:ClientId"] = "reader", ["ApiAuth:1:ClientSecret"] = "reader-secret", ["ApiAuth:1:Scopes:0"] = "QFT",
            ["ConnectionStrings:db"] = "Host=localhost;Database=unused;Username=unused;Password=unused"
        }) builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<StorageBroker>(); services.RemoveAll<IStorageBroker>();
            services.AddScoped(_ => SecurityUnitTests.Database());
            services.AddScoped<IStorageBroker>(sp => sp.GetRequiredService<StorageBroker>());
        });
    }
}

public sealed class SecurityUnitTests
{
    internal static StorageBroker Database() => new(new DbContextOptionsBuilder<StorageBroker>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void ParsingExpiredTokensDoesNotDisableAccessTokenLifetimeChecks()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('s',64)));
        var parameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = "test", ValidateAudience = true,
            ValidAudience = "test", IssuerSigningKey = key, ValidateLifetime = true, ClockSkew = TimeSpan.Zero };
        var service = new TokenService(new JwtSettings(), parameters, new HttpContextAccessor { HttpContext = new DefaultHttpContext() }, new EmptyRoles());
        string Token(DateTime start, DateTime end) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("test","test",
            [new Claim("sub","1")], start,end,new SigningCredentials(key,SecurityAlgorithms.HmacSha256)));
        var expired = Token(DateTime.UtcNow.AddHours(-2),DateTime.UtcNow.AddHours(-1));
        Assert.NotNull(service.GetClaimsPrincipalFromToken(expired));
        Assert.True(parameters.ValidateLifetime);
        Assert.Null(service.ValidateJwt(expired));
        Assert.Null(service.ValidateJwt(Token(DateTime.UtcNow.AddMinutes(2),DateTime.UtcNow.AddMinutes(3))));
        Assert.NotNull(service.ValidateJwt(Token(DateTime.UtcNow.AddSeconds(-1),DateTime.UtcNow.AddMinutes(1))));
    }

    [Theory]
    [InlineData("deleted")]
    [InlineData("inactive")]
    [InlineData("stamp")]
    [InlineData("invalidated")]
    [InlineData("used")]
    [InlineData("expired")]
    public async Task InvalidSessionsAreRejected(string reason)
    {
        await using var db = Database(); var (user, session, principal) = Session();
        if (reason == "deleted") user.DeletedOn = DateTimeOffset.UtcNow;
        if (reason == "inactive") user.Active = false;
        if (reason == "stamp") user.SecurityStamp = "changed";
        if (reason == "invalidated") session.Invalidated = true;
        if (reason == "used") session.Used = true;
        if (reason == "expired") session.ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(-1);
        db.Users.Add(user); db.Tokens.Add(session); await db.SaveChangesAsync();
        Assert.False(await SessionValidation.ValidateAsync(db, principal, default));
    }

    [Fact]
    public async Task SessionUsesCurrentPermissionsInsteadOfJwtSnapshot()
    {
        await using var db = Database(); var (user, session, principal) = Session();
        var role = new Role { Id = Guid.NewGuid(), Name = "reader", NormalizedName = "READER", RoleClaims = [new RoleClaim { Type = "QFT" }] };
        user.Roles.Add(new UserRole { User = user, Role = role, RoleId = role.Id, UserId = user.Id });
        db.Users.Add(user);db.Tokens.Add(session);await db.SaveChangesAsync();
        Assert.True(await SessionValidation.ValidateAsync(db, principal, default));
        Assert.True(principal.HasClaim("role","QFT"));Assert.False(principal.HasClaim("role","stale-admin"));
    }

    [Fact]
    public async Task DelegatedEditorCannotResetExistingAdmin()
    {
        await using var db = Database();
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", NormalizedName = "ADMIN", Super = true };
        var user = new User { Username = "admin", CreatedBy = "test", PasswordHash = "unchanged" };
        user.Roles.Add(new UserRole { User=user,Role=adminRole,RoleId=adminRole.Id });
        db.Users.Add(user);await db.SaveChangesAsync();
        var handler = new EditUserCommandHandler(db,new PasswordHash(),new AppSettings(),new TestIdentity("", "UUA"));
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await handler.Handle(
            new EditUserCommand(user.Id,"123456789","admin@example.test","admin","NewPassword@123",[adminRole.Id]),default));
        Assert.Equal("unchanged",user.PasswordHash);
    }

    [Fact]
    public void AdministrativePasswordResetsEnforcePolicy()
    {
        var result = new EditUserCommandValidator().Validate(new EditUserCommand(1,"123456789","a@example.test","user","x",[Guid.NewGuid()]));
        Assert.Contains(result.Errors,x=>x.PropertyName=="Password");
    }

    [Fact]
    public async Task DeletingUserDisablesAccountAndInvalidatesSessions()
    {
        await using var db=Database();var (user,session,_)=Session();
        db.Users.Add(user);db.Tokens.Add(session);await db.SaveChangesAsync();
        var handler=new SPSS.App.Features.Users.Commands.DeleteUserCommandHandler(db,new AppSettings(),new TestIdentity("", "DUA"));
        await handler.Handle(new SPSS.App.Features.Users.Commands.DeleteUserCommand(user.Id),default);
        Assert.False(user.Active);Assert.True(session.Invalidated);Assert.NotEqual("stamp",user.SecurityStamp);
    }

    [Fact]
    public async Task SavingOtherUserFieldsDoesNotResurrectSoftDeletedUsers()
    {
        var context=new DefaultHttpContext();
        var identity=new IdentityService(new HttpContextAccessor{HttpContext=context},null!,[]);
        await using var db=new StorageBroker(new DbContextOptionsBuilder<StorageBroker>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new SoftDeleteEntityInterceptor(identity,TimeProvider.System)).Options);
        var user=new User{Username="deleted",CreatedBy="test"};db.Users.Add(user);await db.SaveChangesAsync();
        db.Users.Remove(user);await db.SaveChangesAsync();var deletedOn=user.DeletedOn;Assert.NotNull(deletedOn);
        user.Photo="changed";await db.SaveChangesAsync();Assert.Equal(deletedOn,user.DeletedOn);
    }

    [Fact]
    public async Task DeletedUserCannotLogIn()
    {
        await using var db = Database();var hash=new PasswordHash();
        var user=new User { Username="deleted",Phone="123456789",CreatedBy="test",DeletedOn=DateTimeOffset.UtcNow };
        user.PasswordHash=hash.HashPassword(user,"Password@123");db.Users.Add(user);await db.SaveChangesAsync();
        var handler=new LoginCommandHandler(db,null!,new PasswordManager(hash),new LoginAttemptLimiter());
        var result=await handler.Handle(new LoginCommand("deleted","Password@123"),default);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task BootstrapIsExplicitAndUsesConfiguredSecret()
    {
        await using var db=Database(); var hash=new PasswordHash();
        var config=new ConfigurationBuilder().AddInMemoryCollection().Build();
        await new StorageBrokerInitializer(NullLogger<StorageBrokerInitializer>.Instance,db,hash,config).SeedAsync();
        Assert.False(await db.Users.AnyAsync());
        config["Bootstrap:Enabled"]="true";config["Bootstrap:AdminPassword"]="OneTimeBootstrap@123456";
        await new StorageBrokerInitializer(NullLogger<StorageBrokerInitializer>.Instance,db,hash,config).SeedAsync();
        var admin=await db.Users.SingleAsync();Assert.StartsWith("bootstrap:",admin.SecurityStamp);
        Assert.Equal(SPSS.Domain.Enums.PasswordVerificationResult.Success,hash.VerifyHashedPassword(admin,admin.PasswordHash,"OneTimeBootstrap@123456"));
    }

    [Fact]
    public void ExistingPasswordHashesAreUpgradedAfterSuccessfulVerification()
    {
        var salt=RandomNumberGenerator.GetBytes(16);var subkey=Rfc2898DeriveBytes.Pbkdf2("OldPassword@123",salt,10000,HashAlgorithmName.SHA256,32);
        var payload=new byte[61];payload[0]=1;
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(1),1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(5),10000);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(payload.AsSpan(9),16);
        salt.CopyTo(payload,13);subkey.CopyTo(payload,29);
        var user=new User { PasswordHash=Convert.ToBase64String(payload) };
        var manager=new PasswordManager(new PasswordHash());
        Assert.True(manager.CheckPassword(user,"OldPassword@123"));Assert.False(PasswordHash.NeedsRehash(user.PasswordHash));
        Assert.False(manager.CheckPassword(user,"incorrect"));
        Assert.Equal(600000u,System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(Convert.FromBase64String(user.PasswordHash).AsSpan(5)));
    }

    [Fact]
    public void LoginAttemptsAreLimitedAcrossClientsForAnAccount()
    {
        var attempts=new LoginAttemptLimiter();for(var i=0;i<5;i++) Assert.False(attempts.IsBlocked("USER"));
        Assert.True(attempts.IsBlocked("user"));
    }

    [Fact]
    public void ManualReportsCannotRequestUnboundedHistory()
    {
        var validator=new SendManualFailedTransactionNotificationValidator();var now=DateTimeOffset.UtcNow;
        Assert.False(validator.Validate(new SendManualFailedTransactionNotificationCommand("TESTSOSS",now.AddYears(-1),now)).IsValid);
    }

    private static (User,Token,ClaimsPrincipal) Session()
    {
        var user=new User { Id=1,Username="user",Phone="123456789",CreatedBy="test",SecurityStamp="stamp" };
        var session=new Token { Id=Guid.NewGuid(),Jti=Guid.NewGuid(),UserId=1,User=user,ExpiresOn=DateTimeOffset.UtcNow.AddHours(1),
            IPAddress="127.0.0.1",UserAgent="test",OS="test",TokenType=TokenType.Refresh };
        var principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub","1"),new Claim("jti",session.Jti.ToString()),
            new Claim("security_stamp","stamp"),new Claim("role","stale-admin")],"Bearer"));
        return(user,session,principal);
    }
}

internal sealed class EmptyRoles : IRolesCache
{
    public List<ExtendedRoleDto> _roles { get; } = [];
    public Task<ExtendedRoleDto?> GetRole(Guid id)=>Task.FromResult<ExtendedRoleDto?>(null);
    public Task<List<ExtendedRoleDto>> GetRoles()=>Task.FromResult(_roles);
    public Task<List<ExtendedRoleDto>> GetRoles(Guid[] ids)=>Task.FromResult(_roles);
    public Task<string?> GetParentRole(string claim)=>Task.FromResult<string?>(null);
    public void Invalidate() { }
}
