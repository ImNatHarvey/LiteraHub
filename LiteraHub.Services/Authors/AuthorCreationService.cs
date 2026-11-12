
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

        var validation = Validate(input);

        if (!validation.Success)
            return AuthorCreationResult.Failed(
                validation.Error ?? "Invalid author details.");

        var normalized = Normalize(input);

        try
        {
            var duplicate =
                await _repository.GetByNameAsync(normalized);

            if (duplicate is not null)
                return AuthorCreationResult.Failed(
                    "An author with this name already exists.");

            int authorId =
                await _repository.CreateAsync(normalized);

            return AuthorCreationResult.Created(authorId);
        }
        catch (DbException)
        {
            return AuthorCreationResult.Failed(
                "Unable to create the author. " +
                "Check for duplicate records or database issues.");
        }
    }

    public async Task<AuthorRecord?> GetByIdAsync(
        int authorId)
    {
        _permissions.Demand(Permission.ViewCatalog);

        if (authorId <= 0)
            return null;

        var author = await _repository.GetByIdAsync(authorId);

        if (author is not null &&
            !author.IsActive &&
            !_permissions.Can(Permission.ManageLookups))
            return null;

        return author;
    }

    public async Task<ServiceResult> UpdateAsync(
        int authorId,
        AuthorInput input)
    {
        _permissions.Demand(Permission.ManageLookups);

        if (authorId <= 0)
            return ServiceResult.Fail("Invalid author ID.");

        var validation = Validate(input);

        if (!validation.Success)
            return validation;

        var normalized = Normalize(input);

        try
        {
            var existing =
                await _repository.GetByIdAsync(authorId);

            if (existing is null)
                return ServiceResult.Fail("Author not found.");

            var duplicate =
                await _repository.GetByNameAsync(normalized);

            if (duplicate is not null &&
                duplicate.AuthorId != authorId)
            {
                return ServiceResult.Fail(
                    "An author with this name already exists.");
            }

            bool updated = await _repository.UpdateAsync(
                authorId,
                normalized);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail("Author not found.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "Unable to update the author. " +
                "Check for duplicate records or database issues.");
        }
    }

    public async Task<ServiceResult> SetActiveAsync(
        int authorId,
        bool isActive)
    {
        _permissions.Demand(Permission.ManageLookups);

        if (authorId <= 0)
            return ServiceResult.Fail("Invalid author ID.");

        try
        {
            var existing =
                await _repository.GetByIdAsync(authorId);

            if (existing is null)
                return ServiceResult.Fail("Author not found.");

            if (existing.IsActive == isActive)
                return ServiceResult.Ok();

            bool updated = await _repository.SetActiveAsync(
                authorId,
                isActive);

            return updated
                ? ServiceResult.Ok()
                : ServiceResult.Fail(
                    "Unable to change the author's status.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "Unable to change the author's status " +
                "because of a database issue.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int authorId)
    {
        _permissions.Demand(Permission.ManageLookups);
        _permissions.Demand(Permission.HardDelete);

        if (authorId <= 0)
            return ServiceResult.Fail("Invalid author ID.");

        try
        {
            var existing =
                await _repository.GetByIdAsync(authorId);

            if (existing is null)
                return ServiceResult.Fail("Author not found.");

            bool deleted =
                await _repository.DeleteIfUnusedAsync(authorId);

            return deleted
                ? ServiceResult.Ok()
                : ServiceResult.Fail(
                    "This author cannot be deleted because " +
                    "they are referenced by books or are " +
                    "no longer available.");
        }
        catch (DbException)
        {
            return ServiceResult.Fail(
                "Unable to delete this author. " +
                "The record may be referenced elsewhere.");
        }
    }

    private static ServiceResult Validate(AuthorInput? input)
    {
        if (input is null)
            return ServiceResult.Fail(
                "Author details are required.");

        string first = input.FirstName?.Trim() ?? "";
        string middle = input.MiddleName?.Trim() ?? "";
        string last = input.LastName?.Trim() ?? "";
        string gender = input.Gender?.Trim() ?? "";

        if (first.Length == 0 || last.Length == 0)
            return ServiceResult.Fail(
                "First name and last name are required.");

        if (first.Length > 100 ||
            middle.Length > 100 ||
            last.Length > 100)
            return ServiceResult.Fail(
                "Name fields must not exceed 100 characters.");

        if (gender is not
            ("Unknown" or "Male" or "Female" or "Other"))
            return ServiceResult.Fail(
                "Select a valid gender.");

        if (input.BirthDate is not null &&
            input.BirthDate.Value.Date > DateTime.Today)
            return ServiceResult.Fail(
                "Birth date cannot be in the future.");

        return ServiceResult.Ok();
    }

    private static AuthorInput Normalize(AuthorInput input)
    {
        string middle = input.MiddleName?.Trim() ?? "";

        return input with
        {
            FirstName = input.FirstName.Trim(),
            MiddleName = middle.Length == 0 ? null : middle,
            LastName = input.LastName.Trim(),
            Gender = input.Gender.Trim(),
            BirthDate = input.BirthDate?.Date
        };
    }

    public Task<IReadOnlyList<AuthorRecord>> GetAllAsync(
    bool includeInactive = false)
    {
        _permissions.Demand(Permission.ViewCatalog);

        if (includeInactive)
            _permissions.Demand(Permission.ManageLookups);

        return _repository.GetAllAsync(includeInactive);
    }
}
