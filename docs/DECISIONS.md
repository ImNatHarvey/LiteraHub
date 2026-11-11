# LiteraHub — Architecture and Decisions Log

**Updated:** 2026-11-12

This document summarizes current decisions and their rationale. Earlier decisions should be retained in Git history.

## Foundation (2026-11-08 to 2026-11-09)

- Four-project architecture: `LiteraHub.Core`, `.Data`, `.Services`, `.WinForms`.
- C# / .NET 10, Windows Forms built programmatically, SQL Server Express, Dapper.
- Idempotent SQL migrations; never rewrite migrations already applied.
- SQL operations belong in Data; business validation and permissions in Services; UI in WinForms.
- Manual dependency composition in `Program.cs`.
- Admin and Librarian are staff roles; borrower login portal is outside current MVP.
- Seven-day default loan period, maximum three active loans, no fines, and damaged books not borrowable.
- Soft delete by default; hard delete only with appropriate authorization and dependency checks.
- Responsive testing targets: 1280×720, 1920×1080, and 390×844.

## UI and themes

- Nine theme presets: Classic Light, Midnight Library, Sepia Paper, Forest Scholar, Slate & Teal, Lavender Dusk, Ocean Breeze, Rosewood, Graphite.
- Preserve design tokens and theme-change propagation.
- Use existing themed controls rather than native default controls where an equivalent exists.
- Use custom-themed scrolling, loading skeletons, and busy feedback.
- Remove redundant page headings; preserve contextual Add/Edit headings.
- Keep custom chevron navigation and themed table action icons.

## Books decisions (2026-11-09 to 2026-11-12)

- The Book Editor is a **dedicated page inside Books**, not a modal dialog. Do not restore discarded `BookDialog.cs` or `AuthorQuickAddDialog.cs`.
- Keep the approved 1280 and 1920 responsive arrangements documented in `PROGRESS.md`.
- Cover Path/URL is not a required visible input. Prefer remote cover lookup with a first-letter placeholder.
- ISBN metadata comes from Open Library through existing service abstractions; manual entry remains available.
- Imported bibliographic information requires staff review before saving.
- Existing categories must be loaded from the database. Do not automatically create categories from unreliable metadata.
- The metadata contract currently lacks category information; matching/creation requires additional design.
- Author names returned from metadata are **not** automatically equivalent to persisted author IDs.
- Available copies must never exceed total copies; UI validates immediately and the service must validate again.
- On edits, do not discard outstanding/unavailable copies. Determine active-loan restrictions from actual persisted borrowing records.
- Save Book is a visible UI action but end-to-end saving remains pending.
- The custom Lookup button's runtime-only `IsBusy` property must not be designer-serialized (`DesignerSerializationVisibility.Hidden`).

## Workflow decisions

- Each development response should deliver code when sufficient source is available, not merely an acknowledgment.
- Full file replacements are preferred; tiny isolated corrections can use exact replacement snippets.
- Deliver complete downloadable files for very long source files (approximately 1,000+ lines).
- Supply matching documentation updates as development proceeds.
- Developer confirms tests before committing; GitHub commit verification does not replace runtime testing.
