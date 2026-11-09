# LiteraHub — Project File Structure

**Last reviewed:** 2026-11-09  
**Source:** GitHub repository `ImNatHarvey/LiteraHub`  
**Current phase:** Step 4D — Documentation Reconciliation

## 1. Project Location

Local development folder:

`Documents/Knowledge Base/1 - Projects/LiteraHub/`

GitHub repository:

https://github.com/ImNatHarvey/LiteraHub

The project is stored inside the Obsidian PARA vault but maintained as an independent Git repository.

## 2. Solution Structure

```text
LiteraHub/
├── LiteraHub.slnx
├── LiteraHub.md
├── LiteraHub.Core/
├── LiteraHub.Data/
├── LiteraHub.Services/
├── LiteraHub.WinForms/
└── docs/
```

All four projects target .NET 10, with the WinForms project targeting `net10.0-windows`.

## 3. LiteraHub.Core

Contains shared domain models, contracts, security definitions, and configuration models.

```text
LiteraHub.Core/
├── Interfaces/
│   ├── IAuthServices.cs
│   ├── IDbConnectionFactory.cs
│   ├── IPasswordHasher.cs
│   └── IUserRepository.cs
├── Models/
│   ├── ResultModels.cs
│   └── UserModels.cs
├── Security/
│   └── AccessControl.cs
├── Settings/
│   └── DatabaseOptions.cs
└── LiteraHub.Core.csproj
```

## 4. LiteraHub.Data

Contains database initialization, SQL Server connection handling, and Dapper repositories.

```text
LiteraHub.Data/
├── Bootstrap/
│   ├── BootstrapModels.cs
│   ├── DatabaseBootstrapper.cs
│   └── Migrations.cs
├── Repositories/
│   ├── SqlConnectionFactory.cs
│   └── UserRepository.cs
└── LiteraHub.Data.csproj
```

The database migrations define the existing application schema and seed data.

## 5. LiteraHub.Services

Contains application services, authentication, password hashing, and authorization.

```text
LiteraHub.Services/
├── Security/
│   ├── AuthService.cs
│   ├── BcryptPasswordHasher.cs
│   ├── PasswordPolicy.cs
│   ├── PermissionService.cs
│   └── SessionContext.cs
└── LiteraHub.Services.csproj
```

Business permissions must be enforced in the service layer, not only through UI visibility.

## 6. LiteraHub.WinForms

Contains the application entry point, windows, reusable controls, navigation, and theming.

```text
LiteraHub.WinForms/
├── Controls/
│   ├── BusyButtons.cs
│   ├── LoadingHost.cs
│   ├── ScrollHost.cs
│   ├── SidebarNav.cs
│   ├── SkeletonView.cs
│   ├── ThemedUserControl.cs
│   └── ThinScrollBar.cs
├── Forms/
│   ├── ChangePasswordForm.cs
│   ├── LoginForm.cs
│   └── ShellForm.cs
├── Pages/
│   └── PlaceholderPage.cs
├── Theming/
│   ├── AppIcons.cs
│   ├── Gfx.cs
│   └── Theme.cs
├── Program.cs
├── appsettings.json
└── LiteraHub.WinForms.csproj
```

All application UI is created programmatically in C#, without Windows Forms Designer files.

`BusyButtons.cs` contains the `BusyButton` implementation. The temporary loading demonstration page has been removed.

## 7. Project Documentation

```text
docs/
├── DECISIONS.md
├── FILES.md
├── OPEN_QUESTIONS.md
├── PROGRESS.md
├── PROJECT_BRIEF.md
├── SCHEMA.md
└── SESSION_HANDOFF.md
```

| Document | Purpose |
|---|---|
| DECISIONS.md | Architecture and design decisions |
| FILES.md | Repository structure and file responsibilities |
| OPEN_QUESTIONS.md | Unresolved development questions |
| PROGRESS.md | Development milestones and testing status |
| PROJECT_BRIEF.md | Official case-study requirements |
| SCHEMA.md | Database structure and migration reference |
| SESSION_HANDOFF.md | Current development context for future sessions |

## 8. Planned Step 5 Files

The following are planned components, not existing source files:

- Shared native-control theming
- Reusable themed DataGridView
- Category models and repository contracts
- Category repository implementation
- Category service and validation
- Categories management page

Exact filenames and namespaces will be finalized after inspecting the relevant existing source code.

## 9. Development Conventions

- Keep domain models and interfaces in Core.
- Keep SQL queries and repositories in Data.
- Keep business rules and authorization in Services.
- Keep presentation and UI interactions in WinForms.
- Use relative project references.
- Use asynchronous database operations where appropriate.
- Provide full source files for changes.
- Update documentation after completed milestones.

## 10. Maintenance

This document records the repository file tree inspected on 2026-11-09.

Update it when files are added, renamed, moved, or removed. Do not list planned files as implemented.
