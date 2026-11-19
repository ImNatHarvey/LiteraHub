# LiteraHub — Open Questions and Verification Checklist

**Updated:** 2026-11-20

## Step 6 Students

1. What Student ID number format/validation does the school require? Current DB allows up to 30 characters and enforces uniqueness; do not invent a format.
2. Should gender choices match the Authors module (Male/Female/Other), and how should any existing values be handled?
3. Which fields, if any, may be optional at the UI level? Case study specifies full address/contact details; existing migration requires most fields and permits null for MiddleName/LandlineNo.
4. Which student fields should be visible in the grid versus detail editor, while keeping the full record accessible?
5. What exact authorization governs student create/edit/archive/delete?
6. Can students with borrowing history ever be hard-deleted? Default to archive/restrict until requirements and foreign keys are verified.

## Step 7 Authentication

1. Is an email sender/SMTP service available and approved for secure password reset?
2. If not, should Forgot Password direct staff to an audited admin-assisted reset?
3. What are the existing password hashing, password change, account lockout and session rules?
4. Should student accounts be created at all? Not required by the case study; staff users and students are distinct.

## Step 8 Circulation

1. Confirm loan days and maximum active loans from actual seeded settings and requirements.
2. Confirm how damaged/lost/manually unavailable copies are represented separately from active loans.
3. Confirm whether an ISBN identifies one book title with multiple copies (current model) and how returns affect condition.
4. Confirm atomic inventory and concurrency behavior before accepting circulation tests.

## General

- Confirm latest GitHub SHA after the user pushes; old docs remain dated 2026-11-12.
- Confirm new handoff files against the latest uploaded repository ZIP before overwriting any more recent `docs/` edits.
- Verify the demo environment, SQL Server installation, Windows scaling, offline ISBN behavior and release packaging.
- Keep historical `AUTHOR_SELECTION_BATCH.md` as a historical batch record; its old pending notes are superseded by later developer-confirmed work.
