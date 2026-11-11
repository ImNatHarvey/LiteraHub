# LiteraHub — Book Editor Author Selection Batch

**Prepared:** 2026-11-12
**Base GitHub commit reviewed:** `e2ec41a02a7b98a6e5d406968216096a337766cb`
**Status:** Pending local compilation and developer UI tests

## Scope

- `ThemedAuthorPicker` retains the approved 40-pixel Authors field and opens a searchable, theme-aware multi-select popup.
- Picker stores real Author IDs; removing an author from the selection is supported.
- `BookEditorPage` loads author records through existing `IBookService.GetAuthorsAsync()`, restores saved Author IDs for edits, and uniquely matches exact normalized ISBN metadata names.
- Ambiguous and unknown author names are reported to the librarian; no author is invented or auto-created.
- `BooksPage` passes the existing book service into the editor.
- `ThemedAuthorSelector` safely clears/disposes previous filtered row controls.

## Boundaries and limitations

- Save Book remains disabled until the next implementation batch.
- New-author creation and author CRUD UI are not part of this batch.
- ISBN matching uses full-name whitespace normalization only; punctuation, initials and alternate spellings require review.
- Inactive authors are excluded from new selections; check existing books whose linked authors are inactive.
- Windows compilation and runtime UI tests have not been performed by ChatGPT.

## Developer test checklist

1. Build the full solution; verify no compiler errors or WinForms analyzer warnings.
2. Open Add Book and click Authors; search and select multiple existing authors.
3. Remove one author and verify the caption reflects the current selection.
4. Edit a book with existing author links; verify those authors are selected.
5. Lookup ISBN `9780735211292`; match James Clear only if it exists in Authors.
6. Try ISBN metadata containing unknown or ambiguous names; verify there is no silent author creation.
7. Test 1280x720, 1920x1080 and 390x844 and switch several of the nine themes.
8. Confirm Categories CRUD, Books catalog, cover preview and inventory validation still work.

## Documentation update instructions

This file is an additive batch record. Do not replace your latest GitHub
`docs/PROGRESS.md`, `docs/FILES.md`, or `docs/DECISIONS.md` using
the older copies from the separately uploaded project ZIP.

Once local testing passes, summarize this change in:
- `PROGRESS.md`: mark author picker, ID selection and ISBN matching as developer-tested.
- `FILES.md`: add `ThemedAuthorPicker.cs` and update the Book Editor responsibilities.
- `DECISIONS.md`: record the compact popup UI and conservative author matching.
