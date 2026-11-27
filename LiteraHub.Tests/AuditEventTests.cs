using LiteraHub.Core.Models;
using Xunit;

namespace LiteraHub.Tests;

public sealed class AuditEventTests
{
    [Fact]
    public void MutationDefaultsToSuccess()
    {
        var entry = new AuditEvent(AuditModule.Books, AuditAction.Created, 42);
        Assert.Equal(AuditOutcome.Success, entry.Outcome);
        Assert.Equal(42, entry.EntityId);
        Assert.Equal("Books: Created", entry.Description);
    }
    [Fact]
    public void UnknownLoginDoesNotRequireEntityIdentity()
    {
        var entry = new AuditEvent(AuditModule.Authentication, AuditAction.LoginRejected, null, AuditOutcome.Denied);
        Assert.Null(entry.EntityId);
        Assert.Equal(AuditOutcome.Denied, entry.Outcome);
        Assert.Equal("Authentication: LoginRejected", entry.Description);
    }
}