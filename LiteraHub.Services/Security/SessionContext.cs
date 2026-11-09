using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Services.Security;

public sealed class SessionContext : ISessionContext
{
    public UserSession? Current { get; private set; }

    public void SignIn(UserSession session) => Current = session;

    public void SignOut() => Current = null;
}