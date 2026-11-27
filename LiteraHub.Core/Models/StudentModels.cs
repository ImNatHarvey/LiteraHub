namespace LiteraHub.Core.Models;

public sealed class Student
{
    public int StudentId { get; set; }
    public string StudentNo { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string HouseNo { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Municipality { get; set; } = string.Empty;
    public string CityProvince { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? RegionCode { get; set; }
    public string? ProvinceCode { get; set; }
    public string? CityMunicipalityCode { get; set; }
    public string? BarangayCode { get; set; }
    public string? RegionName { get; set; }
    public string? BarangayName { get; set; }
    public DateTime BirthDate { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string MobileNo { get; set; } = string.Empty;
    public string? LandlineNo { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// A student record does not create a login account.
public sealed record StudentInput(
    string StudentNo,
    string FirstName,
    string? MiddleName,
    string LastName,
    string HouseNo,
    string Street,
    string Municipality,
    string CityProvince,
    string ZipCode,
    DateTime BirthDate,
    string Gender,
    string MobileNo,
    string? LandlineNo,
    string Email)
{
    public string? RegionCode { get; init; }
    public string? ProvinceCode { get; init; }
    public string? CityMunicipalityCode { get; init; }
    public string? BarangayCode { get; init; }
    public string? RegionName { get; init; }
    public string? BarangayName { get; init; }
}
