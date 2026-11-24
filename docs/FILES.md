# LiteraHub — File Map and Responsibilities

**Updated:** 2026-11-25  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Four-project solution

```text
LiteraHub.slnx
LiteraHub.Core/           # contracts, models, permission definitions
LiteraHub.Data/           # SQL bootstrap, migrations, Dapper repositories
LiteraHub.Services/       # validation, authentication, book/student domain services
LiteraHub.WinForms/       # WinForms program, pages, controls, themes, assets
  Assets/Brand/           # logo and user-provided monochrome Slide1–Slide3 (verify names/extensions)
  Forms/                  # LoginForm, ChangePasswordForm, ShellForm, ThemedMessageDialog
  Pages/                  # four catalog pages and detail editors
  Controls/               # themed and shared UI components
  Theming/                # palettes, preferences
 docs/                    # eight maintained Markdown documents
```

## Important exact source paths

| Path | Responsibility |
|---|---|
| `LiteraHub.WinForms/Program.cs` | composition, DB bootstrap, login-to-shell flow |
| `LiteraHub.WinForms/Forms/LoginForm.cs` | CURRENT basic login UI; planned split-panel carousel redesign |
| `LiteraHub.WinForms/Forms/ChangePasswordForm.cs` | forced password change |
| `LiteraHub.WinForms/Forms/ShellForm.cs` | dashboard, sidebar, themes/navigation |
| `LiteraHub.WinForms/Forms/ThemedMessageDialog.cs` | theme-aware modal summaries; dynamic validation height |
| `LiteraHub.WinForms/Pages/BooksPage.cs` | books list, search/filter/sort/paging, View navigation |
| `LiteraHub.WinForms/Pages/BookEditorPage.cs` | book view/edit/add, ISBN, authors/category, inventory, F4 validation |
| `LiteraHub.WinForms/Pages/AuthorsPage.cs` | authors list, filters and navigation |
| `LiteraHub.WinForms/Pages/AuthorDetailsPage.cs` | author editor, gallery and F4 validation |
| `LiteraHub.WinForms/Pages/CategoriesPage.cs` | categories list, filters and navigation |
| `LiteraHub.WinForms/Pages/CategoryDetailsPage.cs` | category editor, gallery and F4 validation |
| `LiteraHub.WinForms/Pages/StudentsPage.cs` | students directory and detail navigation |
| `LiteraHub.WinForms/Pages/StudentDetailsPage.cs` | full student editor and local photo cache; required-field F4 validation |
| `LiteraHub.WinForms/Controls/DetailsPageHeader.cs` | shared detail header / Back |
| `LiteraHub.WinForms/Controls/DetailsActionFooter.cs` | shared detail action placement |
| `LiteraHub.WinForms/Controls/AssociatedBooksGallery.cs` | author/category books and scrollbar behavior |
| `LiteraHub.WinForms/Controls/LoadingHost.cs` | loading-skeleton host used in three catalog lists |
| `LiteraHub.WinForms/Controls/CatalogEmptyState.cs` | reusable empty/error/retry card |
| `LiteraHub.WinForms/Controls/CatalogActionFeedback.cs` | themed CRUD success toast |
| `LiteraHub.WinForms/Controls/CatalogValidationController.cs` | shared hybrid validation |
| `LiteraHub.WinForms/Controls/ThemedTextField.cs` | themed text input + danger border |
| `LiteraHub.WinForms/Controls/ThemedAuthorPicker.cs` | searchable book author selection and aligned padding |
| `LiteraHub.WinForms/Controls/ThemedCategoryPicker.cs` | searchable book category selection |
| `LiteraHub.WinForms/Controls/ThemedFilterDropdown.cs` | selection control |
| `LiteraHub.Services/Security/AuthService.cs` | staff sign-in and change password; no forgotten-password flow yet |
| `LiteraHub.Services/Students/StudentService.cs` | authoritative required fields and domain validation |
| `LiteraHub.Data/Bootstrap/Migrations.cs` | authoritative current database schema/versions |
| `LiteraHub.Data/Repositories/BookRepository.cs` | book persistence and active-loan inventory rules |
| `LiteraHub.Data/Repositories/StudentRepository.cs` | student SQL and guarded hard-delete |

## Planned additions — not yet present

- `LiteraHub.WinForms/Controls/LoginCarousel.cs` — working name only; verify asset naming and theme-recolor implementation.
- Offline PSGC asset e.g. `Assets/Data/psgc-2026-q3.json`, plus DTO/repository/selection UI. Choose final names after reviewing data format and Student fields.
- Secure admin-driven password reset service and optional audit migration, subject to explicit permission and security design.

## Docs to maintain

`PROJECT_BRIEF.md`, `PROGRESS.md`, `DECISIONS.md`, `SESSION_HANDOFF.md`, `FILES.md`, `SCHEMA.md`, `OPEN_QUESTIONS.md`, `ENHANCEMENT_ROADMAP.md`.

`AUTHOR_SELECTION_BATCH.md` is intentionally excluded and is to be deleted separately by the user. `LiteraHub.md` at repository root is a separate project note and **not** one of these eight files; its stale next-phase guidance should eventually be reconciled as well.
