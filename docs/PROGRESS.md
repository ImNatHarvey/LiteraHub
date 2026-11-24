# LiteraHub — Development Progress

**Updated:** 2026-11-25  
**Repository:** `ImNatHarvey/LiteraHub`  
**Latest verified push:** `852b6a77d21a776a892abe294d4f4fdd4a52f249` — `feat: overhaul catalog validation and responsive feedback`

> Statuses refer to developer-reported Visual Studio/runtime tests plus observed GitHub commits. ChatGPT has not compiled the Windows solution independently. Older historical batch notes formerly in this file are superseded by this reconciled checkpoint; Git history preserves the implementation sequence.

## Accepted / pushed work

| Workstream | Status | Notes |
|---|---|---|
| Four-project solution | Existing | .NET 10 / Core, Data, Services, WinForms; Dapper + SQL Server |
| Themed shell | Developer-tested / pushed | Nine theme presets; desktop 1280×720 / 1920×1080; independent sidebar collapse |
| Categories | Developer-tested / pushed | CRUD, search/filter/sort/pagination, archive/restore/delete, Associated Books |
| Authors | Developer-tested / pushed | CRUD, authors/gender/birthdate, archive/restore/delete, Associated Books |
| Books | Developer-tested / pushed | CRUD, read-only View/Edit, ISBN lookup, authors/category/condition, inventory and covers |
| Students | Developer-tested / pushed | Directory, details, field validation, photo, archive/restore and guarded deletion |
| F1 image cache | Developer-tested / pushed | Shared session cover/image cache |
| F2 details composition | Developer-tested / pushed | Shared `DetailsPageHeader`, `DetailsActionFooter`, `AssociatedBooksGallery` |
| F3 shared states | Developer-tested / pushed | Loading reliability; empty/errors/retry; success feedback; Back focus correction |
| F4 validation | Developer-tested / pushed | Theme-aware danger borders, summary dialogs, required fields, compact layouts |
| F4.0 gallery | Developer-tested / pushed | Suppress scrollbar when no associated books |

## Important inventory invariants

- `TotalCopies >= 0`; `0 <= AvailableCopies <= TotalCopies`.
- `Total=1, Available=0` must remain valid; raising Total must not silently increase Available.
- `Total - Available` does **not** equal active loans; use actual unreturned transaction records when enforcing borrow constraints.
- Borrow/Return must update loan rows and availability atomically; concurrency requires explicit tests.

## Refactor closure — decision

- **Shared component extraction is substantially finished.** Pages still individually inherit from `ThemedUserControl`; no generalized `CatalogPageBase<T>` was implemented or is required before Login. Prefer composition over risky cross-module inheritance rewrites.
- Deferred maintenance: `StudentsPage` loading does not use the same `LoadingHost` composition as other catalog lists; large page source files; automated testing for service and data invariants.

## Immediate upcoming milestones

1. **F5 Login:** themed split-pane UI with local monochrome `Assets/Brand/Slide1`–`Slide3` artwork recolored per theme (file format/actual build resource names need verification); carousel; field-level validation; secure admin-assisted reset workflow; password-change regression.
2. **PSGC offline addresses:** acquire authoritative dated Philippine Statistics Authority PSGC snapshot, convert/validate offline JSON, create Region → Province (where applicable) → City/Municipality → Barangay selectors, preserve existing Student input/service model until explicit migration approval.
3. **Borrow & Return:** transactional loan/return logic, student/ISBN lookup, due dates, inventory integrity, permissions and history.
4. **Reports:** all four required academic summary lists; exports only if prioritized.
5. Final acceptance: roles, database, case-study search, offline operation, desktop themes, migrations and release packaging.

## Next test gate

Login foundation: valid/invalid credentials, forced password change, logout, theme persistence and slide rendering on 9 themes, 1280×720 and 1920×1080, with no horizontal scrollbars or external network dependency. Reset and PSGC have separate acceptance gates.

## Documentation maintenance

- This document replaces stale November 20 student-development checkpoint notes.
- `docs/AUTHOR_SELECTION_BATCH.md` is retired at user request and intentionally excluded from the eight-file update set; delete it explicitly in Git after checking no active links need it.
