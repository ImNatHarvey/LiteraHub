# LiteraHub — Open Questions and Unverified Items

**Updated:** 2026-11-12

## Books — active decisions required

1. **Author matching:** How should Open Library names map to author records with first/middle/last names? Define normalization, duplicate handling, and confirmation before auto-creation.
2. **Category inference:** The current metadata model has no category field. Which reliable source or controlled mapping should be used, and should staff confirm new categories?
3. **Cover persistence:** Should external URLs be cached locally, stored as URLs, or re-fetched on demand? Confirm offline fallback and licensing/attribution needs.
4. **Save semantics:** For existing books, how should Total/Available counts interact with active loans, missing/damaged copies, and manual inventory adjustments?
5. **ISBN duplicates:** Confirm update/merge behavior versus rejecting duplicate ISBNs on create.
6. **Author selection UI:** Verify existing `ThemedAuthorSelector` integration and whether inline author creation needs a dedicated page.
7. **Metadata descriptions:** Confirm truncation/length limits and whether user edits survive subsequent ISBN lookups.

## Platform / release

- Does the demonstration laptop have SQL Server Express or LocalDB installed and configured?
- Which self-contained release ZIP or installer packaging will be used?
- Windows 10 dark native scrollbar behavior remains unverified.
- DPI/layout tests at additional Windows scale factors may require regression testing.
- Login throttling/lockout is optional and not implemented in the MVP.
- Dark title bar and native context menu theming remain optional polish.
- Borrower portal and digital reader remain future scope.

## Resolved historical questions

- Icon package chosen: FontAwesome.Sharp.
- Dark scrollbar approach investigated and implemented in the app's custom/themed controls.
- Nine application appearance presets implemented.
- Dedicated page chosen over modal Book Editor.
- Cover URL input removed from the editor UI in favor of preview and fallback.

Do not classify a question as resolved solely because a proposed design exists; verify behavior with the source and developer tests.
