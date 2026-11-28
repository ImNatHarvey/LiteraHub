using System.Data;
using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class StudentRepository : IStudentRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ISessionContext _session;

    private const string SelectSql = """
        SELECT StudentId, StudentNo, FirstName, MiddleName, LastName,
               HouseNo, Street, Municipality, CityProvince, ZipCode,
               RegionCode, ProvinceCode, CityMunicipalityCode, BarangayCode, RegionName, BarangayName,
               BirthDate, Gender, MobileNo, LandlineNo, Email,
               IsActive, CreatedAt
        FROM dbo.Students
        """;

    public StudentRepository(IDbConnectionFactory factory, ISessionContext session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(bool includeInactive = false)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        var sql = SelectSql + (includeInactive
            ? " ORDER BY LastName, FirstName, StudentId;"
            : " WHERE IsActive = 1 ORDER BY LastName, FirstName, StudentId;");
        var rows = await conn.QueryAsync<Student>(sql);
        return rows.AsList();
    }

    public async Task<Student?> GetByIdAsync(int studentId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Student>(
            SelectSql + " WHERE StudentId = @StudentId;", new { StudentId = studentId });
    }

    public async Task<Student?> GetByStudentNoAsync(string studentNo)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        return await conn.QuerySingleOrDefaultAsync<Student>(
            SelectSql + " WHERE StudentNo = @StudentNo;", new { StudentNo = studentNo });
    }

    public async Task<int> CreateAsync(StudentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        const string sql = """
            INSERT INTO dbo.Students
                (StudentNo, FirstName, MiddleName, LastName, HouseNo, Street,
                 Municipality, CityProvince, ZipCode, BirthDate, Gender,
                 RegionCode, ProvinceCode, CityMunicipalityCode, BarangayCode, RegionName, BarangayName,
                 MobileNo, LandlineNo, Email)
            OUTPUT INSERTED.StudentId
            VALUES
                (@StudentNo, @FirstName, @MiddleName, @LastName, @HouseNo, @Street,
                 @Municipality, @CityProvince, @ZipCode, @BirthDate, @Gender,
                 @RegionCode, @ProvinceCode, @CityMunicipalityCode, @BarangayCode, @RegionName, @BarangayName,
                 @MobileNo, @LandlineNo, @Email);
            """;
        int createdId = await conn.QuerySingleAsync<int>(sql, input, tx);
        await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Students, AuditAction.Created, createdId));
        await tx.CommitAsync();
        return createdId;
    }

    public async Task<bool> UpdateAsync(int studentId, StudentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        const string sql = """
            UPDATE dbo.Students SET
                StudentNo = @StudentNo, FirstName = @FirstName,
                MiddleName = @MiddleName, LastName = @LastName,
                HouseNo = @HouseNo, Street = @Street,
                Municipality = @Municipality, CityProvince = @CityProvince,
                RegionCode = @RegionCode, ProvinceCode = @ProvinceCode,
                CityMunicipalityCode = @CityMunicipalityCode, BarangayCode = @BarangayCode,
                RegionName = @RegionName, BarangayName = @BarangayName,
                ZipCode = @ZipCode, BirthDate = @BirthDate,
                Gender = @Gender, MobileNo = @MobileNo,
                LandlineNo = @LandlineNo, Email = @Email
            WHERE StudentId = @StudentId;
            """;
        int affected = await conn.ExecuteAsync(sql, new
        {
            StudentId = studentId,
            input.StudentNo,
            input.FirstName,
            input.MiddleName,
            input.LastName,
            input.HouseNo,
            input.Street,
            input.Municipality,
            input.CityProvince,
            input.ZipCode,
            input.BirthDate,
            input.Gender,
            input.MobileNo,
            input.LandlineNo,
            input.Email,
            input.RegionCode,
            input.ProvinceCode,
            input.CityMunicipalityCode,
            input.BarangayCode,
            input.RegionName,
            input.BarangayName
        }, tx);
        if (affected > 0)
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Students, AuditAction.Updated, studentId));
        await tx.CommitAsync();
        return affected > 0;
    }

    // Hard delete is atomic and guarded by the database's existing foreign keys.
    // Never cascade into loan history or user accounts.
    public async Task<bool> DeleteAsync(int studentId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        const string sql = """
            DELETE s FROM dbo.Students AS s
            WHERE s.StudentId = @StudentId
              AND NOT EXISTS (SELECT 1 FROM dbo.BorrowReturns AS br WHERE br.StudentId = s.StudentId)
              AND NOT EXISTS (SELECT 1 FROM dbo.Users AS u WHERE u.StudentId = s.StudentId)
              AND NOT EXISTS (SELECT 1 FROM dbo.StudentRegistrations AS reg WHERE reg.StudentId=s.StudentId);
            """;
        int affected = await conn.ExecuteAsync(sql, new { StudentId = studentId }, tx);
        if (affected > 0)
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Students, AuditAction.Deleted, studentId));
        await tx.CommitAsync();
        return affected > 0;
    }

    public async Task<bool> SetActiveAsync(int studentId, bool isActive)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
        const string sql = """
            UPDATE dbo.Students SET IsActive = @IsActive
            WHERE StudentId = @StudentId;
            """;
        int affected = await conn.ExecuteAsync(sql, new
        {
            StudentId = studentId,
            IsActive = isActive
        }, tx);
        if (affected > 0)
        {
            if (!isActive)
            {
                var disabled = await conn.QueryAsync<int>("""
                    UPDATE u SET IsActive=0,SessionVersion=SessionVersion+1
                    OUTPUT INSERTED.UserId
                    FROM dbo.Users u JOIN dbo.Roles r ON r.RoleId=u.RoleId
                    WHERE u.StudentId=@studentId AND r.Name=N'Borrower' AND r.IsSystem=1 AND u.IsSystemOwner=0 AND u.IsActive=1;
                    """, new { studentId }, tx);
                foreach (int userId in disabled)
                    await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.StudentAccounts, AuditAction.Archived, userId));
                var expired = await conn.QueryAsync<int>("""
                    UPDATE dbo.StudentRegistrations SET Status='Expired',ReviewedAt=SYSUTCDATETIME(),DecisionCode='Expired'
                    OUTPUT INSERTED.RegistrationId WHERE StudentId=@studentId AND Status='Pending';
                    """, new { studentId }, tx);
                foreach (int registrationId in expired)
                    await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.StudentAccounts, AuditAction.RegistrationExpired, registrationId));
            }
            await AuditWriter.WriteAsync(conn, tx, _session.Current, new AuditEvent(AuditModule.Students, (isActive ? AuditAction.Restored : AuditAction.Archived), studentId));
        }
        await tx.CommitAsync();
        return affected > 0;
    }
}
