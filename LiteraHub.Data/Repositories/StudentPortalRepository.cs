using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Security;

namespace LiteraHub.Data.Repositories;

public sealed class StudentPortalInfo
{
    public string StudentNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
}

public sealed class StudentPortalRepository(IDbConnectionFactory factory, ISessionContext session)
{
    public async Task<StudentPortalInfo?> GetOwnAsync()
    {
        var actor = session.Current;
        if (actor is null || !StudentAccountPolicy.IsStudentRole(actor.RoleName) || actor.StudentId is not > 0) return null;
        await using var db = factory.CreateConnection(); await db.OpenAsync();
        var result = await db.QuerySingleOrDefaultAsync<StudentPortalInfo>($$"""
            SELECT s.StudentNo, CONCAT_WS(' ',s.FirstName,NULLIF(s.MiddleName,''),s.LastName) AS FullName,u.Username
            FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId JOIN dbo.Students s ON s.StudentId=u.StudentId
            WHERE {{AccountEligibility.Sql}} AND {{AccountEligibility.OwnSession}};
            """, new { userId = actor.UserId, sessionRole = actor.RoleName, sessionStudentId = actor.StudentId, sessionVersion = actor.SessionVersion });
        return session.Current == actor ? result : null;
    }
}
