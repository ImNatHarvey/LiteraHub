# Decisions Log

## 2026-11-08
- Target .NET 10 (current LTS, supported to Nov 2028; .NET 8 reaches end of life 2026-11-10).
- System name: LiteraHub. Projects renamed LiteraHub.Core / .Data / .Services / .WinForms.
- 4-project solution in one folder; docs in /docs at repo root.
- Windows Forms only, UI built in code (no Designer files).
- DB bootstrapper: plain idempotent SQL scripts + SchemaVersion table, via Dapper. Connects to master first.
- Server and DB name in appsettings.json (LocalDB default, Express fallback). Windows authentication.
- Demo plan: classmate laptop, release zip from GitHub (self-contained publish planned, UNVERIFIED until Step 8).
- Window sizes confirmed: 1920x1080, 1280x720 (primary), about 390x844 portrait (bonus).
- IDs: Student ID typed by librarian; Author ID and Category ID auto-generated.
- Damaged return: librarian chooses to mark whole title Damaged or only note in Remarks.
- Loan rules: 7-day default, max 3 active loans, Damaged books not borrowable, no fines (overdue flag only), configurable in Settings.
- Delete policy: soft delete default; hard delete Admin-only, only if unreferenced, with confirm; BorrowReturns never hard-deleted.
- Return-date search matches actual return date or due date, single date or from-to range.
- Mockup deviations: 14-day loan, borrower self-borrow and "Sign in as" dropdown are NOT used in the MVP.
- ReaLTaiizor and icon package deferred to Step 4 (verify .NET 10 compatibility first).
- - SQL Server: Express (.\SQLEXPRESS) is the primary server; LocalDB is only the fallback because the LocalDB tool was not found on the dev PC.
- Added explicit System.Security.Cryptography.Pkcs (patched, 10.0.13 or higher) to LiteraHub.Data to fix NU1902/NU1903 warnings from a vulnerable transitive 9.0.18 (CVE-2026-84008).