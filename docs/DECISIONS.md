# LiteraHub — Architecture and Decisions Log

**Updated:** 2026-11-27

## Foundation and boundaries

- C#/.NET 10 WinForms, SQL Server Express, Dapper; four-project Core / Data / Services / WinForms architecture.
- Core owns interfaces/models/security contracts; Services owns business rules and permissions; Data owns SQL/migrations; WinForms owns presentation; `Program.cs` composes dependencies manually.
- Staff Admin/Librarian accounts are distinct from Student borrower records. Borrower portal is outside the required MVP.
- Soft archive is the default; hard-delete requires permission, dependency safeguards, and database referential integrity.
- Developer-approved desktop targets are **1280×720 and maximized actual display size**, with sidebar expanded/collapsed independently. Portrait 390×844 is retired from acceptance testing.
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

## F5.7–F5.8 — Responsive cards (completed)

- F5.7: reusable `ThemedSectionCard`, `ResponsiveSectionLayout`, `ResponsiveDetailsPair`; responsive Book, Student, Author, Category, User and Role details; expandable galleries; three-column Profile & Settings; avatar hover-to-edit. Outer card borders align with actions.
- F5.8: reusable `ModuleSummaryCard` and `ModuleSummaryCardStrip` for Books, Authors, Categories, Students, Users and Roles. Metrics use loaded records, meaningful cards integrate with existing filters, and shared `CatalogToolbarExtras` restores toolbar height and spacing. Circulation metrics deferred until actual loans exist.
- Compact 1280×720 and maximized actual display size; independent sidebar state; nine themes. Existing CRUD, permissions and database retained.

## F6 — PSGC offline address integration (completed)

- PSA 2026-Q3 dataset embedded in Services under `LiteraHub.PSGC.Data`: 18 regions, 82 provinces, 1,642 cities/municipalities, 14 Manila districts and 42,010 barangays. See `LiteraHub.Services/Resources/PSGC/SOURCE.md` for attribution and conversion details.
- Searchable cascading Region → Province (where applicable) → City/Municipality → Barangay; independent cities, NCR and province-free localities supported. ZIP remains manual.
- V11 adds nullable structured address codes and names; legacy addresses are preserved until deliberately converted. Final UI removes ordinary address preview and legacy captions while preserving conversion and error handling.
- Converter is tracked at `tools/psgc/convert_psgc.py`; solution reference is repository-relative.

## Inventory and next modules

- `AvailableCopies=0` is valid with positive Total. Do not infer active loans from `Total - Available`.
- Loan policy previously recorded as 7 days, max 3 active loans, no fines, damaged books not borrowable; verify seeded settings and current policy before implementing.
- Next order: F7.0.1 tests/CI → F7.0.2 security → F7.0.3 audit → F7.1 circulation → F7.2 library configuration → F8 operational modules → F9 release.

## Source, testing and commits

- Priority: user decisions, newest working source, verified GitHub, case-study minimum requirements. Do not overwrite accepted work with older snapshots.
- Full `.cs` replacements and exact paths for large changes; focused snippets for small corrections. User builds and pushes; assistant does not claim independent runtime tests.
- Retire `AUTHOR_SELECTION_BATCH.md` by user direction; retain historical code history in Git.
