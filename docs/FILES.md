# LiteraHub — Project Files and Responsibilities

**Updated:** 2026-11-12  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Solution

```text
LiteraHub/
├── LiteraHub.slnx
├── LiteraHub.Core/
├── LiteraHub.Data/
├── LiteraHub.Services/
├── LiteraHub.WinForms/
└── docs/
    ├── DECISIONS.md
    ├── ENHANCEMENT_ROADMAP.md
    ├── FILES.md
    ├── OPEN_QUESTIONS.md
    ├── PROGRESS.md
    ├── PROJECT_BRIEF.md
    ├── SCHEMA.md
    └── SESSION_HANDOFF.md
```

## Project boundaries

| Project | Responsibility |
|---|---|
| `LiteraHub.Core` | Models, contracts, security and shared definitions |
| `LiteraHub.Data` | SQL Server bootstrap/migrations, Dapper repositories |
| `LiteraHub.Services` | Business logic, authentication, authorization and validation |
| `LiteraHub.WinForms` | Programmatic UI, forms, pages, custom controls, theme engine |

## Known Books and Categories files

Exact locations and method signatures must be checked against the current repository before editing.

- `LiteraHub.WinForms/Pages/BooksPage.cs` — Books catalog and editor navigation.
- `LiteraHub.WinForms/Pages/BookEditorPage.cs` — Add/Edit Book UI, ISBN lookup, inventory validation, cover preview.
- `LiteraHub.WinForms/Pages/CategoriesPage.cs` — Categories management UI.
- `LiteraHub.WinForms/Controls/ThemedFilterDropdown.cs` — Theme-aware dropdown reused for category/condition.
- `LiteraHub.WinForms/Controls/ThemedAuthorSelector.cs` — Author selector foundation; integration pending.
- `LiteraHub.Core/Models/BookMetadata.cs` — External bibliographic metadata.
- `LiteraHub.Core/Interfaces/IBookMetadataService.cs` — Metadata service contract.
- `LiteraHub.Services/.../OpenLibraryMetadataService.cs` — Open Library integration (verify exact path).
- `BookModels.cs`, `IBookRepository.cs`, `IBookService.cs`, `BookRepository.cs`, `BookService.cs` — Books domain/data/service layer (verify exact paths).
- `LiteraHub.Data/Bootstrap/Migrations.cs` — Authoritative schema migration definitions.

## Documentation purposes

| File | Responsibility |
|---|---|
| `DECISIONS.md` | Architectural and product decisions |
| `ENHANCEMENT_ROADMAP.md` | Planned enhancements and sequencing |
| `FILES.md` | Structure and file ownership |
| `OPEN_QUESTIONS.md` | Unresolved decisions and unverified behavior |
| `PROGRESS.md` | Implemented/tested/pending milestones |
| `PROJECT_BRIEF.md` | Case Study #5 mandatory scope |
| `SCHEMA.md` | SQL schema and constraints |
| `SESSION_HANDOFF.md` | Restart context and next coding batch |

## Development conventions

- All UI is programmatic WinForms; no designer files.
- Keep SQL out of forms and permissions enforced in services.
- Do not restore discarded `BookDialog.cs` and `AuthorQuickAddDialog.cs` without explicit approval.
- Update this document when files are added, renamed, or removed.
- Do not confuse planned components with implemented components.
