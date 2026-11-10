
using System.Data.Common;
using LiteraHub.Core.Interfaces;
using LiteraHub.Core.Models;
using LiteraHub.Core.Security;
using LiteraHub.Data.Repositories;

namespace LiteraHub.Services.Authors;

public sealed class AuthorCreationService
    : IAuthorCreationService
{
    private readonly AuthorCreationRepository _repository;
    private readonly IPermissionService _permissions;

    public AuthorCreationService(
        AuthorCreationRepository repository,
        IPermissionService permissions)
    {
        _repository = repository ??
            throw new ArgumentNullException(nameof(repository));

        _permissions = permissions ??
            throw new ArgumentNullException(nameof(permissions));
    }

    public async Task<AuthorCreationResult> CreateAsync(
        AuthorInput input)
    {
        _permissions.Demand(Permission.CreateLookups);

        if (input is null)
        {
            return AuthorCreationResult.Failed(
                "Author details are required.");
        }

        string first = input.FirstName?.Trim() ?? "";
        string middle = input.MiddleName?.Trim() ?? "";
        string last = input.LastName?.Trim() ?? "";
        string gender = input.Gender?.Trim() ?? "";

        if (first.Length == 0 || last.Length == 0)
        {
            return AuthorCreationResult.Failed(
                "First name and last name are required.");
        }

        if (first.Length > 100 ||
            middle.Length > 100 ||
            last.Length > 100)
        {
            return AuthorCreationResult.Failed(
                "Author name fields must not exceed " +
                "100 characters.");
        }

        if (gender.Length == 0 || gender.Length > 20)
        {
            return AuthorCreationResult.Failed(
                "Select a valid gender.");
        }

        if (input.BirthDate is not null &&
            input.BirthDate.Value.Date > DateTime.Today)
        {
            return AuthorCreationResult.Failed(
                "Birth date cannot be in the future.");
        }

        var normalized = input with
        {
            FirstName = first,
            MiddleName = middle.Length == 0 ? null : middle,
            LastName = last,
            Gender = gender,
            BirthDate = input.BirthDate?.Date
        };

        try
        {
            int authorId =
                await _repository.CreateAsync(normalized);

            return AuthorCreationResult.Created(authorId);
        }
        catch (DbException)
        {
            return AuthorCreationResult.Failed(
                "Unable to create the author. " +
                "Please check the database connection.");
        }
    }
}

