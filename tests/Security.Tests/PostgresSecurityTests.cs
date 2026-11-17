using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SPSS.Domain.Models.DTOs;
using SPSS.Infrastructure.Identity.Services;
using Npgsql;
using SPSS.Domain.Models.Identity;
using SPSS.Infrastructure.Persistence;
using SPSS.Infrastructure.Services;
using Xunit;

namespace Security.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SPSS_SECURITY_TEST_CONNECTION")))
            Skip = "Set SPSS_SECURITY_TEST_CONNECTION for isolated PostgreSQL integration tests.";
    }
}

// Creates and removes its own randomly named database; never migrates the configured database.
public sealed class PostgresSecurityTests
{
    [PostgresFact]
    public async Task SharedThrottleAtomicallyAllowsOnlyFiveAttemptsAcrossConnectionsAndAliases()
    {
        await using var fixture = await IsolatedDatabase.Create();
        int userId;
        await using (var storage = fixture.Open())
        {
            var user = NewUser(); storage.Users.Add(user); await storage.SaveChangesAsync(); userId = user.Id;
        }
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(async _ =>
        {
            await using var storage = fixture.Open();
            return await storage.TryReserveLoginAttemptAsync(userId, default);
        }));
        Assert.Equal(5, results.Count(x => x));
        await using var check = fixture.Open();
        var stored = await check.Users.SingleAsync(x => x.Id == userId);
        Assert.Equal(5, stored.LoginAttemptCount);
        Assert.False(await check.TryReserveLoginAttemptAsync(userId, default));
        stored.LoginWindowStart = DateTimeOffset.UtcNow.AddMinutes(-16);
        await check.SaveChangesAsync();
        Assert.True(await check.TryReserveLoginAttemptAsync(userId, default));
        await check.Entry(stored).ReloadAsync();
        Assert.Equal(1, stored.LoginAttemptCount);
    }

    [PostgresFact]
    public async Task DisabledAndDeletedAccountsCannotReserveAuthenticationAttempts()
    {
        await using var fixture = await IsolatedDatabase.Create();
        await using var storage = fixture.Open();
        var disabled = NewUser(); disabled.Active = false;
        var deleted = NewUser(); deleted.DeletedOn = DateTimeOffset.UtcNow;
        storage.Users.AddRange(disabled, deleted); await storage.SaveChangesAsync();
        Assert.False(await storage.TryReserveLoginAttemptAsync(disabled.Id, default));
        Assert.False(await storage.TryReserveLoginAttemptAsync(deleted.Id, default));
    }

    [PostgresFact]
    public async Task UsernameAndPasswordProvideAFullSessionAndRefreshWithoutExtraSteps()
    {
        await using var fixture = await IsolatedDatabase.Create();
        var user = NewUser();
        const string password = "TestAccountPassword@123";
        user.PasswordHash = new PasswordHash().HashPassword(user, password);
        await using (var storage = fixture.Open()) { storage.Users.Add(user); await storage.SaveChangesAsync(); }
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            foreach (var setting in new Dictionary<string, string> {
                ["OpenApiExport"] = "true", ["JwtSettings:Secret"] = new string('s', 64),
                ["JwtSettings:Issuer"] = "password-login-test", ["JwtSettings:Audience"] = "password-login-test",
                ["JwtSettings:TokenLifetime"] = "00:15:00", ["JwtSettings:RefreshTokenLifetime"] = "01:00:00",
                ["ConnectionStrings:db"] = fixture.Connection
            }) builder.UseSetting(setting.Key, setting.Value);
        });
        using var client = app.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        var wrong = await client.PostAsJsonAsync("/api/v1/Auth/login", new { user.Username, Password = "incorrect" });
        Assert.False((await wrong.Content.ReadFromJsonAsync<Response<LoginDto>>())!.IsSuccess);
        var login = await client.PostAsJsonAsync("/api/v1/Auth/login", new { user.Username, Password = password });
        login.EnsureSuccessStatusCode();
        var session = (await login.Content.ReadFromJsonAsync<Response<LoginDto>>())!.Data;
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
        Assert.Equal('4', session.RefreshToken.ToString("D")[14]);
        Assert.Equal(4, typeof(LoginDto).GetProperties().Length);
        Assert.Equal(2, typeof(SPSS.App.Features.Auth.Commands.LoginCommand).GetProperties().Length);
        client.DefaultRequestHeaders.Authorization = new("Bearer", session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Auth")).StatusCode);
        var refreshed = await client.PostAsJsonAsync("/api/v1/Auth/refresh", new { session.AccessToken, session.RefreshToken });
        refreshed.EnsureSuccessStatusCode();
        var next = (await refreshed.Content.ReadFromJsonAsync<Response<LoginDto>>())!.Data;
        // Refresh consumes the old session, while the newly issued session works immediately.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/Auth")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new("Bearer", next.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Auth")).StatusCode);
    }

    [PostgresFact]
    public async Task MigrationRemovesRetiredTokenRowsAndPreservesRefreshSessions()
    {
        await using var fixture = await IsolatedDatabase.Create();
        await using var storage = fixture.Open();
        var user = NewUser(); storage.Users.Add(user); await storage.SaveChangesAsync();
        var refresh = new Token { User = user, Jti = Guid.NewGuid(), CreatedOn = DateTimeOffset.UtcNow,
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1), IPAddress = "127.0.0.1", UserAgent = "test", OS = "test" };
        storage.Tokens.Add(refresh); await storage.SaveChangesAsync();
        var migrator = storage.GetService<IMigrator>();
        await migrator.MigrateAsync("20260715050856_OracleFailedTransactionAnalyticsClassification");
        var retiredId = Guid.NewGuid(); var now = DateTimeOffset.UtcNow; var jti = Guid.NewGuid(); var expires = now.AddMinutes(5);
        await storage.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO tokens (id, jti, createdon, expireson, used, invalidated, tokentype, userid, ipaddress, useragent, os, active, value) VALUES ({retiredId}, {jti}, {now}, {expires}, false, false, 2, {user.Id}, '127.0.0.1', 'test', 'test', true, 123456)");
        await migrator.MigrateAsync();
        storage.ChangeTracker.Clear();
        Assert.False(await storage.Tokens.AnyAsync(x => x.Id == retiredId));
        var remaining = await storage.Tokens.SingleAsync();
        Assert.Equal(refresh.Id, remaining.Id);
        Assert.Equal(SPSS.Domain.Enums.TokenType.Refresh, remaining.TokenType);
        Assert.False(storage.Database.HasPendingModelChanges());
    }

    private static User NewUser() => new() { Username = Guid.NewGuid().ToString("N"), Phone = Guid.NewGuid().ToString("N"), CreatedBy = "test" };
}

internal sealed class IsolatedDatabase(string adminConnection, string connection, string name) : IAsyncDisposable
{
    public string Connection => connection;
    public StorageBroker Open() => new(new DbContextOptionsBuilder<StorageBroker>().UseNpgsql(connection).UseLowerCaseNamingConvention().Options);
    public static async Task<IsolatedDatabase> Create()
    {
        var admin = Environment.GetEnvironmentVariable("SPSS_SECURITY_TEST_CONNECTION")!;
        var name = "spss_security_" + Guid.NewGuid().ToString("N");
        await using var db = new NpgsqlConnection(admin); await db.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {name}", db)) await create.ExecuteNonQueryAsync();
        var fixture = new IsolatedDatabase(admin, new NpgsqlConnectionStringBuilder(admin) { Database = name }.ConnectionString, name);
        try
        {
            await using var storage = fixture.Open();
            await storage.Database.MigrateAsync();
            Assert.False(storage.Database.HasPendingModelChanges());
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }
    public async ValueTask DisposeAsync()
    {
        await using var db = new NpgsqlConnection(adminConnection); await db.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE {name} WITH (FORCE)", db);
        await drop.ExecuteNonQueryAsync();
    }
}
