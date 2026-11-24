---
type: handoff
project: LiteraHub
updated: 2026-11-25
status: f4-pushed-login-carousel-planning
---

# LiteraHub — Session Handoff

## Read first — authoritative current checkpoint

Repository: https://github.com/ImNatHarvey/LiteraHub  
Verified latest pushed commit: `852b6a77d21a776a892abe294d4f4fdd4a52f249` (`feat: overhaul catalog validation and responsive feedback`).  
User reports all F4.0–F4.4 acceptance tests passed (Visual Studio); assistant has not compiled the Windows app.

Four major catalog modules are developer-accepted: Books, Authors, Categories and Students. Shared gallery, details header/footer, loading/empty/error/retry feedback, themed CRUD success feedback, and hybrid validation all accepted. 1280×720 with expanded/collapsed sidebar and 1920×1080 are supported; nine themes are mandatory. 390×844 portrait support is retired from the acceptance plan.

## What is NOT finished

- There is no common generic base class for the four catalog pages; intentional **component composition** replaces duplicated details/header/footer/gallery/feedback functionality. This refactor is substantially closed; no inheritance rewrite is needed before Login.
- Existing `LoginForm.cs` remains a basic username/password dialog. **No repository-verified carousel or Forgot Password flow.**
- Borrow & Return and Reports still need implementation/acceptance.
- PSGC offline address selection is a new proposal, not yet implemented.

## New user decisions for the next implementation

1. User says monochrome images already exist under `LiteraHub.WinForms/Assets/Brand/Slide1`–`Slide3`, alongside logo. Inspect actual extensions, resource configuration, alpha channels and dimensions before code changes.
2. Build Login as split panel with local three-slide carousel tinted according to the selected one of nine themes, using the approved theme engine. Do not modify source PNGs or fetch assets from the network. Preserve aspect ratio, antialiasing and accessibility. Handle resize, theme changes, slide animation timer disposal, inactive form and missing assets.
3. No Google/email/SMS for account recovery. Recommend authenticated administrator-assisted reset (one-use temporary credential; hashed at rest; forced change; audit; secure last-admin recovery runbook), **subject to security/permission review**. Do not introduce a reset code that bypasses authentication.
4. PSGC addresses: offline snapshot JSON from Philippine Statistics Authority 3Q 2026 PSGC source, cascading Region/Province (if applicable)/City-or-Municipality/Barangay; handle independent cities, older records, manual ZIP; schema mapping decision precedes code.
5. User will delete `docs/AUTHOR_SELECTION_BATCH.md`; eight maintained docs are PROJECT_BRIEF, PROGRESS, DECISIONS, SESSION_HANDOFF, FILES, SCHEMA, OPEN_QUESTIONS, ENHANCEMENT_ROADMAP.

## Next coding phases and acceptance gates

- **F5.0 asset/theme inspection:** inspect actual assets and existing `LoginForm`, Theme, `AuthService`, `IUserRepository`, ChangePassword, permissions. Decide slide tinting and reset administration scope.
- **F5.1 login visuals:** programmatic WinForms two-panel desktop responsive Login; reusable carousel; keyboard/focus, validation states, show/hide password. Do not alter `AuthService` unless needed.
- **F5.2 carousel rendering:** locally load Slide1–3; tint by theme, theme changes, transitions, disposal/cache, user navigation, DPI, 1280/1920 tests.
- **F5.3 secure recovery:** define user-service permission, audit and temporary credential flow with single-use forced change; test roles, rate limits, disabled staff, last-admin scenario.
- **F5.4 PSGC offline address UI:** separate milestone; source/versioned dataset/legacy mapping migration plan before altering Student records.
- **Later:** atomic Borrow & Return and required Reports.

## Known invariants

- `AvailableCopies` stays explicitly editable; never compute active loans from Total-Available. Avoid negative inventory; actual loans impose constraints.
- Student `HouseNo/Street/Municipality/CityProvince/ZipCode/Gender/MobileNo/Email` are required in service; MiddleName and Landline optional.
- Auth separates staff roles from student records. Existing hashed passwords and `MustChangePassword` are not to be bypassed.
- Deleting a student linked to a user or loan is blocked; photos remain local to the Windows account unless later redesigned.
- Restore/search/filter/pagination/toolbar and Back-button focus must not regress.

## Workflow

Review current GitHub files before edits, send full replacements with exact paths, give focused test checklist, wait for explicit user acceptance before recommending commits. Never claim an independently successful .NET build. Update docs at each accepted checkpoint.
