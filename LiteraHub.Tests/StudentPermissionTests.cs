using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.Services.Security;
using Xunit;

namespace LiteraHub.Tests;

public sealed class StudentPermissionTests
{
    [Theory]
    [InlineData("Borrower", 10)]
    [InlineData("Borrower", null)]
    [InlineData("Admin", 10)]
    public void StudentOrInvalidLinkedIdentityCannotUseStaffPermissions(string role, int? studentId)
    {
        var session = new SessionContext(); session.SignIn(new UserSession(7, "test", role, studentId));
        var service = new PermissionService(session, new AllKeysRepository());
        Assert.Empty(service.GrantedPermissions()); Assert.Empty(service.GrantedRolePermissions());
        Assert.Throws<RolePermissionDeniedException>(() => service.Demand(RolePermission.StudentsManageAccounts));
        Assert.False(service.Can(RolePermission.BooksView));
    }
    [Fact]
    public void UnlinkedStaffKeepsGrantedPermissions()
    {
        var session = new SessionContext(); session.SignIn(new UserSession(7, "staff", "Admin", null));
        Assert.True(new PermissionService(session, new AllKeysRepository()).Can(RolePermission.StudentsManageAccounts));
    }
    private sealed class AllKeysRepository : IRoleRepository
    {
        public IReadOnlyList<string> GetActiveUserPermissionKeys(int userId, string roleName) =>
            Enum.GetNames<Permission>().Concat(Enum.GetNames<RolePermission>()).ToArray();
        public Task<IReadOnlyList<RoleDefinition>> GetAllAsync() => throw new NotSupportedException();
        public Task<int> CreateAsync(RoleInput input, UserSession actor) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(int roleId, RoleInput input, UserSession actor) => throw new NotSupportedException();
        public Task<bool> SetActiveAsync(int roleId, bool active, UserSession actor) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(int roleId, UserSession actor) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetPermissionKeysAsync(int roleId) => throw new NotSupportedException();
    }
}
