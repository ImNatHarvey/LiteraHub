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
    string Email);
