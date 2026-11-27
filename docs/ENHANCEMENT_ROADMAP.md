# LiteraHub — Development Roadmap

**Updated:** 2026-11-28  
**Primary scope:** STI Balagtas Case Study #5  
**Verified GitHub master:** `468f8f8187026dd933dd6a7861e27325cc736dce` — F7.0.3, checked during handoff. **Local acceptance:** 29/29 tests passed; V13, category CRUD audit, authentication audit and student PSGC persistence verified by user screenshots. No independent Windows build or SQL execution claimed. F7.1 approved, not delivered or accepted.

## Complete: Catalog and shared foundation

- Existing .NET 10 / SQL Server / Dapper four-project architecture, login/auth baseline, nine-theme shell and desktop presets.
- Categories, Authors, Books, Students CRUD, search/filter/sort/pagination, themed details, permissions and navigation.
- F1 cover cache; F2 shared details header/footer/gallery; F3 loading/empty/error/retry/success-feedback; F4 theme-aware validation and compact-layout fixes.
- Refactor conclusion: **shared composition is substantially complete**. No generic catalog inheritance rewrite necessary before circulation.

## F5 — Completed (user-tested)

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

## F7.0 — Accepted engineering and security baseline

- F7.0.1: `LiteraHub.Tests` xUnit project, Windows GitHub Actions workflow and portable solution references. Initial 13 tests passed locally. CI definition exists; successful CI run on current commit is not independently established. SQL integration tests remain outstanding.
- F7.0.2: secure random initial administrator credential shown through authorized provisioning, no hardcoded universal password; preserve existing credentials. BCrypt input policy uses UTF-8 byte length, not character count. Pending mandatory password change cannot establish an ordinary session before completion.
- F7.0.2.1: five ordinary failed login attempts trigger a 3-minute persistent lockout; recovery and Owner authorization remain five failures/15 minutes. Exact remaining lockout countdown only disclosed when correct credentials are supplied for an eligible locked account. Recovery passkeys are hashed, single-use; successful recovery/unlock clears ordinary login lockout. Existing recovery is staff-only until F7.1 extends it safely.
- V12 adds `Users.LoginFailures` and `LoginLockedUntil` with consistent-state constraint.
- F7.0.3: `AuditEvent` and `AuditWriter` record successful catalog, staff, role and profile mutations in the same SQL transaction, and selected authentication events. Preserve RecoveryAudit and OwnerAuthorizationAudit. Credential verification is not proof of completed mandatory password change. Audit logs contain fixed event descriptions and no secrets.
- V13 adds `dbo.ActivityAudit`. No standalone audit reader/UI yet. Not all denied actions, sign-outs, direct SQL modifications or tamper-resistance are covered. No historical backfill invented.
- StudentRepository UPDATE now persists all six PSGC code/name fields. User verified Central Luzon/Bocaue/Turo codes after editing a legacy record. Legacy unconverted rows can remain NULL.
- Final local acceptance: 29/29 tests, V13 startup, authentication audit, student update and five category lifecycle audit events; deleted category history preserved. Latest pushed commit verified.

## Current implementation order

- [x] F1–F4 — Catalog and reusable UI foundation.
- [x] F5.0–F5.6.1 — Staff authentication, RBAC, recovery, Owner and peer authorization.
- [x] F5.7 — Responsive section cards and details layouts.
- [x] F5.8 — Module summary cards and filter integration.
- [x] F6 — Offline PSGC integration (V11); persistence correction verified in F7.0.3.
- [x] F7.0.1 — xUnit and Windows CI definition; local acceptance (13 tests initially).
- [x] F7.0.2 / F7.0.2.1 — Secure bootstrap, BCrypt UTF-8 policy, persistent login throttling (V12), recovery unlock and countdown.
- [x] F7.0.3 — Transactional ActivityAudit foundation (V13); 29 tests passed locally.
- [ ] **F7.1 — Complete Student Account System (NEXT).**
- [ ] F7.2 — Circulation backend: borrow/return, requests, reservations, renewals, waitlists, fines.
- [ ] F7.3 — Staff circulation interface.
- [ ] F7.4 — Student self-service portal.
- [ ] F7.5 — Library configuration and circulation policies.
- [ ] F8.1 — Staff and student dashboards.
- [ ] F8.2 — Reports and PDF export.
- [ ] F8.3 — Activity Log UI, including existing security audit sources.
- [ ] F8.4 — Staff/student Notification Center.
- [ ] F9.1 — Security and database hardening.
- [ ] F9.2 — Backup, installation and deployment.
- [ ] F9.3 — Final regression, academic demonstration and documentation.

## F7.1 — Complete Student Account System (approved; not implemented)

Approved scope combines former F7.1.1–F7.1.4. No further scope confirmation is needed. Internal role name remains `Borrower`; user-facing display is **Student**, a protected system role, not a custom staff role. System Owner remains a protected Admin identity, not another role.

### Identity and permissions
- [ ] Verify current `RoleNames`, RolePermission enum, role seeding, permission UI, DelegationGuard, PermissionService, StaffUserRepository and services. Preserve all existing stable permission keys; add dedicated student-account management grants and seed them through a new migration if needed. Do not grant staff permissions to Borrower or allow a custom staff role to masquerade as Student.
- [ ] Reuse `Users.StudentId` foreign key and filtered unique index `UX_Users_StudentId`. One account per verified active student, including archived accounts; handle account restoration rather than duplicate creation.
- [ ] Protect system role from rename/archive/delete/permission changes. Map display name consistently, never rename persisted key. Staff Users remains staff-only. Reject cross-role assignment or student identity reassignment outside explicit authorized workflow.
- [ ] Students module hosts linked account status, create/manage account and registration approval queue. Staff permission checks apply independently of student CRUD. Student-facing access is limited to their own identity; validate live user, role and student status at sensitive actions.

### Registration and approval
- [ ] Shared LoginForm registration option using existing theme-aware panel transitions and validation; submit request for an existing student, never auto-create an approved identity.
- [ ] Staff verify actual enrollment/identity via institution-approved evidence, not merely public student number or name. Generic public responses prevent enumeration; rate-limit unauthenticated submissions; avoid storing excessive personal information.
- [ ] Design pending/approved/rejected lifecycle; preserve reviewer, dates, linkage and safe rejection reason; prevent duplicate pending registration and concurrent double approval. Decide whether pending requests store a password hash or credentials are provisioned after approval; no plaintext persistence. Expiry/cancellation and rejected retry rules should be explicit.
- [ ] Authorized staff can create linked accounts directly for verified active students, issue secure temporary password and one-time recovery passkey, force password change. Self-registration approval must securely convey account/recovery instructions; prevent registration takeover or approval of unverified identity.
- [ ] Account activation/archival/restoration and recovery/reset/unlock management with current role/status checks; student archival disables access and existing sessions while retaining circulation history; coordinate account/student state atomically. No routine hard-delete of history.
- [ ] Add next migration V14 only after inspecting V13; constraints, filtered uniqueness, indexes and transactional upgrades. Do not modify applied migrations. Log registration/approval/rejection/account creation and status transitions atomically with safe audit identifiers, no credentials.

### Authentication, shell and profile
- [ ] Inspect AuthService (`BorrowerPortalEnabled = false`), UserRepository, RecoveryRepository, LoginForm and Program composition. Do not flip flag alone. Enable only with safe linked-student validation, restricted session routing, service/repository guards and recovery support ready.
- [ ] Route approved active Student accounts to separate Student Shell; staff continue existing ShellForm. Preserve forced-password-change before session establishment, logout, 3-minute login lockout and 15-minute recovery limit. Never expose student to staff Shell or staff services; reject inactive/unlinked student accounts and invalid sessions.
- [ ] Dedicated student navigation: Home/account status, Profile & Settings now; future catalog/requests/loans/history entries only once functional (no misleading fake data). Reuse existing borderless chrome, theme picker, shared controls and layouts. Restrict changes to authoritative student identity fields; self-service username/password and recovery actions appropriately validated.
- [ ] Preserve all nine themes, compact 1280×720 and maximized actual display size, sidebar states, accessibility, safe disposal and loading feedback.

### Acceptance gate (one cohesive batch)
- [ ] Build solution and run all existing 29 plus new tests; report actual count. Test V14 upgrade and fresh install in isolated database.
- [ ] Self-registration, pending state, duplicate submission, staff rejection and verified approval; staff direct creation; one account per student; credential delivery and mandatory password change.
- [ ] Student login routes correctly; pending, rejected, disabled, archived, unlinked and invalid roles denied. Staff routing and permissions unchanged; student cannot call staff services or view another student’s data. Test direct service calls and transactional races, not just hidden buttons.
- [ ] Verify lockout/countdown, recovery unlock/reset, passkey reuse denial, profile restrictions, logout and role protection; preserve Owner and peer safeguards.
- [ ] Verify atomic audit events, no secret leakage, no data loss, all nine themes and desktop/sidebar combinations. User tests, commits and pushes; verify push before F7.2.

## F7.2 — Circulation backend (combined)

- [ ] Inspect existing `BorrowReturns`, `BookStatuses`, `Settings`, foreign keys/indexes, RBAC, BookRepository active-loan checks, and audit writer. Seeded defaults: 7 loan days, max 3 active loans, Good borrowable/Damaged not borrowable. Other policy values are configurable, not official STI policy.
- [ ] Implement domain models/interfaces, repository and service with server-side search/filter/sort/pagination, full-dataset summary metrics and live authorization. Revalidate actor/identity inside sensitive transactions. Use parameterized SQL and stable lock order, guarded updates, rollback, uniqueness and date constraints. Test concurrent last-copy issue, same-student limit, double approval/return and expiry races in isolated SQL database.
- [ ] Walk-in borrowing: active verified student (account not required), active borrowable book, positive availability, loan limit and overdue/duplicate-title/fine policies; due date from policy, remarks, responsible staff. Decrement availability and insert loan and audit atomically.
- [ ] Return: active loan only, date validation, condition, remarks and responsible staff; preserve historical record; good copy becomes available, damaged copy stays unavailable without marking whole title damaged. Permit return for archived student/book. Return and audit atomic; no duplicate returns.
- [ ] Derive Borrowed, Due Today, Overdue, Returned using institution local date; timestamps UTC. Returned cannot remain overdue. Preserve original dates and history.
- [ ] Requests: pending does not consume stock; staff approval reserves an available copy (Ready for Pickup); pickup issues loan and starts due date; rejection/cancellation/expiry release reservation once where appropriate. Use explicit state machine and transaction-safe deadlines; duplicate requests prevented. Revalidate eligibility at pickup.
- [ ] Waitlist: fair FIFO by stable timestamp/id among eligible students; prevent bypass by direct issue or approval according to policy; handle ineligible candidates and expiry deterministically; students see only own queue position. Reallocate released borrowable copies atomically and notify next eligible member.
- [ ] Renewals: request/approve/reject, max count, overdue/fine/waitlist eligibility and policy snapshot; update due date and preserve renewal history with audit. No silent deadline changes.
- [ ] Fines: configurable daily rate, grace, cap, restrictions; decimal monetary amounts; idempotent accrual/assessment, preserve applied policy and prevent double charge; record authorized counter payments/waivers with reasons, ledger/balance history, no online payments. Do not impose a made-up institutional rate. Default disabled or zero until approved policy configured.
- [ ] Inventory invariant `A + R + B <= T`, all nonnegative: available + approved reserved + active borrowed <= total physical copies. Other unavailable = T-A-R-B. Pending/waitlist do not reserve; pickup moves R→B without second decrement; release R→A; good return B→A, damaged B→other unavailable. Update BookRepository edits/archive/delete rules to protect reservations and active loans, preserve manually entered zero availability, and protect historical references.
- [ ] Expiry/overdue processing during startup/relevant operations; idempotent across instances. No processing while all desktop instances are closed; scheduled server worker is future architecture.
- [ ] Tests: success/rollback and concurrent conflicts, expired pickup, duplicate requests/returns/payments, fine cap/grace, archived records, renewal/waitlist fairness, policy changes, audit/no secrets, permissions and own-data isolation. Do not claim SQL tests ran unless they actually ran.

## F7.3 — Complete staff circulation UI
- [ ] Reuse summary cards, search, separate Filter/Sort, chips, themed table, status icons, chevrons, pagination and shared details header/footer. Cards: active loans, overdue, due today, returned today; requests/reservations, renewals, queue and fines metrics as meaningful.
- [ ] Table fields: transaction ID, student, title, ISBN, borrow/due/return dates, status; support mandatory search by borrow date, return date, student first/last name and ISBN. Extra search by student ID/title/transaction ID; filters for status, date ranges and returned condition.
- [ ] Full-page issue and return details, searchable student/book picker, cover, availability, eligibility, loan counts, dates, condition, remarks, confirmation, authorized actions and read-only history. Prevent duplicate submissions and refresh inventory/cards after successful changes.
- [ ] Manage requests, approve/reject, reserve, cancel/expire, pickup, renewals, waitlist and fine ledger/payments/waivers; enforce permissions, show real data, loading/empty/error states and feedback. Registration approvals already belong to F7.1; avoid duplicating pages.
- [ ] Test workflows across all nine themes, compact/maximized and sidebar states; verify mandatory search, stock, history and audit in database.

## F7.4 — Complete Student Portal
- [ ] Browse searchable/filterable book gallery using existing cover cache/gallery principles; title/author/category/ISBN, availability, book detail and request action. No staff CRUD or sensitive borrower data.
- [ ] My Requests: submit, track, cancel eligible requests; show Ready for Pickup, deadline, rejection/expiry and actual status. Waitlist join/leave, own position only.
- [ ] My Borrowings and history: own active loans, due/overdue dates, returned records, renewal request and decisions; own fine balance, payment/waiver history. No self-issued loan or self-return.
- [ ] Personal summary cards and profile/recovery settings from F7.1; in-app notifications and unread state as F8.4 completes. Use database identity from session, never arbitrary student ID provided by caller. Test cross-account isolation and stale sessions.

## F7.5 — Library Configuration
- [ ] Separate institutional settings from personal profile. Permission-controlled UI/service/transaction and audit.
- [ ] Loan duration, max active loans, overdue/duplicate-title/damaged policy, pickup duration, waitlist fairness, renewal count/duration, fine rate/grace/cap/restrictions, low-availability thresholds and authorized due-date overrides. Validate ranges and dependencies.
- [ ] Preserve existing loan/reservation deadlines and previously assessed fines when changing settings unless explicitly authorized; document policy effective dates. Confirm actual institution rules, avoid arbitrary defaults presented as official.

## F8.1 — Dashboards
- [ ] Staff: actual total titles, physical/available/reserved/borrowed/other unavailable copies, students, active/overdue/due-today/returned loans; recent transactions, low stock, pending requests and overdue items; date-filtered trends, popular categories and most borrowed titles; permission-aware quick actions and contextual navigation.
- [ ] Student: own active loans, due dates, requests, reservations, queue, fines and notifications; no other student data. Reuse cards, charts, responsive layouts; no invented zero on query failure.

## F8.2 — Reports and PDF export
- [ ] Mandatory reports: Books per Category, Books per Author, Books per Status/Condition, All Books. Additional circulation, overdue, returned/damaged, student history, most borrowed, stock/reservations and fines reports.
- [ ] Report type/filter/date ranges, preview, actual totals and generated-by/date; handle multi-author/category joins without inflated unique-title totals; specify archived inclusion. Permission-controlled PDF export with title, filters, repeated table headers, page numbers and long text; consider CSV; audit export. Restrict PII.

## F8.3 — Activity Log
- [ ] Read-only UI and authorized query service; unify ActivityAudit, RecoveryAudit and OwnerAuthorizationAudit without inventing historical actor snapshots. Search, filter, sort, database pagination, event detail, semantic outcome/severity, local display of UTC timestamps, actual summary metrics.
- [ ] Extend coverage to registration, circulation, policy, reports, backup and useful denied operations/sign-out; preserve history, no secrets or unnecessary PII; retention and least-privilege review. No ordinary edit/delete; privileged SQL operators remain trust boundary.

## F8.4 — Notification Center
- [ ] Themed header bell, unread count, read/unread and mark-all, contextual navigation, deduplication and efficient refresh. Staff: approvals, overdue/due today, low/out-of-stock, security events, waitlists. Student: own registration decision, request/renewal decisions, pickup/expiry, due/overdue, fines. Persist relevant recipient/read state and restrict access. No email/SMS/push in initial release.

## F9 — Quality and release
- [ ] F9.1: full RBAC/Owner/delegation/registration/recovery/own-data tests, SQL least privilege and certificate config, migration/runtime privilege separation, no secrets, privacy/retention, audit coverage, concurrency and constraints; documented Owner recovery without bypass password. Trusted kiosk only for direct database connection; do not distribute privileged DB connectivity to untrusted student devices.
- [ ] F9.2: backup/restore runbook, authorized operations, safe backup before restore, isolated restore verification, protect student data, clean Windows deployment, SQL prerequisites, fresh initialization/upgrade, self-contained vs framework-dependent publish, package PSGC/theme/assets, versioning/release notes; test without Visual Studio.
- [ ] F9.3: full catalog/staff/student/circulation/report/notification/backup regression, automated and isolated SQL tests, nine themes, both desktop sizes/sidebar states, 100/125/150% DPI where practical, repeated navigation/theme/resize, keyboard/focus, scrollbars, no white artifacts or disposal failures; warnings, dead code, performance and asynchronous lifecycle review; updated docs/ERD/README/user and technical manuals, fictional demo data, presentation script and clean release.

## Optional / post-MVP
- [ ] USB keyboard-emulation ISBN barcode scanning, CSV import with preview/validation/duplicates/transactions, borrowing/return receipts, targeted maintainability and accessibility improvements.
- [ ] QR student ID, web/mobile portal, secured ASP.NET Core API for untrusted clients, individual-copy accession/barcodes/condition, EPUB/digital lending/reader/notes/bookmarks, cloud sync, external notifications and recommendations. These are deferred; Student desktop accounts, renewal, waitlist and fines are NOT deferred.

## Academic requirements (final verification still pending)
- [x] C# and Microsoft SQL Server, Books/Authors/Categories/Students CRUD foundations and search.
- [x] Student structured address integration and persistence accepted; retain case-study address/contact fields.
- [ ] Verify Books search ISBN/title/author/category/status; Authors ID/name/gender; Students ID/name against original case study.
- [ ] Borrow/Return fields: student ID, borrow date, return date, remarks/comments, ISBN; mandatory search borrow date, return date, student first/last name, ISBN.
- [ ] Four required reports: Books per Category, Books per Author, Books per Status, All Books.
- [ ] Final original case-study compliance and presentation. If schedule constrained, mandatory circulation and four reports outrank optional enhancements; do not silently drop approved scope.
