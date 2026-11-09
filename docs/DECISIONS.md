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

## 2026-11-09 (Step 3)
- Borrower accounts cannot sign in during the MVP (AuthService.BorrowerPortalEnabled = false); portal is bonus.
- Admin has every permission. Librarian: ViewCatalog, ManageBooks, ManageStudents, CreateLookups, ProcessBorrowReturn, ViewReports. Borrower: ViewCatalog, ViewOwnLoans.
- Added CreateLookups (inline create from forms) separate from ManageLookups (edit/archive, Admin only) to reconcile "Admin manages lookups" with "create on the fly".
- Permissions are enforced in the service layer via IPermissionService.Demand (throws PermissionDeniedException); UI only hides buttons.
- Login failure messages are generic; a dummy BCrypt check runs for unknown users to avoid timing leaks.
- Password policy: 8-72 characters, at least one letter and one number, different from current, not equal to username. (72 because BCrypt ignores bytes beyond that.)
- Forced password change for the seeded admin; completing it signs the user in.
- No login lockout in the MVP (see OPEN_QUESTIONS).
- Composition root is Program.cs (manual wiring, no DI container).

## 2026-11-09 (Step 4A)
- Step 4 split: 4A theme + shell + icons; 4B dark-scrollbar spike; 4C loading states.
- ReaLTaiizor deferred (NuGet shows net10.0-windows7.0 target, active releases; added in Step 5 only if a control justifies it). Reason: every borrowed control must be re-skinned to Theme tokens.
- Icons: FontAwesome.Sharp 6.6.0 (last published Jul 2024, net8 target; .NET 10 fit UNVERIFIED). All icon choices live in Theming/AppIcons.cs.
- Accent: light #B45309 (about 5:1 on white by hand calc), dark #F59E0B (about 8:1 on #16181D). Mockup orange #D9730D was about 3.3:1 on white: fails AA.
- Theme: static Theme class, Palette (light + dark), Space scale 4/8/12/16/24, Radius, type scale (Segoe UI). Screens derive from ThemedUserControl and never hardcode colors.
- ApplicationHighDpiMode = PerMonitorV2 in csproj. Window presets are logical pixels scaled by DeviceDpi and clamped to the screen working area.
- Portrait/narrow windows (< 700 logical px) switch the sidebar to icon-only (compact) mode.
- Navigation is filtered by IPermissionService; pages are placeholders until their steps.
- Login/ChangePassword forms keep default styling until a later polish step.
- Added a Users screen (Admin creates staff accounts) to Step 5; otherwise no way to create Librarian accounts.

## 2026-11-09 (Step 4A fixes)
- Page renamed "Borrow and Return" (the & in WinForms Text is a mnemonic marker and was swallowed). Set UseMnemonic = false on labels that may show user text later.
- Table cells get Margin = Padding.Empty to remove the default 3px gaps.

## 2026-11-09 (Step 4B result)
- Tested 4 conditions on Windows 11 (build 26300): Application.SetColorMode(Dark) does NOT darken native scrollbars; SetWindowTheme "DarkMode_Explorer" DOES (DataGridView incl. its internal scrollbars, ListView, AutoScroll Panel); custom ThinScrollBar is identical everywhere.
- Decision: tables use DataGridView + DarkMode_Explorer/Explorer by theme (NativeTheme helper and ThemedGrid built at the start of Step 5); card lists use ScrollHost + ThinScrollBar; Application.SetColorMode is not used.
- ListView rejected: its header stays white in dark mode.
- DarkMode_Explorer on Windows 10: UNVERIFIED; worst case a light scrollbar in dark mode.

## 2026-11-09 (Step 4C)
- Loading states: LoadingHost shows a skeleton only if the work exceeds 150 ms, and keeps it at least 300 ms once shown. SkeletonView is custom-painted (rows/cards), pulses via a 60 ms timer that runs only while visible, and stays static when SystemInformation.UIEffectsEnabled is false (UNVERIFIED mapping to the Windows animation setting).
- Actions use BusyButton (spinner + busy text, ignores clicks while busy) to prevent double submits.
- Shared drawing helpers live in Theming/Gfx.cs (RoundedRect, Mix, DrawSpinner).
- ShellForm.SwapPage(control, title) is the way to show any page in the host.


## 2026-11-10 (Enhancement Planning and Theme System)

### Product Direction

- LiteraHub will evolve into an intelligent hybrid
  library management and digital reading system.
- STI Balagtas Case Study #5 remains the source
  of truth for mandatory features.
- New features are documented in
  docs/ENHANCEMENT_ROADMAP.md.
- Step 5A.4B is temporarily paused while the
  enhancement documentation and theme system
  are implemented and tested.

### Nine Theme Presets

Replace the two-choice appearance experience
with nine named color presets:

1. Classic Light
2. Midnight Library
3. Sepia Paper
4. Forest Scholar
5. Slate & Teal
6. Lavender Dusk
7. Ocean Breeze
8. Rosewood
9. Graphite

- Midnight Library is the default dark preset.
- Classic Light is the default light preset.
- Existing orange theme colors are historical
  decisions and will be superseded.
- The existing Theme.Colors design-token pattern
  will be preserved.
- The theme engine must distinguish light and
  dark native-control styling independently
  of the selected preset name.
- Theme changes must update existing controls
  without requiring application restart.
- Theme preferences should persist across sessions.
- Existing application functionality must continue
  working after the theme migration.

### Theme Architecture

- Application and reader appearance will have
  separate user-selectable preferences.
- The same nine presets may be shared by both.
- Reader-specific typography, margins, and
  line spacing will be added with the reader.
- Reader appearance is a planned feature, not
  an implemented feature.
- No new database migration is required solely
  for the initial local theme selector.
- Per-account theme synchronization is deferred
  until user preferences are designed.

### Enhanced Library Features

Planned:
- ISBN-powered metadata import and book covers
- Member book requests and staff approval
- Physical and digital catalog support
- PDF and EPUB reading
- Bookmarks, highlights, notes, and reading progress
- Parallel Read with two reading panes

Optional:
- Wikipedia lookup
- DeepL translation
- Text-to-speech
- AI-assisted descriptions and recommendations

### Implementation Order

P1. Update project documentation.
P2. Implement nine-preset theme engine.
P3. Implement theme selector and local persistence.
P4. Test theme switching and regressions.
5A.4B. Resume Categories database integration.

### Engineering Constraints

- Preserve the existing four-project architecture.
- Continue using programmatic Windows Forms.
- Avoid changing already-applied SQL migrations.
- Keep API credentials outside source control.
- Validate third-party integrations before adoption.
- Do not implement optional features before their
  architecture, permissions, and data requirements
  are reviewed.
