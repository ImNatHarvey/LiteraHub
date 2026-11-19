# LiteraHub — Development Progress

**Updated:** 2026-11-20  
**Repository:** `ImNatHarvey/LiteraHub`  
**Handoff checkpoint:** After Step 5B.6; plan Step 6 Student Management

> **Status definitions:** “Developer-tested” means the user reported successful Visual Studio/runtime tests. “Pushed” means the user reported pushing; check GitHub again to establish the current remote SHA. ChatGPT has not independently compiled the Windows application. Older GitHub `docs/` files were still dated 2026-11-12 when this handoff was prepared.

## Developer-reported completed milestones

| Area | Status | Details |
|---|---|---|
| Solution foundation | Developer-tested | .NET 10, four-project architecture, SQL Server/Dapper, login foundation, permissions, programmatic WinForms |
| Themes and responsiveness | Developer-tested | Nine theme presets; custom controls/scrollbars; 1280×720, 1920×1080, 390×844 testing |
| Categories | Developer-tested/pushed | CRUD, archive/restore/delete, search, pagination, themed filter/sort |
| Authors | Developer-tested/pushed | CRUD, birth date/gender fields, themed selectors, search/filter/sort, gender submenu icons |
| Books | Developer-tested/pushed | CRUD, Add/Edit page, authors, category/condition, ISBN lookup, cover preview, inventory, search/filter/sort |
| Filter & Sort polish | Developer-tested/pushed | Themed menus/submenus across Categories, Authors and Books; selection labels; gender icons |
| Step 5B.6 inventory fix | Developer-tested/pushed | Save explicitly entered Available=0; validate availability against real active loans rather than `Total - Available` inference |

## Critical inventory behavior

- `TotalCopies >= 0`.
- `0 <= AvailableCopies <= TotalCopies`.
- Total=1, Available=0 is valid and must persist as zero.
- Increasing Total must not silently increase Available.
- Active loans, when present, impose additional server/repository-side constraints.
- User confirmed the Step 5B.6 fix passed all requested tests and was pushed; inspect latest repository before further changes.

## Pending / not yet accepted

- **Step 6: Student Management:** inspect schema, interfaces, repositories, service, permissions, sidebar and page placeholders; implement Student CRUD and full required search.
- **Step 7: Login polish and Forgot Password:** review existing login/auth contracts, design a secure recovery mechanism, implement UI and tests; no recovery mechanism is yet verified.
- **Step 8: Borrow & Return:** connect student records and book inventory with atomic transaction handling, due dates, return conditions, borrowing history and required search.
- **Reports:** four required summary lists and final case-study acceptance.
- End-to-end regression, role checks, data integrity, accessibility, release/demo setup.

## Immediate next batch

**Step 6A — Student Module Foundation:** inspect current GitHub source plus the newly uploaded latest repository ZIP; reconcile differences; identify schema, navigation, permissions and existing student contracts. Then implement the first small testable Student UI/backend batch with full file replacements and matching docs.

## UI invariants

- Preserve the approved Book Editor arrangements at 1280×720 and 1920×1080.
- Preserve usable themed portrait scrolling at 390×844.
- No native-white artifacts, unthemed controls, unnecessary scrollbars, or broken table action icons.
- Preserve themed dialogs, selection behavior, active/inactive icons, tooltips and pagination.

## Development protocol

- Latest user instruction + working local source take priority for implementation; mandatory case-study scope must still be met.
- Check GitHub and latest uploaded ZIP before editing. Do not assume GitHub contains unpushed files.
- Deliver complete `.cs` replacements, exact paths and focused tests; tiny isolated fixes may use precise snippets.
- For very long files, attach downloadable source/ZIP; do not force the user to reconstruct large files.
- After the developer confirms all tests pass, suggest a commit; never commit or push without authorization.
- Update `docs/` at each completed batch.
