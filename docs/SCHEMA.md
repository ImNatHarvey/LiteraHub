# LiteraHub — Database Schema and Inventory Rules

**Updated:** 2026-11-12  
**Database:** SQL Server Express  
**Authoritative migration source:** `LiteraHub.Data/Bootstrap/Migrations.cs`

This is a documentation summary of the existing migration model, **not** a newly applied migration. Verify the exact current schema from `Migrations.cs` and the runtime database before changing it.

## Core tables

| Table | Primary key | Purpose |
|---|---|---|
| Roles | RoleId | Access roles |
| Users | UserId | Staff/borrower accounts |
| Students | StudentId | Student records |
| Authors | AuthorId | Author records |
| Categories | CategoryId | Book classification |
| BookStatuses | StatusId | Condition and borrowability |
| Books | BookId | Catalog and inventory |
| BookAuthors | BookId + AuthorId | Book-author relationships |
| BorrowReturns | BorrowId | Loan transactions |
| Settings | SettingKey | Loan settings |

## Categories

`dbo.Categories`: CategoryId (identity), Name (unique, required), Description (nullable), IsActive.

Seed names previously documented: Fiction, Non-Fiction, Science, Technology, Mathematics, History, Literature, Reference.

## Authors

`dbo.Authors`: AuthorId, FirstName, MiddleName, LastName, BirthDate, Gender, IsActive, CreatedAt.

## Books

`dbo.Books`: BookId, Isbn, Title, Description, CategoryId, TotalCopies, AvailableCopies, StatusId, CoverPath, IsActive, CreatedAt.

Inventory constraints:

```text
TotalCopies >= 0
AvailableCopies >= 0
AvailableCopies <= TotalCopies
```

The Book Editor now enforces these constraints at the UI level. **The service and database remain authoritative when saving.**

The UI currently reserves the difference `TotalCopies - AvailableCopies` when editing, but this difference must not be assumed to equal the number of outstanding loans. Verify borrowing records before permitting inventory reductions.

## Relationships

- `Books.CategoryId` references `Categories`.
- `Books.StatusId` references `BookStatuses`.
- `BookAuthors` links Books and Authors using a composite primary key.
- `BorrowReturns` references the borrower and book and records borrow/due/return dates, remarks and status.

## Book statuses

Previously documented seed statuses:

- Good Condition — borrowable.
- Damaged — not borrowable.

The UI may abbreviate Good Condition as “Good”; verify mapping to actual database status IDs before persistence.

## Borrowing

`dbo.BorrowReturns` previously documented fields: BorrowId, StudentId, BookId, Isbn, BorrowDate, DueDate, ReturnDate, Remarks, Status, ReturnConditionStatusId, CreatedByUserId, CreatedAt.

## Settings

Previously documented seed values:

- `LoanDays = 7`
- `MaxActiveLoans = 3`

## Schema change policy

- Do not edit already-applied migrations.
- Introduce schema changes through new versioned migrations.
- Never auto-create categories or authors from external metadata without validation, duplicate handling and permissions.
- Do not assume a new database column exists because the UI displays a field.
- No new migration is claimed in the current Book Editor integration checkpoint.
