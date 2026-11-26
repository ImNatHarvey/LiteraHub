# LiteraHub — Architecture and Decisions Log

**Updated:** 2026-11-26

## Foundation and boundaries

- C#/.NET 10 WinForms, SQL Server Express, Dapper; four-project Core / Data / Services / WinForms architecture.
- Core owns interfaces/models/security contracts; Services owns business rules and permissions; Data owns SQL/migrations; WinForms owns presentation; `Program.cs` composes dependencies manually.
- Staff Admin/Librarian accounts are distinct from Student borrower records. Borrower portal is outside the required MVP.
- Soft archive is the default; hard-delete requires permission, dependency safeguards, and database referential integrity.
- Developer-approved desktop targets are **1280×720 and 1920×1080**, with sidebar expanded/collapsed independently. Portrait 390×844 is retired from acceptance testing.
- Nine existing theme presets remain supported; no unexpected horizontal scrollbars, native-white artifacts, inaccessible focus, or clipped dialogs.

## Refactor status

- Reuse `DetailsPageHeader`, `DetailsActionFooter`, `AssociatedBooksGallery`, `LoadingHost` (where adopted), `CatalogEmptyState`, `CatalogActionFeedback`, and `CatalogValidationController` rather than duplicating UI.
- The refactor strategy is **composition**, not mandatory inheritance from one generic catalog page base. Remaining localized duplication is acceptable until a specific feature justifies extraction.
- All four catalog workflows and hybrid validations were developer-reported passed and pushed through commit `852b6a77` on 2026-11-25.

## Authentication and security — accepted F5

- F5.0–F5.2: theme-adaptive split-screen login, three local carousel slides, validation, password visibility, and borderless themed window controls.
- F5.3: staff CRUD, temporary passwords, personal names and Full Name search.
- F5.3.5: granular RBAC, custom roles, permission-based navigation and transactional delegation checks.
- F5.4: Profile & Settings, self-service name/username/password changes, recovery passkey management, nine-card theme picker, Go to Profile from own User Details.
- F5.4.1: shared `ThemedDialogChrome`, 1px outline, flush-left accent stripe, draggable dialogs and themed notification feedback.
- F5.5: offline recovery with hashed single-use 16-character alphanumeric passkeys, five failed attempts/15-minute lockout, issuance and regeneration audits, animated Forgot Password panel swap; mandatory password change stays in right login panel, confirmed sign-out returns to login.
- F5.6: immutable protected System Owner identity for the bootstrap administrator; staff management cannot modify owner account; personal changes remain in Profile.
- F5.6.1: equivalent-authority account changes require operation-specific owner authorization, existing RBAC permission and transactional checks; five failed attempts/15-minute persistent lockout, audit records, role escalation restrictions. Higher/incomparable permissions are blocked for non-owner actors. User-reported tests passed.

The owner is an account identity, not an assignable role. The underlying Admin role remains. Own-account management routes through Profile. Owner approval does not confer missing permissions; approval is tied to a specific operation. The owner personally enters the password without disclosing it; separate owner sign-in is the stronger alternative. Audit records contain no plaintext credentials. Database safeguards do not defend against privileged SQL Server operators.

Use shared composition, theme-aware dialogs, and no native white caption bars. The compact shell is 1280×720; maximize fills the available display rather than forcing fixed 1920×1080. Preserve all nine themes.

## PSGC address selection — proposed design, not implemented

- Prefer a **versioned, locally bundled, read-only JSON dataset** converted from the Philippine Statistics Authority PSGC publication, rather than an online API call on every dropdown selection.
- PSA reference: https://psa.gov.ph/classification/psgc/ ; PSA API: https://psa.gov.ph/classifications-api/psgc . As of this planning checkpoint, 3Q 2026 / 30 September 2026 is the latest verified publication; validate the exact source file and license/attribution before packaging.
- Order: Region → Province (if applicable) → City/Municipality → Barangay. Handle NCR, independent cities and BARMM/other special geography without assuming every city has a province.
- Store PSGC codes as strings (retain leading zeros); display names from versioned snapshot; normalize text for search but do not guess official codes. Existing Student service requires `HouseNo`, `Street`, `Municipality`, `CityProvince`, `ZipCode`. PSGC does **not** automatically supply reliable ZIP codes; keep ZIP editable/validated separately.
- Decide how to persist province, region and barangay without destroying current addresses before migration. Preserve ability to view/edit older free-text student records. Dataset upgrades must not silently reassign saved addresses.

## Inventory and next modules

- `AvailableCopies=0` is valid with positive Total. Do not infer active loans from `Total - Available`.
- Loan policy previously recorded as 7 days, max 3 active loans, no fines, damaged books not borrowable; verify seeded settings and current policy before implementing.
- Next order: Login foundation/carousel → secure reset → offline PSGC → Borrow & Return → Reports → final academic acceptance.

## Source, testing and commits

- Priority: user decisions, newest working source, verified GitHub, case-study minimum requirements. Do not overwrite accepted work with older snapshots.
- Full `.cs` replacements and exact paths for large changes; focused snippets for small corrections. User builds and pushes; assistant does not claim independent runtime tests.
- Retire `AUTHOR_SELECTION_BATCH.md` by user direction; retain historical code history in Git.
