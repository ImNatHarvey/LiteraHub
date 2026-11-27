# LiteraHub — Development Progress

**Updated:** 2026-11-27  
**Repository:** `ImNatHarvey/LiteraHub`  
Verified GitHub baseline: `8801495d1b1a323c0e12a9465d776b8bb629cfa9` (portable PSGC converter and solution reference). F5.7, F5.8 and F6 passed user-reported acceptance tests and were pushed. F7.0.1 test project and CI are delivered for acceptance; no independent .NET build or SQL runtime test has been performed.

> Statuses refer to developer-reported Visual Studio/runtime tests plus observed GitHub commits. ChatGPT has not compiled the Windows solution independently. Older historical batch notes formerly in this file are superseded by this reconciled checkpoint; Git history preserves the implementation sequence.

## Accepted / pushed work

| Workstream | Status | Notes |
|---|---|---|
| Four-project solution | Existing | .NET 10 / Core, Data, Services, WinForms; Dapper + SQL Server |
| Themed shell | Developer-tested / pushed | Nine theme presets; desktop 1280×720 / maximized actual display size; independent sidebar collapse |
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

- **Shared component extraction is substantially finished.** Pages still individually inherit from `ThemedUserControl`; no generalized `CatalogPageBase<T>` was implemented or is required before circulation. Prefer composition over risky cross-module inheritance rewrites.
- Deferred maintenance: `StudentsPage` loading does not use the same `LoadingHost` composition as other catalog lists; large page source files; automated testing for service and data invariants.

## F5 accepted by user

- F5.0–F5.2: theme-adaptive split-screen login, three local carousel slides, validation, password visibility, and borderless themed window controls.
- F5.3: staff CRUD, temporary passwords, personal names and Full Name search.
- F5.3.5: granular RBAC, custom roles, permission-based navigation and transactional delegation checks.
- F5.4: Profile & Settings, self-service name/username/password changes, recovery passkey management, nine-card theme picker, Go to Profile from own User Details.
- F5.4.1: shared `ThemedDialogChrome`, 1px outline, flush-left accent stripe, draggable dialogs and themed notification feedback.
- F5.5: offline recovery with hashed single-use 16-character alphanumeric passkeys, five failed attempts/15-minute lockout, issuance and regeneration audits, animated Forgot Password panel swap; mandatory password change stays in right login panel, confirmed sign-out returns to login.
- F5.6: immutable protected System Owner identity for the bootstrap administrator; staff management cannot modify owner account; personal changes remain in Profile.
- F5.6.1: equivalent-authority account changes require operation-specific owner authorization, existing RBAC permission and transactional checks; five failed attempts/15-minute persistent lockout, audit records, role escalation restrictions. Higher/incomparable permissions are blocked for non-owner actors. User-reported tests passed.

Verified GitHub baseline: `8801495d1b1a323c0e12a9465d776b8bb629cfa9` (portable PSGC converter and solution reference). F5.7, F5.8 and F6 passed user-reported acceptance tests and were pushed. F7.0.1 test project and CI are delivered for acceptance; no independent .NET build or SQL runtime test has been performed.

## F5.7–F5.8 — Responsive cards (completed)

- F5.7: reusable `ThemedSectionCard`, `ResponsiveSectionLayout`, `ResponsiveDetailsPair`; responsive Book, Student, Author, Category, User and Role details; expandable galleries; three-column Profile & Settings; avatar hover-to-edit. Outer card borders align with actions.
- F5.8: reusable `ModuleSummaryCard` and `ModuleSummaryCardStrip` for Books, Authors, Categories, Students, Users and Roles. Metrics use loaded records, meaningful cards integrate with existing filters, and shared `CatalogToolbarExtras` restores toolbar height and spacing. Circulation metrics deferred until actual loans exist.
- Compact 1280×720 and maximized actual display size; independent sidebar state; nine themes. Existing CRUD, permissions and database retained.

## F6 — PSGC offline address integration (completed)

- PSA 2026-Q3 dataset embedded in Services under `LiteraHub.PSGC.Data`: 18 regions, 82 provinces, 1,642 cities/municipalities, 14 Manila districts and 42,010 barangays. See `LiteraHub.Services/Resources/PSGC/SOURCE.md` for attribution and conversion details.
- Searchable cascading Region → Province (where applicable) → City/Municipality → Barangay; independent cities, NCR and province-free localities supported. ZIP remains manual.
- V11 adds nullable structured address codes and names; legacy addresses are preserved until deliberately converted. Final UI removes ordinary address preview and legacy captions while preserving conversion and error handling.
- Converter is tracked at `tools/psgc/convert_psgc.py`; solution reference is repository-relative.

## F7 — Engineering foundation and Borrow & Return

- F7.0.1 (current): xUnit `LiteraHub.Tests` and Windows GitHub Actions restore/build/test workflow. Initial tests cover existing ASCII password policy and embedded PSGC counts, hierarchy, invalid combinations, province-free geography and caching. No SQL Server required for this batch. Test execution pending acceptance.
- F7.0.2: correct BCrypt UTF-8 byte limit, securely provision initial owner credentials, and implement persistent login throttling/lockout; preserve existing accounts and protections.
- F7.0.3: central audit model, repository and service; preserve existing recovery/owner audit records. Use transaction-aware success logging and never log credentials. Add isolated SQL integration and authorization tests.
- F7.1: implement student/book selection, loan dates, limits, overdue detection, returns, condition and remarks, search/filter/sort/pagination and RBAC. Atomic concurrency-safe transactions, no duplicate return, explicit zero availability preserved. Reuse shared cards and toolbar.
- F7.2: permission-controlled institutional loan settings. Existing defaults are 7 days and 3 active loans; approve other policy choices before implementation.

## F8 — Dashboard, Reports & Audit

- Actual database counts, trends, mandatory books by category/author/status/all books reports, circulation and overdue reports, date filters, PDF export; activity log and notification center; integrate existing security audit events and enforce permissions.

## F9 — Quality & Release

- Automated service/integration tests, fresh-install and upgrade tests, permissions/security regression, all nine themes and compact/maximized desktop, warnings/obsolete code cleanup, documentation and academic demonstration.

## Documentation maintenance

- This document reconciles the completed F5.7–F6 work and introduces the pending F7.0.1 acceptance checkpoint.
- `docs/AUTHOR_SELECTION_BATCH.md` is retired at user request and intentionally excluded from the eight-file update set; delete it explicitly in Git after checking no active links need it.
