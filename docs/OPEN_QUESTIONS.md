# LiteraHub — Open Questions and Verification Checklist

**Updated:** 2026-11-27

## Resolved/accepted decisions

- The four catalog modules and shared validation/feedback were developer-tested and pushed through `852b6a77`.
- Desktop acceptance sizes are 1280×720 and maximized actual display size, sidebar expanded and collapsed, nine themes; portrait no longer required.
- Student required fields follow `StudentService.Validate`: Student ID, first/last name, House No, Street, Municipality, CityProvince, ZipCode, BirthDate, Gender, Mobile, Email. MiddleName and Landline optional.
- No Google/email/SMS identity provider; offline hashed single-use recovery passkeys and authorized regeneration are accepted.
- User requests removal of `AUTHOR_SELECTION_BATCH.md`, retaining eight current docs.

## Remaining security review (F7.0.2 and F9)

- Define institution-authorized recovery runbook for loss of System Owner credentials/passkey; no universal bypass password.
- Review initial bootstrap credential provisioning, global authorization lockout denial-of-service tradeoff, audit retention, and independent negative-path tests.
- Verify role assignment UI matches backend restrictions and that authorization is operation-specific. Do not reopen accepted F5 UI unless a regression is found.

## F6 — PSGC offline address integration (completed)

- PSA 2026-Q3 dataset embedded in Services under `LiteraHub.PSGC.Data`: 18 regions, 82 provinces, 1,642 cities/municipalities, 14 Manila districts and 42,010 barangays. See `LiteraHub.Services/Resources/PSGC/SOURCE.md` for attribution and conversion details.
- Searchable cascading Region → Province (where applicable) → City/Municipality → Barangay; independent cities, NCR and province-free localities supported. ZIP remains manual.
- V11 adds nullable structured address codes and names; legacy addresses are preserved until deliberately converted. Final UI removes ordinary address preview and legacy captions while preserving conversion and error handling.
- Converter is tracked at `tools/psgc/convert_psgc.py`; solution reference is repository-relative.

## Remaining PSGC maintenance

- Verify future releases and retired-code mappings before replacing the embedded snapshot; do not guess replacements or derive ZIP from PSGC.
- Future tests: legacy student service persistence, V11 upgrade in isolated SQL database, offline UI regression.

## Remaining academic scope

1. Verify seeded loan limits, allowed book conditions, loan duration and fines policy (previously noted seven days / three loans / no fines).
2. Design atomic Borrow & Return transactions and concurrency/rollback tests.
3. Implement all four required Reports and academic search facilities.
4. Test staff role permissions, logout/login, account recovery, metadata offline failure, SQL migrations and desktop packaging.

## Documentation

The eight maintained docs should be reconciled at major milestones. `LiteraHub.md` at repo root still has older next-phase guidance and can be refreshed separately if user authorizes a ninth document.
