
# LiteraHub — Enhancement Roadmap

Updated: 2026-11-10
Status: Planning and design
Project: LiteraHub

## 1. Product Vision

LiteraHub is an intelligent hybrid library management
and digital reading system.

Its primary purpose is to satisfy STI Balagtas
Case Study #5 using C#, Windows Forms, and SQL Server.

Its expanded purpose is to improve book discovery,
cataloging, borrowing, and reading through modern
digital features.

The official case study remains the source of truth
for required functionality.

## 2. Core Development Principles

1. Preserve all official case-study requirements.
2. Keep the existing four-project architecture.
3. Implement one testable change at a time.
4. Use free or low-cost dependencies where practical.
5. Keep AI optional, not required for core operations.
6. Require staff review of imported or generated data.
7. Enforce permissions in the service layer.
8. Respect ebook copyright and lending licenses.
9. Never store API secrets in GitHub.
10. Distinguish implemented features from planned ones.

## 3. Current Development Status

Completed and developer-tested:
- Steps 1 through 4C
- Categories models and repository
- Categories service and permission enforcement
- Category dependency wiring in Program.cs
- CategoriesPage layout foundation (5A.4A)

Current pause:
- Step 5A.4B is postponed temporarily.

Next:
- Enhancement roadmap documentation
- Midnight Library visual theme
- Resume Step 5A.4B

## 4. Visual Identity — Midnight Library

Dark theme palette:
- Background: #171C29
- Surface: #232B3B
- Surface alternative: #2D374A
- Border: #3A455A
- Text: #E9EDF7
- Muted text: #A9B6CC
- Accent: #8BA4F5
- Accent text: #171C29
- Soft accent: #303D61

Design direction:
- Calm, academic, modern appearance
- Navy and periwinkle rather than orange and black
- Consistent theme tokens across controls
- Readable text and accessible contrast
- Preserve light mode
- Add optional sepia mode for the future reader

## 5. Core Library Features

Complete the existing modules:
- Categories CRUD
- Authors CRUD
- Students CRUD
- Books CRUD and search
- Borrow and return transactions
- Required summary reports
- Role-based authorization
- Staff account management

These remain essential for the school case study.

## 6. Smart Book Cataloging

Planned features:
- ISBN lookup through external bibliographic APIs
- Automatic retrieval of available book metadata
- Book cover lookup
- Source attribution
- Manual review before saving
- Local metadata caching
- Manual entry when the API is unavailable

Optional AI enhancements:
- Generate brief book descriptions
- Suggest categories and keywords
- Assist with catalog searches
- Recommend related books

AI must not fabricate bibliographic facts.

## 7. Member Requests and Approval

Planned workflow:

Member searches catalog
    -> Requests book
    -> Request marked Pending
    -> Admin or Librarian reviews
    -> Approved or Rejected
    -> Member receives notification

Physical books:
- Approval reserves an available copy.
- Checkout occurs when the copy is handed over.
- Return and due-date rules remain enforced.

Digital books:
- Access requires appropriate distribution rights.
- Approved access may expire according to policy.

Proposed request statuses:
- Pending
- Approved
- Rejected
- Cancelled
- Fulfilled
- Expired

The final state model will be designed before
creating database migrations.

## 8. Hybrid Physical and Digital Catalog

Planned features:
- Physical books with available-copy tracking
- Digital titles and supported file formats
- Book format indicators
- Shared bibliographic information
- Format-specific borrowing and access rules
- Public-domain or appropriately licensed ebooks

Potential formats:
- PDF
- EPUB

Digital file storage and access controls require
a separate technical design.

## 9. Digital Reading Experience

Planned features:
- Integrated PDF reader
- Integrated EPUB reader
- Reading progress
- Light, dark, and sepia reader themes
- Font and typography preferences
- Bookmarks
- Highlights
- Personal notes
- Search within documents

Standout feature:
- Parallel Read: two documents side by side
- Independent navigation in each pane
- Potential synchronized reading later

Potential implementation:
- Microsoft WebView2
- Suitable EPUB rendering library

Technical compatibility must be verified first.

## 10. Reading Assistance

Optional integrations:
- Wikipedia word and topic lookup
- DeepL translation
- Text-to-speech
- AI-powered explanations
- Contextual reading assistance

Requirements:
- User-controlled activation
- Privacy and copyright safeguards
- Clear indication of AI-generated content
- Fallback behavior when services are unavailable
- API cost and quota review

## 11. Architecture Direction

Existing architecture:
- LiteraHub.Core
- LiteraHub.Data
- LiteraHub.Services
- LiteraHub.WinForms

Continue using:
- .NET 10
- C# Windows Forms
- SQL Server Express
- Dapper
- Programmatic UI
- Service-layer permissions

Future possibility:
- A separate member-facing web portal
- Shared backend business rules through an API

The web portal is not part of the current MVP.

## 12. Delivery Phases

Phase A — Current enhancement checkpoint
- Document new goals and decisions
- Implement Midnight Library dark theme
- Test theme across existing UI
- Resume Step 5A.4B

Phase B — Required library system
- Finish Categories, Authors, Students, Books
- Finish borrowing and returns
- Finish required reports
- Complete core regression testing

Phase C — Practical standout features
- ISBN metadata import
- Book covers
- Member request and approval workflow
- Physical/digital catalog design

Phase D — Digital reader
- PDF/EPUB support
- Reader themes
- Bookmarks, notes, highlights
- Parallel Read

Phase E — Optional intelligent features
- AI-assisted cataloging
- Wikipedia and translation
- Read aloud
- Recommendations

## 13. Implementation Rules

- Do not rewrite applied database migrations.
- Add new schema changes through versioned migrations.
- Do not begin a large feature without a design review.
- Validate third-party API and library compatibility.
- Test each feature before proceeding.
- Commit and push each verified checkpoint.
- Keep docs/PROGRESS.md and docs/DECISIONS.md updated.

## 14. Immediate Next Step

Complete enhancement documentation and Midnight
Library theme before resuming Step 5A.4B.

Step 5A.4B remains the next Categories implementation:
connect navigation and load real SQL Server categories.


## 15. Nine-Preset Appearance System

The application will support nine named presets:

| Preset | Background | Surface | Accent | Text | Style |
|---|---|---|---|---|---|
| Classic Light | #FAFAF8 | #FFFFFF | #536C9B | #242B35 | Light |
| Midnight Library | #171C29 | #232B3B | #8BA4F5 | #E9EDF7 | Dark |
| Sepia Paper | #F4E8D1 | #FAF0DD | #8B6746 | #483C30 | Light |
| Forest Scholar | #17231F | #24352E | #83B89B | #E6F0E9 | Dark |
| Slate & Teal | #181F28 | #263340 | #57C2BE | #E8F0F2 | Dark |
| Lavender Dusk | #242034 | #342D48 | #C2A9F5 | #F0EAF9 | Dark |
| Ocean Breeze | #EAF4F8 | #F7FCFE | #287A9A | #233E50 | Light |
| Rosewood | #2D2028 | #3D2C35 | #E7A3B8 | #F8E9EF | Dark |
| Graphite | #1C1E22 | #2A2D33 | #B5C0D0 | #F0F2F5 | Dark |

These are design targets. Remaining palette tokens,
contrast, and native-control styling must be tested.

### Application Appearance

Requirements:
- Select any of the nine presets.
- Apply changes immediately.
- Save the selected preset locally.
- Restore the selection on application startup.
- Provide a clearly marked selected state.
- Preserve readable controls and navigation.
- Support light and dark native scrollbars.

### Reader Appearance

Planned requirements:
- Independent theme selection.
- The same nine presets.
- Font family and size.
- Line spacing and margins.
- Reading progress persistence.
- Distraction-free reading mode.

Reader settings will be implemented when the
digital reader is developed.

### Preference Storage

Initial implementation:
- Local application preferences.
- No database migration required.
- No API credentials.
- Graceful fallback when settings are invalid.

Future implementation:
- Optional per-account synchronization.
- Separate application and reader settings.

### Theme Compatibility

Existing code uses:
- Theme.Colors
- Theme.Mode
- Theme.SetMode(...)
- Theme.Changed

The migration must preserve compatibility or update
every caller together in the same implementation batch.

NativeTheme and ThemedDataGridView must correctly
handle light and dark preset families.

### Acceptance Criteria

- All nine presets are selectable.
- Theme switching works without restarting.
- Settings survive an application restart.
- No orange accent remains in the new presets.
- Login, navigation, logout, and Categories layout
  continue working.
- Existing database behavior is unchanged.

## 16. Updated Immediate Development Sequence

P1 — Documentation updates.
P2 — Nine-preset theme engine.
P3 — Theme selection UI and persistence.
P4 — Appearance and regression testing.

Then resume Step 5A.4B.

Optional reading, AI, and catalog enhancements remain
scheduled for later phases unless reprioritized.
