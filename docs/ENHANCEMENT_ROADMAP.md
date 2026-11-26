# LiteraHub — Development Roadmap

**Updated:** 2026-11-26  
**Primary scope:** STI Balagtas Case Study #5  
**Verified GitHub baseline:** `b2c2e4960bc572205e282c99321c8dc4197458cc`; F5.6.1 user-tested, final push pending verification.

## Complete: Catalog and shared foundation

- Existing .NET 10 / SQL Server / Dapper four-project architecture, login/auth baseline, nine-theme shell and desktop presets.
- Categories, Authors, Books, Students CRUD, search/filter/sort/pagination, themed details, permissions and navigation.
- F1 cover cache; F2 shared details header/footer/gallery; F3 loading/empty/error/retry/success-feedback; F4 theme-aware validation and compact-layout fixes.
- Refactor conclusion: **shared composition is substantially complete**. No generic catalog inheritance rewrite necessary before F6.

## F5 — Completed (user-tested)

- F5.0–F5.2: theme-adaptive split-screen login, three local carousel slides, validation, password visibility, and borderless themed window controls.
- F5.3: staff CRUD, temporary passwords, personal names and Full Name search.
- F5.3.5: granular RBAC, custom roles, permission-based navigation and transactional delegation checks.
- F5.4: Profile & Settings, self-service name/username/password changes, recovery passkey management, nine-card theme picker, Go to Profile from own User Details.
- F5.4.1: shared `ThemedDialogChrome`, 1px outline, flush-left accent stripe, draggable dialogs and themed notification feedback.
- F5.5: offline recovery with hashed single-use 16-character alphanumeric passkeys, five failed attempts/15-minute lockout, issuance and regeneration audits, animated Forgot Password panel swap; mandatory password change stays in right login panel, confirmed sign-out returns to login.
- F5.6: immutable protected System Owner identity for the bootstrap administrator; staff management cannot modify owner account; personal changes remain in Profile.
- F5.6.1: equivalent-authority account changes require operation-specific owner authorization, existing RBAC permission and transactional checks; five failed attempts/15-minute persistent lockout, audit records, role escalation restrictions. Higher/incomparable permissions are blocked for non-owner actors. User-reported tests passed.

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

## Deferred / optional cleanup

- Consolidate Student list loading architecture with `LoadingHost` if a concrete bug or maintenance need arises.
- Extract painting/layout helpers from large forms as needed; do not destabilize accepted UI for arbitrary line-count reduction.
- Add automated service/data tests prior to transactional circulation changes.
- Future digital reader / notes / bookmarks / translation are non-MVP enhancements.

## Collaboration protocol

Full replacement `.cs` files with exact paths for larger changes; targeted snippets for one-line fixes. One tested batch at a time, user-controlled Git commits. `AUTHOR_SELECTION_BATCH.md` intentionally retired; eight updated docs form the maintained baseline.
