# LiteraHub — Open Questions and Verification Checklist

**Updated:** 2026-11-28

## Resolved/accepted decisions

- The four catalog modules and shared validation/feedback were developer-tested and pushed through `852b6a77`.
- Desktop acceptance sizes are 1280×720 and maximized actual display size, sidebar expanded and collapsed, nine themes; portrait no longer required.
- Student required fields follow `StudentService.Validate`: Student ID, first/last name, House No, Street, Municipality, CityProvince, ZipCode, BirthDate, Gender, Mobile, Email. MiddleName and Landline optional.
- No Google/email/SMS identity provider; offline hashed single-use recovery passkeys and authorized regeneration are accepted.
- User requests removal of `AUTHOR_SELECTION_BATCH.md`, retaining eight current docs.

## Resolved scope and immediate next work

F7.0.3 accepted/pushed, F7.1 approved. Student display name maps to Borrower system key. Hybrid registration, staff approval/direct creation, dedicated student shell, requests, reservation expiry, renewal, waitlist, fines and notifications approved. No additional scope approval required.

## Open implementation decisions (do not block safe work unnecessarily)
- Verify exact registration identity evidence and credential handoff with institution; student number/name alone insufficient. Decide pending request expiration/retry, account archive/reactivation and self-service profile field policy.
- New student-account staff permissions, role protection, transactional review and live linked-student checks; verify current grant and recovery behavior. Do not enable student login prematurely.
- LoanDays=7 and MaxActiveLoans=3 are existing seeds; pickup deadline, renewal count/duration, waitlist eligibility, fine rate/grace/cap, overdue and balance restrictions remain institutional policy choices. Use configurable safe defaults; no assumed fines.
- Determine aggregate inventory reservation representation and stable lock ordering; preserve existing unavailable stock. Decide fine ledger and policy snapshots, expiry processing, historic deletion protection.
- Verify SQL integration/concurrency tests in isolated database, CI run status, clean install and upgrade; no independent runtime claims. Review authorized Owner recovery, audit retention, privacy, direct SQL trust boundary and least privilege before release.
- Maintain PSGC version/provenance, legacy conversion and province-free cases; do not infer ZIP or retired codes.
- Mandatory circulation searches and four reports remain open. Final academic compliance, deployment and documentation pending.
