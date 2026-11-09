# LiteraHub — Database Schema

**Last reviewed:** 2026-11-09  
**Database:** SQL Server Express  
**Source of truth:** `LiteraHub.Data/Bootstrap/Migrations.cs`

## 1. Database Overview

LiteraHub uses SQL Server with Dapper for data access.

Database initialization is managed by the application bootstrapper. The current migration definitions are:

- **Migration V1:** Create core tables and indexes.
- **Migration V2:** Seed roles, book statuses, categories, and settings.

This document describes the schema defined in source code. Runtime database state should be verified separately.

## 2. Core Tables

| Table | Primary Key | Purpose |
|---|---|---|
| Roles | RoleId | User role definitions |
| Users | UserId | Authentication accounts |
| Students | StudentId | Student/borrower records |
| Authors | AuthorId | Author information |
| Categories | CategoryId | Book classifications |
| BookStatuses | StatusId | Book condition and borrowing eligibility |
| Books | BookId | Book catalog and copy counts |
| BookAuthors | BookId + AuthorId | Many-to-many book-author relationships |
| BorrowReturns | BorrowId | Borrowing and return transactions |
| Settings | SettingKey | Application configuration |

The bootstrapper also maintains migration-version tracking; its implementation should be consulted for the exact tracking-table structure.

## 3. Categories

**Table:** `dbo.Categories`

| Column | SQL Type | Constraints |
|---|---|---|
| CategoryId | INT | Primary key, identity |
| Name | NVARCHAR(100) | Required, unique |
| Description | NVARCHAR(500) | Nullable |
| IsActive | BIT | Required, default 1 |

### Seed Categories

- Fiction
- Non-Fiction
- Science
- Technology
- Mathematics
- History
- Literature
- Reference

These are inserted only when their names do not already exist.

## 4. Authors

**Table:** `dbo.Authors`

Fields: AuthorId, FirstName, MiddleName, LastName, BirthDate, Gender, IsActive, CreatedAt.

AuthorId is an identity primary key. FirstName, LastName, and Gender are required. MiddleName and BirthDate are optional.

## 5. Students

**Table:** `dbo.Students`

Fields:

- StudentId, StudentNo
- FirstName, MiddleName, LastName
- HouseNo, Street, Municipality, CityProvince, ZipCode
- BirthDate, Gender
- MobileNo, LandlineNo, Email
- IsActive, CreatedAt

StudentNo is unique. MiddleName and LandlineNo are nullable. StudentId is an identity primary key.

## 6. Books

**Table:** `dbo.Books`

Fields:

- BookId, Isbn, Title, Description
- CategoryId, TotalCopies, AvailableCopies
- StatusId, CoverPath
- IsActive, CreatedAt

ISBN is unique. CategoryId references Categories, and StatusId references BookStatuses.

Copy counts must satisfy:

`TotalCopies >= 0`

`AvailableCopies >= 0`

`AvailableCopies <= TotalCopies`

## 7. Book-Author Relationships

**Table:** `dbo.BookAuthors`

- BookId references Books.
- AuthorId references Authors.
- Composite primary key: BookId + AuthorId.

This supports multiple authors per book and multiple books per author.

## 8. Users and Roles

**Roles:** RoleId, Name, IsActive.

**Users:** UserId, Username, PasswordHash, RoleId, StudentId, MustChangePassword, IsActive, CreatedAt.

Username and role name are unique. Users reference Roles. StudentId is optional and has a filtered unique index when non-null.

Seed roles:

- Admin
- Librarian
- Borrower

## 9. Book Statuses

**Table:** `dbo.BookStatuses`

Fields: StatusId, Name, IsBorrowable, IsActive.

Seed statuses:

| Status | Borrowable |
|---|---|
| Good Condition | Yes |
| Damaged | No |

## 10. Borrowing and Returns

**Table:** `dbo.BorrowReturns`

Fields:

- BorrowId
- StudentId, BookId, Isbn
- BorrowDate, DueDate, ReturnDate
- Remarks, Status
- ReturnConditionStatusId
- CreatedByUserId, CreatedAt

Status is restricted to `Borrowed` or `Returned`, with `Borrowed` as the default.

ReturnDate, Remarks, ReturnConditionStatusId, and CreatedByUserId are nullable.

## 11. Application Settings

**Table:** `dbo.Settings`

| SettingKey | Seed Value |
|---|---|
| LoanDays | 7 |
| MaxActiveLoans | 3 |

Both keys and values are stored as strings.

## 12. Indexes and Constraints

The migrations define indexes for book titles, category and status references, author and student names, borrowing dates, student/book references, and ISBN references.

Unique constraints exist for role names, usernames, student numbers, category names, book-status names, and book ISBNs.

Foreign keys enforce the defined relationships.

## 13. Planned Development

Step 5 will implement CRUD services and user interfaces for Categories, Authors, Students, and Books using the existing database schema.

Step 6 will implement borrowing and returning workflows.

Additional digital-library features are postponed until the required MVP is complete.

## 14. Verification Notes

- The migration source has been inspected through GitHub.
- Previous application initialization and repeat-run tests were reported as successful by the developer.
- No direct SQL Server schema inspection has been performed in this documentation update.
- Future schema changes must use new migrations rather than silently rewriting already-applied migrations.
