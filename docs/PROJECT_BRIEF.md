# LiteraHub — Project Brief and Academic Scope

**Updated:** 2026-11-26  
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

## Implementation checkpoint — 2026-11-26

Catalog F1–F4 and authentication F5 are user-accepted. Baseline GitHub commit: `b2c2e4960bc572205e282c99321c8dc4197458cc` (overlap fix user module). F5.6.1 replacement batch passed user-reported acceptance tests; its subsequent commit is not yet verified. No independent Windows build or SQL runtime test was performed for this documentation update.

- F5.0–F5.2: theme-adaptive split-screen login, three local carousel slides, validation, password visibility, and borderless themed window controls.
- F5.3: staff CRUD, temporary passwords, personal names and Full Name search.
- F5.3.5: granular RBAC, custom roles, permission-based navigation and transactional delegation checks.
- F5.4: Profile & Settings, self-service name/username/password changes, recovery passkey management, nine-card theme picker, Go to Profile from own User Details.
- F5.4.1: shared `ThemedDialogChrome`, 1px outline, flush-left accent stripe, draggable dialogs and themed notification feedback.
- F5.5: offline recovery with hashed single-use 16-character alphanumeric passkeys, five failed attempts/15-minute lockout, issuance and regeneration audits, animated Forgot Password panel swap; mandatory password change stays in right login panel, confirmed sign-out returns to login.
- F5.6: immutable protected System Owner identity for the bootstrap administrator; staff management cannot modify owner account; personal changes remain in Profile.
- F5.6.1: equivalent-authority account changes require operation-specific owner authorization, existing RBAC permission and transactional checks; five failed attempts/15-minute persistent lockout, audit records, role escalation restrictions. Higher/incomparable permissions are blocked for non-owner actors. User-reported tests passed.

Pending: PSGC offline address selectors (F6), circulation (F7), dashboard/reports/audit (F8), release testing (F9).

## Optional enhancements and scope control

- Nine-themed Login carousel, offline single-use passkey recovery, offline PSGC address lookup, metadata/cover lookup and student photo previews are product enhancements, not substitutes for mandatory case-study functionality.
- The case study names Municipality and City/Province address fields but does not require Region, Province and Barangay selectors. Adding such selectors must preserve the existing schema or use a separately reviewed versioned migration.
- Staff roles and student records are distinct; a borrower login portal is not required for the academic MVP.
