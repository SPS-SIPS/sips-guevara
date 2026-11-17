using Microsoft.EntityFrameworkCore;
using NTran.App.Features.Users.Commands;
using SPSS.App.Common;
using SPSS.App.Features.Roles.Commands;
using SPSS.App.Features.Users.Commands;
using SPSS.App.Features.Users.Queries;
using SPSS.Domain.Exceptions;
using SPSS.Domain.Models.DTOs;
using SPSS.Domain.Models.Identity;
using SPSS.Infrastructure.Services;
using Xunit;
using static SPSS.Domain.Common.KnownClaims;

namespace Security.Tests;

public sealed class PermissionDelegationTests
{
    [Fact]
    public async Task RoleEditorCannotIncreaseItsOwnPermissionsThroughThePipeline()
    {
        await using var storage = SecurityUnitTests.Database();
        var role = NewRole(UpdateRole);
        var user = NewUser(role); storage.Users.Add(user); await storage.SaveChangesAsync();
        var caller = new TestIdentity("", UpdateRole);
        var handler = new EditRoleCommandHandler(storage, new EmptyRoles(), new AppSettings(), caller);
        var pipeline = new AuthorizationBehavior<EditRoleCommand, Response<Guid>>(caller);
        var request = new EditRoleCommand(role.Id, role.Name, [UpdateRole, QueryTransactions]);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await pipeline.Handle(request, (message, ct) => handler.Handle(message, ct), default));
        storage.ChangeTracker.Clear();
        var saved = await storage.Roles.Include(x => x.RoleClaims).SingleAsync();
        Assert.DoesNotContain(QueryTransactions, saved.RoleClaims.Select(x => x.Type));
    }

    [Fact]
    public async Task RoleCreatorCannotManufactureAnEquivalentPrivilegedRole()
    {
        await using var storage = SecurityUnitTests.Database();
        var handler = new AddRoleCommandHandler(storage, new EmptyRoles(), new TestIdentity("", CreateRole));
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await handler.Handle(new("Ordinary-looking role", [QueryTransactions]), default));
        Assert.Empty(await storage.Roles.ToListAsync());
    }

    [Fact]
    public async Task LimitedEditorCannotDemoteOrDeleteAHigherAuthorityRole()
    {
        await using var storage = SecurityUnitTests.Database();
        var role = NewRole(QueryTransactions); storage.Roles.Add(role); await storage.SaveChangesAsync();
        var caller = new TestIdentity("", UpdateRole, RemoveRole);
        var edit = new EditRoleCommandHandler(storage, new EmptyRoles(), new AppSettings(), caller);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await edit.Handle(new(role.Id,"Renamed",[UpdateRole]),default));
        var remove = new RemoveRoleCommandHandler(storage, new EmptyRoles(), new AppSettings(), caller);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await remove.Handle(new(role.Id),default));
        Assert.Equal("Delegated role", role.Name);
        Assert.True(await storage.Roles.AnyAsync(x => x.Id == role.Id));
    }

    [Fact]
    public async Task UserCreatorCannotAssignAnExistingHigherAuthorityRole()
    {
        await using var storage = SecurityUnitTests.Database();
        var role = NewRole(QueryTransactions); storage.Roles.Add(role); await storage.SaveChangesAsync();
        var handler = new AddUserCommandHandler(storage, new PasswordHash(), new AppSettings(), new TestIdentity("", CreateUser));
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await handler.Handle(new("123456789", "test@example.test", "new-user", "TestPassword@123", [role.Id]),default));
        Assert.Empty(await storage.Users.ToListAsync());
    }

    [Fact]
    public async Task UserEditorCannotAssignItselfAnExistingHigherAuthorityRole()
    {
        await using var storage = SecurityUnitTests.Database();
        var own = NewRole(UpdateUser); var elevated = NewRole(QueryTransactions); elevated.Name = "Elevated";
        var user = NewUser(own); storage.Users.Add(user); storage.Roles.Add(elevated); await storage.SaveChangesAsync();
        var handler = new EditUserCommandHandler(storage, new PasswordHash(), new AppSettings(), new TestIdentity("", UpdateUser));
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await handler.Handle(new(user.Id,user.Phone,user.Email,user.Username,null,[own.Id,elevated.Id]),default));
        Assert.DoesNotContain(user.Roles,x => x.RoleId == elevated.Id);
    }

    [Fact]
    public async Task LimitedUserEditorCannotResetOrDeleteAHigherAuthorityUser()
    {
        await using var storage = SecurityUnitTests.Database();
        var user = NewUser(NewRole(QueryTransactions)); user.PasswordHash = "unchanged";
        storage.Users.Add(user); await storage.SaveChangesAsync();
        var caller = new TestIdentity("", UpdateUser, DeleteUser);
        var edit = new EditUserCommandHandler(storage,new PasswordHash(),new AppSettings(),caller);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await edit.Handle(new(user.Id,user.Phone,user.Email,user.Username,"TestPassword@123",[user.Roles.Single().RoleId]),default));
        var delete = new DeleteUserCommandHandler(storage,new AppSettings(),caller);
        await Assert.ThrowsAsync<ForbiddenAccessException>(async () => await delete.Handle(new(user.Id),default));
        Assert.Equal("unchanged",user.PasswordHash); Assert.True(user.Active);
    }

    [Fact]
    public async Task AuthorizedStaffCanCreateEditAndAssignPermissionsItAlreadyHolds()
    {
        await using var storage = SecurityUnitTests.Database();
        var caller = new TestIdentity("", CreateRole, UpdateRole, CreateUser, QueryTransactions);
        var create = new AddRoleCommandHandler(storage,new EmptyRoles(),caller);
        var result = await create.Handle(new("Reader",[QueryTransactions]),default);
        var edit = new EditRoleCommandHandler(storage,new EmptyRoles(),new AppSettings(),caller);
        await edit.Handle(new(result.Data,"Reader updated",[QueryTransactions]),default);
        var createUser = new AddUserCommandHandler(storage,new PasswordHash(),new AppSettings(),caller);
        await createUser.Handle(new("123456789","reader@example.test","reader","TestPassword@123",[result.Data]),default);
        Assert.Equal(result.Data,(await storage.UserRoles.SingleAsync()).RoleId);
    }

    private static Role NewRole(params string[] permissions) => new() { Id=Guid.NewGuid(), Name="Delegated role", NormalizedName=Guid.NewGuid().ToString(),
        RoleClaims=permissions.Select(x => new RoleClaim { Type=x }).ToList() };
    private static User NewUser(Role role) => new() { Id=1, Username="delegated", Phone="123456789",Email="delegated@example.test",
        Roles=[new() { Role=role,RoleId=role.Id,UserId=1 }] };
}
