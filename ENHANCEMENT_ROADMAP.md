
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
