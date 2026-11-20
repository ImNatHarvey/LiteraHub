# LiteraHub — Architecture and Decisions Log

**Updated:** 2026-11-20

## Foundation

- Four-project layered solution: Core, Data, Services, WinForms.
- C#/.NET 10, programmatic WinForms, SQL Server Express, Dapper, idempotent versioned migration approach.
- SQL belongs in Data; validation/permissions in Services; UI in WinForms; manual dependency wiring in `Program.cs`.
- Staff Admin/Librarian roles; student record != user login. Borrower portal is not required for the present academic MVP.
- Previously recorded loan policy: 7-day default, maximum 3 active loans, no fines, damaged books not borrowable. Verify actual seeded settings and service rules when implementing circulation.
- Soft delete/archive preferred; hard deletion requires authorization and referential safety.

## Visual system

- Nine presets: Classic Light, Midnight Library, Sepia Paper, Forest Scholar, Slate & Teal, Lavender Dusk, Ocean Breeze, Rosewood, Graphite.
- Use existing themed controls, custom scrollbars, action icons, tooltip and pagination patterns.
- Preserve approved responsive sizes 1280×720, 1920×1080 and 390×844; do not hardcode layouts that break portrait mode.
- Books uses dedicated Add/Edit page rather than discarded modal BookDialog.

## Books and inventory

- Open Library metadata and cover preview assist staff; imported data requires review.
- Authors may be selected/created through approved workflows; do not silently create categories from unverified metadata.
- Good condition appears as “Good” in the UI; verify stored status mapping before changes.
- `AvailableCopies=0` is a legitimate explicit value even when `TotalCopies>0`.
- `TotalCopies - AvailableCopies` is **not** equivalent to active loans.
- Step 5B.6 user-tested repository fix persists the requested available count and validates against actual unreturned loan rows.
- Future Borrow/Return operations must adjust inventory atomically and preserve loan history; avoid stale UI calculations.

## Current implementation order (user approved)

1. Student Management.
2. Login UI polish and secure Forgot Password.
3. Borrow & Return.
4. Reports and full academic acceptance.

## Authentication recovery design constraints

- Never store plaintext passwords or email reset tokens.
- Do not claim Forgot Password is implemented; select recovery method only after checking available infrastructure and security requirements.
- Prefer time-limited, single-use, securely hashed reset tokens with rate limiting and no account-enumeration disclosures if email recovery is feasible; otherwise propose an auditable admin-assisted staff reset workflow.
- Never create a universal bypass/master password.

## Workflow

- Source-of-truth priority: latest explicit user direction, latest working files, verified GitHub, developer-confirmed decisions; the case study remains binding for mandatory scope.
- Complete replacements, exact paths, concise tests, docs updates, user-controlled commits.
- Developer “All passed” is evidence of developer testing, not independent ChatGPT execution.

## 2026-11-21 — Desktop-only UI decision

- Supported desktop presets: 1280×720 and 1920×1080.
- The 390×844 portrait preset is retired.
- The native Shell maximize button switches between the two desktop presets.
- Sidebar collapse is independent of window size.
- The LiteraHub logo uses an embedded transparent PNG,
  dynamically tinted using Theme.Colors.Accent.
- The hamburger uses the existing FontAwesome.Sharp library.
- Existing responsive fallback code may remain, but portrait
  testing is no longer an acceptance requirement.