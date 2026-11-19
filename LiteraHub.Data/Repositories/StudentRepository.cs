using Dapper;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;

namespace LiteraHub.Data.Repositories;

public sealed class StudentRepository : IStudentRepository
{
    private readonly IDbConnectionFactory _factory;

    private const string SelectSql = """
        SELECT StudentId, StudentNo, FirstName, MiddleName, LastName,
               HouseNo, Street, Municipality, CityProvince, ZipCode,
               BirthDate, Gender, MobileNo, LandlineNo, Email,
               IsActive, CreatedAt
        FROM dbo.Students
        """;

    public StudentRepository(IDbConnectionFactory factory)
    {
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
        const string sql = """
            INSERT INTO dbo.Students
                (StudentNo, FirstName, MiddleName, LastName, HouseNo, Street,
                 Municipality, CityProvince, ZipCode, BirthDate, Gender,
                 MobileNo, LandlineNo, Email)
            OUTPUT INSERTED.StudentId
            VALUES
                (@StudentNo, @FirstName, @MiddleName, @LastName, @HouseNo, @Street,
                 @Municipality, @CityProvince, @ZipCode, @BirthDate, @Gender,
                 @MobileNo, @LandlineNo, @Email);
            """;
        return await conn.QuerySingleAsync<int>(sql, input);
    }

    public async Task<bool> UpdateAsync(int studentId, StudentInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        const string sql = """
            UPDATE dbo.Students SET
                StudentNo = @StudentNo, FirstName = @FirstName,
                MiddleName = @MiddleName, LastName = @LastName,
                HouseNo = @HouseNo, Street = @Street,
                Municipality = @Municipality, CityProvince = @CityProvince,
                ZipCode = @ZipCode, BirthDate = @BirthDate,
                Gender = @Gender, MobileNo = @MobileNo,
                LandlineNo = @LandlineNo, Email = @Email
            WHERE StudentId = @StudentId;
            """;
        return await conn.ExecuteAsync(sql, new
        {
            StudentId = studentId,
            input.StudentNo, input.FirstName, input.MiddleName, input.LastName,
            input.HouseNo, input.Street, input.Municipality, input.CityProvince,
            input.ZipCode, input.BirthDate, input.Gender, input.MobileNo,
            input.LandlineNo, input.Email
        }) > 0;
    }

    // Hard delete is atomic and guarded by the database's existing foreign keys.
    // Never cascade into loan history or user accounts.
    public async Task<bool> DeleteAsync(int studentId)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        const string sql = """
            DELETE s FROM dbo.Students AS s
            WHERE s.StudentId = @StudentId
              AND NOT EXISTS (SELECT 1 FROM dbo.BorrowReturns AS br WHERE br.StudentId = s.StudentId)
              AND NOT EXISTS (SELECT 1 FROM dbo.Users AS u WHERE u.StudentId = s.StudentId);
            """;
        return await conn.ExecuteAsync(sql, new { StudentId = studentId }) > 0;
    }

    public async Task<bool> SetActiveAsync(int studentId, bool isActive)
    {
        await using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
        const string sql = """
            UPDATE dbo.Students SET IsActive = @IsActive
            WHERE StudentId = @StudentId;
            """;
        return await conn.ExecuteAsync(sql, new
        {
            StudentId = studentId, IsActive = isActive
        }) > 0;
    }
}
