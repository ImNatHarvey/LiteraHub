namespace LiteraHub.Data.Repositories;

internal static class AccountEligibility
{
    internal const string Sql = """
        u.IsActive=1 AND r.IsActive=1 AND
        ((r.Name=N'Borrower' AND r.IsSystem=1 AND u.IsSystemOwner=0 AND u.StudentId IS NOT NULL
          AND EXISTS (SELECT 1 FROM dbo.Students eligibleStudent WHERE eligibleStudent.StudentId=u.StudentId AND eligibleStudent.IsActive=1))
         OR (r.Name NOT IN(N'Borrower',N'Student') AND u.StudentId IS NULL))
        """;

    internal const string OwnSession = """
        u.UserId=@userId AND r.Name=@sessionRole AND u.SessionVersion=@sessionVersion
        AND ((u.StudentId IS NULL AND @sessionStudentId IS NULL) OR u.StudentId=@sessionStudentId)
        AND u.MustChangePassword=0
        """;
}
