# LiteraHub — Development Progress

**Updated:** 2026-11-28  
**Repository:** `ImNatHarvey/LiteraHub`  
**Verified GitHub master:** `468f8f8187026dd933dd6a7861e27325cc736dce` — F7.0.3, checked during handoff. **Local acceptance:** 29/29 tests passed; V13, category CRUD audit, authentication audit and student PSGC persistence verified by user screenshots. No independent Windows build or SQL execution claimed. F7.1 approved, not delivered or accepted.

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

**Verified GitHub master:** `468f8f8187026dd933dd6a7861e27325cc736dce` — F7.0.3, checked during handoff. **Local acceptance:** 29/29 tests passed; V13, category CRUD audit, authentication audit and student PSGC persistence verified by user screenshots. No independent Windows build or SQL execution claimed. F7.1 approved, not delivered or accepted.

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

## Current roadmap

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

No F7.1 source has been delivered or accepted. Do not treat any incomplete scratch implementation as baseline.
