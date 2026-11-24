# LiteraHub — Development Roadmap

**Updated:** 2026-11-25  
**Primary scope:** STI Balagtas Case Study #5  
**Verified GitHub checkpoint:** `852b6a77d21a776a892abe294d4f4fdd4a52f249`

## Complete: Catalog and shared foundation

- Existing .NET 10 / SQL Server / Dapper four-project architecture, login/auth baseline, nine-theme shell and desktop presets.
- Categories, Authors, Books, Students CRUD, search/filter/sort/pagination, themed details, permissions and navigation.
- F1 cover cache; F2 shared details header/footer/gallery; F3 loading/empty/error/retry/success-feedback; F4 theme-aware validation and compact-layout fixes.
- Refactor conclusion: **shared composition is substantially complete**. No generic catalog inheritance rewrite necessary before Login.

## F5: Login and identity (NEXT)

1. **F5.0 Source/asset verification:** inspect `LoginForm`, `AuthService`, password policy, account roles, theme preferences, and exact `Assets/Brand/Slide1`–`Slide3` artwork.
2. **F5.1 Split-panel Login UI:** themed credentials form, show/hide password, focus and accessibility, 1280×720 / 1920×1080 adaptive sizing, nine themes.
3. **F5.2 Carousel:** locally bundled slides tinted per theme, preserved alpha/contrast, simple indicators and navigation, optional gentle automatic transitions with lifecycle-safe timers and bitmap disposal.
4. **F5.3 Login validation/security:** hybrid error presentation, no raw exception details, duplicate-request protection, existing `MustChangePassword` flow unchanged; verify auth behavior.
5. **F5.4 Staff recovery:** authenticate an authorized administrator, issue a single-use random temporary credential with hashed storage and forced change, audit resets and rate-limit; explicit last-admin recovery runbook. No email and no universal/reset bypass code.
6. **F5.5 Regression:** successful/invalid/disabled login, permissions, logout, forced change, all nine themes, both desktop sizes, asset fallback and no login-time internet dependency.

## F6: Offline PSGC address enhancement

- Review official PSA Philippine Standard Geographic Code 3Q 2026 publication; convert to a **versioned local JSON snapshot** with provenance and validation.
- Add cascading Region → Province (when applicable) → City/Municipality → Barangay choices with search and accessible keyboard interaction. Handle NCR and independent cities.
- Preserve the current Student fields and older free-text addresses; plan an optional, separately approved migration if storing PSGC codes/barangay/region/province.
- Do not infer ZIP from PSGC. Validate at 1280×720 expanded/collapsed sidebar, 1920×1080, all nine themes; no unwanted horizontal scrollbar.

## F7: Borrow & Return (mandatory)

- Select student and borrowable book; search by student/name/ISBN, borrow/return dates.
- Enforce policy, active-loan limits, permissions and inventory conditions. Use SQL transactions and concurrency-safe availability updates.
- Record DueDate, ReturnDate, remarks, condition and staff identity; preserve history and negative-path tests.

## F8: Reports and academic case-study acceptance (mandatory)

- Books per category, books per author, books per status, all books.
- Verify mandatory search facilities in Books, Authors, Students and Borrow/Return.
- Test migrations, database seed/upgrade, role controls, accessibility, offline behavior, theme stability and release/demo setup.

## Deferred / optional cleanup

- Consolidate Student list loading architecture with `LoadingHost` if a concrete bug or maintenance need arises.
- Extract painting/layout helpers from large forms as needed; do not destabilize accepted UI for arbitrary line-count reduction.
- Add automated service/data tests prior to transactional circulation changes.
- Future digital reader / notes / bookmarks / translation are non-MVP enhancements.

## Collaboration protocol

Full replacement `.cs` files with exact paths for larger changes; targeted snippets for one-line fixes. One tested batch at a time, user-controlled Git commits. `AUTHOR_SELECTION_BATCH.md` intentionally retired; eight updated docs form the maintained baseline.
