using System.Data.Common;
using System.Net.Mail;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;

namespace LiteraHub.Services.Students;

public sealed class StudentService : IStudentService
{
    private readonly IStudentRepository _repository;
    private readonly IPermissionService _permissions;

    public StudentService(IStudentRepository repository, IPermissionService permissions)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public Task<IReadOnlyList<Student>> GetAllAsync(bool includeInactive = false)
    {
        _permissions.Demand(RolePermission.StudentsView);
        return _repository.GetAllAsync(includeInactive);
    }

    public Task<Student?> GetByIdAsync(int studentId)
    {
        _permissions.Demand(RolePermission.StudentsView);
        return studentId > 0
            ? _repository.GetByIdAsync(studentId)
            : Task.FromResult<Student?>(null);
    }

    public async Task<ServiceResult> CreateAsync(StudentInput input)
    {
        _permissions.Demand(RolePermission.StudentsAdd);
        var address = PrepareAddress(input, null, out input);
        if (!address.Success) return address;
        var validation = Validate(input);
        if (!validation.Success) return validation;
        var normalized = Normalize(input);
        if (await _repository.GetByStudentNoAsync(normalized.StudentNo) is not null)
            return ServiceResult.Fail("Student ID number already exists.");
        try
        {
            await _repository.CreateAsync(normalized);
            return ServiceResult.Ok();
        }
        catch (DbException)
        {
            return ServiceResult.Fail("Unable to save student. Check the student ID number or database connection.");
        }
    }

    public async Task<ServiceResult> UpdateAsync(int studentId, StudentInput input)
    {
        _permissions.Demand(RolePermission.StudentsEdit);
        if (studentId <= 0) return ServiceResult.Fail("Invalid student record ID.");
        var existing = await _repository.GetByIdAsync(studentId);
        if (existing is null) return ServiceResult.Fail("Student not found.");
        var address = PrepareAddress(input, existing, out input);
        if (!address.Success) return address;
        var validation = Validate(input);
        if (!validation.Success) return validation;
        var normalized = Normalize(input);
        var duplicate = await _repository.GetByStudentNoAsync(normalized.StudentNo);
        if (duplicate is not null && duplicate.StudentId != studentId)
            return ServiceResult.Fail("Student ID number already exists.");
        try
        {
            return await _repository.UpdateAsync(studentId, normalized)
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Student not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail("Unable to update student. Check the student ID number or database connection.");
        }
    }

    public async Task<ServiceResult> SetActiveAsync(int studentId, bool isActive)
    {
        _permissions.Demand(RolePermission.StudentsArchiveRestore);
        if (studentId <= 0) return ServiceResult.Fail("Invalid student record ID.");
        var existing = await _repository.GetByIdAsync(studentId);
        if (existing is null) return ServiceResult.Fail("Student not found.");
        if (existing.IsActive == isActive) return ServiceResult.Ok();
        try
        {
            return await _repository.SetActiveAsync(studentId, isActive)
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Student not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail("Unable to change student status due to a database issue.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int studentId)
    {
        _permissions.Demand(RolePermission.StudentsDelete);
        if (studentId <= 0) return ServiceResult.Fail("Invalid student record ID.");
        if (await _repository.GetByIdAsync(studentId) is null)
            return ServiceResult.Fail("Student not found.");
        try
        {
            return await _repository.DeleteAsync(studentId)
                ? ServiceResult.Ok()
                : ServiceResult.Fail("This student cannot be deleted because the record is referenced by borrowing history or a linked account.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail("Unable to delete student. The record may be in use by other records.");
        }
    }

    private static ServiceResult PrepareAddress(StudentInput input, Student? existing, out StudentInput prepared)
    {
        prepared = input;
        if (input is null) return ServiceResult.Fail("Student data is required.");
        bool legacy = input.RegionCode is null && input.ProvinceCode is null && input.CityMunicipalityCode is null && input.BarangayCode is null;
        if (legacy)
        {
            if (existing is null || existing.RegionCode is not null || existing.CityMunicipalityCode is not null || existing.BarangayCode is not null)
                return ServiceResult.Fail("Select a complete PSGC address.");
            if (!string.Equals(input.Municipality, existing.Municipality, StringComparison.Ordinal) ||
                !string.Equals(input.CityProvince, existing.CityProvince, StringComparison.Ordinal))
                return ServiceResult.Fail("Use the PSGC selectors to update the geographic portion of this legacy address.");
            prepared = input with { RegionName = null, BarangayName = null };
            return ServiceResult.Ok();
        }
        try
        {
            if (!PsgcCatalog.Instance.TryResolve(input.RegionCode, input.ProvinceCode, input.CityMunicipalityCode, input.BarangayCode, out var selected) || selected is null)
                return ServiceResult.Fail("Select a valid region, province (if applicable), city or municipality, and barangay.");
            prepared = input with
            {
                Municipality = selected.Locality.Name,
                CityProvince = selected.Province?.Name ?? selected.Region.Name,
                RegionName = selected.Region.Name,
                BarangayName = selected.Barangay.Name
            };
            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or ArgumentException)
        {
            return ServiceResult.Fail("The offline PSGC dataset could not be loaded. Contact the administrator.");
        }
    }

    private static ServiceResult Validate(StudentInput? input)
    {
        if (input is null) return ServiceResult.Fail("Student data is required.");
        var required = new (string? Value, int Limit, string Label)[]
        {
            (input.StudentNo, 30, "Student ID number"),
            (input.FirstName, 100, "First name"),
            (input.LastName, 100, "Last name"),
            (input.HouseNo, 50, "House number"),
            (input.Street, 150, "Street"),
            (input.Municipality, 100, "Municipality"),
            (input.CityProvince, 100, "City/province"),
            (input.ZipCode, 10, "ZIP code"),
            (input.Gender, 20, "Gender"),
            (input.MobileNo, 30, "Mobile number"),
            (input.Email, 150, "Email")
        };
        foreach (var field in required)
        {
            if (string.IsNullOrWhiteSpace(field.Value))
                return ServiceResult.Fail($"{field.Label} is required.");
            if (field.Value.Trim().Length > field.Limit)
                return ServiceResult.Fail($"{field.Label} must not exceed {field.Limit} characters.");
        }
        if (input.MiddleName?.Trim().Length > 100)
            return ServiceResult.Fail("Middle name must not exceed 100 characters.");
        if (input.LandlineNo?.Trim().Length > 30)
            return ServiceResult.Fail("Landline number must not exceed 30 characters.");
        if (input.BirthDate.Date > DateTime.Today || input.BirthDate.Date < new DateTime(1900, 1, 1))
            return ServiceResult.Fail("Enter a valid birthdate between 1900 and today.");
        try
        {
            var address = new MailAddress(input.Email.Trim());
            if (!string.Equals(address.Address, input.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                return ServiceResult.Fail("Enter a valid email address.");
        }
        catch (FormatException)
        {
            return ServiceResult.Fail("Enter a valid email address.");
        }
        return ServiceResult.Ok();
    }

    private static StudentInput Normalize(StudentInput input)
    {
        static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return input with
        {
            StudentNo = input.StudentNo.Trim(),
            FirstName = input.FirstName.Trim(),
            MiddleName = Optional(input.MiddleName),
            LastName = input.LastName.Trim(),
            HouseNo = input.HouseNo.Trim(),
            Street = input.Street.Trim(),
            Municipality = input.RegionCode is null ? input.Municipality : input.Municipality.Trim(),
            CityProvince = input.RegionCode is null ? input.CityProvince : input.CityProvince.Trim(),
            ZipCode = input.ZipCode.Trim(),
            BirthDate = input.BirthDate.Date,
            Gender = input.Gender.Trim(),
            MobileNo = input.MobileNo.Trim(),
            LandlineNo = Optional(input.LandlineNo),
            Email = input.Email.Trim()
        };
    }
}
