# LiteraHub — Open Questions and Verification Checklist

**Updated:** 2026-11-25

## Resolved/accepted decisions

- The four catalog modules and shared validation/feedback were developer-tested and pushed through `852b6a77`.
- Desktop acceptance sizes are 1280×720 and 1920×1080, sidebar expanded and collapsed, nine themes; portrait no longer required.
- Student required fields follow `StudentService.Validate`: Student ID, first/last name, House No, Street, Municipality, CityProvince, ZipCode, BirthDate, Gender, Mobile, Email. MiddleName and Landline optional.
- No Google/email/SMS identity provider is intended for LiteraHub; prefer an administrator-assisted password reset to insecure reset codes.
- User requests removal of `AUTHOR_SELECTION_BATCH.md`, retaining eight current docs.

## Login and carousel — verify before implementation

1. Confirm exact file extensions, naming, dimensions and alpha background for `Assets/Brand/Slide1`–`Slide3` and existing logo; verify Build Action (Content/EmbeddedResource) and Copy to Output.
2. Are all illustrations true monochrome alpha masks or do they contain grayscale shading? Choose luminance-preserving tint where appropriate, and test contrast in all nine themes.
3. Should users manually control slides and is a 5–7-second low-motion auto-advance acceptable? Ensure pause on form deactivation and animations can be disabled.
4. Confirm form minimum dimensions and whether login should always appear centered rather than fill the entire 1280/1920 desktop.
5. Should the Login theme follow the last saved global preference? Proposed yes, with no separate theme store.

## Password reset — decisions required

1. Which staff role can reset another staff user's credentials? Prefer a distinct permission, not automatically every Librarian.
2. How does the administrator re-authenticate before a reset? Define lockout/throttling and auditable events.
3. Should a random temporary password be presented once to the administrator, or delivered personally by staff using established procedures? Never persist plaintext.
4. How will the app handle the **sole administrator locked out**? Define an explicit offline recovery runbook with institutional authorization, not an in-app universal code.
5. Confirm admin reset account scopes, disabled-user behavior, forced-password-change rules, audit retention, and permission migrations.

## PSGC offline data — decisions required

1. Use the 3Q 2026 PSA PSGC publication or another explicitly verified quarterly release as snapshot. Confirm file provenance, conversion accuracy, attribution and bundling rights.
2. Include **Barangay** as a new persisted field? It is not currently part of the known case-study Student schema. Decide whether Region/Province/City/Barangay codes become separate columns.
3. How should older free-text `Municipality` and `CityProvince` be interpreted and edited without automatically guessing PSGC codes?
4. Handle NCR/independent-city province omission, special geographic regions, code leading zeros, retired/renamed units, and data-version upgrades.
5. ZIP is not derivable from PSGC; retain editable ZIP and choose any separate postal validation only with an authoritative source.
6. Should dataset updates be manually imported during app updates, instead of an internet fetch? Offline-by-default proposal: yes.

## Remaining academic scope

1. Verify seeded loan limits, allowed book conditions, loan duration and fines policy (previously noted seven days / three loans / no fines).
2. Design atomic Borrow & Return transactions and concurrency/rollback tests.
3. Implement all four required Reports and academic search facilities.
4. Test staff role permissions, logout/login, account recovery, metadata offline failure, SQL migrations and desktop packaging.

## Documentation

The eight maintained docs should be reconciled at major milestones. `LiteraHub.md` at repo root still has older next-phase guidance and can be refreshed separately if user authorizes a ninth document.
