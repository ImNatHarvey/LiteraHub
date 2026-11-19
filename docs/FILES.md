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
