# LiteraHub — Session Handoff

**Updated:** 2026-11-28

**Verified GitHub master:** `468f8f8187026dd933dd6a7861e27325cc736dce` — F7.0.3, checked during handoff. **Local acceptance:** 29/29 tests passed; V13, category CRUD audit, authentication audit and student PSGC persistence verified by user screenshots. No independent Windows build or SQL execution claimed. F7.1 approved, not delivered or accepted.

## Current state

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

## F7.0 — Accepted engineering and security baseline

- F7.0.1: `LiteraHub.Tests` xUnit project, Windows GitHub Actions workflow and portable solution references. Initial 13 tests passed locally. CI definition exists; successful CI run on current commit is not independently established. SQL integration tests remain outstanding.
- F7.0.2: secure random initial administrator credential shown through authorized provisioning, no hardcoded universal password; preserve existing credentials. BCrypt input policy uses UTF-8 byte length, not character count. Pending mandatory password change cannot establish an ordinary session before completion.
- F7.0.2.1: five ordinary failed login attempts trigger a 3-minute persistent lockout; recovery and Owner authorization remain five failures/15 minutes. Exact remaining lockout countdown only disclosed when correct credentials are supplied for an eligible locked account. Recovery passkeys are hashed, single-use; successful recovery/unlock clears ordinary login lockout. Existing recovery is staff-only until F7.1 extends it safely.
- V12 adds `Users.LoginFailures` and `LoginLockedUntil` with consistent-state constraint.
- F7.0.3: `AuditEvent` and `AuditWriter` record successful catalog, staff, role and profile mutations in the same SQL transaction, and selected authentication events. Preserve RecoveryAudit and OwnerAuthorizationAudit. Credential verification is not proof of completed mandatory password change. Audit logs contain fixed event descriptions and no secrets.
- V13 adds `dbo.ActivityAudit`. No standalone audit reader/UI yet. Not all denied actions, sign-outs, direct SQL modifications or tamper-resistance are covered. No historical backfill invented.
- StudentRepository UPDATE now persists all six PSGC code/name fields. User verified Central Luzon/Bocaue/Turo codes after editing a legacy record. Legacy unconverted rows can remain NULL.
- Final local acceptance: 29/29 tests, V13 startup, authentication audit, student update and five category lifecycle audit events; deleted category history preserved. Latest pushed commit verified.

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

## Development delivery contract (mandatory)

- Inspect latest pushed GitHub source and relevant files first; current source wins over old ZIP/docs. If newest user ZIP differs, identify exact conflict; do not overwrite accepted work. Work against verified commit and provide exact paths.
- When user says **Proceed / Continue / Pushed**, execute the next approved coding batch in that same response; do not ask for approval again or respond with another roadmap. Verify push when relevant. Start actual file generation immediately after targeted inspection. No background-work promises. Do not claim generated files until exact paths exist and are checked. If a complete batch is impossible, deliver the largest coherent safe implementation possible, explain precisely what remains and whether it is installable; never present unfinished code as a complete batch or enable insecure login. Avoid repeated planning-only responses.
- Prefer one feature-complete batch, one acceptance session, one commit; combine backend/UI/migration/tests for F7.1 rather than four tiny replacements. Minimize shared file churn. Full individual downloadable `.cs` replacements with exact destination paths; `.Designer.cs` only if needed; precise snippets for tiny 1–10 line fixes. No source-code ZIP. Documentation-only ZIP is acceptable for handoff. Do not commit/push automatically; user tests and pushes.
- Preserve existing four application projects plus Tests, .NET 10, SQL Server, Dapper, BCrypt, automatic transactional migrations, existing data, RBAC, Owner and peer safeguards, explicitly entered zero inventory. Never reset DB to fix migration. Diagnostic SSMS queries read-only unless approved repair; never destructive test on real database.
- Preserve all nine themes, borderless chrome, shared UI controls, compact 1280×720 and maximize actual display, expanded/collapsed sidebar, no mobile/portrait. Reuse cards, galleries/cache, filters/sort/pagination, themed feedback/dialogs and validation. Avoid arbitrary refactors and duplicated controls; inspect exact signatures first.
- Provide focused acceptance checklist and actual verification limits. Do not claim compile/runtime/CI success without evidence. User accepts, commits/pushes, assistant verifies next baseline. Keep docs synchronized after milestones.

## New chat first action

Read master prompt and checklist; verify master commit and inspect exact source. Implement F7.1 now, with actual source files and testing instructions. User has not missed a file or unfinished previous step. Prior planning-only turns did not deliver F7.1. Ignore incomplete scratch files unless independently reconciled and completed against GitHub. Do not ask the user to repeat scope or approval.
