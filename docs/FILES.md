# LiteraHub — File Map and Responsibilities

**Updated:** 2026-11-28  
**Repository:** https://github.com/ImNatHarvey/LiteraHub

## Application projects and test project

```text
LiteraHub.slnx
LiteraHub.Core/           # contracts, models, permission definitions
LiteraHub.Data/           # SQL bootstrap, migrations, Dapper repositories
LiteraHub.Services/       # validation, authentication, book/student domain services
LiteraHub.WinForms/       # WinForms program, pages, controls, themes, assets
  Assets/Brand/           # logo and user-provided monochrome Slide1–Slide3 (verify names/extensions)
  Forms/                  # LoginForm, ShellForm, ThemedMessageDialog and specialized dialogs
  Pages/                  # four catalog pages and detail editors
  Controls/               # themed and shared UI components
  Theming/                # palettes, preferences
LiteraHub.Tests/          # F7.0.1 test project (accepted locally)
.github/workflows/        # build.yml
tools/psgc/               # converter
docs/                     # eight maintained Markdown documents
```

## Important exact source paths

| Path | Responsibility |
|---|---|
| `LiteraHub.WinForms/Program.cs` | composition, DB bootstrap, login-to-shell flow |
| `LiteraHub.WinForms/Forms/LoginForm.cs` | split-panel login, carousel, inline recovery and mandatory password change |
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
| `LiteraHub.Services/Security/AuthService.cs` | staff sign-in and password changes |
| `LiteraHub.Services/Students/StudentService.cs` | authoritative required fields and domain validation |
| `LiteraHub.Data/Bootstrap/Migrations.cs` | authoritative current database schema/versions |
| `LiteraHub.Data/Repositories/BookRepository.cs` | book persistence and active-loan inventory rules |
| `LiteraHub.Data/Repositories/StudentRepository.cs` | student SQL and guarded hard-delete |

## F5 source map (verify exact signatures in latest source)

| Path | Responsibility |
|---|---|
| `LiteraHub.WinForms/Pages/UsersPage.cs`, `UserDetailsPage.cs` | Staff directory and permission-aware details; owner/peer protection |
| `LiteraHub.WinForms/Pages/ProfilePage.cs` | Self-service account/security and appearance |
| `LiteraHub.WinForms/Controls/ThemedDialogChrome.cs` | Shared frame and drag behavior |
| `LiteraHub.WinForms/Controls/ThemedWindowControls.cs` | Shared caption controls |
| `LiteraHub.WinForms/Forms/ThemePickerDialog.cs`, `ChangeNameDialog.cs`, `ChangeUsernameDialog.cs` | Profile dialogs |
| `LiteraHub.WinForms/Forms/StaffCredentialsDialog.cs`, `OwnerAuthorizationDialog.cs` | One-time credential display and owner approval |
| `LiteraHub.Core/Interfaces/IProfileService.cs`, `IRecoveryService.cs`, `IStaffUserService.cs`, `IStaffUserRepository.cs` | Security contracts |
| `LiteraHub.Core/Models/ProfileModels.cs`, `StaffUserModels.cs` | Account models |
| `LiteraHub.Services/Security/ProfileService.cs`, `RecoveryService.cs`, `StaffUserService.cs`, `PermissionService.cs` | Security/business rules |
| `LiteraHub.Data/Repositories/ProfileRepository.cs`, `RecoveryRepository.cs`, `StaffUserRepository.cs`, `DelegationGuard.cs`, `RoleRepository.cs` | Transactional security persistence and enforcement |
| `LiteraHub.Data/Bootstrap/Migrations.cs`, `DatabaseBootstrapper.cs` | Automatic schema upgrades and seed |

## F5.7–F7.0.1 source map

| Path | Responsibility |
|---|---|
| `LiteraHub.WinForms/Controls/ThemedSectionCard.cs`, `ResponsiveSectionLayout.cs`, `ResponsiveDetailsPair.cs` | Responsive detail cards |
| `LiteraHub.WinForms/Controls/ModuleSummaryCard.cs`, `ModuleSummaryCardStrip.cs`, `CatalogToolbarExtras.cs` | Summary metrics and list toolbar layout |
| `LiteraHub.WinForms/Controls/ThemedPsgcPicker.cs` | Searchable geographic picker |
| `LiteraHub.Services/Students/PsgcCatalog.cs` | Embedded immutable geographic catalog |
| `LiteraHub.Services/Resources/PSGC/psgc.json`, `SOURCE.md` | Offline data and attribution |
| `tools/psgc/convert_psgc.py` | Reproducible dataset conversion |
| `LiteraHub.Tests/LiteraHub.Tests.csproj`, `PasswordPolicyTests.cs`, `PsgcCatalogTests.cs` | Initial automated tests (accepted locally) |
| `.github/workflows/build.yml` | Windows CI restore/build/test definition; current run unverified |

## F7.0.2–F7.0.3 source map

| Path | Responsibility |
|---|---|
| `LiteraHub.Core/Security/RolePermission.cs` | Stable granular permission keys; inspect before adding grants |
| `LiteraHub.Core/Models/AuditEvent.cs` | Fixed audit module/action/outcome and safe description |
| `LiteraHub.Data/Repositories/AuditWriter.cs` | Transaction-aware audit insertion |
| `LiteraHub.Data/Repositories/UserRepository.cs` | Persistent authentication attempts and selected authentication audit |
| `LiteraHub.Data/Repositories/RecoveryRepository.cs` | Single-use staff recovery, reset and unlock |
| `LiteraHub.Data/Repositories/AuthorCreationRepository.cs`, `CategoryRepository.cs`, `BookRepository.cs`, `StudentRepository.cs` | Transactional catalog audit; PSGC UPDATE fix in StudentRepository |
| `LiteraHub.Data/Repositories/StaffUserRepository.cs`, `RoleRepository.cs`, `ProfileRepository.cs` | Account, role and profile audit |
| `LiteraHub.Data/Bootstrap/Migrations.cs` | V12 login state, V13 audit table |
| `LiteraHub.Tests/AuthServiceTests.cs`, `LoginAttemptPolicyTests.cs`, `AuditEventTests.cs` | Security and audit regression tests |

## F7.1 inspection targets (not implemented)

Inspect current LoginForm, Program, ShellForm, StudentDetailsPage, StudentsPage, RolesPage/RoleDetailsPage, ProfilePage, AuthService, UserRepository, RecoveryRepository, StaffUserService/Repository, StudentService/Repository, RoleRepository, DelegationGuard, PermissionService, session/role models, password policy, migration/bootstrap and existing tests. Confirm precise names/signatures using current tree. New student account repository/service/models, registration/approval UI and Student Shell are proposed, not existing accepted files.

## Documentation

Eight maintained `docs/` files; master prompt/checklist supplied separately for chat handoff. AUTHOR_SELECTION_BATCH.md retired; root LiteraHub.md may need separate reconciliation. No source files changed by documentation update.
