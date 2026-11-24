# LiteraHub — Database Schema and Inventory Rules

**Updated:** 2026-11-25  
**Authoritative source:** `LiteraHub.Data/Bootstrap/Migrations.cs` and live SQL Server schema.

> This is documentation, **not a migration**. Do not modify already-applied migrations in place. New fields/audit tables require new versioned migrations after examining current database constraints.

## Existing core tables

| Table | Key | Role |
|---|---|---|
| `dbo.Roles` | `RoleId` | Staff and account roles |
| `dbo.Users` | `UserId` | Hashed-password authentication; optional StudentId |
| `dbo.Students` | `StudentId` | Borrower records |
| `dbo.Authors` | `AuthorId` | Author metadata |
| `dbo.Categories` | `CategoryId` | Book classifications |
| `dbo.BookStatuses` | `StatusId` | Condition/borrowability |
| `dbo.Books` | `BookId` | Catalog and copies |
| `dbo.BookAuthors` | BookId + AuthorId | Many-to-many authorship |
| `dbo.BorrowReturns` | `BorrowId` | Loan and return history |
| `dbo.Settings` | `SettingKey` | Loan settings |

## Student fields (current known schema)

| Field | SQL type / policy |
|---|---|
| `StudentId` | `INT IDENTITY PRIMARY KEY` |
| `StudentNo` | `NVARCHAR(30) NOT NULL UNIQUE` |
| `FirstName` | `NVARCHAR(100) NOT NULL` |
| `MiddleName` | `NVARCHAR(100) NULL` |
| `LastName` | `NVARCHAR(100) NOT NULL` |
| `HouseNo` | `NVARCHAR(50) NOT NULL` |
| `Street` | `NVARCHAR(150) NOT NULL` |
| `Municipality` | `NVARCHAR(100) NOT NULL` |
| `CityProvince` | `NVARCHAR(100) NOT NULL` |
| `ZipCode` | `NVARCHAR(10) NOT NULL` |
| `BirthDate` | `DATE NOT NULL` |
| `Gender` | `NVARCHAR(20) NOT NULL` |
| `MobileNo` | `NVARCHAR(30) NOT NULL` |
| `LandlineNo` | `NVARCHAR(30) NULL` |
| `Email` | `NVARCHAR(150) NOT NULL` |
| `IsActive` | `BIT NOT NULL DEFAULT 1` |
| `CreatedAt` | `DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME()` |

Service-layer required fields include all above nonnullable textual fields. Optional text fields: MiddleName, LandlineNo. Student hard-delete is guarded against `BorrowReturns.StudentId` and `Users.StudentId` dependencies; archive is not deletion.

## Books and inventory

Known book columns: `BookId`, `Isbn`, `Title`, `Description`, `CategoryId`, `TotalCopies`, `AvailableCopies`, `StatusId`, `CoverPath`, `IsActive`, `CreatedAt`.

```text
TotalCopies >= 0
AvailableCopies >= 0
AvailableCopies <= TotalCopies
```

Persist explicitly chosen available count, including zero. `TotalCopies - AvailableCopies` must **not** be inferred as active loans. Verify actual active `BorrowReturns` rows when enforcing availability. Borrow and return must use atomic, concurrency-safe transactions.

## BorrowReturns (existing migration fields)

`BorrowId`, `StudentId`, `BookId`, `Isbn`, `BorrowDate`, `DueDate`, `ReturnDate`, `Remarks`, `Status`, `ReturnConditionStatusId`, `CreatedByUserId`, `CreatedAt`.

Status is constrained to `Borrowed`/`Returned`; `ReturnDate` may be null. The inventory fix identifies active loans as `Status='Borrowed' AND ReturnDate IS NULL`.

## Users and offline password recovery plan

Known fields: `UserId`, `Username`, `PasswordHash`, `RoleId`, nullable `StudentId`, `MustChangePassword`, `IsActive`, `CreatedAt`; filtered unique index on nonnull StudentId.

**No confirmed password-reset schema exists yet.** Proposed admin-assisted reset must reuse hashed password storage, force `MustChangePassword`, add an auditable change record via **new migration** if approved, protect authorization and prevent self-service bypass. No stored plaintext temporary credentials or universal recovery code.

## Proposed PSGC change impact — not yet migrated

The current Student schema has **no separately verified columns** for PSGC Region/Province/City/Barangay codes. Offline PSGC is intended first as a selection/validation dataset; storing full hierarchical codes would require a planned migration and backwards-compatible import/mapping. Always store official codes as strings, handle independent cities/NCR, and keep ZIP independent (PSGC is not a postal-code authority). Existing free-text addresses must not be deleted or overwritten by snapshot upgrades.

## Migration checklist

1. Inspect current `Migrations.cs` and live SQL constraints.
2. Decide whether the new value is purely UI-backed or persisted.
3. Add a new idempotent versioned migration if needed.
4. Backfill safely, preserving legacy addresses and auditability.
5. Test a clean install and an upgrade from the existing database.
