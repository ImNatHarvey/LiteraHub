# LiteraHub — Database Schema and Inventory Rules

**Updated:** 2026-11-20  
**Authoritative source:** `LiteraHub.Data/Bootstrap/Migrations.cs` and live SQL Server schema.

> This document is a schema summary, **not** a migration. Verify the latest repository and runtime DB before writing SQL. Do not edit previously applied migrations in place.

## Existing core tables

| Table | Key | Role |
|---|---|---|
| `dbo.Roles` | `RoleId` | Staff role permissions |
| `dbo.Users` | `UserId` | Authentication accounts; optional `StudentId` association |
| `dbo.Students` | `StudentId` | Student borrower records |
| `dbo.Authors` | `AuthorId` | Author metadata |
| `dbo.Categories` | `CategoryId` | Book categories |
| `dbo.BookStatuses` | `StatusId` | Book condition/borrowability |
| `dbo.Books` | `BookId` | Catalog/inventory |
| `dbo.BookAuthors` | `BookId` + `AuthorId` | Many-to-many links |
| `dbo.BorrowReturns` | `BorrowId` | Loan and return history |
| `dbo.Settings` | `SettingKey` | Loan settings |

## Students — current migration fields

- `StudentId INT IDENTITY PRIMARY KEY` — internal record key.
- `StudentNo NVARCHAR(30) NOT NULL UNIQUE` — visible Student ID number.
- `FirstName NVARCHAR(100) NOT NULL`.
- `MiddleName NVARCHAR(100) NULL`.
- `LastName NVARCHAR(100) NOT NULL`.
- `HouseNo NVARCHAR(50) NOT NULL`.
- `Street NVARCHAR(150) NOT NULL`.
- `Municipality NVARCHAR(100) NOT NULL`.
- `CityProvince NVARCHAR(100) NOT NULL`.
- `ZipCode NVARCHAR(10) NOT NULL`.
- `BirthDate DATE NOT NULL`.
- `Gender NVARCHAR(20) NOT NULL`.
- `MobileNo NVARCHAR(30) NOT NULL`.
- `LandlineNo NVARCHAR(30) NULL`.
- `Email NVARCHAR(150) NOT NULL`.
- `IsActive BIT NOT NULL DEFAULT 1`.
- `CreatedAt DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()`.

Student hard-delete must respect `BorrowReturns.StudentId` and any `Users.StudentId` references. Do not assume archive means permanent delete.

## Books — current migration fields and constraints

`BookId`, `Isbn`, `Title`, `Description`, `CategoryId`, `TotalCopies`, `AvailableCopies`, `StatusId`, `CoverPath`, `IsActive`, `CreatedAt`.

```text
TotalCopies >= 0
AvailableCopies >= 0
AvailableCopies <= TotalCopies
```

Step 5B.6 fix: on edit, preserve the explicit available count (including 0) while checking actual active `BorrowReturns` records. Do not recompute availability as `Total - (old Total - old Available)`.

## BorrowReturns — existing migration fields

`BorrowId`, `StudentId`, `BookId`, `Isbn`, `BorrowDate`, `DueDate`, `ReturnDate`, `Remarks`, `Status`, `ReturnConditionStatusId`, `CreatedByUserId`, `CreatedAt`. `Status` is constrained to `Borrowed`/`Returned`; `ReturnDate` is nullable. An active loan is presently identified by `Status='Borrowed' AND ReturnDate IS NULL` in the inventory fix.

## Users

`UserId`, `Username`, `PasswordHash`, `RoleId`, nullable `StudentId`, `MustChangePassword`, `IsActive`, `CreatedAt`; unique filtered index on non-null `StudentId`. Forgot Password is not currently a verified feature; assess whether token storage/new migration or admin-assisted reset is appropriate.

## Data integrity policy

- Add schema changes via new versioned migrations, never rewrite applied versions.
- Validate in service layer and database/repository.
- Parameterize SQL, use transactions and concurrency-safe updates for loans and returns.
- Never infer that unavailable copies are all borrowed.
