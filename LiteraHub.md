---
type: project
status: active
updated: 2026-11-09
technology: [C#, WinForms, SQL Server, Dapper, .NET 10]
repository: https://github.com/ImNatHarvey/LiteraHub
---
# LiteraHub

STI Balagtas Library Management System (Case Study #5). The school case study is the minimum scope. The Kindle-inspired HTML mockup is a visual reference, not a deployed website.

## Current handoff
**Next phase:** Step 5A — shared CRUD UI infrastructure, followed by Categories CRUD.

Open [[docs/SESSION_HANDOFF|SESSION_HANDOFF]] before starting another AI coding session.

## Source of truth
- [[docs/PROJECT_BRIEF|PROJECT_BRIEF]] — official requirements checklist
- [[docs/DECISIONS|DECISIONS]] — architectural decisions
- [[docs/PROGRESS|PROGRESS]] — implementation and test status (requires reconciliation)
- [[docs/SCHEMA|SCHEMA]] — intended database schema (requires reconciliation)
- [[docs/FILES|FILES]] — file inventory (requires reconciliation)
- [[docs/OPEN_QUESTIONS|OPEN_QUESTIONS]] — unresolved issues

## Technology
- `LiteraHub.Core` — models/interfaces/access control
- `LiteraHub.Data` — Dapper, SQL Server access, bootstrapper
- `LiteraHub.Services` — authentication, permissions, business rules
- `LiteraHub.WinForms` — programmatic desktop UI

## Daily workflow
1. Check `git status` before changes.
2. Open solution `LiteraHub.slnx` in Visual Studio.
3. Build/test the current step, record results in `docs/PROGRESS.md`.
4. Commit/push the LiteraHub repository separately from the Knowledge-Base repository.
5. Ask ChatGPT to re-read current GitHub files and the handoff.
