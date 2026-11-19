# LiteraHub — File Map and Responsibilities

**Updated:** 2026-11-20  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

```text
LiteraHub/
├── LiteraHub.slnx
├── LiteraHub.Core/         # models, interfaces, security contracts
├── LiteraHub.Data/         # migrations, Dapper repositories
├── LiteraHub.Services/     # business rules, authorization, metadata
├── LiteraHub.WinForms/     # UI, pages, forms, custom controls, themes
└── docs/                   # handoff, progress, architecture, scope
```

## Verified/known implementation paths

| Path | Purpose |
|---|---|
| `LiteraHub.Data/Bootstrap/Migrations.cs` | SQL tables, constraints and seed/migration logic |
| `LiteraHub.Data/Repositories/BookRepository.cs` | Book persistence and Step 5B.6 inventory fix |
| `LiteraHub.Services/Books/BookService.cs` | Book validation, permissions and orchestration |
| `LiteraHub.Core/Models/BookModels.cs` | Book records and input models |
| `LiteraHub.WinForms/Pages/BooksPage.cs` | Catalog grid, search/filter/sort, navigation |
| `LiteraHub.WinForms/Pages/BookEditorPage.cs` | Dedicated Add/Edit Book page and inventory controls |
| `LiteraHub.WinForms/Pages/CategoriesPage.cs` | Categories CRUD and filtering |
| `LiteraHub.WinForms/Pages/AuthorsPage.cs` | Authors CRUD and filtering |
| `LiteraHub.WinForms/Controls/ThemedBooksFilterSort.cs` | Books filter/sort menu |
| `LiteraHub.WinForms/Controls/ThemeCategoryFilterSort.cs` | Shared Categories/Authors catalog filter/sort control (class name differs from file name) |
| `LiteraHub.WinForms/Forms/LoginForm.cs` | Existing login form; UI polish pending |
| `LiteraHub.Services/Security/AuthService.cs` | Existing authentication service |
| `LiteraHub.WinForms/Program.cs` | Dependency composition and application bootstrap |

**Important:** Recheck all exact paths against the newest source ZIP/GitHub; this map is a handoff aid, not a claim that no files changed after it was prepared.

## Step 6 source inspection targets

- `Migrations.cs` student table and foreign keys.
- Existing Student model, repository, service and interface (if any); do not assume they exist merely because the table exists.
- Main shell/sidebar navigation and placeholder Student page.
- Role/permission declarations and authorization service.
- Existing themed table, form controls, filter/sort, pagination, confirmation dialogs.

## Documentation index

- `PROJECT_BRIEF.md` — mandatory academic requirements.
- `PROGRESS.md` — completed vs pending work.
- `SESSION_HANDOFF.md` — next conversation context.
- `DECISIONS.md` — architectural and product choices.
- `SCHEMA.md` — migration-based schema summary.
- `FILES.md` — project/file ownership.
- `OPEN_QUESTIONS.md` — unresolved decisions.
- `ENHANCEMENT_ROADMAP.md` — phased plan.
- `AUTHOR_SELECTION_BATCH.md` — historical implementation batch record; may contain superseded “pending” notes.

## Student Management — Step 6A/6B additions

- `LiteraHub.Core/Models/StudentModels.cs` — Student and StudentInput models.
- `LiteraHub.Core/Interfaces/IStudentRepository.cs`, `IStudentService.cs` — student contracts.
- `LiteraHub.Data/Repositories/StudentRepository.cs` — Dapper student persistence.
- `LiteraHub.Services/Students/StudentService.cs` — validation and permissions.
- `LiteraHub.WinForms/Pages/StudentsPage.cs` — read-only Student List UI (Step 6B).
- `LiteraHub.WinForms/Program.cs` — student service composition.
- `LiteraHub.WinForms/Forms/ShellForm.cs` — Students navigation.

## Combined Step 6B.1 + 6C

- `LiteraHub.WinForms/Pages/StudentsPage.cs` — Books-style themed Student directory, search, status/sort, paging, chevron navigation, and in-shell details host.
- `LiteraHub.WinForms/Pages/StudentDetailsPage.cs` — dedicated read-only Student Details and editable Add/Edit form; archive/restore actions.
- Existing `Program.cs` and `ShellForm.cs` from 6B remain in use; no changes in this package.


### Student UI Step 6C.1
- `LiteraHub.WinForms/Controls/ThemedStudentsFilterSort.cs` — combined themed Student status/sort popup.
- `LiteraHub.WinForms/Pages/StudentsPage.cs` — Books-aligned toolbar, table and pagination.
- `LiteraHub.WinForms/Pages/StudentDetailsPage.cs` — responsive in-shell Student Details layout.


### Step 6C.2 replacements
- `LiteraHub.WinForms/Pages/StudentsPage.cs` — Student list themed typography and unchanged in-shell chevron navigation.
- `LiteraHub.WinForms/Pages/StudentDetailsPage.cs` — grouped, responsive student form using existing `ScrollHost` and `ThemedBirthDateDialog`.
- `LiteraHub.WinForms/Controls/ThemedStudentsFilterSort.cs` — custom painted menu consistent with Books/Categories instead of native `ContextMenuStrip`.
- Existing `ScrollHost.cs`, `ThinScrollBar.cs`, `ThemedBirthDateDialog.cs`, `Theme.cs` are **dependencies**; do not overwrite them with this batch.

### Step 6C.3 updated UI files
- `LiteraHub.WinForms/Pages/StudentsPage.cs`: button/font/footer matching for student list.
- `LiteraHub.WinForms/Pages/StudentDetailsPage.cs`: three-column details; themed controls and avatar; nested local-only `StudentPhotoStore`. No added `.cs` file or database migration.

### Step 6C.4 affected files
- `LiteraHub.WinForms/Pages/BookEditorPage.cs` — Book actions in a separate footer outside the book content surface.
- `LiteraHub.WinForms/Pages/StudentDetailsPage.cs` — Student actions in a separate footer, custom Back chevron, centered and responsive avatar.
- `LiteraHub.WinForms/Pages/StudentsPage.cs` — pagination alignment/typography and `Rows:` label.
