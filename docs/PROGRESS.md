# LiteraHub — Development Progress

**Updated:** 2026-11-26  
**Repository:** `ImNatHarvey/LiteraHub`  
**Latest verified baseline:** `b2c2e4960bc572205e282c99321c8dc4197458cc` — overlap fix user module; F5.6.1 user-tested, final push pending verification.

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

- **Shared component extraction is substantially finished.** Pages still individually inherit from `ThemedUserControl`; no generalized `CatalogPageBase<T>` was implemented or is required before F6. Prefer composition over risky cross-module inheritance rewrites.
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

Baseline GitHub commit: `b2c2e4960bc572205e282c99321c8dc4197458cc` (overlap fix user module). F5.6.1 replacement batch passed user-reported acceptance tests; its subsequent commit is not yet verified. No independent Windows build or SQL runtime test was performed for this documentation update.

## F6 — PSGC offline address integration (NEXT)

- Verify official PSA dataset release, provenance, format, attribution and redistribution terms; package versioned local snapshot. No live network required.
- Searchable cascading Region → Province (where applicable) → City/Municipality → Barangay; reset dependent selections; handle NCR and independent cities, leading zeros and retired codes.
- Preserve existing addresses and required fields, including editable house number, street and ZIP; never infer ZIP from PSGC. Decide persistence and legacy mapping before schema changes; use new migration only if needed.
- Inspect Student models, repository, service, details page and themed selectors first. Test Add/Edit/View, legacy records, offline use, all nine themes, 1280×720 and maximized desktop with sidebar expanded/collapsed.

## F7 — Borrow & Return

- Implement student/book selection, due dates, loan limits, overdue detection, return condition/remarks and history. Atomic transactions, concurrency-safe inventory, no duplicate return; preserve explicitly entered zero availability. Search/filter/sort/pagination and RBAC.

## F8 — Dashboard, Reports & Audit

- Actual database counts, trends, mandatory books by category/author/status/all books reports, circulation and overdue reports, date filters, PDF export; activity log and notification center; integrate existing security audit events and enforce permissions.

## F9 — Quality & Release

- Automated service/integration tests, fresh-install and upgrade tests, permissions/security regression, all nine themes and compact/maximized desktop, warnings/obsolete code cleanup, documentation and academic demonstration.

## Documentation maintenance

- This document replaces stale November 20 student-development checkpoint notes.
- `docs/AUTHOR_SELECTION_BATCH.md` is retired at user request and intentionally excluded from the eight-file update set; delete it explicitly in Git after checking no active links need it.
