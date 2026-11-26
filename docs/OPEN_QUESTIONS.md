# LiteraHub — Open Questions and Verification Checklist

**Updated:** 2026-11-26

## Resolved/accepted decisions

- The four catalog modules and shared validation/feedback were developer-tested and pushed through `852b6a77`.
- Desktop acceptance sizes are 1280×720 and 1920×1080, sidebar expanded and collapsed, nine themes; portrait no longer required.
- Student required fields follow `StudentService.Validate`: Student ID, first/last name, House No, Street, Municipality, CityProvince, ZipCode, BirthDate, Gender, Mobile, Email. MiddleName and Landline optional.
- No Google/email/SMS identity provider; offline hashed single-use recovery passkeys and authorized regeneration are accepted.
- User requests removal of `AUTHOR_SELECTION_BATCH.md`, retaining eight current docs.

## Remaining security review (F9)

- Define institution-authorized recovery runbook for loss of System Owner credentials/passkey; no universal bypass password.
- Review initial bootstrap credential provisioning, global authorization lockout denial-of-service tradeoff, audit retention, and independent negative-path tests.
- Verify role assignment UI matches backend restrictions and that authorization is operation-specific. Do not reopen accepted F5 UI unless a regression is found.

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
