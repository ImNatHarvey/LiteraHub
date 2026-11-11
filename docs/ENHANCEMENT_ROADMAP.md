# LiteraHub — Enhancement Roadmap

**Updated:** 2026-11-12  
**Primary scope:** STI Balagtas Case Study #5  
**Direction:** Library management first; hybrid digital reading features after the required MVP.

## Development principles

1. Complete required case-study modules before optional enhancements.
2. Preserve four-project .NET 10 / WinForms / SQL Server / Dapper architecture.
3. Use testable batches with user-confirmed results.
4. Keep source-of-truth distinctions between implemented, tested, and planned.
5. Preserve role permissions, database integrity and applied migrations.
6. Use existing themed controls and approved responsive layouts.
7. Treat imported metadata as suggestions until staff confirms it.

## Phase A — Foundation and visual system

**Developer-tested:** solution, migrations, authentication, application shell, loading states, dark/themed scrollbars, nine-preset appearance.

Presets: Classic Light, Midnight Library, Sepia Paper, Forest Scholar, Slate & Teal, Lavender Dusk, Ocean Breeze, Rosewood, Graphite.

Reader appearance will eventually be independent from application appearance.

## Phase B — Required library management

| Module | Current state |
|---|---|
| Categories | CRUD developer-tested and pushed |
| Books catalog | UI developer-tested |
| Book Editor | Responsive UI, inventory validation, category dropdown, ISBN lookup and cover preview integrated |
| Authors | Backend/creation foundation exists; selector and CRUD completion pending |
| Students | Pending completion |
| Borrow/Return | Pending completion |
| Reports | Pending completion |
| Users | Role/account foundation exists; complete UI pending |

### Immediate sequence

1. **Author selector:** search/select/create/match author records.
2. **Book save:** validate and persist book, author relationships and inventory safely.
3. **Book edit:** refresh catalog and enforce outstanding-loan restrictions.
4. **Complete Authors and Students CRUD.**
5. **Borrow/Return, Reports and end-to-end case-study testing.**

## Phase C — Smart cataloging

**Implemented in editor:** ISBN lookup through Open Library, title/description import, cover-image retrieval, first-letter fallback.

**Pending:** author ID resolution, safe category matching, cover caching strategy, metadata attribution, offline handling and comprehensive save validation.

The current metadata contract does not provide category data; do not invent category matches.

## Phase D — Member requests and hybrid catalog

Planned, not implemented:

- Member book requests and staff approval.
- Physical and digital title types.
- Licensed/public-domain PDF and EPUB support.
- Access and lending controls.

## Phase E — Reader

Planned, not implemented:

- PDF/EPUB reading, bookmarks, highlights, notes, reading progress.
- Independent reader appearance, typography, margins and line spacing.
- Parallel Read (two independent reading panes).

## Phase F — Optional intelligence

Planned only: Wikipedia lookup, translation, text-to-speech, AI-assisted descriptions and recommendations. All require privacy, licensing, quota and cost review.

## Quality gates

- 1280×720 and 1920×1080: no unnecessary scrollbar, overlap or clipping.
- 390×844: usable portrait scrolling and themed scrollbar.
- Nine themes: no native-white control regressions.
- Permission enforcement in services.
- SQL validation and transaction safety.
- Developer testing before commit and push.
- Matching updates to `PROGRESS.md`, `DECISIONS.md`, and `SESSION_HANDOFF.md`.

See `OPEN_QUESTIONS.md` for unresolved design choices.
