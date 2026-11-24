# LiteraHub — Architecture and Decisions Log

**Updated:** 2026-11-25

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

## Login carousel — planned, not implemented

- Use the user's monochrome art in `LiteraHub.WinForms/Assets/Brand/Slide1`–`Slide3` and existing brand logo; inspect actual extensions and build actions when implementing.
- For art designed as a monochrome foreground with transparency, apply theme tint using a cached colorized image derived from source alpha/luminance; never overwrite original assets. Choose suitable colors from `Theme.Colors` (Accent/Text/Surface) after checking each theme's contrast; keep backgrounds themed independently.
- Recolor on theme changes and cache by source/theme/size. Preserve alpha, antialiasing, original aspect ratio and DPI; dispose generated bitmaps correctly. Avoid double tinting already colorized images.
- Use local assets only; login works with animation disabled and without image loading. Respect reduced-motion preference where available; accessible Next/Previous/slide indicators and pause on form deactivation. No network dependency.

## Password recovery — proposed design, pending security review

- Desktop app runs without email/SMS/Google; **do not use a self-service reusable PIN, answer to security questions, public reset code or hardcoded master password**.
- Preferred design: an authenticated administrator with an explicit `ResetStaffPasswords` permission can issue a single-use random temporary credential to another staff account, stored only as a strong password hash; force password change at next login, and write an auditable record of actor/target/time/outcome. Admin authentication/reverification, rate limiting and secure offline handling are required.
- Do not permit an administrator to reset their own account via an unauthenticated dialog. Define a separate initial provisioning / last-admin lockout recovery runbook and authorization before coding.
- The Login **Forgot Password?** action should explain "Contact your system administrator" and how a reset request is handled; no identity lookup disclosure.
- Existing `AuthService` password hashing and `MustChangePassword` remain authoritative. Proposed changes may need a permission, user-service operation and versioned audit migration; not implemented yet.

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
