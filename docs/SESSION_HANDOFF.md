---
type: handoff
project: LiteraHub
updated: 2026-11-20
status: step-6-student-management-next
---

# LiteraHub — Session Handoff

## Read this first

We are **continuing an existing project**, not starting from scratch. The user is moving development into a ChatGPT Project called **LiteraHub**. The new conversation should review the academic case study, mockup, Claude history, original and previous master prompts, latest source ZIP and GitHub repository before coding.

**Repository:** https://github.com/ImNatHarvey/LiteraHub  
**Stack:** C# / .NET 10 / WinForms / SQL Server Express / Dapper  
**Solution:** `LiteraHub.Core`, `LiteraHub.Data`, `LiteraHub.Services`, `LiteraHub.WinForms`.

## Last developer-confirmed milestone

**Step 5B.6 — Inventory Save Fix: all tests passed; user reported pushing.** Before that, Step 5B.5 filter/sort polish and gender icons also passed and were pushed. The last commit SHA was **not independently established in this handoff**; fetch it on restart. The repository's older docs may still describe author selection and book persistence as pending—those notes are stale.

### Step 5B.6 root cause and fix

`BookRepository.UpdateAsync` had calculated available copies from total minus an inferred outstanding count, causing a saved `Available=0` to appear as 1 when Total was raised to 1. The tested fix preserves `input.AvailableCopies` and counts actual active rows in `dbo.BorrowReturns` (`Status='Borrowed'`, `ReturnDate IS NULL`) for additional constraints. The Book Editor no longer assumes `Total-Available` is the active loan count. Confirm this implementation in the latest ZIP/GitHub before modifying it.

## User-approved next sequence

1. **Step 6 — Students:** student records and case-study-compliant search, CRUD, archive/restore, safe deletion, themed responsive UI.
2. **Step 7 — Authentication:** polish login UI and implement secure Forgot Password; review existing roles, account model and recovery constraints first.
3. **Step 8 — Borrow & Return:** implement only after reliable student records, account access, and inventory are established.
4. **Reports and final acceptance:** required four summary lists, case-study search and end-to-end testing.

## Mandatory Student fields from case study

Student ID number; first, middle, last name; house number, street, municipality, city/province, ZIP; date of birth; gender; mobile, landline, email. The existing migration already creates `dbo.Students` with `StudentId` internal identity and `StudentNo` unique external ID; inspect its exact column constraints before designing forms.

## Architecture and UI rules

- Business rules and permissions in Services; SQL and transactions in Data; contracts/models in Core; themed controls/pages in WinForms.
- Preserve nine themes: Classic Light, Midnight Library, Sepia Paper, Forest Scholar, Slate & Teal, Lavender Dusk, Ocean Breeze, Rosewood, Graphite.
- Respect tested layouts at 1280×720, 1920×1080, and 390×844.
- Follow Categories/Authors/Books established patterns: toolbar, search, themed filter/sort submenus, active icons, action tooltips, pagination, confirmation dialogs, theme-safe scrollbars.
- Student records and login accounts are distinct. The current `Users.StudentId` relationship is nullable; do not create student logins by default without explicit scope.
- Preserve actual active-loan constraints and do not treat unavailable inventory as proof of an active loan.

## User's coding workflow

- Be concise; no repetitive acknowledgment or empty “next we will code” messages.
- For feature/fix requests, inspect source and provide code in the same reply when possible.
- Complete replacement `.cs` files with exact paths by default; tiny errors may use exact snippets.
- For 1,000+ lines, supply downloadable full replacement file or ZIP.
- Do not send PowerShell-based file modification scripts.
- No fabricated compilation/test results. Wait for user to report “All passed” before suggesting commit.
- Do not commit/push without explicit authorization.
- Maintain `docs/*.md` as development evolves.

## First instruction to new chat

Inspect the latest uploaded source ZIP, GitHub and Case Study #5. Check `Migrations.cs` for Students, `Program.cs` for service wiring, `MainForm`/navigation for Student page placeholders, existing auth/permissions, and any student models/contracts. Propose/implement **Step 6A** in one safe, testable batch. Do not begin Borrow & Return or redesign the whole application.
