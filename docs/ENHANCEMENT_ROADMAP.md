# LiteraHub — Development Roadmap

**Updated:** 2026-11-20  
**Primary scope:** STI Balagtas Case Study #5

## Phase 1 — Foundation and completed catalog work

**Developer-tested/pushed (reported):** architecture and authentication foundation, themed shell, nine presets, responsive controls and scrollbars, Categories/Authors/Books CRUD, ISBN lookup, book cover preview, author selection, themed filter/sort menus, gender icons, and Step 5B.6 inventory consistency fix.

## Phase 2 — Student Management (NEXT)

- **6A:** inspect schema, permissions, sidebar, existing contracts; establish a testable foundation.
- **6B:** Students listing: columns, search, filter/sort, pagination, action icons, theme/portrait behavior.
- **6C:** Add/Edit Student: all case-study fields, unique StudentNo, validation, accessible controls.
- **6D:** Archive/Restore/Delete with permissions, confirmation, and foreign-key/history safeguards.
- **6E:** Test full-name/partial-name/StudentNo search, duplicate handling, database round trips, theme and responsive regressions.

The sequence may be adjusted after inspecting actual source, but do not skip mandatory student fields or required search.

## Phase 3 — Authentication polish and recovery

- Audit existing `LoginForm`, `AuthService`, users/roles, password hash and account provisioning.
- Improve responsive login UI, themed focus/error states, show/hide password, keyboard access and feedback.
- Choose and implement a secure Forgot Password flow appropriate to the desktop deployment (verified email delivery + expiring single-use tokens, or audited admin-assisted reset).
- Add appropriate service/database protections and regression tests; do not introduce an insecure reset bypass.

## Phase 4 — Borrow & Return

- Select existing student and available/borrowable book; validate permissions and loan policy.
- Borrow and return in database transactions; prevent double borrowing and negative availability.
- Record borrow/due/return dates, remarks, ISBN and return condition; preserve transaction history.
- Search by dates, student name and ISBN; report overdue status.
- Verify concurrent edits and inventory invariants.

## Phase 5 — Reports and case-study acceptance

- Books by category, author, status; all books.
- Validate mandatory search facilities across Books, Authors, Students and Borrow/Return.
- Run role, theme, portrait, database and packaging regression tests.

## Future optional enhancements

Hybrid physical/digital catalog, licensed PDF/EPUB reader, highlights/notes/bookmarks, Parallel Read, translation/lookup, accessibility improvements and advanced analytics. These are **not** prerequisites for the academic MVP.
