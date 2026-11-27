# LiteraHub — Project Brief and Academic Scope

**Updated:** 2026-11-27  
**Academic source:** `05 Case Study -Library Management System` (STI Balagtas, Case Study #5)  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Objective

Develop a scaled-down C# and Microsoft SQL library management system for STI Balagtas library staff to manage books, authors, students, and borrowing/return transactions accurately. The academic case study is authoritative; the mockup is a visual reference rather than a replacement for a working WinForms application.

## Required modules and case-study data

| Module | Required information |
|---|---|
| Books | Title, brief description, ISBN, category, author, available copies, condition/status (Good Condition or Damaged) |
| Authors | Author ID, first/middle/last name, birth date, gender |
| Students | Student ID number; first/middle/last name; full address (house number, street, municipality, city/province, ZIP code); birth date; gender; mobile, landline, email |
| Categories | Category ID, category name, category description |
| Borrow and Returns | Student ID, borrow date, return date, remarks/comments, ISBN |

## Required summary lists

1. Books per category.
2. Books per author.
3. Books per status.
4. All books regardless of category or author.

## Required search facilities

- **Books:** ISBN, title, author, category, status.
- **Authors:** Author ID, first/middle/last/complete name, gender.
- **Students:** Student ID, first/middle/last/complete name.
- **Borrow/Return:** Borrow date, return date, student last/first name, ISBN.

## Implementation checkpoint — 2026-11-27

Catalog F1–F4 and authentication F5 are user-accepted. Verified GitHub baseline: `8801495d1b1a323c0e12a9465d776b8bb629cfa9` (portable PSGC converter and solution reference). F5.7, F5.8 and F6 passed user-reported acceptance tests and were pushed. F7.0.1 test project and CI are delivered for acceptance; no independent .NET build or SQL runtime test has been performed.

- F5.0–F5.2: theme-adaptive split-screen login, three local carousel slides, validation, password visibility, and borderless themed window controls.
- F5.3: staff CRUD, temporary passwords, personal names and Full Name search.
- F5.3.5: granular RBAC, custom roles, permission-based navigation and transactional delegation checks.
- F5.4: Profile & Settings, self-service name/username/password changes, recovery passkey management, nine-card theme picker, Go to Profile from own User Details.
- F5.4.1: shared `ThemedDialogChrome`, 1px outline, flush-left accent stripe, draggable dialogs and themed notification feedback.
- F5.5: offline recovery with hashed single-use 16-character alphanumeric passkeys, five failed attempts/15-minute lockout, issuance and regeneration audits, animated Forgot Password panel swap; mandatory password change stays in right login panel, confirmed sign-out returns to login.
- F5.6: immutable protected System Owner identity for the bootstrap administrator; staff management cannot modify owner account; personal changes remain in Profile.
- F5.6.1: equivalent-authority account changes require operation-specific owner authorization, existing RBAC permission and transactional checks; five failed attempts/15-minute persistent lockout, audit records, role escalation restrictions. Higher/incomparable permissions are blocked for non-owner actors. User-reported tests passed.

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

Pending: F8 dashboard/reports/activity log/notifications and F9 release testing.

## Optional enhancements and scope control

- Nine-themed Login carousel, offline single-use passkey recovery, offline PSGC address lookup, metadata/cover lookup and student photo previews are product enhancements, not substitutes for mandatory case-study functionality.
- The case study names Municipality and City/Province address fields but does not require Region, Province and Barangay selectors. F6 adds these selectors through the backwards-compatible V11 migration.
- Staff roles and student records are distinct; a borrower login portal is not required for the academic MVP.
