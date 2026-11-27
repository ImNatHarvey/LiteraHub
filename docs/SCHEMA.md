# LiteraHub — Database Schema and Inventory Rules

**Updated:** 2026-11-28  
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

## Accounts, roles and security migrations

| Version | Changes |
|---|---|
| V1–V2 | Core tables, indexes and initial seed data |
| V3 | Rename Good Condition to Good |
| V4 | Multi-category book links |
| V5 | Role descriptions/system flag, RolePermissions and seeded granular grants |
| V6 | Users.RecoveryPasskeyHash NVARCHAR(100) NULL; RecoveryPasskeyCreatedAt DATETIME2(0) NULL |
| V7 | Users.FirstName, MiddleName, LastName NVARCHAR(100) NULL (legacy rows preserved; service requires first/last for new/edited records) |
| V8 | Users.RecoveryFailures INT default 0, RecoveryLockedUntil DATETIME2 NULL; RecoveryAudit |
| V9 | Users.IsSystemOwner BIT default 0; filtered unique index UX_Users_OneSystemOwner; composite UQ_Users_OwnerIdentity; SystemOwnerAnchor composite FK; immutable anchor trigger |
| V10 | OwnerAuthorizationState singleton and OwnerAuthorizationAudit |
| V11 | Structured PSGC student address fields and consistency constraint |
| V12 | Persistent login failure count and lockout |
| V13 | ActivityAudit and indexes |

Users retain UserId, Username, PasswordHash, RoleId, nullable StudentId, MustChangePassword, IsActive and CreatedAt. RecoveryAudit: AuditId, nullable UserId and ActorUserId, EventType, CreatedAt. OwnerAuthorizationState: SingletonId, Failures, LockedUntil. OwnerAuthorizationAudit: AuditId, ActorUserId, TargetUserId, OwnerUserId, RequestedAction, Outcome, CreatedAt. Audit identifiers are historical records, not necessarily live user references.

V9 existing database upgrade identifies UserId 1 only if active with active system Admin role; otherwise fails closed. During actual upgrade the original account was inactive and was explicitly restored after read-only verification; migration then passed user testing. Fresh installation seeds owner after migrations. Never silently promote another account or reset the database. Owner anchor protects identity, role and active state; app checks protect other personal/security fields. V10 checks permissions and approval within the account mutation transaction; failed attempts commit before mutation. Five failures/15 minutes for recovery and owner authorization. Hash secrets only, never log plaintext. Source: F5.6.1 Migrations.cs and DelegationGuard.cs. Verify live schema before any further migrations.

## PSGC address persistence — V11 (pushed)

| Column | SQL type |
|---|---|
| RegionCode | VARCHAR(10) NULL |
| ProvinceCode | VARCHAR(10) NULL |
| CityMunicipalityCode | VARCHAR(10) NULL |
| BarangayCode | VARCHAR(10) NULL |
| RegionName | NVARCHAR(100) NULL |
| BarangayName | NVARCHAR(100) NULL |

`CK_Students_PsgcAddress` allows fully legacy null structured fields or complete structured addresses (province optional), with ten-digit code format checks. Service validates hierarchy against embedded PSA catalog. Existing Municipality and CityProvince names remain; province-free addresses store region name in CityProvince. ZIP is independent. Existing migrations remain immutable. V12 and V13 are now applied. Next candidate version is V14 after checking current source. StudentRepository UPDATE persists all six structured fields as of F7.0.3.

## V12 and V13 — verified migration definitions

V12: `LoginFailures INT NOT NULL DEFAULT 0`, `LoginLockedUntil DATETIME2 NULL`; `CK_Users_LoginAttempts` permits 0–4 failures with NULL deadline or 5 with non-NULL deadline. Ordinary lockout 3 minutes; recovery/Owner authorization 15 minutes remain separate.

V13 `dbo.ActivityAudit`: `AuditId BIGINT IDENTITY PRIMARY KEY`, nullable `ActorUserId INT`, `ActorUsername NVARCHAR(50)`, `ActorRole NVARCHAR(50)`, `Module NVARCHAR(40) NOT NULL`, `Action NVARCHAR(40) NOT NULL`, nullable `EntityId INT`, `Outcome NVARCHAR(20) NOT NULL` constrained to Success/Failed/Denied, `Description NVARCHAR(160) NOT NULL`, `CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()`. Indexes `IX_ActivityAudit_CreatedAt` (CreatedAt DESC, AuditId DESC) and `IX_ActivityAudit_Module` (Module, CreatedAt DESC). Historical IDs intentionally lack live-entity FKs. Keep existing RecoveryAudit and OwnerAuthorizationAudit.

## Proposed F7.1+ schema (not yet applied)

Existing Users.StudentId FK and filtered unique index UX_Users_StudentId enforce one linked account per student. Existing Borrower role is protected system role and will display as Student. V14 candidate: registration lifecycle with reviewer, timestamps, status, linked identity, safe reason, unique pending request and transactional approval; exact tables/columns and permission keys to be determined from source. No plaintext secrets or automatic creation of verified student identities. Later migrations for reservations, renewals, waitlist, fine ledger/policies and notifications. Preserve existing BorrowReturns and historical rows; no schema claims for unimplemented features.

Inventory future invariant: A+R+B<=T, other unavailable=T-A-R-B. R is approved reservation count; B active unreturned loans. Pending requests/waitlist consume no stock. Reserve/issue/return/release and audit atomically; preserve manually unavailable copies. Store UTC event timestamps, derive local calendar deadlines according to configured institution policy; decimal fines and immutable historical ledger.

## Migration checklist

Inspect latest source/live schema, add new version only, use transactional upgrades, preserve all data, test fresh and existing isolated DB, verify constraints/indexes and rollback. Never reset real database. Update ERD after actual schema changes.
